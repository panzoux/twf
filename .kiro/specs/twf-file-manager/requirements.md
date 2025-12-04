# Requirements Document

## Introduction

TWF (Two-pane Window Filer) is a comprehensive keyboard-driven, dual-pane file manager application for Windows, inspired by the classic AFxW file manager. The system provides extensive file management operations through a terminal-based user interface with two independent file list panes, supporting archive handling, image viewing, text viewing, file marking, batch operations, customizable key bindings, and extensive configuration options.

## Glossary

- **TWF**: Two-pane Window Filer - the file manager application
- **File Pane**: A vertical panel displaying a list of files and directories
- **Active Pane**: The file pane that currently has keyboard focus
- **Inactive Pane**: The file pane that does not have keyboard focus
- **Cursor**: The highlighted selection in a file pane
- **Entry**: A file or directory item displayed in a file pane
- **Mark**: A selection flag on files/directories for batch operations
- **Parent Directory**: The directory one level up in the directory hierarchy
- **Detail View**: Display mode showing file size, timestamp, and attributes
- **Status Bar**: The bottom line of the screen displaying messages and prompts
- **Message Area**: The area displaying operation progress and system messages
- **Archive**: A compressed file container (LZH, ZIP, TAR, CAB, RAR, 7Z, etc.)
- **Virtual Folder**: An archive file displayed as if it were a directory
- **File Mask**: A wildcard pattern for filtering displayed files
- **Wildcard Mark**: A pattern-based marking of multiple files
- **Sort Mode**: The ordering method for file list display
- **Registered Folder**: A bookmarked directory path for quick access
- **Extension Association**: A mapping between file extensions and execution commands
- **Thumbnail View**: Display mode showing image previews
- **Icon View**: Display mode showing file type icons
- **Text Viewer**: Built-in viewer for text files with encoding support
- **Image Viewer**: Built-in viewer for image files with zoom and rotation
- **Susie Plugin**: External plugin for image format support
- **Archive DLL**: External library for archive format support

## Requirements

**Note:** All key bindings mentioned in the acceptance criteria below represent the default key assignments. The system SHALL support full customization of all key bindings through a configuration file (see Requirement 21), allowing users to reassign any function to any key combination.

### Requirement 1

**User Story:** As a user, I want to navigate directories using keyboard shortcuts, so that I can quickly browse the file system without using a mouse.

#### Acceptance Criteria

1. WHEN the user presses Enter on a directory entry THEN the system SHALL change the active pane to display the contents of that directory
2. WHEN the user presses Backspace THEN the system SHALL navigate to the parent directory of the current path
3. WHEN the user presses Tab THEN the system SHALL switch focus between the left and right file panes
4. WHEN the user presses arrow keys THEN the system SHALL move the cursor up or down within the file list
5. WHEN the user presses Ctrl+PageUp THEN the system SHALL move the cursor to the first entry
6. WHEN the user presses Ctrl+PageDown THEN the system SHALL move the cursor to the last entry
7. WHEN the user presses Home THEN the system SHALL jump to the root directory of the current drive

### Requirement 2

**User Story:** As a user, I want to view file information in multiple display modes, so that I can see the details I need for my current task.

#### Acceptance Criteria

1. WHEN the user presses number keys 1-8 THEN the system SHALL switch to the corresponding display mode (1-8 columns)
2. WHEN detail view is active THEN the system SHALL display file name, size, timestamp, and attributes for each entry
3. WHEN an entry is a directory THEN the system SHALL display "<DIR>" in the size column
4. WHEN thumbnail view is active THEN the system SHALL display image file previews
5. WHEN icon view is active THEN the system SHALL display file type icons
6. WHEN the display mode changes THEN the system SHALL preserve the cursor position and scroll offset

### Requirement 3

**User Story:** As a user, I want to mark multiple files for batch operations, so that I can perform actions on many files at once.

#### Acceptance Criteria

1. WHEN the user presses Space on an entry THEN the system SHALL toggle the mark on that entry and move the cursor down
2. WHEN the user presses Shift+Space THEN the system SHALL toggle the mark and move the cursor up
3. WHEN the user presses Ctrl+Space THEN the system SHALL mark all entries from the previous mark to the cursor position
4. WHEN the user presses Home or backtick THEN the system SHALL invert all marks in the current pane
5. WHEN marked files exist THEN the system SHALL display them with a visual indicator

### Requirement 4

**User Story:** As a user, I want to use wildcard patterns to mark files, so that I can quickly select files matching specific criteria.

