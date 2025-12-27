using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using TWF.Models;
using TWF.Services;

namespace TWF.UI
{
    public class JobManagerDialog : Dialog
    {
        private readonly JobManager _jobManager;
        private ListView _jobsList;
        private List<BackgroundJob> _currentJobs;

        public JobManagerDialog(JobManager jobManager) : base("Background Jobs", 60, 20)
        {
            _jobManager = jobManager;
            _currentJobs = new List<BackgroundJob>();

            _jobsList = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(2),
                ColorScheme = new ColorScheme
                {
                    Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
                    Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                    HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
                }
            };

            Add(_jobsList);

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
            Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), (loop) =>
            {
                RefreshList();
                return true;
            });

            RefreshList();
        }

        private void RefreshList()
        {
            var oldSelection = _jobsList.SelectedItem;
            _currentJobs = _jobManager.GetActiveJobs().ToList();
            
            var displayList = _currentJobs.Select(j => 
                $"[{GetStatusChar(j.Status)}] {j.Name} - {j.ProgressPercent:F0}% ({j.ProgressMessage})").ToList();

            if (displayList.Count == 0)
            {
                displayList.Add("No active jobs");
            }

            _jobsList.SetSource(displayList);
            
            if (oldSelection < displayList.Count && oldSelection >= 0)
            {
                _jobsList.SelectedItem = oldSelection;
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
                if (MessageBox.Query("Confirm", $"Cancel job '{job.Name}'?", "Yes", "No") == 0)
                {
                    _jobManager.CancelJob(job.Id);
                    RefreshList();
                }
            }
        }
    }
}
