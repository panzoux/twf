# TWF Rust Port - Models Specification

## Overview

This document specifies the data models for the Rust port of TWF. These models represent the core data structures used throughout the application, corresponding to the C# models in the original implementation.

## Core Models

### FileEntry
Represents a file or directory entry in the file system.

```rust
pub struct FileEntry {
    pub name: String,              // Display name of the file/directory
    pub full_path: PathBuf,        // Full path to the file/directory
    pub size: u64,                 // Size in bytes (0 for directories)
    pub is_directory: bool,        // True if this is a directory
    pub is_hidden: bool,           // True if this is a hidden file/directory
    pub is_system: bool,           // True if this is a system file/directory
    pub modified_time: SystemTime, // Last modification time
    pub created_time: SystemTime,  // Creation time
    pub accessed_time: SystemTime, // Last access time
    pub is_marked: bool,           // True if this entry is marked
    pub attributes: FileAttributes,// Additional file attributes
}
```

### PaneState
Manages the state of a single pane (left or right).

```rust
pub struct PaneState {
    pub current_path: PathBuf,        // Current directory path
    pub file_mask: String,            // File filter mask (e.g., "*.txt")
    pub sort_mode: SortMode,          // Current sorting method
    pub display_mode: DisplayMode,    // How to display files (details, list, etc.)
    pub selected_index: usize,        // Index of currently selected item
    pub scroll_offset: usize,         // Scroll position in the file list
    pub file_entries: Vec<FileEntry>, // List of files/directories in current path
    pub marked_count: usize,          // Number of marked files/directories
    pub marked_size: u64,             // Total size of marked items
    pub last_refresh: Option<SystemTime>, // Time of last refresh
}
```

### TabSession
Manages the state of a single tab containing two panes.

```rust
pub struct TabSession {
    pub left_state: PaneState,        // State of the left pane
    pub right_state: PaneState,       // State of the right pane
    pub is_left_pane_active: bool,    // True if left pane has focus
    pub history: HistoryManager,      // Navigation history for this tab
    pub tab_name: Option<String>,     // Custom name for the tab
    pub tab_color: Option<Color>,     // Custom color for the tab
    pub id: Uuid,                     // Unique identifier for the tab
}
```

### Configuration
Main configuration structure containing all application settings.

```rust
pub struct Configuration {
    pub display: DisplaySettings,              // Display-related settings
    pub key_bindings: KeyBindings,             // Key binding configuration
    pub registered_folders: Vec<RegisteredFolder>, // Bookmarked folders
    pub extension_associations: HashMap<String, String>, // File associations
    pub archive: ArchiveSettings,              // Archive-related settings
    pub viewer: ViewerSettings,                // Viewer settings
    pub migemo: MigemoSettings,                // Migemo search settings
    pub save_session_state: bool,              // Whether to save session state
    pub configuration_program_path: String,    // Path to config editor
    pub text_editor_path: String,              // Path to text editor
    pub log_level: String,                     // Logging level
    pub max_history_items: usize,              // Max items in history
    pub shell: ShellSettings,                  // Shell configuration
}
```

### DisplaySettings
Settings related to the visual appearance of the application.

