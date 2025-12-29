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

        public string CurrentSpinnerFrame => _spinnerFrames[_spinnerFrameIndex];
        
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
            string message = $"{tabPrefix}{job.Name}: {action}";
            
            if (action == "Started")
            {
                message += $" - JobID:{job.Id}";
            }
            else if (!string.IsNullOrEmpty(job.ProgressMessage))
            {
                // Only append the detail message if it's not identical to the status (action)
                if (!string.Equals(job.ProgressMessage, action, StringComparison.OrdinalIgnoreCase))
                {
                    message += $" - {job.ProgressMessage}";
                }
            }
            
            AddLog(message);
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
            return Bounds.Height;
        }

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            var normal = ColorScheme.Normal;
            Driver.SetAttribute(normal);

            // Draw Log Area
            // Clear background
            for (int y = 0; y < bounds.Height; y++)
            {
                Move(0, y);
                Driver.AddStr(new string(' ', bounds.Width));
            }

            int visibleLines = bounds.Height;
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
        }
        
        public void Tick()
        {
            _spinnerFrameIndex = (_spinnerFrameIndex + 1) % _spinnerFrames.Length;
            SetNeedsDisplay();
        }
    }
}