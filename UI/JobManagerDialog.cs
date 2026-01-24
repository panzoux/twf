using System;
using System.Collections.Generic;
using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    public class JobManagerDialog : Dialog
    {
        private readonly JobManager _jobManager;
        private readonly Configuration _config;
        private ListView _jobsList;
        private TextView _detailView;
        private List<BackgroundJob> _currentJobs;

        public JobManagerDialog(JobManager jobManager, Configuration config) : base("Background Jobs", 72, 26)
        {
            _jobManager = jobManager;
            _config = config;
            _currentJobs = new List<BackgroundJob>();

            var normalFg = ColorHelper.ParseConfigColor(config.Display.ForegroundColor, Color.White);
            var normalBg = ColorHelper.ParseConfigColor(config.Display.BackgroundColor, Color.Black);
            var highlightFg = ColorHelper.ParseConfigColor(config.Display.HighlightForegroundColor, Color.Black);
            var highlightBg = ColorHelper.ParseConfigColor(config.Display.HighlightBackgroundColor, Color.Cyan);
            
            var dialogFg = ColorHelper.ParseConfigColor(config.Display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(config.Display.DialogBackgroundColor, Color.Gray);

            // Apply Dialog Colors
            this.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };

            // Jobs List (Top half)
            _jobsList = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = 8, // Fixed height 8 lines
                ColorScheme = new ColorScheme
                {
                    Normal = Application.Driver.MakeAttribute(normalFg, normalBg),
                    Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                    HotNormal = Application.Driver.MakeAttribute(normalFg, normalBg),
                }
            };

            Add(_jobsList);
            
            // Separator at Y=9 (1+8)
            Add(new LineView(Terminal.Gui.Graphs.Orientation.Horizontal)
            {
                X = 1,
                Y = 9,
                Width = Dim.Fill(1)
            });
            
            // Detail Area (Bottom half)
            var detailLabel = new Label("Selected Job Details:")
            {
                X = 1,
                Y = 10
            };
            Add(detailLabel);

            _detailView = new TextView()
            {
                X = 1,
                Y = 11,
                Width = Dim.Fill(1),
                Height = 12, // Remaining space
                ReadOnly = true,
                WordWrap = true,
                ColorScheme = new ColorScheme
                {
                    Normal = Application.Driver.MakeAttribute(normalFg, normalBg),
                    Focus = Application.Driver.MakeAttribute(normalFg, normalBg), // No focus highlight needed really
                }
            };
            Add(_detailView);

            var closeButton = new Button("Close")
            {
                X = Pos.Center() - 10
            };
            closeButton.Clicked += () => Application.RequestStop();
            AddButton(closeButton);

            var cancelButton = new Button("Cancel Job")
            {
                X = Pos.Center() + 2
            };
            cancelButton.Clicked += CancelSelectedJob;
            AddButton(cancelButton);

            // Timer to refresh list
            int interval = _config.Display.JobManagerRefreshIntervalMs > 0 ? _config.Display.JobManagerRefreshIntervalMs : 500;
            Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(interval), (loop) =>
            {
                RefreshList();
                return true;
            });

            // Initial Refresh
            RefreshList();
            
            // Update details when selection changes
            _jobsList.SelectedItemChanged += (e) => UpdateDetailView();
        }

        private void RefreshList()
        {
            var oldSelection = _jobsList.SelectedItem;
            
            _currentJobs = new List<BackgroundJob>(_jobManager.GetActiveJobs());
            _currentJobs.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            
            var displayList = new List<string>(_currentJobs.Count);
            foreach (var j in _currentJobs)
            {
                string percent = j.ProgressPercent >= 0 ? $"{j.ProgressPercent:F0}% " : "";
                // Format: [#{ShortId}] [{Status}] {Name} - {Percent}
                string prefix = $"[#{j.ShortId}] [{GetStatusChar(j.Status)}] ";
                string suffix = $" - {percent}";
                
                // Calculate available width for name: 64(Dialog) - 2(Border) - 2(Padding) - Prefix - Suffix
                // Approx 60 total usable width.
                int availableWidth = 60 - CharacterWidthHelper.GetStringWidth(prefix) - CharacterWidthHelper.GetStringWidth(suffix);
                if (availableWidth < 10) availableWidth = 10; // Minimum width safety

                string truncatedName = CharacterWidthHelper.SmartTruncate(j.Name, availableWidth, _config.Display.Ellipsis);
                displayList.Add($"{prefix}{truncatedName}{suffix}");
            }

            if (displayList.Count == 0)
            {
                displayList.Add("No active jobs");
            }

            _jobsList.SetSource(displayList);
            
            if (oldSelection < displayList.Count && oldSelection >= 0)
            {
                _jobsList.SelectedItem = oldSelection;
            }
            
            UpdateDetailView();
        }

        private void UpdateDetailView()
        {
            if (_currentJobs.Count > 0 && _jobsList.SelectedItem >= 0 && _jobsList.SelectedItem < _currentJobs.Count)
            {
                var job = _currentJobs[_jobsList.SelectedItem];

                string details = $"Job ID: {job.Id}\n" +
                                 $"Name: {job.Name}\n" +
                                 $"Started: {job.StartTime:HH:mm:ss}\n" +
                                 $"Status: {job.Status}\n" +
                                 $"Progress: {job.ProgressMessage}\n" +
                                 $"Overall Progress: {job.ProgressPercent:F1}%\n";

                if (!string.IsNullOrEmpty(job.SourcePath)) details += $"Source: {job.SourcePath}\n";
                if (!string.IsNullOrEmpty(job.DestinationPath)) details += $"Destination: {job.DestinationPath}\n";
                
                details += $"Current Item: {job.CurrentOperationDetail}\n";

                // Remove the old manual inference logic as we now have explicit properties
                _detailView.Text = details;
            }
            else
            {
                _detailView.Text = "No job selected";
            }
        }

        private char GetStatusChar(JobStatus status)
        {
            return status switch
            {
                JobStatus.Running => 'R',
                JobStatus.Pending => 'P',
                JobStatus.Completed => 'C',
                JobStatus.Failed => 'F',
                JobStatus.Cancelled => 'X',
                _ => '?'
            };
        }

        private void CancelSelectedJob()
        {
            if (_currentJobs.Count > 0 && _jobsList.SelectedItem >= 0 && _jobsList.SelectedItem < _currentJobs.Count)
            {
                var job = _currentJobs[_jobsList.SelectedItem];
                if (MessageBox.Query("Confirm", $"Cancel job #{job.ShortId} '{job.Name}'?", "Yes", "No") == 0)
                {
                    _jobManager.CancelJob(job.Id);
                    RefreshList();
                }
            }
        }
    }
}
