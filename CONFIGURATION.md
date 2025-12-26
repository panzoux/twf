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

The application has **hardcoded key bindings**. All the following keys should now work properly:

### Navigation
- **Arrow Keys** - Move cursor up/down, switch panes left/right
- **Tab** - Switch between left and right panes
- **Enter** - Navigate into directory, open file, or execute
- **Backspace** - Navigate to parent directory
- **Home** - Invert marks (or navigate to root with Ctrl)
- **End** - Clear all marks
- **Ctrl+PageUp** - Move cursor to first entry
- **Ctrl+PageDown** - Move cursor to last entry
- **Ctrl+T** - Open a new tab
- **Ctrl+W** - Close current tab
- **Ctrl+Right** - Switch to next tab
- **Ctrl+Left** - Switch to previous tab

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
- **:** (colon) - File mask filter dialog
- **@** - Wildcard marking dialog

### Archive Operations
- **P** - Compress marked files
- **Shift+Enter** (on archive) - Extract archive

### File Viewing
- **V** - View file as text
- **F8** - View file as hex (binary view)

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

## Recent Fix (2025-12-03)

**Issue Fixed:** Keys were not being captured properly because the ListView controls were consuming key events before they reached the main window handler.

**Solution:** Attached the key handler directly to both ListView controls in addition to the main window, ensuring all key presses are properly processed.

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

All actions from the default key bindings are available:
- `EnterSearchMode`, `SwitchPane`, `HandleEnterKey`, `HandleShiftEnter`, `HandleCtrlEnter`
- `NavigateToParent`, `NavigateToRoot`, `InvertMarks`
- `MoveCursorUp`, `MoveCursorDown`, `SwitchToLeftPane`, `SwitchToRightPane`
- `PageUp`, `PageDown`, `MoveCursorToFirst`, `MoveCursorToLast`, `RefreshPane`
- `ToggleMarkAndMoveDown`, `ToggleMarkAndMoveUp`, `MarkRange`, `MarkAll`, `ClearMarks`
- `SyncPanes` - Sync opposite pane to active pane's directory
- `SwapPanes` - Swap paths of left and right panes
- `NewTab`, `CloseTab`, `NextTab`, `PreviousTab`
- `DisplayMode1` through `DisplayMode8`, `DisplayModeDetailed` (key 0)
- `HandleCopyOperation`, `HandleMoveOperation`, `HandleDeleteOperation`
- `HandleCreateDirectory`, `ShowDriveChangeDialog`, `CycleSortMode`
- `ShowFileMaskDialog`, `ShowWildcardMarkingDialog`, `HandleContextMenu`
- `HandleCompressionOperation`, `HandleArchiveExtraction`
- `ShowRegisteredFolderDialog`, `RegisterCurrentDirectory`, `MoveToRegisteredFolder`
- `HandlePatternRename`, `HandleFileComparison`, `HandleFileSplitOrJoin`
- `HandleLaunchConfigurationProgram`, `ReloadConfiguration`, `ShowFileInfoForCursor`
- `ViewFileAsText`, `ExitApplication`, `ExitApplicationAndChangeDirectory`

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
| `TextViewer.GoToFileTop` | Scroll to the first line of the file | F5 |
| `TextViewer.GoToFileBottom` | Scroll to the last line of the file | F6 |
| `TextViewer.GoToLineStart` | Move cursor to the start of the current line | Home |
| `TextViewer.GoToLineEnd` | Move cursor to the end of the current line | End |
| `TextViewer.PageUp` | Scroll up one page | PageUp |
| `TextViewer.PageDown` | Scroll down one page | PageDown |
| `TextViewer.Close` | Close the text viewer and return to file list | Escape, Enter |
| `TextViewer.Search` | Open the search dialog to find text | F4 |
| `TextViewer.FindNext` | Jump to the next search match | F3 |
| `TextViewer.FindPrevious` | Jump to the previous search match | Shift+F3 |
| `TextViewer.CycleEncoding` | Cycle through text encodings (UTF-8, ASCII, etc.) | Shift+E |
| `TextViewer.ToggleHexMode` | Toggle between text and hexadecimal view | B |

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
    "FontName": "Consolas",
    "FontSize": 12,
    "ForegroundColor": "White",
    "BackgroundColor": "Black",
    "HighlightForegroundColor": "Black",
    "HighlightBackgroundColor": "Cyan",
    "ShowHiddenFiles": true,
    "FilenameLabelForegroundColor": "White",
    "FilenameLabelBackgroundColor": "Blue",
    "PaneBorderColor": "Black",
    "TopSeparatorForegroundColor": "White",
    "TopSeparatorBackgroundColor": "Black"
  },
  "Shell": {
    "Windows": "cmd.exe",
    "Linux": "/bin/bash",
    "Mac": "/bin/zsh",
    "Default": "/bin/sh"
  },
  "SaveSessionState": true,
  "LogLevel": "Information"
}
```

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

### Logging Configuration

The `LogLevel` setting controls how much information is written to the log file (`%APPDATA%\TWF\twf_errors.log`).

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

**Note:** The log file is automatically rotated when it exceeds 10MB. Old logs are renamed with a timestamp.

## Dynamic Configuration Reloading

You can reload configuration settings without restarting the application by pressing `Y` to launch your configuration editor. After you close the editor, TWF will ask if you want to reload the configuration.

### Reloadable Settings
The following settings take effect immediately after checking "Yes" to reload:
- **Colors**: All display colors (foreground, background, highlight, etc.)
- **Key Bindings**: Modifications to `keybindings.json`
- **Custom Functions**: Modifications to `custom_functions.json`
- **Registered Folders**: Manual edits to `registered_directory.json`

### Non-Reloadable Settings (Requires Restart)
Some settings require a full application restart to take effect:
- **Migemo Settings**: Enabled status
- **Logging Configuration**: Log level and log providers
- **Font Settings**: Font name and size (for some UI elements)

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
- **Format**: Left pane information │ Right pane information (similar to the path display format)

### Top Separator Colors

You can customize the colors of the top separator in the Display settings:

- `TopSeparatorForegroundColor`: Text color for the information displayed in the top separator (default: "White")
- `TopSeparatorBackgroundColor`: Background color of the top separator (default: "Black")

Example configuration:
```json
{
  "Display": {
    "TopSeparatorForegroundColor": "Cyan",
    "TopSeparatorBackgroundColor": "DarkGray"
  }
}
```

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

