# TWF Configuration Guide

## Configuration File Location

TWF stores its configuration files in:
```
%APPDATA%\TWF\
```

On your system, this is:
```
C:\Users\currypain\AppData\Roaming\TWF\
```

## Configuration Files

- **config.json** - Main configuration file (auto-created on first run)
- **session.json** - Session state (last used paths, sort modes, tab information, etc.)
- **keybindings.json** - Custom key bindings (optional, see below)

## Command Line Arguments

TWF supports the following command line arguments:

- `-cwd <filepath>`: Specifies a file where TWF will write the active pane's path when exiting via `ExitApplicationAndChangeDirectory`.
- `--cwd-file=<filepath>`: Same as `-cwd`.

This feature allows shell integration where the shell can change its working directory to the one active in TWF upon exit. See the included `twf_wrapper.cmd` for an example of how to implement this.

## Key Bindings

TWF features a fully configurable key binding system. The following keys represent the default configuration:

### Navigation
- **Arrow Keys** - Move cursor up/down, switch panes left/right
- **Tab** - Switch between left and right panes
- **Enter** - Navigate into directory, open file, or execute
- **Backspace** - Navigate to parent directory
- **Home** - Invert marks (or navigate to root with Ctrl)
- **End** - Refresh and clear all marks
- **J** - Jump to directory (VS Code-style autocomplete). Supports PageUp/PageDown to scroll results.
- **@** - Jump to file (Recursive search). Supports PageUp/PageDown to scroll results.
- **Ctrl+PageUp** - Move cursor to first entry
- **Ctrl+PageDown** - Move cursor to last entry
- **Ctrl+T** - Open a new tab
- **Ctrl+W** - Close current tab
- **Ctrl+Right** - Switch to next tab
- **Ctrl+Left** - Switch to previous tab
- **Alt+Left** - Go back in directory history
- **Alt+Right** - Go forward in directory history
- **Ctrl+B** - Open Tab Selector dialog

### Background Operations
- **Ctrl+J** - Open Job Manager (monitor and cancel tasks)
- **Ctrl+L** - Toggle Task Pane (expand/collapse log)
- **Ctrl+Up/Down** - Resize Task Pane height
- **Alt+Up/Down** - Scroll Task Pane log history

### File Marking
- **Space** - Toggle mark and move cursor down
- **Shift+Space** - Toggle mark and move cursor up
- **Ctrl+Space** - Mark range from previous mark to cursor

### Display
- **1-8** - Switch display modes (1-8 columns)

### File Operations
- **C** - Copy marked files
- **M** - Move marked files
- **D** - Delete marked files
- **K** - Create directory
- **O** - Sync opposite pane to active pane's directory
- **Shift+O** - Swap paths of left and right panes

### Search & Filter
- **F** - Enter incremental search mode
- **S** - Cycle sort mode (name, extension, size, date, unsorted)
- **Shift+S** - Open sort selection dialog
- **:** (colon) - File mask filter dialog
- **\*** (Shift+8) - Wildcard marking dialog

### Archive Operations
- **P** - Compress marked files (supports 7z, ZIP, TAR, etc.)
- **Shift+Enter** (on archive) - Extract archive
- **O** - Quick extraction to current folder

### File Viewing
- **V** - View file. Uses internal text viewer for text files, and the `ImageViewer` custom function for images.
- **Alt+E** - Open the current file in the external text editor (configured via `TextEditorPath`).
- **F8** / **B** - View file as hex (binary view)

#### Image Viewer Configuration
To view images, you must define a custom function named **"ImageViewer"** in your `custom_functions.json`. This allows you to use any external tool with full macro support.

**Example (Windows - using a console image viewer):**
```json
{
  "Name": "ImageViewer",
  "Command": "console_imgviewer.exe -fileinfo \"$P\\$F\"",
  "Description": "View image in console"
}
```

**Example (Linux - using xdg-open):**
```json
{
  "Name": "ImageViewer",
  "Command": "xdg-open \"$P/$F\"",
  "Description": "Open image with system viewer"
}
```

