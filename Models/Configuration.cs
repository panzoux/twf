using System.Text.Json.Serialization;

namespace TWF.Models
{
    /// <summary>
    /// Main configuration class containing all application settings
    /// </summary>
    public class Configuration
    {
        public DisplaySettings Display { get; set; } = new DisplaySettings();
        public KeyBindings KeyBindings { get; set; } = new KeyBindings();

        [JsonIgnore]
        public List<RegisteredFolder> RegisteredFolders { get; set; } = new List<RegisteredFolder>();
        public Dictionary<string, string> ExtensionAssociations { get; set; } = new Dictionary<string, string>();
        public ArchiveSettings Archive { get; set; } = new ArchiveSettings();
        public ViewerSettings Viewer { get; set; } = new ViewerSettings();
        public MigemoSettings Migemo { get; set; } = new MigemoSettings();
        public bool SaveSessionState { get; set; } = true;
        public string ConfigurationProgramPath { get; set; } = OperatingSystem.IsWindows() ? "notepad.exe" : "vim";
        public bool ExternalEditorIsGui { get; set; } = false;
        public string LogLevel { get; set; } = "Information";
        public int MaxHistoryItems { get; set; } = 50;
        public ShellSettings Shell { get; set; } = new ShellSettings();
        public NavigationSettings Navigation { get; set; } = new NavigationSettings();
    }

    /// <summary>
    /// Navigation-related settings
    /// </summary>
    public class NavigationSettings
    {
        /// <summary>
        /// The initial directory to open when the application starts if no session is restored.
        /// </summary>
        public string StartDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        /// <summary>
        /// Recursion depth for "Jump to File" search. Default: 3.
        /// </summary>
        public int JumpToFileSearchDepth { get; set; } = 3;

        /// <summary>
        /// Maximum number of results to display in "Jump to File" dialog. Default: 100.
        /// </summary>
        public int JumpToFileMaxResults { get; set; } = 100;

        /// <summary>
        /// Recursion depth for "Jump to Directory" search. Default: 2.
        /// </summary>
        public int JumpToPathSearchDepth { get; set; } = 2;

        /// <summary>
        /// Maximum number of results to display in "Jump to Directory" dialog. Default: 100.
        /// </summary>
        public int JumpToPathMaxResults { get; set; } = 100;

        /// <summary>
        /// List of folder names to ignore during jump searches (e.g., ".git", "node_modules").
        /// </summary>
        public List<string> JumpIgnoreList { get; set; } = new List<string> { ".git" };

        /// <summary>
        /// Maximum length for path input in jump dialogs. Default: 4096.
        /// </summary>
        public int MaxPathInputLength { get; set; } = 4096;

        /// <summary>
        /// Maximum number of items to display in the Pattern Rename preview. Default: 100.
        /// </summary>
        public int MaxRenamePreviewResults { get; set; } = 100;
    }

    /// <summary>
    /// Display-related settings
    /// </summary>
    public class DisplaySettings
    {
        public string ForegroundColor { get; set; } = "White";
        public string BackgroundColor { get; set; } = "Black";
        public string HighlightForegroundColor { get; set; } = "Black";
        public string HighlightBackgroundColor { get; set; } = "Cyan";
        public string MarkedFileColor { get; set; } = "Cyan";
        public string DirectoryColor { get; set; } = "BrightCyan";
        public string DirectoryBackgroundColor { get; set; } = "Black";
        public string InactiveDirectoryColor { get; set; } = "Cyan";
        public string InactiveDirectoryBackgroundColor { get; set; } = "Black";
        public string FilenameLabelForegroundColor { get; set; } = "White";
        public string FilenameLabelBackgroundColor { get; set; } = "Blue";
        public string PaneBorderColor { get; set; } = "Green";
        public string TopSeparatorForegroundColor { get; set; } = "Black";
        public string TopSeparatorBackgroundColor { get; set; } = "Gray";
        public string VerticalSeparatorForegroundColor { get; set; } = "White";
        public string VerticalSeparatorBackgroundColor { get; set; } = "DarkGray";
        public DisplayMode DefaultDisplayMode { get; set; } = DisplayMode.Details;
        public bool ShowHiddenFiles { get; set; } = true;
        public bool ShowSystemFiles { get; set; } = false;
        /// <summary>
        /// Width for CJK (Chinese, Japanese, Korean) characters. Default: 2 (double-width).
        /// Set to 0 to disable CJK width calculation and use standard string length.
        /// </summary>
        public int CJK_CharacterWidth { get; set; } = 2;
        /// <summary>
        /// File list auto-refresh interval in milliseconds. Default: 500ms.
        /// Set to 0 to disable auto-refresh (refresh only on user input).
        /// </summary>
        public int FileListRefreshIntervalMs { get; set; } = 500;
        /// <summary>
        /// Default height for the expanded task log panel.
        /// </summary>
        public int TaskPanelHeight { get; set; } = 10;
        /// <summary>
        /// Refresh interval for the task panel spinner and progress in milliseconds. Default: 300ms.
        /// </summary>
        public int TaskPanelUpdateIntervalMs { get; set; } = 300;

