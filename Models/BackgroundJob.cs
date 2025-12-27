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
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public JobStatus Status { get; set; } = JobStatus.Pending;
        public double ProgressPercent { get; set; }
        public string ProgressMessage { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        
        // Context info
        public int TabId { get; set; } = -1;
        public string TabName { get; set; } = string.Empty;
        
        public bool IsActive => Status == JobStatus.Pending || Status == JobStatus.Running;
    }
}