### Other Operations
- **I** - Show registered folders
- **Shift+B** - Register current directory
- **Shift+R** - Pattern-based rename
- **W** - File comparison
- **Shift+W** - File split/join
- **Y** - Launch configuration program
- **`** (backtick) - Context menu
- **ESC** - Cancel operation / Exit application
- **Shift+Q** - Exit application and change directory (if `-cwd` is specified)

## Custom Key Bindings

**All key bindings are fully configurable!** There are no hardcoded keys - everything is loaded from a JSON configuration file.

### How Key Bindings Work

1. The application always loads key bindings from `keybindings.json` (configurable in `config.json`)

2. The application looks for the key bindings file in:
   - `%APPDATA%\TWF\` directory first
   - Current directory if not found in AppData

3. To customize keys, edit `%APPDATA%\TWF\keybindings.json`

4. Changes take effect after restarting the application

### Configuration

You can change the key bindings filename in `config.json`:
```json
{
  "KeyBindings": {
    "KeyBindingFile": "keybindings.json"
  }
}
```

### Key Bindings File Format

The key bindings file uses JSON format:

```json
{
  "version": "1.0",
  "description": "My Custom Key Bindings",
  "bindings": {
    "C": "HandleCopyOperation",
    "X": "HandleCopyOperation",
    "Ctrl+C": "HandleCopyOperation",
    "Shift+Enter": "HandleShiftEnter"
  }
}
```

### Supported Key Formats

- **Simple keys**: `"A"`, `"C"`, `"1"`, `"@"`, `":"`, etc.
- **With Shift**: `"Shift+A"`, `"Shift+Enter"`, `"Shift+Space"`
- **With Ctrl**: `"Ctrl+C"`, `"Ctrl+PageUp"`, `"Ctrl+Home"`
- **With Alt**: `"Alt+F"`, `"Alt+X"`
- **Combinations**: `"Ctrl+Shift+A"`, `"Ctrl+Alt+Delete"`
- **Special keys**: `"Enter"`, `"Escape"`, `"Tab"`, `"Space"`, `"Backspace"`, `"Home"`, `"End"`, `"PageUp"`, `"PageDown"`, `"Up"`, `"Down"`, `"Left"`, `"Right"`, `"F1"`-`"F10"`

### Available Actions

Actions are grouped by functionality. You can bind any of these to keys in `keybindings.json`.

#### Navigation
- `SwitchPane`: Toggle focus between left and right panes.
- `SwitchToLeftPane`: Focus the left pane.
- `SwitchToRightPane`: Focus the right pane.
- `NavigateToParent`: Go up one directory level.
- `NavigateToRoot`: Go to the drive's root directory.
- `MoveCursorUp` / `MoveCursorDown`: Move the selection cursor.
- `PageUp` / `PageDown`: Scroll the file list by one page.
- `MoveCursorToFirst` / `MoveCursorToLast`: Jump to the top or bottom of the list.
- `JumpToPath`: Navigate directly to a specific path (prompts for input).
- `JumpToFile`: Find and jump to a file recursively in subdirectories.
- `SyncCurrentPaneWithOppositePane`: Change current pane's directory to match the opposite pane.
- `SyncOppositePaneWithCurrentPane`: Change opposite pane's directory to match current pane.
- `SwapPanes`: Swap the directories displayed in both panes.
- `ShowDriveChangeDialog`: Open the drive selection menu.
- `ShowHistoryDialog`: Open the directory history menu.
- `GoBackHistory`: Navigate backward in the directory history.
- `GoForwardHistory`: Navigate forward in the directory history.

#### File Operations
- `HandleEnterKey`: Open folder, view file, or execute based on context.
- `HandleShiftEnter`: Open file with editor or perform quick extraction on archives.
- `HandleCtrlEnter`: Open file using system shell association.
- `HandleCopyOperation`: Copy marked files to the opposite pane.
- `HandleMoveOperation`: Move marked files to the opposite pane.
- `HandleDeleteOperation`: Delete marked files (with confirmation).
- `HandleCreateDirectory`: Create a new folder.
- `HandleEditNewFile`: Create and edit a new 0-byte file.
- `HandleSimpleRename`: Rename the file under the cursor.
- `HandlePatternRename`: Perform bulk rename using regex/patterns.
- `HandleFileComparison`: Compare files between panes by size/date/name.
- `HandleFileSplitOrJoin`: Split a large file or join parts back together.
- `MoveToRegisteredFolder`: Move files to a pre-defined "Registered Folder".
- `HandleArchiveExtraction`: Extract selected archive contents.
- `ExecuteFile`: Run the file under the cursor.
- `ExecuteFileWithEditor`: Open the file using the configured text editor.

#### Marking
- `ToggleMarkAndMoveDown`: Mark/unmark current file and move cursor down.
- `ToggleMarkAndMoveUp`: Mark/unmark current file and move cursor up.
- `MarkRange`: Mark all files between the previous mark and the cursor.
- `MarkAll`: Mark all items in the current pane (except '..').
- `ClearMarks`: Unmark all items in the current pane.
- `InvertMarks`: Toggle marks for all items in the pane.
- `ShowWildcardMarkingDialog`: Mark files using wildcard/regex patterns.

#### Display & View
- `DisplayMode1` to `DisplayMode8`: Switch between 1 to 8 column view.
- `DisplayModeDetailed`: Switch to detailed file view (name, size, date).
- `CycleSortMode`: Cycle through different sort orders (Name, Size, Date, etc.).
- `ShowSortDialog`: Open sort selection menu.
- `RefreshPane`: Reload the current directory contents.
- `RefreshAndClearMarks`: Reload directory and reset all marks.
- `RefreshNoClearMarks`: Reload directory while preserving current marks.
- `ViewFile`: View file using the appropriate viewer (text or image).
- `ViewFileAsText`: Force open file in the internal text viewer.
- `ViewFileAsHex`: Open file in the hex viewer.
- `ShowFileInfoForCursor`: Display detailed size/metadata for the current item.

#### Archives
- `HandleCompressionOperation`: Compress marked files into an archive.

#### Tabs
- `NewTab`: Create a new tab session.
- `CloseTab`: Close the currently active tab.
- `NextTab` / `PreviousTab`: Cycle through open tabs.
- `ShowTabSelector`: Open the tab management/selection menu.

#### Search & Filter
- `EnterSearchMode`: Activate incremental search (supports Migemo).
- `ShowFileMaskDialog`: Filter the file list using wildcards (e.g., `*.txt`).

#### Task Management
- `ToggleTaskPanel`: Show/hide the background task log panel.
- `ResizeTaskPanelUp` / `ResizeTaskPanelDown`: Change log panel height.
- `ScrollTaskPanelUp` / `ScrollTaskPanelDown`: Scroll through the log history.
- `ShowJobManager`: Open the background job management dialog.
- `SaveLog`: Save the current task log to a file.

#### Application
- `ShowHelp`: Display the help dialog.
- `ShowVersion`: Show application version and environment info.
- `HandleContextMenu`: Open the context menu for the selected file.
- `ShowCustomFunctionsDialog`: Open the custom functions menu.
- `ReloadConfiguration`: Refresh settings from disk.
- `ExitApplication`: Close TWF.
- `ExitApplicationAndChangeDirectory`: Close TWF and sync shell CWD (requires wrapper).

### Text Viewer Key Bindings

**Text viewer keybindings are fully configurable!** When viewing a text file (press `V` on a file), the text viewer uses its own set of keybindings defined in the `textViewerBindings` section.

#### How Text Viewer Bindings Work

1. Text viewer bindings are separate from the main file manager bindings
2. They are defined in the `textViewerBindings` section of `keybindings.json`
3. If the `textViewerBindings` section is missing, default bindings are used
4. All text viewer actions use the `TextViewer.` prefix

#### Text Viewer Bindings File Format

Add a `textViewerBindings` section to your `keybindings.json`:

```json
{
  "version": "1.0",
  "description": "TWF Key Bindings",
  "bindings": {
    "V": "ViewFileAsText",
    ...
  },
  "textViewerBindings": {
    "F5": "TextViewer.GoToFileTop",
    "F6": "TextViewer.GoToFileBottom",
    "Home": "TextViewer.GoToLineStart",
    "End": "TextViewer.GoToLineEnd",
    "PageUp": "TextViewer.PageUp",
    "PageDown": "TextViewer.PageDown",
    "Escape": "TextViewer.Close",
    "Enter": "TextViewer.Close",
    "F4": "TextViewer.Search",
    "F3": "TextViewer.FindNext",
    "Shift+F3": "TextViewer.FindPrevious",
    "Shift+E": "TextViewer.CycleEncoding"
  }
}
```

#### Available Text Viewer Actions

| Action Name | Description | Default Key |
|-------------|-------------|-------------|
| `TextViewer.GoToFileTop` | Scroll to the first line of the file | F5, Home |
| `TextViewer.GoToTop` | (Alias) Scroll to the first line of the file | |
| `TextViewer.GoToFileBottom` | Scroll to the last line of the file | F6, End |
| `TextViewer.GoToBottom` | (Alias) Scroll to the last line of the file | |
| `TextViewer.GoToLineStart` | Move cursor to the start of the current line | |
| `TextViewer.GoToLineEnd` | Move cursor to the end of the current line | |
| `TextViewer.PageUp` | Scroll up one page | PageUp |
| `TextViewer.PageDown` | Scroll down one page | PageDown |
| `TextViewer.ScrollLeft` | Scroll horizontally left | Left |
| `TextViewer.ScrollRight` | Scroll horizontally right | Right |
| `TextViewer.Close` | Close the text viewer and return to file list | Escape, Enter |
| `TextViewer.Search` | Open the search dialog to find text | F4, F9 |
| `TextViewer.StartForwardSearch` | (Alias) Start forward search | |
| `TextViewer.StartBackwardSearch` | Start backward search | |
| `TextViewer.FindNext` | Jump to the next search match | F3 |
| `TextViewer.FindPrevious` | Jump to the previous search match | Shift+F3 |
| `TextViewer.CycleEncoding` | Cycle through text encodings (UTF-8, ASCII, etc.) | F7, Shift+E |
| `TextViewer.ToggleHexMode` | Toggle between text and hexadecimal view | F8, Shift+B |
| `TextViewer.ClearHighlight` | Clear current search highlight | C |

#### Hexadecimal View Mode

The text viewer supports a hexadecimal view mode that displays file contents as hex bytes with ASCII representation. This is useful for:
- Viewing binary files
- Examining file encoding issues
- Debugging file content problems
- Inspecting non-text files

**Hex Display Format:**
```
00000000  54 68 69 73 20 77 69 6c  6c 20 62 65 20 6f 75 72  |This will be our|
00000010  20 73 61 6d 70 6c 65 20  74 65 78 74 20 69 6e 20  | sample text in |
00000020  74 68 65 20 66 69 6c 65  2c 20 77 65 20 77 69 6c  |the file, we wil|
```

Each line shows:
- **Offset** (8 hex digits): Byte position in the file
- **Hex bytes** (16 bytes per line): Two-digit hexadecimal values
- **ASCII column**: Printable characters or `.` for non-printable

**Usage:**
1. Open a file in text viewer (press `V`)
2. Press `B` to toggle hex mode
3. Press `B` again to return to text mode
4. Scroll position is preserved when switching modes

#### Example: Custom Text Viewer Bindings

Here's an example of customizing text viewer keys to use Vim-style navigation:

```json
{
  "version": "1.0",
  "description": "Vim-style Text Viewer Bindings",
  "bindings": {
    "V": "ViewFileAsText",
    ...
  },
  "textViewerBindings": {
    "G": "TextViewer.GoToFileTop",
    "Shift+G": "TextViewer.GoToFileBottom",
    "0": "TextViewer.GoToLineStart",
    "Shift+4": "TextViewer.GoToLineEnd",
    "Ctrl+U": "TextViewer.PageUp",
    "Ctrl+D": "TextViewer.PageDown",
    "Q": "TextViewer.Close",
    "Escape": "TextViewer.Close",
    "/": "TextViewer.Search",
    "N": "TextViewer.FindNext",
    "Shift+N": "TextViewer.FindPrevious",
    "E": "TextViewer.CycleEncoding",
    "H": "TextViewer.ToggleHexMode"
  }
}
```

#### Example: Minimal Text Viewer Bindings

If you only want to customize a few keys and keep the rest as defaults:

```json
{
  "version": "1.0",
  "description": "Custom Text Viewer - Only Change Close Key",
  "bindings": {
    ...
  },
  "textViewerBindings": {
    "Q": "TextViewer.Close",
    "F5": "TextViewer.GoToFileTop",
    "F6": "TextViewer.GoToFileBottom",
    "Home": "TextViewer.GoToLineStart",
    "End": "TextViewer.GoToLineEnd",
    "PageUp": "TextViewer.PageUp",
    "PageDown": "TextViewer.PageDown",
    "F4": "TextViewer.Search",
    "F3": "TextViewer.FindNext",
    "Shift+F3": "TextViewer.FindPrevious",
    "Shift+E": "TextViewer.CycleEncoding",
    "B": "TextViewer.ToggleHexMode"
  }
}
```

#### Error Handling

- **Invalid action name**: The system will log a warning and ignore the binding
- **Missing textViewerBindings section**: Default hardcoded bindings will be used
- **Invalid key format**: The binding will be ignored and logged as an error
- **Duplicate keys**: The last binding in the file will take precedence

### Testing Custom Key Bindings

A test key bindings file (`keybindings_test.json`) is included with different mappings:
- `X` instead of `C` for copy
- `N` instead of `M` for move
- `R` instead of `D` for delete
- `T` instead of `S` for sort
- `Q` instead of `A` for mark all

To test:
1. Set `"KeyBindingFile": "keybindings_test.json"` in config.json
2. Set `"UseCustomBindings": true`
3. Restart the application
4. Try the new key mappings

## Configuration File Format

The config.json file is automatically created with default settings. You can edit it to customize:
- Display settings (fonts, colors)
- File masks
- Extension associations
- Archive settings
- Viewer settings
- Shell settings (for custom functions)
- Session state saving
- Logging level

Example config.json structure:
```json
{
  "Display": {
    "ForegroundColor": "White",
    "BackgroundColor": "Black",
    "HighlightForegroundColor": "Black",
    "HighlightBackgroundColor": "Cyan",
    "ShowHiddenFiles": true,
    "FilenameLabelForegroundColor": "White",
    "FilenameLabelBackgroundColor": "Blue",
    "PaneBorderColor": "Black",
    "TopSeparatorForegroundColor": "White",
    "TopSeparatorBackgroundColor": "Black",
    "VerticalSeparatorForegroundColor": "White",
    "VerticalSeparatorBackgroundColor": "DarkGray",
    "DialogForegroundColor": "Black",
    "DialogBackgroundColor": "Gray",
    "InputForegroundColor": "White",
    "InputBackgroundColor": "Black",
    "ActiveTabForegroundColor": "White",
    "ActiveTabBackgroundColor": "Blue",
    "InactiveTabForegroundColor": "Gray",
    "InactiveTabBackgroundColor": "Black",
    "TabbarBackgroundColor": "Black"
  },
  "Navigation": {
    "JumpToFileSearchDepth": 3,
    "JumpToFileMaxResults": 100,
    "JumpIgnoreList": [ ".git", "node_modules", "obj", "bin" ]
  }
}