#### Acceptance Criteria

1. WHEN the user presses @ key THEN the system SHALL display a wildcard mark input dialog
2. WHEN a wildcard pattern is entered THEN the system SHALL mark all files matching the pattern
3. WHEN the pattern starts with a colon THEN the system SHALL treat it as an exclusion pattern
4. WHEN multiple patterns are specified THEN the system SHALL apply them in order with later patterns taking precedence
5. WHEN the pattern uses regex syntax (m/) THEN the system SHALL apply regular expression matching

### Requirement 5

**User Story:** As a user, I want to copy files and directories, so that I can duplicate them to different locations.

#### Acceptance Criteria

1. WHEN the user presses C key with marked files THEN the system SHALL copy all marked files to the opposite pane's directory
2. WHEN no files are marked THEN the system SHALL copy the file at the cursor position
3. WHEN a file with the same name exists at the destination THEN the system SHALL display a confirmation dialog
4. WHEN copying directories THEN the system SHALL recursively copy all contents
5. WHEN the copy operation completes THEN the system SHALL refresh both panes and display a status message

### Requirement 6

**User Story:** As a user, I want to move files and directories, so that I can reorganize my file system.

#### Acceptance Criteria

1. WHEN the user presses M key with marked files THEN the system SHALL move all marked files to the opposite pane's directory
2. WHEN no files are marked THEN the system SHALL move the file at the cursor position
3. WHEN a file with the same name exists at the destination THEN the system SHALL display a confirmation dialog
4. WHEN moving directories THEN the system SHALL move the entire directory tree
5. WHEN the move operation completes THEN the system SHALL refresh both panes and display a status message

### Requirement 7

**User Story:** As a user, I want to delete files and directories with confirmation, so that I can safely remove unwanted items.

#### Acceptance Criteria

1. WHEN the user presses D key with marked files THEN the system SHALL display a confirmation dialog listing all marked files
2. WHEN no files are marked THEN the system SHALL confirm deletion of the file at the cursor position
3. WHEN the user confirms deletion THEN the system SHALL delete all selected entries
4. WHEN deleting directories THEN the system SHALL recursively delete all contents
5. WHEN the user holds Shift during confirmation THEN the system SHALL skip individual confirmations for marked files

### Requirement 8

**User Story:** As a user, I want to create new directories, so that I can organize my files into folders.

#### Acceptance Criteria

1. WHEN the user presses J key THEN the system SHALL display an input field for entering the directory name
2. WHEN a directory name is entered and confirmed THEN the system SHALL create the directory in the active pane's current path
3. WHEN the directory is created successfully THEN the system SHALL position the cursor on the newly created directory
4. WHEN the user presses Escape during name entry THEN the system SHALL cancel the operation
5. WHEN the directory creation fails THEN the system SHALL display an error message

### Requirement 9

**User Story:** As a user, I want to sort file lists by different criteria, so that I can find files more easily.

#### Acceptance Criteria

1. WHEN the user presses S key THEN the system SHALL cycle through sort modes (name, extension, size, date, unsorted)
2. WHEN sorting by name THEN the system SHALL sort alphabetically with directories first
3. WHEN sorting by extension THEN the system SHALL group files by extension
4. WHEN sorting by size THEN the system SHALL order files from smallest to largest
5. WHEN sorting by date THEN the system SHALL order files from oldest to newest
6. WHEN the sort mode changes THEN the system SHALL display the current sort mode in the status area

### Requirement 10

**User Story:** As a user, I want to filter displayed files using file masks, so that I can focus on specific file types.

#### Acceptance Criteria

1. WHEN the user presses colon key THEN the system SHALL display a file mask input dialog
2. WHEN a file mask is entered THEN the system SHALL display only files matching the mask
3. WHEN multiple masks are specified with spaces THEN the system SHALL match files against any of the masks
4. WHEN a mask starts with colon THEN the system SHALL treat it as an exclusion pattern
5. WHEN the mask is changed THEN the system SHALL immediately update the file list display

### Requirement 11

**User Story:** As a user, I want to search for files by name incrementally, so that I can quickly locate specific files.

#### Acceptance Criteria

1. WHEN the user presses F key THEN the system SHALL enter incremental search mode
2. WHEN characters are typed in search mode THEN the system SHALL move the cursor to the first matching file
3. WHEN Space is pressed in search mode THEN the system SHALL toggle mark on the current file and find the next match
4. WHEN arrow keys are pressed in search mode THEN the system SHALL move to the next or previous match
5. WHEN Escape or Enter is pressed THEN the system SHALL exit search mode

