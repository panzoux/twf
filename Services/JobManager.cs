using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TWF.Models;

namespace TWF.Services
{
    public class JobManager
    {
        private readonly ConcurrentDictionary<Guid, BackgroundJob> _jobs = new ConcurrentDictionary<Guid, BackgroundJob>();
        private readonly ILogger<JobManager> _logger;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private int _updateIntervalMs = 300;

        public event EventHandler<BackgroundJob>? JobStarted;
        public event EventHandler<BackgroundJob>? JobUpdated;
        public event EventHandler<BackgroundJob>? JobCompleted;
        public event EventHandler<(BackgroundJob Job, JobProgress Progress)>? ProgressReported;

        public JobManager(ILogger<JobManager> logger, int maxSimultaneousJobs = 4, int updateIntervalMs = 300)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _concurrencySemaphore = new SemaphoreSlim(maxSimultaneousJobs);
            _updateIntervalMs = updateIntervalMs;
        }

        public BackgroundJob StartJob(string name, string description, int tabId, string tabName, Func<BackgroundJob, CancellationToken, IProgress<JobProgress>, Task> action, string sourcePath = "", string destinationPath = "")
        {
            var job = new BackgroundJob
            {
                Name = name,
                Description = description,
                Status = JobStatus.Pending,
                TabId = tabId,
                TabName = tabName,
                SourcePath = sourcePath,
                DestinationPath = destinationPath
            };

            _jobs[job.Id] = job;
            OnJobStarted(job);

            // Run the job in background
            Task.Run(async () =>
            {
                try
                {
                    await _concurrencySemaphore.WaitAsync(job.CancellationTokenSource.Token);
                    job.Status = JobStatus.Running;
                    OnJobUpdated(job);

                    DateTime lastUpdateTime = DateTime.MinValue;

                    var progress = new Progress<JobProgress>(p =>
                    {
                        job.ProgressPercent = p.Percent;
                        job.ProgressMessage = p.Message;
                        job.CurrentOperationDetail = p.CurrentOperationDetail;
                        job.CurrentItemFullPath = p.CurrentItemFullPath;
                        
                        // Fire unthrottled event for logging listeners
                        ProgressReported?.Invoke(this, (job, p));

                        // Log significant file events to disk (Audit Trail)
                        // Only log completion verbs to avoid spamming "Processing..."
                        if (p.Message.StartsWith("Deleted") || 
                            p.Message.StartsWith("Renamed") ||
                            p.Message.StartsWith("Copied") ||
                            p.Message.StartsWith("Moved") ||
                            p.Message.StartsWith("Skipped") ||
                            p.Message.StartsWith("Failed") ||
                            p.Message.StartsWith("Created"))
                        {
                            // Use separate tracking for disk to avoid race conditions with UI
                            if (job.LastDiskLoggedItem != p.CurrentItemFullPath || job.LastDiskLoggedDetail != p.Message)
                            {
                                string actionVerb = p.Message.Split(' ')[0];
                                string logPath = p.CurrentItemFullPath;
                                if (!string.IsNullOrEmpty(p.DestinationPath))
                                {
                                    logPath += $" -> {p.DestinationPath}";
                                }

                                _logger.LogInformation("Executing: {Action} {Path}", actionVerb, logPath);
                                
                                job.LastDiskLoggedItem = p.CurrentItemFullPath;
                                job.LastDiskLoggedDetail = p.Message;
                            }
                        }

                        // Throttle UI updates
                        if ((DateTime.Now - lastUpdateTime).TotalMilliseconds >= _updateIntervalMs)
                        {
                            OnJobUpdated(job);
                            lastUpdateTime = DateTime.Now;
                        }
                    });

                    await action(job, job.CancellationTokenSource.Token, progress);
                    
                    if (job.CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        job.Status = JobStatus.Cancelled;
                        job.ProgressMessage = "Cancelled";
                        _logger.LogWarning("Job '{Name}' cancelled. Stopped at: {Path}", job.Name, job.CurrentItemFullPath);
                    }
                    else
                    {
                        job.Status = JobStatus.Completed;
                        job.ProgressPercent = 100;
                        job.ProgressMessage = "Completed";
                        _logger.LogInformation("Job '{Name}' completed successfully", job.Name);
                    }
                }
                catch (OperationCanceledException)
                {
                    job.Status = JobStatus.Cancelled;
                    job.ProgressMessage = "Cancelled";
                    _logger.LogWarning("Job '{Name}' cancelled. Stopped at: {Path}", job.Name, job.CurrentItemFullPath);
                }
                catch (Exception ex)
                {
                    job.Status = JobStatus.Failed;
                    job.ProgressMessage = $"Failed: {ex.Message}";
                    _logger.LogError(ex, "Job '{Name}' failed. Stopped at: {Path}", job.Name, job.CurrentItemFullPath);
                }
                finally
                {
                    _concurrencySemaphore.Release();
                    job.EndTime = DateTime.Now;
                    OnJobCompleted(job);
                }
            });

            return job;
        }