```rust
pub struct DisplaySettings {
    pub foreground_color: Color,              // Default foreground color
    pub background_color: Color,              // Default background color
    pub highlight_foreground_color: Color,    // Highlight foreground color
    pub highlight_background_color: Color,    // Highlight background color
    pub marked_file_color: Color,             // Color for marked files
    pub directory_color: Color,               // Color for directories
    pub directory_background_color: Color,    // Background color for directories
    pub inactive_directory_color: Color,      // Color for inactive directories
    pub inactive_directory_background_color: Color, // Background for inactive dirs
    pub filename_label_foreground_color: Color, // Filename label foreground
    pub filename_label_background_color: Color, // Filename label background
    pub pane_border_color: Color,             // Pane border color
    pub top_separator_foreground_color: Color, // Top separator foreground
    pub top_separator_background_color: Color, // Top separator background
    pub default_display_mode: DisplayMode,    // Default display mode
    pub show_hidden_files: bool,              // Whether to show hidden files
    pub show_system_files: bool,              // Whether to show system files
    pub cjk_character_width: u8,              // Width for CJK characters
    pub file_list_refresh_interval_ms: u64,   // Auto-refresh interval (ms)
    pub task_panel_height: u16,               // Default task panel height
    pub task_panel_update_interval_ms: u64,   // Task panel update interval
    pub smart_refresh_enabled: bool,          // Enable smart refresh
    pub max_simultaneous_jobs: u8,            // Max concurrent background jobs
    pub tab_name_truncation_length: usize,    // Max length for tab names
    pub dialog_help_foreground_color: Color,  // Dialog help text foreground
    pub dialog_help_background_color: Color,  // Dialog help text background
    pub active_tab_foreground_color: Color,   // Active tab foreground
    pub active_tab_background_color: Color,   // Active tab background
    pub inactive_tab_foreground_color: Color, // Inactive tab foreground
    pub inactive_tab_background_color: Color, // Inactive tab background
    pub tabbar_background_color: Color,       // Tab bar background
    pub ok_color: Color,                      // OK message color
    pub warning_color: Color,                 // Warning message color
    pub error_color: Color,                   // Error message color
    pub task_status_view_refresh_interval_ms: u64, // Task status refresh interval
    pub job_manager_refresh_interval_ms: u64,      // Job manager refresh interval
    pub log_file_progress_threshold_ms: u64,       // Slow operation threshold
    pub max_log_lines_in_memory: usize,            // Max log lines in memory
    pub log_save_path: PathBuf,                    // Path to save session log
    pub save_log_on_exit: bool,                    // Save log on exit
    pub ellipsis: String,                          // Ellipsis string
    pub max_log_files: usize,                      // Max rotated log files
    pub help_language: String,                     // Preferred help language
}
```

### KeyBindings
Configuration for keyboard shortcuts.

```rust
pub struct KeyBindings {
    pub key_binding_file: String,  // Path to key bindings file
    pub bindings: HashMap<String, Vec<KeyCombination>>, // Action to key mapping
}
```

### ArchiveSettings
Settings for archive handling.

```rust
pub struct ArchiveSettings {
    pub default_archive_format: ArchiveFormat, // Default format for new archives
    pub compression_level: u8,                // Compression level (0-9)
    pub show_archive_contents_as_virtual_folder: bool, // Show archives as folders
    pub archive_dll_paths: Vec<PathBuf>,      // Paths to archive DLLs
}
```

### ViewerSettings
Settings for file viewers.

```rust
pub struct ViewerSettings {
    pub default_text_encoding: String,        // Default text encoding
    pub show_line_numbers: bool,              // Show line numbers in text viewer
    pub text_viewer_foreground_color: Color,  // Text viewer foreground
    pub text_viewer_background_color: Color,  // Text viewer background
    pub text_viewer_status_foreground_color: Color, // Status bar foreground
    pub text_viewer_status_background_color: Color, // Status bar background
    pub text_viewer_message_foreground_color: Color, // Message foreground
    pub text_viewer_message_background_color: Color, // Message background
    pub default_image_view_mode: ViewMode,    // Default image view mode
    pub supported_image_extensions: Vec<String>, // Supported image extensions
    pub supported_text_extensions: Vec<String>,  // Supported text extensions
}
```

### MigemoSettings
Settings for Japanese incremental search.

```rust
pub struct MigemoSettings {
    pub enabled: bool,           // Enable Migemo search
    pub dict_path: PathBuf,      // Path to Migemo dictionary
}
```

### ShellSettings
Shell configuration for executing custom functions.

```rust
pub struct ShellSettings {
    pub windows: String,         // Windows shell command
    pub linux: String,           // Linux shell command
    pub mac: String,             // macOS shell command
    pub default: String,         // Default shell command
}
```

### RegisteredFolder
A bookmarked folder with custom properties.

```rust
pub struct RegisteredFolder {
    pub name: String,            // Display name for the folder
    pub path: PathBuf,           // Full path to the folder
    pub color: Option<Color>,    // Optional color for the folder
    pub icon: Option<String>,    // Optional icon for the folder
}
```

### SessionState
State to be saved/restored between application runs.

