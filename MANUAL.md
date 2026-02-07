# TWF Manual

## Table of Contents
1. [Basic Navigation](#basic-navigation)
2. [File Operations](#file-operations)
3. [Search and Filter](#search-and-filter)
4. [Custom Functions](#custom-functions)
5. [Advanced Features](#advanced-features)

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
- **Ctrl+B** - Open Tab Selector Dialog (filter, jump to, or close tabs)

**Tab Bar Behavior**:
- **Brackets**: The active tab is always wrapped in square brackets (e.g., `[1:Windows *|Backup ]`).
- **Scrolling**: If you have more tabs than can fit on your screen, the tab bar will automatically scroll to keep the active tab visible. Indicators (`<` and `>`) show if there are more tabs off-screen.
- **Custom Colors**: The active and inactive tabs can be assigned distinct colors in the configuration.

**Tab Selector Dialog**:
- Provides a list of all open tabs with expanded multi-line path previews (up to 4 lines each) for the left and right panes.
- Supports incremental search (including Migemo).
- Press **Enter** to jump to the selected tab.
- Press **Delete** to close the highlighted tab directly from the list.
- Press **Ctrl+K** to clear the search filter.

### Background Operations
- **Ctrl+J** - Open Job Manager (monitor and cancel background tasks).
    - **Detail View**: Select a job in the list to see detailed progress statistics (Count: X/Y, Overall %, File %) and the full current filename.
- **Ctrl+L** - Toggle Task Pane (expand/collapse log area).
- **Ctrl+Up/Down** - Resize expanded Task Pane.
- **Alt+Up/Down** - Scroll Task Pane log history.

Operations like Copy, Move, and Delete run in the background. Tabs with active jobs show a `~` indicator.
Logs are automatically buffered and saved to `%APPDATA%\TWF\twf_tasks.log`. You can also manually save the current view with the command bound to `SaveTaskLog` (if configured).

## File Operations

### File Marking
- **Space** - Toggle mark and move cursor down
- **Shift+Space** - Toggle mark and move cursor up
- **Ctrl+Space** - Mark range from previous mark to cursor
- **End** - Clear all marks

### File Operations
- **C** - Copy marked files
- **M** - Move marked files
- **D** - Delete marked files
- **K** - Create directory
- **Create and Edit New File**: Create and edit a new 0-byte file
- **Simple Rename**: Rename the file under the cursor
- **Pattern Rename**: Perform bulk rename using regex/patterns
- **File Comparison**: Compare files between panes by size, date, and name
- **File Split/Join**: Split large files into smaller parts or join parts back together
- **Move to Registered Folder**: Move files to a pre-defined "Registered Folder"
- **Archive Extraction**: Extract selected archive contents
- **Execute File**: Run the file under the cursor
- **Execute File with Editor**: Open the file using the configured text editor
- **System Association Execution**: Open file using system shell association

### Display Modes
- **1-8** - Switch display modes (1-8 columns)

## Search and Filter

### File Filtering
- **:** (colon) - File mask filter dialog
- **@** - Wildcard marking dialog

#### Traditional Wildcards (Case-Insensitive)
- `*.txt` - Matches all .txt files
- `?.log` - Matches single-character name with .log extension
- `test*` - Matches files starting with "test"

#### Regular Expressions
- `/pattern/` - Case-sensitive regex pattern (e.g., `/test/` matches only "test", not "Test")
- `/pattern/i` - Case-insensitive regex pattern (e.g., `/test/i` matches both "test" and "Test")
- `/(jpg|jpeg|png|gif)$/i` - Matches image files using alternation

#### Combined Patterns
- `*.json :*.txt` - Include JSON files, exclude TXT files
- `/.*\.json$/ :/.*\.txt$/` - Same functionality using regex patterns

#### Multi-Keyword Search
- **AND Logic**: Type multiple keywords separated by spaces to find items that match all keywords (order-independent)
- Example: `src config` will find `src/app/config.json` and `infrastructure/src/config_helper.cs`

#### Escaping Spaces
- **Literal Space Search**: Use backslash to search for literal spaces in filenames
- Example: `my\ folder docs` will find files containing "my folder" AND "docs"

#### Environment Variable Support
- **Direct Input**: Type environment variables directly in search fields
- Examples: `%TEMP%\log` or `$HOME/docs`

The file mask is displayed in the paths label, showing the current filter applied to each pane.

### Search
- **F** - Enter incremental search mode
    - **Up/Down** - Jump to previous/next matching file
    - **Ctrl+P/Ctrl+N** - Navigate search history (Previous/Next)
    - **Enter** - Exit search mode and save pattern to history
- **Ctrl+K** - Clear search or filter input (works in File Pane, Text Viewer, and List Dialogs)
- **S** - Cycle sort mode (Name, Ext, Size, Date, Unsorted)
- **Shift+S** - Open Sort Selection Dialog

#### Hybrid Binary Search (in Hex Mode)
The Binary Viewer supports searching three domains at once:
- **Address**: Search for offsets (e.g., `00001A`).
- **Hex**: Search for byte sequences (e.g., `EB FE`).
- **ASCII**: Search for text strings (e.g., `MZ`).

Matches that span across lines wrap naturally, and all visible matches are highlighted.

## Custom Functions

### Macro Reference
- `$P` - Active pane path
- `$O` - Other pane path
- `$L` - Left pane path
- `$R` - Right pane path
- `$F` - Current filename
- `$W` - Filename without extension
- `$E` - File extension
- `$M` - Marked files (spaces separated, quoted)
- `$*` - Active pane file mask
- `$I"Prompt"` - Input dialog
- `$~` - Home directory
- `$V"VAR"` - Environment variable value
- `$V"twf"` - TWF application directory
- `$#XX` - ASCII character (hex code)
- `$\"` - Literal double quote

### Key Bindings
- Custom functions can be bound to keys in `keybindings.json`
- **Shift+F** - Show custom functions dialog

## Archive Operations
- **Enter** (on archive) - Open archive as a virtual folder for hierarchical browsing.
    - Navigate into internal directories with **Enter**.
    - Move up one level or exit the archive with **Backspace**.
- **P** - Compress marked files.
- **Shift+Enter** (on archive) - Extract archive to the opposite pane.

## File Viewing
- **V** - View file as text.
    - **Auto-Detection**: Automatically detects file encoding using a sophisticated multi-tier system:
        - Checks for standard Byte Order Marks (BOM) first
        - Performs strict UTF-8 validation using a state-machine check
        - Applies Japanese encoding heuristics for Shift-JIS and EUC-JP based on byte pattern frequency
        - Falls back to the first encoding in the priority list if heuristics are inconclusive
    - **Encoding Cycling**: Manually cycle through encodings when auto-detection is incorrect
    - **Configurable Encoding Priority**: Set the order of encodings tried during auto-detection in configuration
    - **Line Numbers**: Option to display line numbers in the text viewer
    - **F4 (or /)** - Enter incremental search mode
        - **Up/Down** - Jump to previous/next match
        - **Ctrl+P/Ctrl+N** - Navigate search history
        - **Enter** - Exit search mode
    - **Separate Key Bindings**: Text viewer has its own customizable key bindings system
    - **Hex Mode Toggle**: Switch between text and hexadecimal view modes
- **F8** - View file as hex (binary view)
    - **Hybrid Search Capability**: Advanced search across multiple domains simultaneously:
        - **Address Search**: Find specific file offsets (e.g., searching "1A0" finds the row at `000001A0`)
        - **Hex Byte Search**: Find sequences of hex bytes with support for spaced or continuous input (e.g., `41 42` or `4142`)
        - **ASCII Search**: Automatically search the ASCII representation of file bytes when query is not a valid hex string
    - **Cross-Line Match Highlighting**: Matches that span across lines wrap naturally
    - **Multi-Domain Highlighting**: All visible matches are highlighted across Address, Hex, and ASCII domains
- **H** - Show file info (for directories, triggers background size calculation reported to Task Pane)

## Advanced Features

### Registered Folders
- **I/G** - Show registered folders dialog (with filter support)
- **Shift+M** - Move to registered folder
- **Shift+B** - Register current directory

### High-Performance Navigation
- **Directory Caching**: Frequently visited folders are cached for instant access.
- **Cursor Memory**: TWF remembers your last cursor position and scroll offset for every visited directory.
- **Async Loading**: Directory listing and network share verification are non-blocking. A spinner in the status bar indicates background loading.

### Migemo Search
- Japanese incremental search using romaji input (e.g., 'nihon' matches '日本')
- Enabled/disabled in configuration

### Status Bar Information
The status bar shows:
- Directory and file counts
- Drive usage information
- Marked file statistics

### Top Separator Information
The top separator shows:
- Volume names or network share names
- Marked file statistics

### Configuration Reload
- **Shift+Z** - Reload configuration without restart
- Updates log level, colors, and other settings dynamically

### Version Information
- **Ctrl+Shift+V** - Show version and environment information in the task log area.

### Sort Options
- **S** - Cycle through sort modes
- Name, extension, size, date, unsorted
- Case-insensitive name sorting

### Display Features
- **8 Display Modes**: Switch between 1-8 column layouts for different viewing preferences
- **Smart Refresh**: Periodically updates file size/date for visible files without full reload
- **Configurable Refresh Interval**: Set how often the file list auto-refreshes
- **Work-in-Progress Indicators**: Special colors for files and directories currently being processed in background jobs
- **Customizable Colors**: Configure colors for buttons, dialogs, inputs, and help text
- **Tab Customization**: Customize colors for active/inactive tabs and the tab bar
- **Tab Scrolling**: Automatic scrolling when too many tabs for screen with `<` and `>` indicators

### Custom Functions with Shell Support
Custom functions can specify which shell to use:
- **Shell configuration** in `config.json` for default shells per OS
- **Per-function shell** specification in `custom_functions.json`
- Cross-platform shell support for Windows, Linux, and macOS