### Key Bindings

- `KeyBindingFile`: The filename of your custom key bindings (e.g., `"keybindings.json"`).
- `UnlockPaneKey`: The key used to force-unlock a pane that is currently waiting for an external editor to close. (Default: `"Ctrl+U"`)

### General Settings

- `ConfigurationProgramPath`: Path to the executable used to edit the TWF configuration files.
- `ExternalEditorIsGui`: If true, launching the external editor (via Alt+E or PipeToAction) will not block the UI. The current pane will show a "Waiting" frame and can be manually unlocked with `Esc`. (Default: false)
- `MaxHistoryItems`: Maximum number of recently visited directories to keep per pane. (Default: 50)
- `SaveSessionState`: Whether to persist the state of tabs and history upon exit. (Default: true)
- `LogLevel`: The verbosity of the application log. Options: None, Trace, Debug, Information, Warning, Error, Critical. (Default: Information)

### Navigation Settings

- `JumpToFileSearchDepth`: Recursion depth for "Jump to File" search. (Default: 3)
- `JumpToFileMaxResults`: Maximum number of results to display in jump dialogs. (Default: 100)
- `JumpIgnoreList`: List of folder names to ignore during jump searches to improve performance. (Default: [".git"])
```

### Text Viewer Settings

- `AutoDetectEncoding`: If true, automatically detects file encoding using a tiered scoring system. (Default: true)
- `EncodingPriority`: A prioritized list of encodings to try during auto-detection and manual cycling (F7). (Default: `["utf-8", "shift_jis", "euc-jp", "unicode", "ascii"]`)

#### How Auto-Detection Works
1. **BOM Check**: Checks for standard Byte Order Marks (UTF-8, UTF-16).
2. **Strict UTF-8 Validation**: A state-machine check for 100% valid UTF-8 sequences.
3. **Japanese Heuristics**: Calculates confidence scores for Shift-JIS and EUC-JP based on byte pattern frequency.
4. **Fallback**: If heuristics are inconclusive, the first item in the `EncodingPriority` list is used.

### Session State

When `SaveSessionState` is set to `true`, TWF automatically persists the state of all open tabs to `session.json` upon exit. This includes:
- Current path for both panes in each tab
- Active pane selection
- File masks and sort modes
- History of visited directories for each pane
- Currently active tab

If set to `false`, TWF will start with a single tab at the user's home directory every time it is launched.

### Shell Configuration

The `Shell` section allows you to configure which shell/executable is used for custom functions on different operating systems. This provides cross-platform compatibility for custom functions.

**Properties:**
- `Windows`: Shell executable for Windows systems (default: "cmd.exe")
- `Linux`: Shell executable for Linux systems (default: "/bin/sh")
- `Mac`: Shell executable for macOS systems (default: "/bin/sh")
- `Default`: Fallback shell for other systems (default: "/bin/sh")

**Example:**
```json
{
  "Shell": {
    "Windows": "powershell.exe",  // Use PowerShell on Windows
    "Linux": "/bin/bash",         // Use bash on Linux
    "Mac": "/bin/zsh",            // Use zsh on macOS
    "Default": "/bin/sh"          // Fallback for other systems
  }
}
```

**Note:** Individual custom functions can override these defaults by specifying their own `Shell` property in `custom_functions.json`.

## Background Operations

TWF features a non-blocking background operation system for long-running file tasks.

### Job Manager
The Job Manager (`Ctrl+J`) allows you to monitor all active, completed, or failed background tasks. You can manually cancel any running job from this dialog.

### Task Status View (Log Pane)
The log pane at the bottom of the screen (`Ctrl+L`) shows a real-time history of operations. 
- **Busy Spinner**: When jobs are running, a spinner (`|`, `/`, `-`, `\`) appears in the status line.
- **Tab Busy Indicator**: Tabs with active background jobs are marked with a `~` (e.g., `[1:Docs]~*`).
- **Resizing**: You can increase or decrease the height of the expanded log pane using `Ctrl+Up` and `Ctrl+Down`.
- **Scrolling**: Use `Alt+Up` and `Alt+Down` to scroll through the log history when the pane is expanded.

### Logging Configuration

The `LogLevel` setting controls how much information is written to the log file (`%APPDATA%\TWF\logs\twf_errors.log`).

**Available Log Levels:**
- **None** - Disables all logging (no log file will be written)
- **Trace** - Most verbose, logs everything including detailed trace information
- **Debug** - Logs debug information, useful for troubleshooting
- **Information** - Default level, logs general informational messages
- **Warning** - Only logs warnings and errors
- **Error** - Only logs errors and critical issues
- **Critical** - Only logs critical failures

**Example:**
```json
{
  "LogLevel": "Warning"
}
```

To disable logging completely, set:
```json
{
  "LogLevel": "None"
}
```

**Note:** The log file is automatically rotated when it exceeds 10MB. Old logs are renamed using the format `{basename}_{yyyyMMdd_HHmmss}.log` (e.g., `twf_errors_20260113_220000.log`). The number of old logs kept is controlled by `MaxLogFiles`.

## Dynamic Configuration Reloading

You can reload configuration settings without restarting the application by pressing `Y` to launch your configuration editor. After you close the editor, TWF will ask if you want to reload the configuration.

### Reloadable Settings
The following settings take effect immediately after checking "Yes" to reload:
- **Colors**: All display colors (foreground, background, highlight, etc.)
- **Key Bindings**: Modifications to `keybindings.json`
- **Custom Functions**: Modifications to `custom_functions.json`
- **Registered Folders**: Manual edits to `registered_directory.json`

### Non-Reloadable Settings (Requires Restart)
Some core settings require a full application restart to take effect:
- **Migemo Settings**: Enabled status or dictionary path
- **Simultaneous Jobs**: The `MaxSimultaneousJobs` limit
- **CJK Width**: The `CJK_CharacterWidth` setting

### Migemo Dictionary Path Resolution

The Migemo dictionary path resolution follows this priority order:
1. Configured path (from `DictPath` setting in config.json, if provided and directory exists)
2. User profile directory (`%APPDATA%\TWF\dict` on Windows, `~/.twf/dict` on Linux/macOS)
3. Executable's directory (`<exe_dir>/dict`)
4. Common system paths for Linux/macOS:
   - `/usr/share/cmigemo/utf-8`
   - `/usr/local/share/migemo/utf-8`
   - `/opt/homebrew/share/migemo/utf-8`

**Note:** The dictionary file should be named `migemo-dict` or placed in a `utf-8` subdirectory.

## Top Separator Information Display

The top separator (the horizontal line between the path display and the file panes) now shows additional information:

- **Drive/Share Name**: Shows the drive letter (e.g., "C:") or network share name (e.g., "\\server")
- **Marked File Statistics**: Shows the number of marked files and their total size (e.g., "3 Files 1.5 MB marked")
- **Format**: Left pane information ━ERight pane information (similar to the path display format)

### Top Separator Colors

You can customize the colors of the top separator in the Display settings:

- `TopSeparatorForegroundColor`: Text color for the information displayed in the top separator (default: "White")
- `TopSeparatorBackgroundColor`: Background color of the top separator (default: "Black")

### Vertical Separator Colors

You can customize the colors of the vertical separator between the two file panes:

- `VerticalSeparatorForegroundColor`: Text color for the vertical separator line (default: "White")
- `VerticalSeparatorBackgroundColor`: Background color of the vertical separator (default: "DarkGray")

- `TaskPanelHeight`: Default height for the expanded log panel (default: 10)
- `TaskPanelUpdateIntervalMs`: Refresh rate for UI animations/progress (default: 300, min: 100)
- `MaxSimultaneousJobs`: Limit for simultaneous background tasks (default: 4)
- `TabNameTruncationLength`: Maximum characters per pane name in the tab bar (default: 8)
- `MaxLogFiles`: Maximum number of rotated log files to keep for each log type (default: 5). If set to 0, no logs are deleted.
- `HelpLanguage`: Preferred language for help descriptions (e.g., "en", "jp"). (default: "en")

Example configuration:
```json
{
  "Display": {
    "TopSeparatorForegroundColor": "Cyan",
    "TopSeparatorBackgroundColor": "DarkGray",
    "VerticalSeparatorForegroundColor": "White",
    "VerticalSeparatorBackgroundColor": "DarkGray"
  }
}
```

## Task Panel & Logging Settings

The new Task Panel (Log View) and Job Manager have specific configuration options under the `Display` section:

### Logging Colors
Customize the colors for different log levels in the Task Panel:
- `OkColor`: Color for success messages (e.g., "[OK]") - Default: "Green"
- `WarningColor`: Color for warnings (e.g., "[WARN]") - Default: "Yellow"
- `ErrorColor`: Color for errors (e.g., "[FAIL]") - Default: "Red"

### Formatting
- `Ellipsis`: The string used when truncating long filenames (e.g., "..." or "…") - Default: "..."

### Refresh Intervals & Behavior
- `TaskPanelUpdateIntervalMs`: Refresh rate for spinners and "Processing..." indicators - Default: 300ms
- `TaskStatusViewRefreshIntervalMs`: Frequency of flushing buffered logs to the UI - Default: 500ms
- `JobManagerRefreshIntervalMs`: Refresh rate for the Job Manager dialog - Default: 500ms

### Log Persistence
Control how logs are managed in memory and on disk:
- `MaxLogLinesInMemory`: Maximum lines kept in the scrollable view before flushing to file - Default: 2000
- `SaveLogOnExit`: If true, saves the current session log to file when closing TWF - Default: true
- `LogSavePath`: Path relative to AppData where logs are saved - Default: "logs/session.log"
- `LogFileProgressThresholdMs`: Time in ms before a single file operation is considered "slow" and explicitly logged - Default: 5000ms


## Registered Folders Environment Variable Support

Registered folders now support environment variable expansion in their paths. You can use the following formats in registered folder paths:

- **Windows batch format**: `%VAR%` (e.g., `%USERPROFILE%\Documents`)
- **Unix shell format**: `$VAR` (e.g., `$HOME/Documents`)
- **Curly brace format**: `${VAR}` (e.g., `${USERPROFILE}/Documents`)
- **PowerShell format**: `$env:VAR` (e.g., `$env:USERPROFILE\Documents`)

### Example Registered Folder Configuration

```json
{
  "RegisteredFolders": [
    {
      "Name": "My Documents",
      "Path": "%USERPROFILE%\\Documents"
    },
    {
      "Name": "Temp Directory",
      "Path": "${TEMP}"
    },
    {
      "Name": "Home Directory",
      "Path": "$HOME"
    },
    {
      "Name": "App Data",
      "Path": "$env:APPDATA"
    }
  ]
}
```

## Custom Functions and Macros

You can define custom functions in `custom_functions.json`. These functions can use macros which are expanded before execution.

### Available Macros
- `$P`: Active pane path
- `$O`: Other pane path
- `$L`: Left pane path
- `$R`: Right pane path
- `$F`: Current filename
- `$W`: Current filename without extension
- `$E`: File extension
- `$M`: Marked files (spaces separated, quoted)
- `$*`: Active pane file mask
- `$I"Prompt"`: Input dialog
- `$~`: Home directory
- `$V"VAR"`: Environment variable value
- `$V"twf"`: TWF application directory
- `$#XX`: ASCII character (hex code)

### Pipping Output to Actions

You can pipe the output of a custom command to an internal action using the `PipeToAction` property.

Example:
```json
{
    "Name": "Jump to FZF Selection",
    "Command": "fzf",
    "PipeToAction": "JumpToPath",
    "Description": "Use fzf to select a directory and jump to it"
}
```
In this example, the output of `fzf` is captured and passed as an argument to `JumpToPath`.

**Supported Actions for Piping:**
- `JumpToPath`: Jumps to the directory provided in the argument.


## Help System

The help system (triggered by `F1`) is dynamic and supports multiple languages.

### Language Definitions
Help descriptions are loaded from JSON files located in the `help/` subdirectory of the TWF configuration folder:
- **Windows**: `%APPDATA%\TWF\help\help.{lang}.json`
- **Linux/macOS**: `~/.config/TWF/help/help.{lang}.json`

### Customizing Help
You can customize the descriptions or categories by editing these JSON files.

### Multi-language Support
- Set `Display.HelpLanguage` in `config.json` to change the default language.
- Press `L` while the help dialog is open to rotate between available languages.