```rust
pub struct SessionState {
    pub left_path: Option<PathBuf>,      // Left pane path on exit
    pub right_path: Option<PathBuf>,     // Right pane path on exit
    pub left_mask: Option<String>,       // Left pane file mask
    pub right_mask: Option<String>,      // Right pane file mask
    pub left_sort: SortMode,             // Left pane sort mode
    pub right_sort: SortMode,            // Right pane sort mode
    pub left_display_mode: DisplayMode,  // Left pane display mode
    pub right_display_mode: DisplayMode, // Right pane display mode
    pub left_history: Vec<PathBuf>,      // Left pane navigation history
    pub right_history: Vec<PathBuf>,     // Right pane navigation history
    pub left_focus_target: Option<String>, // Left pane focus target on startup
    pub right_focus_target: Option<String>, // Right pane focus target on startup
    pub left_pane_active: bool,          // Which pane was active
    pub task_pane_height: u16,           // Task pane height
    pub task_pane_expanded: bool,        // Whether task pane was expanded
    pub active_tab_index: usize,         // Active tab index
    pub tabs: Vec<TabSessionState>,      // State of all tabs
}
```

### TabSessionState
State of a single tab for session persistence.

```rust
pub struct TabSessionState {
    pub left_path: Option<PathBuf>,      // Left pane path
    pub right_path: Option<PathBuf>,     // Right pane path
    pub left_mask: Option<String>,       // Left pane file mask
    pub right_mask: Option<String>,      // Right pane file mask
    pub left_sort: SortMode,             // Left pane sort mode
    pub right_sort: SortMode,            // Right pane sort mode
    pub left_display_mode: DisplayMode,  // Left pane display mode
    pub right_display_mode: DisplayMode, // Right pane display mode
    pub left_history: Vec<PathBuf>,      // Left pane navigation history
    pub right_history: Vec<PathBuf>,     // Right pane navigation history
    pub left_focus_target: Option<String>, // Left pane focus target
    pub right_focus_target: Option<String>, // Right pane focus target
    pub left_pane_active: bool,          // Which pane is active
    pub tab_name: Option<String>,        // Custom tab name
    pub tab_color: Option<Color>,        // Custom tab color
}
```

## Enums

### SortMode
Defines how files are sorted in the pane.

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SortMode {
    NameAsc,
    NameDesc,
    SizeAsc,
    SizeDesc,
    DateAsc,
    DateDesc,
    TypeAsc,
    TypeDesc,
}
```

### DisplayMode
Defines how files are displayed in the pane.

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum DisplayMode {
    Details,    // Name, size, date, type
    List,       // Just filenames
    Compact,    // Compact list with minimal info
}
```

### ArchiveFormat
Supported archive formats.

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ArchiveFormat {
    Zip,
    Tar,
    TarGz,
    SevenZip,
}
```

### ViewMode
How images are displayed.

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ViewMode {
    ActualSize,
    FitToScreen,
    Zoom,
}
```

### UiMode
Current UI mode of the application.

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum UiMode {
    Normal,         // Normal file browsing
    VisualSelect,   // Visual selection mode
    Search,         // Search mode
    Command,        // Command mode
    Dialog,         // Dialog mode
}
```

### FileAttributes
Additional attributes for files/directories.

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct FileAttributes {
    pub read_only: bool,
    pub compressed: bool,
    pub encrypted: bool,
    pub temporary: bool,
}
```

## Traits

### Identifiable
Trait for objects that have unique identifiers.

```rust
pub trait Identifiable {
    fn id(&self) -> &Uuid;
}
```

### Refreshable
Trait for objects that can be refreshed.

```rust
pub trait Refreshable {
    fn refresh(&mut self) -> Result<(), Box<dyn std::error::Error>>;
}
```

### Configurable
Trait for objects that can be configured.

```rust
pub trait Configurable {
    fn apply_config(&mut self, config: &Configuration);
}
```

## Relationships

- `App` contains multiple `TabSession`s
- `TabSession` contains two `PaneState`s (left and right)
- `PaneState` contains multiple `FileEntry`s
- `Configuration` contains various setting structs
- `FileEntry` belongs to a `PaneState`
- `TabSessionState` is used to serialize `TabSession` for persistence

## Serialization

All models that need to be persisted should implement `Serialize` and `Deserialize` traits from `serde`.

For configuration and session state, we'll use JSON format for compatibility with the existing C# version.