### Requirement 12

**User Story:** As a user, I want to view archive files as virtual folders, so that I can browse their contents without extracting.

#### Acceptance Criteria

1. WHEN the user presses Enter on an archive file THEN the system SHALL display the archive contents as a virtual folder
2. WHEN browsing a virtual folder THEN the system SHALL display files with their paths within the archive
3. WHEN the user presses Backspace in a virtual folder THEN the system SHALL return to the parent directory
4. WHEN the user presses Shift+Enter on an archive THEN the system SHALL extract the archive to the current directory
5. WHEN extraction completes THEN the system SHALL display a status message and refresh the pane

### Requirement 13

**User Story:** As a user, I want to compress files into archives, so that I can save disk space and bundle files together.

#### Acceptance Criteria

1. WHEN the user presses P key with marked files THEN the system SHALL display an archive creation dialog
2. WHEN an archive format is selected THEN the system SHALL compress the marked files into an archive
3. WHEN no files are marked THEN the system SHALL compress the file at the cursor position
4. WHEN the archive is created THEN the system SHALL place it in the opposite pane's directory
5. WHEN compression completes THEN the system SHALL display the compression ratio and refresh the panes

### Requirement 14

**User Story:** As a user, I want to view text files with proper encoding support, so that I can read file contents without external editors.

#### Acceptance Criteria

1. WHEN the user presses Enter on a text file THEN the system SHALL open the built-in text viewer
2. WHEN the text viewer is open THEN the system SHALL display the file contents with line numbers
3. WHEN the user presses Shift+E in the viewer THEN the system SHALL cycle through encodings (SJIS, EUC, JIS, UTF-8, UTF-16)
4. WHEN the user presses F4 in the viewer THEN the system SHALL enter incremental search mode
5. WHEN the user presses Escape or Enter in the viewer THEN the system SHALL close the viewer and return to the file list

### Requirement 15

**User Story:** As a user, I want to view image files with zoom and rotation, so that I can preview images without external applications.

#### Acceptance Criteria

1. WHEN the user presses Enter on an image file THEN the system SHALL open the built-in image viewer
2. WHEN the image viewer is open THEN the system SHALL display the image with fit-to-screen mode
3. WHEN the user presses Home THEN the system SHALL switch to original size mode
4. WHEN the user presses End THEN the system SHALL switch to fit-to-window mode
5. WHEN the user presses Q or K keys THEN the system SHALL rotate the image 90 degrees
6. WHEN the user presses G or U keys THEN the system SHALL flip the image horizontally or vertically
7. WHEN the user presses arrow keys THEN the system SHALL scroll the image
8. WHEN the user presses Escape THEN the system SHALL close the viewer and return to the file list

### Requirement 16

**User Story:** As a user, I want to use registered folders for quick navigation, so that I can access frequently used directories instantly.

#### Acceptance Criteria

1. WHEN the user presses I key THEN the system SHALL display a registered folder selection dialog
2. WHEN a registered folder is selected THEN the system SHALL change the active pane to that directory
3. WHEN the user presses Shift+B THEN the system SHALL register the current directory
4. WHEN the user presses Shift+M THEN the system SHALL move marked files to a selected registered folder
5. WHEN registered folders are displayed THEN the system SHALL show them with descriptive names

### Requirement 17

**User Story:** As a user, I want to execute files with associated programs, so that I can open files with their default applications.

#### Acceptance Criteria

1. WHEN the user presses Enter on an executable file THEN the system SHALL execute the file
2. WHEN the user presses Enter on a file with an extension association THEN the system SHALL open the file with the associated program
3. WHEN the user presses Shift+Enter THEN the system SHALL open the file with an editor
4. WHEN the user presses Ctrl+Enter THEN the system SHALL open the file with Explorer's associated program
5. WHEN execution fails THEN the system SHALL display an error message

### Requirement 18

**User Story:** As a user, I want to rename files with pattern-based transformations, so that I can batch rename multiple files efficiently.

#### Acceptance Criteria

1. WHEN the user presses Shift+R with marked files THEN the system SHALL display a rename dialog
2. WHEN a search pattern and replacement are specified THEN the system SHALL preview the new names
3. WHEN the user confirms the rename THEN the system SHALL rename all marked files according to the pattern
4. WHEN regex patterns are used (s/ or tr/) THEN the system SHALL apply regular expression transformations
5. WHEN renaming completes THEN the system SHALL display the number of files renamed

