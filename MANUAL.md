# TWF Manual

## Table of Contents
1. [Basic Navigation](#basic-navigation)
2. [File Operations](#file-operations)
3. [Search and Filter](#search-and-filter)
4. [File Viewing](#file-viewing)
5. [Custom Functions](#custom-functions)
6. [Advanced Features](#advanced-features)

## Basic Navigation

### Key Navigation
- **Arrow Keys** - Move cursor up/down, switch panes left/right
- **Tab** - Switch between left and right panes
- **Enter** - Navigate into directory, open file, or execute
- **Backspace** - Navigate to parent directory
- **Home** - Invert marks (or navigate to root with Ctrl)
- **End** - Clear all marks
- **Ctrl+PageUp** - Move cursor to first entry
- **Ctrl+PageDown** - Move cursor to last entry

### Pane Operations
- **Shift+J** - Open Jump to Path dialog (fuzzy search or manual entry)
- **Shift+O** - Swap paths of left and right panes
- **O** - Sync opposite pane to active pane's directory

### Tab Management
- **Ctrl+T** - Open a new tab (clones the current paths)
- **Ctrl+W** - Close the current tab
- **Ctrl+Right** - Switch to the next tab
- **Ctrl+Left** - Switch to the previous tab
- **Ctrl+B** - Open Tab Selector Dialog

**Tab Selector Dialog**:
- Provides a list of all open tabs with expanded **multi-line path previews** (4 lines each) for the left and right panes.
- Press **Enter** to jump to the selected tab.
- Press **Delete** to close the highlighted tab.
- Press **Ctrl+K** to clear the search filter.

### Background Operations
- **Ctrl+J** - Open Job Manager (monitor and cancel background tasks).
- **Ctrl+L** - Toggle Task Pane (expand/collapse log area).
- **Ctrl+Up/Down** - Resize expanded Task Pane.
- **Alt+Up/Down** - Scroll Task Pane log history.

Operations like Copy, Move, and Delete run in the background. Tabs with active jobs show a `~` indicator.
All file operations (including deletions and renames) are logged with **full paths** to `%APPDATA%\TWF\logs\twf_errors.log`.

## File Operations

### File Marking
- **Space** - Toggle mark and move cursor down
- **Shift+Space** - Toggle mark and move cursor up
- **Ctrl+Space** - Mark range from previous mark to cursor

### Basic Operations
- **C** - Copy marked files
- **M** - Move marked files
- **D** - Delete marked files (Warns if directories are not empty)
- **K** - Create directory
- **Shift+R** - Pattern Rename (see below)

### Comparative Rename (Shift+R)
The Pattern Rename dialog allows for complex batch renaming with a synchronized live preview.

**View Modes (Alt+P):**
- **Side-by-Side (Default)**: Shows Original names on the left and Preview on the right.
- **Preview Only**: Full-width view of the resulting names.
- **Original Only**: Full-width view of the current names.

**Filter Modes (Alt+A):**
- **All (Default)**: Shows every file in the folder (Changed files are highlighted).
- **Matches Only**: Shows only files that will be renamed by the current pattern.

**Shortcuts:**
- **Alt+R**: Toggle Regex mode.
- **Alt+S**: Toggle Case Sensitivity.
- **Up/Down**: Scroll the file list while typing in the Find/Replace fields.
- **Left/Right / h/l / Ctrl+F/Ctrl+B**: Scroll the file list horizontally to inspect long names.

## Search and Filter

### File Filtering
- **:** (colon) - File mask filter (e.g., `*.txt` or `/regex/i`)
- **\*** (asterisk) - Wildcard marking dialog

### Search
- **F** - Enter incremental search mode (supports Migemo)
    - **Up/Down** - Jump to previous/next matching file
- **S** - Cycle sort mode (Name, Ext, Size, Date, Unsorted)
- **Shift+S** - Open Sort Selection Dialog

## File Viewing

- **V** - View file as text.
    - **Auto-Detection**: Automatically detects file encoding (supporting UTF-8, UTF-16, Shift-JIS, and EUC-JP).
    - **Horizontal Scrolling**: The viewer automatically clamps horizontal scrolling based on the longest line found in the file. 
    - **Configurable Limit**: The scrolling range is estimated using a `HorizontalScrollMultiplier` (helps with CJK/Tabs).
    - **F4 (or /)** - Enter incremental search mode.
- **F8** - View file as hex (binary view).
    - **Address Column**: Shows offsets in the color configured via `ViewerLineNumberColor`.
- **H** - Show file info (for directories, triggers background size calculation).

## Custom Functions

### Macro Reference
- `$P` / `$O` - Active / Other pane path
- `$F` / `$W` / `$E` - Filename / Base name / Extension
- `$M` - Marked files (quoted, space-separated)
- `$I"Prompt"` - Styled input dialog
- `$~` - Home directory (User Profile)
- `$\"` - Literal double quote
- `$V"VAR"` - Environment variable

## Advanced Features

### Registered Folders
- **I** - Show registered folders dialog.
- **Shift+B** - Register current directory.

### High-Performance Navigation
- **Incremental Refresh**: Panes update every second during long background moves/copies.
- **Directory Caching**: Instant access to previously visited folders.
- **Configurable Limits**: `MaxPathInputLength` and `MaxRenamePreviewResults` for system optimization.