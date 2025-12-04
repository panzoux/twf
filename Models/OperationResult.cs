namespace TWF.Models
{
    /// <summary>
    /// Represents the result of a file operation
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int FilesProcessed { get; set; }
        public int FilesSkipped { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Event arguments for operation progress reporting
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public string CurrentFile { get; set; } = string.Empty;
        public int CurrentFileIndex { get; set; }
        public int TotalFiles { get; set; }
        public long BytesProcessed { get; set; }
        public long TotalBytes { get; set; }
        public double PercentComplete { get; set; }
    }
}