        /// <summary>
        /// If true, periodically updates file size/date for visible files without full reload.
        /// </summary>
        public bool SmartRefreshEnabled { get; set; } = true;

        /// <summary>
        /// Foreground color for files currently being processed in background jobs.
        /// </summary>
        public string WorkInProgressFileColor { get; set; } = "Yellow";

        /// <summary>
        /// Foreground color for directories currently being processed in background jobs.
        /// </summary>
        public string WorkInProgressDirectoryColor { get; set; } = "Magenta";

        /// <summary>
        /// Maximum number of simultaneous background jobs. Default: 4.
        /// </summary>
        public int MaxSimultaneousJobs { get; set; } = 4;
        /// <summary>
        /// Maximum length for pane names in the tab bar. Default: 8.
        /// </summary>
        public int TabNameTruncationLength { get; set; } = 8;
        /// <summary>
        /// Foreground color for dialogs.
        /// </summary>
        public string DialogForegroundColor { get; set; } = "Black";
        /// <summary>
        /// Background color for dialogs.
        /// </summary>
        public string DialogBackgroundColor { get; set; } = "Gray";
        /// <summary>
        /// Foreground color for buttons in dialogs.
        /// </summary>
        public string ButtonForegroundColor { get; set; } = "Black";
        /// <summary>
        /// Background color for buttons in dialogs.
        /// </summary>
        public string ButtonBackgroundColor { get; set; } = "Gray";
        /// <summary>
        /// Foreground color for focused buttons in dialogs.
        /// </summary>
        public string ButtonFocusForegroundColor { get; set; } = "White";
        /// <summary>
        /// Background color for focused buttons in dialogs.
        /// </summary>
        public string ButtonFocusBackgroundColor { get; set; } = "DarkGray";
        /// <summary>
        /// Foreground color for input fields (text boxes).
        /// </summary>
        public string InputForegroundColor { get; set; } = "White";
        /// <summary>
        /// Background color for input fields (text boxes).
        /// </summary>
        public string InputBackgroundColor { get; set; } = "DarkGray";
        /// <summary>
        /// Foreground color for help/hint text in dialogs.
        /// </summary>
        public string DialogHelpForegroundColor { get; set; } = "BrightYellow";
        /// <summary>
        /// Background color for help/hint text in dialogs.
        /// </summary>
        public string DialogHelpBackgroundColor { get; set; } = "Blue";
        public string ActiveTabForegroundColor { get; set; } = "White";
        public string ActiveTabBackgroundColor { get; set; } = "Blue";
        public string InactiveTabForegroundColor { get; set; } = "Gray";
        public string InactiveTabBackgroundColor { get; set; } = "Black";
        public string TabbarBackgroundColor { get; set; } = "Black";
        
        public string DialogListBoxForegroundColor { get; set; } = "Gray";
        public string DialogListBoxBackgroundColor { get; set; } = "Black";
        public string DialogListBoxSelectedForegroundColor { get; set; } = "BrightYellow";
        public string DialogListBoxSelectedBackgroundColor { get; set; } = "Black";

        // Task Panel & Logging Colors
        public string OkColor { get; set; } = "Green";
        public string WarningColor { get; set; } = "Yellow";
        public string ErrorColor { get; set; } = "Red";

        // Task Panel & Job Manager Intervals
        /// <summary>
        /// Refresh interval for the Task Status View log updates in milliseconds. Default: 500ms.
        /// </summary>
        public int TaskStatusViewRefreshIntervalMs { get; set; } = 500;
        
        /// <summary>
        /// Refresh interval for the Job Manager Dialog in milliseconds. Default: 500ms.
        /// </summary>
        public int JobManagerRefreshIntervalMs { get; set; } = 500;