        public void CancelJob(Guid jobId)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                if (job.IsActive)
                {
                    job.CancellationTokenSource.Cancel();
                    job.Status = JobStatus.Cancelled; // Optimistic update
                    OnJobUpdated(job);
                }
            }
        }

        public IEnumerable<BackgroundJob> GetActiveJobs()
        {
            var activeJobs = new List<BackgroundJob>();
            // Use ToArray() to get a snapshot and avoid "Collection was modified" exceptions
            foreach (var job in _jobs.ToArray())
            {
                if (job.Value.IsActive)
                {
                    activeJobs.Add(job.Value);
                }
            }
            activeJobs.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            return activeJobs;
        }

        public IEnumerable<BackgroundJob> GetAllJobs()
        {
            var allJobs = new List<BackgroundJob>();
            foreach (var job in _jobs.ToArray())
            {
                allJobs.Add(job.Value);
            }
            allJobs.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            return allJobs;
        }

        public bool IsTabBusy(int tabId)
        {
            foreach (var job in _jobs.Values)
            {
                if (job.TabId == tabId && job.IsActive) return true;
            }
            return false;
        }

        public int GetActiveJobCount(int tabId)
        {
            int count = 0;
            foreach (var job in _jobs.Values)
            {
                if (job.TabId == tabId && job.IsActive) count++;
            }
            return count;
        }

        /// <summary>
        /// Updates manager settings dynamically
        /// </summary>
        public void UpdateSettings(int updateIntervalMs)
        {
            _updateIntervalMs = updateIntervalMs;
            _logger.LogInformation("JobManager: Settings updated. UpdateInterval={Interval}ms", _updateIntervalMs);
        }

        public IEnumerable<string> GetBusyPaths()
        {
            var activeJobs = GetActiveJobs();
            foreach (var job in activeJobs)
            {
                if (!string.IsNullOrEmpty(job.CurrentItemFullPath))
                {
                    yield return job.CurrentItemFullPath;
                }
                
                // Snapshot the RelatedPaths to avoid modification while enumerating
                string[] pathsSnapshot;
                lock (job.RelatedPaths)
                {
                    pathsSnapshot = new string[job.RelatedPaths.Count];
                    job.RelatedPaths.CopyTo(pathsSnapshot, 0);
                }

                foreach (var path in pathsSnapshot)
                {
                    yield return path;
                }
            }
        }

        protected virtual void OnJobStarted(BackgroundJob job)
        {
            JobStarted?.Invoke(this, job);
        }

        protected virtual void OnJobUpdated(BackgroundJob job)
        {
            JobUpdated?.Invoke(this, job);
        }

        protected virtual void OnJobCompleted(BackgroundJob job)
        {
            JobCompleted?.Invoke(this, job);
        }
    }

    public class JobProgress
    {
        public double Percent { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CurrentOperationDetail { get; set; } = string.Empty;
        public string CurrentItemFullPath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
    }
}