### Requirement 19

**User Story:** As a user, I want to compare files by attributes, so that I can identify duplicate or similar files.

#### Acceptance Criteria

1. WHEN the user presses W key THEN the system SHALL display a file comparison dialog
2. WHEN comparison criteria are selected THEN the system SHALL mark files matching the criteria
3. WHEN comparing by size THEN the system SHALL mark files with identical sizes
4. WHEN comparing by timestamp THEN the system SHALL mark files with matching timestamps within a tolerance
5. WHEN comparing by name THEN the system SHALL mark files with identical names

### Requirement 20

**User Story:** As a user, I want to split and join files, so that I can handle large files across multiple storage media.

#### Acceptance Criteria

1. WHEN the user presses Shift+W on a file THEN the system SHALL display a file split dialog
2. WHEN a split size is specified THEN the system SHALL split the file into multiple parts
3. WHEN the user presses Shift+W on split file parts THEN the system SHALL display a join dialog
4. WHEN join is confirmed THEN the system SHALL combine the parts into the original file
5. WHEN split or join completes THEN the system SHALL display a status message

### Requirement 21

**User Story:** As a user, I want to customize all key bindings, so that I can adapt the interface to my preferences and workflow.

#### Acceptance Criteria

1. WHEN a custom key binding file (AFXW.KEY) exists THEN the system SHALL load the custom bindings at startup
2. WHEN a custom binding is defined for a function THEN the system SHALL override the default key binding for that function
3. WHEN a binding maps one key to another key THEN the system SHALL execute the target key's function
4. WHEN a binding maps a key to a command string THEN the system SHALL execute the specified command
5. WHEN key bindings are defined with modifiers (SHIFT, CTRL, ALT) THEN the system SHALL recognize the modifier combinations
6. WHEN different key bindings are defined for different modes (Normal, Image Viewer, Text Viewer) THEN the system SHALL apply the appropriate bindings for each mode
7. WHEN a key binding uses virtual key codes THEN the system SHALL map the codes to the corresponding keys
8. WHEN key bindings are invalid or malformed THEN the system SHALL log an error and use default bindings for those keys
9. WHEN no custom key binding file exists THEN the system SHALL use the built-in default key bindings

### Requirement 22

**User Story:** As a user, I want to configure display settings, so that I can customize the appearance to my preferences.

#### Acceptance Criteria

1. WHEN the user presses Y key THEN the system SHALL open the configuration program
2. WHEN configuration changes are saved THEN the system SHALL apply them immediately or on next startup
3. WHEN font settings are changed THEN the system SHALL update the display with the new font
4. WHEN color settings are changed THEN the system SHALL update the display with the new colors
5. WHEN the configuration file is invalid THEN the system SHALL use default settings

### Requirement 23

**User Story:** As a user, I want to see file operation progress, so that I understand what the system is doing during long operations.

#### Acceptance Criteria

1. WHEN a file operation begins THEN the system SHALL display a progress indicator
2. WHEN copying or moving multiple files THEN the system SHALL show the current file name and progress percentage
3. WHEN the user presses Escape during an operation THEN the system SHALL cancel the operation
4. WHEN an operation completes THEN the system SHALL display a summary message
5. WHEN an operation fails THEN the system SHALL display the error and allow continuation or cancellation

### Requirement 24

**User Story:** As a user, I want to use context menus, so that I can access file operations through a menu interface.

#### Acceptance Criteria

1. WHEN the user presses @ key (or configured context menu key) THEN the system SHALL display a context menu
2. WHEN a menu item is selected THEN the system SHALL execute the corresponding operation
3. WHEN the menu is displayed THEN the system SHALL show operations applicable to the current selection
4. WHEN marked files exist THEN the system SHALL show batch operation options
5. WHEN the user presses Escape THEN the system SHALL close the menu without executing any operation

### Requirement 25

**User Story:** As a user, I want the application to remember my last session state, so that I can resume where I left off.

#### Acceptance Criteria

1. WHEN the application starts THEN the system SHALL restore the last used directory paths for both panes
2. WHEN the application starts THEN the system SHALL restore the last used file mask
3. WHEN the application starts THEN the system SHALL restore the last used sort mode
4. WHEN the application exits THEN the system SHALL save the current session state
5. WHEN configuration disables state saving THEN the system SHALL start with default paths