        // Logging Thresholds & Persistence
        /// <summary>
        /// Threshold in milliseconds to consider a file operation "slow" and show progress.
        /// </summary>
        public int LogFileProgressThresholdMs { get; set; } = 5000;
        
        /// <summary>
        /// Maximum number of log lines to keep in memory before flushing to file.
        /// </summary>
        public int MaxLogLinesInMemory { get; set; } = 2000;
        
        /// <summary>
        /// Path to save the session log file. Relative to AppData/TWF or absolute.
        /// </summary>
        public string LogSavePath { get; set; } = "logs/session.log";
        
        /// <summary>
        /// Whether to save the in-memory log to file on application exit.
        /// </summary>
        public bool SaveLogOnExit { get; set; } = true;

        /// <summary>
        /// String to use for truncation ellipsis. Default: "..."
        /// </summary>
        public string Ellipsis { get; set; } = "...";

        /// <summary>
        /// Maximum number of rotated log files to keep. Set to 0 to keep all. Default: 5.
        /// </summary>
        public int MaxLogFiles { get; set; } = 5;

        /// <summary>
        /// Preferred language for help files (e.g., "en", "jp"). Default: "en".
        /// </summary>
        public string HelpLanguage { get; set; } = "en";
    }

    /// <summary>
    /// Key binding configuration
    /// </summary>
    public class KeyBindings
    {
        public string KeyBindingFile { get; set; } = "keybindings.json";
        public string UnlockPaneKey { get; set; } = "Ctrl+U";
    }

    /// <summary>
    /// Archive-related settings
    /// </summary>
    public class ArchiveSettings
    {
        public string DefaultArchiveFormat { get; set; } = "ZIP";
        public int CompressionLevel { get; set; } = 5;
        public bool ShowArchiveContentsAsVirtualFolder { get; set; } = true;
        public List<string> ArchiveDllPaths { get; set; } = new List<string>();
    }

    /// <summary>
    /// Viewer-related settings
    /// </summary>
    public class ViewerSettings
    {
        public bool ShowLineNumbers { get; set; } = true;
        public string ViewerLineNumberColor { get; set; } = "Green";
        public string TextViewerForegroundColor { get; set; } = "White";
        public string TextViewerBackgroundColor { get; set; } = "Black";
        public string TextViewerStatusForegroundColor { get; set; } = "White";
        public string TextViewerStatusBackgroundColor { get; set; } = "Gray";
        public string TextViewerMessageForegroundColor { get; set; } = "White";
        public string TextViewerMessageBackgroundColor { get; set; } = "Blue";
        public bool AutoDetectEncoding { get; set; } = true;
        public List<string> EncodingPriority { get; set; } = new List<string> { "utf-8", "shift_jis", "euc-jp", "unicode", "ascii" };
        public double HorizontalScrollMultiplier { get; set; } = 2.0;
        public List<string> SupportedImageExtensions { get; set; } = new List<string> 
        { 
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico" 
        };
        public List<string> SupportedTextExtensions { get; set; } = new List<string>
        {
            ".txt", ".md", ".json", ".xml", ".cs", ".js", ".ts", ".html", ".css", ".ini", ".conf", ".log", ".bat", ".sh", ".ps1", ".cmd", ".cpp", ".h", ".c", ".py", ".rb", ".java", ".go", ".rs", ".php", ".yaml", ".yml", ".toml", ".gitignore", ".gitattributes", ".editorconfig", ".sln", ".csproj", ".fsproj", ".vbproj", ".props", ".targets", ".xaml", ".razor", ".svg", ".sql"
        };
    }

    /// <summary>
    /// Migemo-related settings for Japanese incremental search
    /// </summary>
    public class MigemoSettings
    {
        /// <summary>
        /// Enable or disable Migemo search functionality
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Path to Migemo dictionary directory
        /// Can be relative to application directory or absolute path
        /// </summary>
        public string DictPath { get; set; } = "dict";
    }

    /// <summary>
    /// Shell configuration for executing custom functions
    /// </summary>
    public class ShellSettings
    {
        public string Windows { get; set; } = "cmd.exe";
        public string Linux { get; set; } = "/bin/sh";
        public string Mac { get; set; } = "/bin/sh";
        public string Default { get; set; } = "/bin/sh";
    }
}
