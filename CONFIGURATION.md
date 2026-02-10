# TWF Configuration Guide

## Configuration File Location

TWF stores its configuration files in:
```
%APPDATA%\TWF\
```

## Configuration Files

- **config.json** - Main configuration file (auto-created on first run)
- **session.json** - Session state (last used paths, sort modes, tab information, etc.)
- **keybindings.json** - Custom key bindings (optional)

## Display Settings

These settings are located under the `"Display"` section in `config.json`.

### Basic Colors
- `ForegroundColor`: Default text color (default: "White")
- `BackgroundColor`: Default background color (default: "Black")
- `HighlightForegroundColor`: Text color for the selection bar (default: "Black")
- `HighlightBackgroundColor`: Background color for the selection bar (default: "Cyan")
- `DirectoryColor`: Text color for directories (default: "BrightCyan")
- `PaneBorderColor`: Color for pane boundaries (default: "Green")

### Top & Vertical Separators
- `TopSeparatorForegroundColor`: Text color for the information line above panes (default: "White")
- `TopSeparatorBackgroundColor`: Background color of the top separator (default: "Black")
- `VerticalSeparatorForegroundColor`: Color of the vertical line between panes (default: "White")
- `VerticalSeparatorBackgroundColor`: Background color of the vertical separator (default: "DarkGray")

### Dialog & ListBox Colors
- `DialogForegroundColor`: Default text color for dialogs (default: "Black")
- `DialogBackgroundColor`: Default background color for dialogs (default: "Gray")
- `DialogListBoxForegroundColor`: Text color for list items in dialogs (default: "Gray")
- `DialogListBoxBackgroundColor`: Background color for list area in dialogs (default: "Black")
- `DialogListBoxSelectedForegroundColor`: Text color for highlighted/changed items in dialog lists (default: "BrightYellow")
- `DialogListBoxSelectedBackgroundColor`: Background color for the selection line in dialog lists (default: "Black")
- `InputForegroundColor`: Text color for text boxes (default: "White")
- `InputBackgroundColor`: Background color for text boxes (default: "DarkGray")

### Tabs & Navigation
- `ActiveTabForegroundColor`: Foreground color for the active tab (default: "White")
- `ActiveTabBackgroundColor`: Background color for the active tab (default: "Blue")
- `InactiveTabForegroundColor`: Foreground color for inactive tabs (default: "Gray")
- `InactiveTabBackgroundColor`: Background color for inactive tabs (default: "Black")
- `TabbarBackgroundColor`: Background color for the tab bar area (default: "Black")
- `TabNameTruncationLength`: Maximum characters per pane name in the tab bar (default: 8)

### Operational Display
- `FileListRefreshIntervalMs`: Refresh interval in milliseconds. Set to 0 to disable auto-refresh. (default: 500ms)
- `SmartRefreshEnabled`: If true, periodically updates visible file metadata without full reload. (default: true)
- `WorkInProgressFileColor`: Color for files being processed by background jobs (default: "Yellow")
- `WorkInProgressDirectoryColor`: Color for directories being processed (default: "Magenta")
- `Ellipsis`: String used for truncation (default: "...")

## Navigation Settings

These settings are located under the `"Navigation"` section in `config.json`.

- `StartDirectory`: Initial directory on startup if no session is restored. (Default: User Profile)
- `JumpToFileSearchDepth`: Recursion depth for "Jump to File" (@) search. (Default: 3)
- `JumpToFileMaxResults`: Max results shown in "Jump to File" dialog. (Default: 100)
- `JumpToPathSearchDepth`: Recursion depth for "Jump to Directory" (J) search. (Default: 2)
- `JumpToPathMaxResults`: Max results shown in "Jump to Directory" dialog. (Default: 100)
- `MaxPathInputLength`: Max characters for manual path input in jump dialogs. (Default: 4096)
- `MaxRenamePreviewResults`: Max items to process in Pattern Rename live preview. (Default: 100)
- `JumpIgnoreList`: List of folder names to skip during recursive searches (e.g., [".git", "node_modules"]).

## Task Panel & Logging Settings

### Logging Colors
- `OkColor`: Color for success messages (e.g., "[OK]") - Default: "Green"
- `WarningColor`: Color for warnings (e.g., "[WARN]") - Default: "Yellow"
- `ErrorColor`: Color for errors (e.g., "[FAIL]") - Default: "Red"

### Behavior
- `TaskPanelHeight`: Default height for the expanded log panel (default: 10)
- `TaskPanelUpdateIntervalMs`: Refresh rate for spinners and progress indicators (default: 300ms)
- `MaxSimultaneousJobs`: Limit for concurrent background tasks (default: 4)

### Persistence
- `LogLevel`: Application log verbosity (None, Trace, Debug, Information, Warning, Error, Critical).
- `MaxLogLinesInMemory`: Max lines kept in the scrollable view before flushing to file. (Default: 2000)
- `LogSavePath`: Path where logs are saved (relative to AppData/TWF/). (Default: "logs/session.log")
- `MaxLogFiles`: Number of rotated log files to keep. (Default: 5)

## Custom Functions and Macros

Define functions in `custom_functions.json` to extend TWF capability.

### Available Macros
- `$P`: Active pane path
- `$O`: Other pane path
- `$L`: Left pane path
- `$R`: Right pane path
- `$F`: Current filename
- `$W`: Current filename without extension
- `$E`: File extension
- `$M`: Marked files (quoted, space-separated)
- `$*`: Active pane file mask
- `$I"Prompt"`: Input dialog (shows a styled dialog with the prompt)
- `$~`: Home directory (User profile)
- `$V"VAR"`: Environment variable value
- `$V"twf"`: TWF application directory
- `$#XX`: ASCII character (hex code)
- `$\"`: Literal double quote

### Piping Output to Actions
Use `PipeToAction` to send command output to an internal TWF action.
- **Supported Actions:** `JumpToPath`, `ExecuteFile`, `ExecuteFileWithEditor`, `EditConfig`.

#### Argument Cleansing (Implicit)
Any path passed via `PipeToAction` or macro arguments automatically undergoes **Quote Cleansing** (stripping surrounding double quotes and extra whitespace). This allows safe use of quotes in shell commands to handle spaces.

## Help System

- **Language Support**: Loaded from `help/help.{lang}.json`.
- **Default Language**: Set via `Display.HelpLanguage` in `config.json`.
- **Rotation**: Press `L` while the help dialog is open to cycle languages.