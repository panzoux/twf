namespace TWF.Models
{
    /// <summary>
    /// Represents the session state for persisting pane states between application runs
    /// </summary>
    public class SessionState
    {
        public string LeftPath { get; set; } = string.Empty;
        public string RightPath { get; set; } = string.Empty;
        public string LeftMask { get; set; } = "*";
        public string RightMask { get; set; } = "*";
        public SortMode LeftSort { get; set; } = SortMode.NameAscending;
        public SortMode RightSort { get; set; } = SortMode.NameAscending;
        public DisplayMode LeftDisplayMode { get; set; } = DisplayMode.Details;
        public DisplayMode RightDisplayMode { get; set; } = DisplayMode.Details;
        public bool LeftPaneActive { get; set; } = true;
    }
}
