using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    /// <summary>
    /// Non-modal view for displaying status logs and task progress.
    /// Replaces the traditional status bar with a resizable, scrollable log pane.
    /// </summary>
    public class TaskStatusView : View
    {
        private readonly JobManager _jobManager;
        private readonly List<string> _logEntries = new List<string>();
        private readonly int _maxLogEntries = 100;
        private bool _isExpanded = false;
        private int _expandedHeight = 10;
        private int _scrollOffset = 0;
        
        // Configuration for busy spinner - Using Unicode for backslash to avoid escape issues
        private readonly string[] _spinnerFrames = { "|", "/", "-", "\u005C" };
        private int _spinnerFrameIndex = 0;
        
        public bool IsExpanded 
        { 
            get => _isExpanded; 
            set 
            {
                _isExpanded = value;
                SetNeedsDisplay();
            }
        }

        public int ExpandedHeight
        {
            get => _expandedHeight;
            set
            {
                _expandedHeight = Math.Max(3, Math.Min(20, value));
                if (_isExpanded) SetNeedsDisplay();
            }
        }

        public TaskStatusView(JobManager jobManager)
        {
            _jobManager = jobManager;
            CanFocus = false;
            
            // Subscribe to job updates
            _jobManager.JobStarted += (s, j) => AddLogEntry(j, "Started");
            _jobManager.JobCompleted += (s, j) => AddLogEntry(j, j.Status.ToString());
            _jobManager.JobUpdated += (s, j) => 
            {
                Application.MainLoop.Invoke(() => SetNeedsDisplay());
            };
        }

        public void AddLog(string message)
        {
            _logEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            if (_logEntries.Count > _maxLogEntries)
            {
                _logEntries.RemoveAt(0);
            }
            _scrollOffset = Math.Max(0, _logEntries.Count - GetVisibleLogLines());
            
            Application.MainLoop.Invoke(() => SetNeedsDisplay());
        }

        private void AddLogEntry(BackgroundJob job, string action)
        {
            string tabPrefix = job.TabId >= 0 ? $"[Tab {job.TabId+1}:{job.TabName}] " : "";
            AddLog($"{tabPrefix}{job.Name}: {action} - {job.ProgressMessage}");
        }

        public void ScrollUp()
        {
            _scrollOffset = Math.Max(0, _scrollOffset - 1);
            SetNeedsDisplay();
        }

        public void ScrollDown()
        {
            int maxScroll = Math.Max(0, _logEntries.Count - GetVisibleLogLines());
            _scrollOffset = Math.Min(maxScroll, _scrollOffset + 1);
            SetNeedsDisplay();
        }

        private int GetVisibleLogLines()
        {
            return _isExpanded ? Bounds.Height - 1 : 1;
        }

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            var normal = ColorScheme.Normal;
            
            if (_isExpanded && bounds.Height > 1)
            {
                Driver.SetAttribute(normal);
                for (int y = 0; y < bounds.Height - 1; y++)
                {
                    Move(0, y);
                    Driver.AddStr(new string(' ', bounds.Width));
                }

                int visibleLines = bounds.Height - 1;
                for (int i = 0; i < visibleLines; i++)
                {
                    int logIndex = _scrollOffset + i;
                    if (logIndex < _logEntries.Count)
                    {
                        Move(0, i);
                        string line = _logEntries[logIndex];
                        if (CharacterWidthHelper.GetStringWidth(line) > bounds.Width)
                            line = CharacterWidthHelper.TruncateToWidth(line, bounds.Width);
                        Driver.AddStr(line);
                    }
                }
                
                Move(0, bounds.Height - 1);
                Driver.AddStr(new string('─', bounds.Width));
            }

            int statusLineY = bounds.Height > 0 ? bounds.Height - 1 : 0;
            Move(0, statusLineY);
            Driver.SetAttribute(normal);
            
            Driver.AddStr(new string(' ', bounds.Width));
            Move(0, statusLineY);

            var activeJobs = _jobManager.GetActiveJobs().ToList();
            if (activeJobs.Count > 0)
            {
                var primaryJob = activeJobs[0];
                string spinner = _spinnerFrames[_spinnerFrameIndex];
                string jobStatus = $"{spinner} {primaryJob.Name}: {primaryJob.ProgressPercent:F0}%";
                
                if (activeJobs.Count > 1)
                {
                    jobStatus += $" (+{activeJobs.Count - 1} others)";
                }
                
                Driver.AddStr(jobStatus);
            }
            else
            {
                if (_logEntries.Count > 0)
                {
                    string lastLog = _logEntries.Last();
                    if (CharacterWidthHelper.GetStringWidth(lastLog) > bounds.Width)
                        lastLog = CharacterWidthHelper.TruncateToWidth(lastLog, bounds.Width);
                    Driver.AddStr(lastLog);
                }
                else
                {
                    Driver.AddStr("Ready");
                }
            }
        }
        
        public void Tick()
        {
            _spinnerFrameIndex = (_spinnerFrameIndex + 1) % _spinnerFrames.Length;
            if (_jobManager.GetActiveJobs().Any())
            {
                SetNeedsDisplay();
            }
        }
    }
}