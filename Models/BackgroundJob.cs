using System;
using System.Threading;

namespace TWF.Models
{
    public enum JobStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public class BackgroundJob
    {
        private static int _nextShortId = 1;
        
        public Guid Id { get; } = Guid.NewGuid();
        public int ShortId { get; }
        
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public double ProgressPercent { get; set; }
        public string ProgressMessage { get; set; } = string.Empty;
        
        // For detailed tracking in JobManagerDialog
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public string CurrentOperationDetail { get; set; } = string.Empty;
        public string CurrentItemFullPath { get; set; } = string.Empty;
        public string LastLoggedItem { get; set; } = string.Empty;
        public string LastOperationDetail { get; set; } = string.Empty;
        public string LastDiskLoggedItem { get; set; } = string.Empty;
        public string LastDiskLoggedDetail { get; set; } = string.Empty;
        public System.Collections.Generic.HashSet<string> RelatedPaths { get; } = new System.Collections.Generic.HashSet<string>();
        public DateTime CurrentFileStartTime { get; set; } = DateTime.MinValue;
        
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        
        public BackgroundJob()
        {
            ShortId = Interlocked.Increment(ref _nextShortId);
        }
        
        // Context info
        public int TabId { get; set; } = -1;
        public string TabName { get; set; } = string.Empty;
        
        public bool IsActive => Status == JobStatus.Pending || Status == JobStatus.Running;
    }
}
