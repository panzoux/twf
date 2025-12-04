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
- **session.json** - Session state (last used paths, sort modes, etc.)
- **AFXW.KEY** - Custom key bindings (not currently implemented)

## Key Bindings

The application has **hardcoded key bindings**. All the following keys should now work properly:

### Navigation
- **Arrow Keys** - Move cursor up/down, switch panes left/right
- **Tab** - Switch between left and right panes
- **Enter** - Navigate into directory, open file, or execute
- **Backspace** - Navigate to parent directory
- **Home** - Invert marks (or navigate to root with Ctrl)
- **Ctrl+PageUp** - Move cursor to first entry
- **Ctrl+PageDown** - Move cursor to last entry

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
- **J** - Create directory

### Search & Filter
- **F** - Enter incremental search mode
- **S** - Cycle sort mode (name, extension, size, date, unsorted)
- **:** (colon) - File mask filter dialog
- **@** - Wildcard marking dialog

### Archive Operations
- **P** - Compress marked files
- **Shift+Enter** (on archive) - Extract archive

### Other Operations
- **I** - Show registered folders
- **Shift+B** - Register current directory
- **Shift+R** - Pattern-based rename
- **W** - File comparison
- **Shift+W** - File split/join
- **Y** - Launch configuration program
- **`** (backtick) - Context menu
- **ESC** - Cancel operation / Exit application

## Recent Fix (2025-12-03)

**Issue Fixed:** Keys were not being captured properly because the ListView controls were consuming key events before they reached the main window handler.

**Solution:** Attached the key handler directly to both ListView controls in addition to the main window, ensuring all key presses are properly processed.

## Custom Key Bindings

**Custom key bindings are now fully functional!** You can customize all key mappings using a JSON configuration file.

### How to Enable Custom Key Bindings

1. Edit your `config.json` file and set:
```json
{
  "KeyBindings": {
    "KeyBindingFile": "keybindings.json",
    "UseCustomBindings": true
  }
}
```

2. Create or edit your key bindings file (default: `keybindings.json`)

3. The application will look for the key bindings file in:
   - `%APPDATA%\TWF\` directory first
   - Current directory if not found in AppData

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
- `ToggleMarkAndMoveDown`, `ToggleMarkAndMoveUp`, `MarkRange`, `MarkAll`
- `DisplayMode1` through `DisplayMode8`
- `HandleCopyOperation`, `HandleMoveOperation`, `HandleDeleteOperation`
- `HandleCreateDirectory`, `ShowDriveChangeDialog`, `CycleSortMode`
- `ShowFileMaskDialog`, `ShowWildcardMarkingDialog`, `HandleContextMenu`
- `HandleCompressionOperation`, `HandleArchiveExtraction`
- `ShowRegisteredFolderDialog`, `RegisterCurrentDirectory`, `MoveToRegisteredFolder`
- `HandlePatternRename`, `HandleFileComparison`, `HandleFileSplitOrJoin`
- `HandleLaunchConfigurationProgram`, `ShowFileInfoForCursor`
- `ViewFileAsText`, `ExitApplication`

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
- Session state saving

Example config.json structure:
```json
{
  "Display": {
    "FontName": "Consolas",
    "FontSize": 12,
    "ForegroundColor": "White",
    "BackgroundColor": "Black",
    "ShowHiddenFiles": true
  },
  "SaveSessionState": true
}
```
