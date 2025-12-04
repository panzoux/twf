# TRACEABILITY DB

## COVERAGE ANALYSIS

Total requirements: 136
Coverage: 0

## TRACEABILITY

## DATA

### ACCEPTANCE CRITERIA (136 total)
- 1.1: WHEN the user presses Enter on a directory entry THEN the system SHALL change the active pane to display the contents of that directory (not covered)
- 1.2: WHEN the user presses Backspace THEN the system SHALL navigate to the parent directory of the current path (not covered)
- 1.3: WHEN the user presses Tab THEN the system SHALL switch focus between the left and right file panes (not covered)
- 1.4: WHEN the user presses arrow keys THEN the system SHALL move the cursor up or down within the file list (not covered)
- 1.5: WHEN the user presses Ctrl+PageUp THEN the system SHALL move the cursor to the first entry (not covered)
- 1.6: WHEN the user presses Ctrl+PageDown THEN the system SHALL move the cursor to the last entry (not covered)
- 1.7: WHEN the user presses Home THEN the system SHALL jump to the root directory of the current drive (not covered)
- 2.1: WHEN the user presses number keys 1-8 THEN the system SHALL switch to the corresponding display mode (1-8 columns) (not covered)
- 2.2: WHEN detail view is active THEN the system SHALL display file name, size, timestamp, and attributes for each entry (not covered)
- 2.3: WHEN an entry is a directory THEN the system SHALL display "<DIR>" in the size column (not covered)
- 2.4: WHEN thumbnail view is active THEN the system SHALL display image file previews (not covered)
- 2.5: WHEN icon view is active THEN the system SHALL display file type icons (not covered)
- 2.6: WHEN the display mode changes THEN the system SHALL preserve the cursor position and scroll offset (not covered)
- 3.1: WHEN the user presses Space on an entry THEN the system SHALL toggle the mark on that entry and move the cursor down (not covered)
- 3.2: WHEN the user presses Shift+Space THEN the system SHALL toggle the mark and move the cursor up (not covered)
- 3.3: WHEN the user presses Ctrl+Space THEN the system SHALL mark all entries from the previous mark to the cursor position (not covered)
- 3.4: WHEN the user presses Home or backtick THEN the system SHALL invert all marks in the current pane (not covered)
- 3.5: WHEN marked files exist THEN the system SHALL display them with a visual indicator (not covered)
- 4.1: WHEN the user presses @ key THEN the system SHALL display a wildcard mark input dialog (not covered)
- 4.2: WHEN a wildcard pattern is entered THEN the system SHALL mark all files matching the pattern (not covered)
- 4.3: WHEN the pattern starts with a colon THEN the system SHALL treat it as an exclusion pattern (not covered)
- 4.4: WHEN multiple patterns are specified THEN the system SHALL apply them in order with later patterns taking precedence (not covered)
- 4.5: WHEN the pattern uses regex syntax (m/) THEN the system SHALL apply regular expression matching (not covered)
- 5.1: WHEN the user presses C key with marked files THEN the system SHALL copy all marked files to the opposite pane's directory (not covered)
- 5.2: WHEN no files are marked THEN the system SHALL copy the file at the cursor position (not covered)
- 5.3: WHEN a file with the same name exists at the destination THEN the system SHALL display a confirmation dialog (not covered)
- 5.4: WHEN copying directories THEN the system SHALL recursively copy all contents (not covered)
- 5.5: WHEN the copy operation completes THEN the system SHALL refresh both panes and display a status message (not covered)
- 6.1: WHEN the user presses M key with marked files THEN the system SHALL move all marked files to the opposite pane's directory (not covered)
- 6.2: WHEN no files are marked THEN the system SHALL move the file at the cursor position (not covered)
- 6.3: WHEN a file with the same name exists at the destination THEN the system SHALL display a confirmation dialog (not covered)
- 6.4: WHEN moving directories THEN the system SHALL move the entire directory tree (not covered)
- 6.5: WHEN the move operation completes THEN the system SHALL refresh both panes and display a status message (not covered)
- 7.1: WHEN the user presses D key with marked files THEN the system SHALL display a confirmation dialog listing all marked files (not covered)
- 7.2: WHEN no files are marked THEN the system SHALL confirm deletion of the file at the cursor position (not covered)
- 7.3: WHEN the user confirms deletion THEN the system SHALL delete all selected entries (not covered)
- 7.4: WHEN deleting directories THEN the system SHALL recursively delete all contents (not covered)
- 7.5: WHEN the user holds Shift during confirmation THEN the system SHALL skip individual confirmations for marked files (not covered)
- 8.1: WHEN the user presses J key THEN the system SHALL display an input field for entering the directory name (not covered)
- 8.2: WHEN a directory name is entered and confirmed THEN the system SHALL create the directory in the active pane's current path (not covered)
- 8.3: WHEN the directory is created successfully THEN the system SHALL position the cursor on the newly created directory (not covered)
- 8.4: WHEN the user presses Escape during name entry THEN the system SHALL cancel the operation (not covered)
- 8.5: WHEN the directory creation fails THEN the system SHALL display an error message (not covered)
- 9.1: WHEN the user presses S key THEN the system SHALL cycle through sort modes (name, extension, size, date, unsorted) (not covered)
- 9.2: WHEN sorting by name THEN the system SHALL sort alphabetically with directories first (not covered)
- 9.3: WHEN sorting by extension THEN the system SHALL group files by extension (not covered)
- 9.4: WHEN sorting by size THEN the system SHALL order files from smallest to largest (not covered)
- 9.5: WHEN sorting by date THEN the system SHALL order files from oldest to newest (not covered)
- 9.6: WHEN the sort mode changes THEN the system SHALL display the current sort mode in the status area (not covered)
- 10.1: WHEN the user presses colon key THEN the system SHALL display a file mask input dialog (not covered)
- 10.2: WHEN a file mask is entered THEN the system SHALL display only files matching the mask (not covered)
- 10.3: WHEN multiple masks are specified with spaces THEN the system SHALL match files against any of the masks (not covered)
- 10.4: WHEN a mask starts with colon THEN the system SHALL treat it as an exclusion pattern (not covered)
- 10.5: WHEN the mask is changed THEN the system SHALL immediately update the file list display (not covered)
- 11.1: WHEN the user presses F key THEN the system SHALL enter incremental search mode (not covered)
- 11.2: WHEN characters are typed in search mode THEN the system SHALL move the cursor to the first matching file (not covered)
- 11.3: WHEN Space is pressed in search mode THEN the system SHALL toggle mark on the current file and find the next match (not covered)
- 11.4: WHEN arrow keys are pressed in search mode THEN the system SHALL move to the next or previous match (not covered)
- 11.5: WHEN Escape or Enter is pressed THEN the system SHALL exit search mode (not covered)
- 12.1: WHEN the user presses Enter on an archive file THEN the system SHALL display the archive contents as a virtual folder (not covered)
- 12.2: WHEN browsing a virtual folder THEN the system SHALL display files with their paths within the archive (not covered)
- 12.3: WHEN the user presses Backspace in a virtual folder THEN the system SHALL return to the parent directory (not covered)
- 12.4: WHEN the user presses Shift+Enter on an archive THEN the system SHALL extract the archive to the current directory (not covered)
- 12.5: WHEN extraction completes THEN the system SHALL display a status message and refresh the pane (not covered)
- 13.1: WHEN the user presses P key with marked files THEN the system SHALL display an archive creation dialog (not covered)
- 13.2: WHEN an archive format is selected THEN the system SHALL compress the marked files into an archive (not covered)
- 13.3: WHEN no files are marked THEN the system SHALL compress the file at the cursor position (not covered)
- 13.4: WHEN the archive is created THEN the system SHALL place it in the opposite pane's directory (not covered)
- 13.5: WHEN compression completes THEN the system SHALL display the compression ratio and refresh the panes (not covered)
- 14.1: WHEN the user presses Enter on a text file THEN the system SHALL open the built-in text viewer (not covered)
- 14.2: WHEN the text viewer is open THEN the system SHALL display the file contents with line numbers (not covered)
- 14.3: WHEN the user presses Shift+E in the viewer THEN the system SHALL cycle through encodings (SJIS, EUC, JIS, UTF-8, UTF-16) (not covered)
- 14.4: WHEN the user presses F4 in the viewer THEN the system SHALL enter incremental search mode (not covered)
- 14.5: WHEN the user presses Escape or Enter in the viewer THEN the system SHALL close the viewer and return to the file list (not covered)
- 15.1: WHEN the user presses Enter on an image file THEN the system SHALL open the built-in image viewer (not covered)
- 15.2: WHEN the image viewer is open THEN the system SHALL display the image with fit-to-screen mode (not covered)
- 15.3: WHEN the user presses Home THEN the system SHALL switch to original size mode (not covered)
- 15.4: WHEN the user presses End THEN the system SHALL switch to fit-to-window mode (not covered)
- 15.5: WHEN the user presses Q or K keys THEN the system SHALL rotate the image 90 degrees (not covered)
- 15.6: WHEN the user presses G or U keys THEN the system SHALL flip the image horizontally or vertically (not covered)
- 15.7: WHEN the user presses arrow keys THEN the system SHALL scroll the image (not covered)
- 15.8: WHEN the user presses Escape THEN the system SHALL close the viewer and return to the file list (not covered)
- 16.1: WHEN the user presses I key THEN the system SHALL display a registered folder selection dialog (not covered)
- 16.2: WHEN a registered folder is selected THEN the system SHALL change the active pane to that directory (not covered)
- 16.3: WHEN the user presses Shift+B THEN the system SHALL register the current directory (not covered)
- 16.4: WHEN the user presses Shift+M THEN the system SHALL move marked files to a selected registered folder (not covered)
- 16.5: WHEN registered folders are displayed THEN the system SHALL show them with descriptive names (not covered)
- 17.1: WHEN the user presses Enter on an executable file THEN the system SHALL execute the file (not covered)
- 17.2: WHEN the user presses Enter on a file with an extension association THEN the system SHALL open the file with the associated program (not covered)
- 17.3: WHEN the user presses Shift+Enter THEN the system SHALL open the file with an editor (not covered)
- 17.4: WHEN the user presses Ctrl+Enter THEN the system SHALL open the file with Explorer's associated program (not covered)
- 17.5: WHEN execution fails THEN the system SHALL display an error message (not covered)
- 18.1: WHEN the user presses Shift+R with marked files THEN the system SHALL display a rename dialog (not covered)
- 18.2: WHEN a search pattern and replacement are specified THEN the system SHALL preview the new names (not covered)
- 18.3: WHEN the user confirms the rename THEN the system SHALL rename all marked files according to the pattern (not covered)
- 18.4: WHEN regex patterns are used (s/ or tr/) THEN the system SHALL apply regular expression transformations (not covered)
- 18.5: WHEN renaming completes THEN the system SHALL display the number of files renamed (not covered)
- 19.1: WHEN the user presses W key THEN the system SHALL display a file comparison dialog (not covered)
- 19.2: WHEN comparison criteria are selected THEN the system SHALL mark files matching the criteria (not covered)
- 19.3: WHEN comparing by size THEN the system SHALL mark files with identical sizes (not covered)
- 19.4: WHEN comparing by timestamp THEN the system SHALL mark files with matching timestamps within a tolerance (not covered)
- 19.5: WHEN comparing by name THEN the system SHALL mark files with identical names (not covered)
- 20.1: WHEN the user presses Shift+W on a file THEN the system SHALL display a file split dialog (not covered)
- 20.2: WHEN a split size is specified THEN the system SHALL split the file into multiple parts (not covered)
- 20.3: WHEN the user presses Shift+W on split file parts THEN the system SHALL display a join dialog (not covered)
- 20.4: WHEN join is confirmed THEN the system SHALL combine the parts into the original file (not covered)
- 20.5: WHEN split or join completes THEN the system SHALL display a status message (not covered)
- 21.1: WHEN a custom key binding file (AFXW.KEY) exists THEN the system SHALL load the custom bindings at startup (not covered)
- 21.2: WHEN a custom binding is defined for a function THEN the system SHALL override the default key binding for that function (not covered)
- 21.3: WHEN a binding maps one key to another key THEN the system SHALL execute the target key's function (not covered)
- 21.4: WHEN a binding maps a key to a command string THEN the system SHALL execute the specified command (not covered)
- 21.5: WHEN key bindings are defined with modifiers (SHIFT, CTRL, ALT) THEN the system SHALL recognize the modifier combinations (not covered)
- 21.6: WHEN different key bindings are defined for different modes (Normal, Image Viewer, Text Viewer) THEN the system SHALL apply the appropriate bindings for each mode (not covered)
- 21.7: WHEN a key binding uses virtual key codes THEN the system SHALL map the codes to the corresponding keys (not covered)
- 21.8: WHEN key bindings are invalid or malformed THEN the system SHALL log an error and use default bindings for those keys (not covered)
- 21.9: WHEN no custom key binding file exists THEN the system SHALL use the built-in default key bindings (not covered)
- 22.1: WHEN the user presses Y key THEN the system SHALL open the configuration program (not covered)
- 22.2: WHEN configuration changes are saved THEN the system SHALL apply them immediately or on next startup (not covered)
- 22.3: WHEN font settings are changed THEN the system SHALL update the display with the new font (not covered)
- 22.4: WHEN color settings are changed THEN the system SHALL update the display with the new colors (not covered)
- 22.5: WHEN the configuration file is invalid THEN the system SHALL use default settings (not covered)
- 23.1: WHEN a file operation begins THEN the system SHALL display a progress indicator (not covered)
- 23.2: WHEN copying or moving multiple files THEN the system SHALL show the current file name and progress percentage (not covered)
- 23.3: WHEN the user presses Escape during an operation THEN the system SHALL cancel the operation (not covered)
- 23.4: WHEN an operation completes THEN the system SHALL display a summary message (not covered)
- 23.5: WHEN an operation fails THEN the system SHALL display the error and allow continuation or cancellation (not covered)
- 24.1: WHEN the user presses @ key (or configured context menu key) THEN the system SHALL display a context menu (not covered)
- 24.2: WHEN a menu item is selected THEN the system SHALL execute the corresponding operation (not covered)
- 24.3: WHEN the menu is displayed THEN the system SHALL show operations applicable to the current selection (not covered)
- 24.4: WHEN marked files exist THEN the system SHALL show batch operation options (not covered)
- 24.5: WHEN the user presses Escape THEN the system SHALL close the menu without executing any operation (not covered)
- 25.1: WHEN the application starts THEN the system SHALL restore the last used directory paths for both panes (not covered)
- 25.2: WHEN the application starts THEN the system SHALL restore the last used file mask (not covered)
- 25.3: WHEN the application starts THEN the system SHALL restore the last used sort mode (not covered)
- 25.4: WHEN the application exits THEN the system SHALL save the current session state (not covered)
- 25.5: WHEN configuration disables state saving THEN the system SHALL start with default paths (not covered)

### IMPORTANT ACCEPTANCE CRITERIA (0 total)

### CORRECTNESS PROPERTIES (0 total)

### IMPLEMENTATION TASKS (85 total)
1. Set up project structure and core infrastructure
2. Implement core data models and enumerations
2.1 Write property test for FileEntry display formatting
2.2 Write property test for FileEntry directory display
3. Implement FileSystemProvider for directory and file access
4. Implement SortEngine for file list sorting
4.1 Write property test for sort by name
5. Implement MarkingEngine for file selection
5.1 Write property test for wildcard marking
5.2 Write property test for mark toggle
5.3 Write property test for mark range
6. Implement SearchEngine with Migemo support
6.1 Write property test for incremental search
7. Implement KeyBindingManager for customizable key mappings
7.1 Write property test for custom key bindings
7.2 Write property test for modifier key bindings
7.3 Write property test for mode-specific bindings
8. Implement configuration models and providers
8.1 Create Configuration and SessionState models
8.2 Implement ConfigurationProvider for settings management
8.3 Write property test for session state restoration
9. Implement ListProvider for generic list data
10. Implement FileOperations for file system operations
10.1 Write property test for copy operation
10.2 Write property test for move operation
10.3 Write property test for delete operation
10.4 Write property test for directory creation
10.5 Write property test for pattern rename
11. Implement ArchiveManager and archive providers
11.1 Write property test for archive as virtual folder
11.2 Write property test for archive extraction
11.3 Write property test for compression
12. Implement ViewerManager and text viewer
12.1 Write property test for text viewer
12.2 Write property test for encoding change
13. Implement image viewer
13.1 Write property test for image viewer
13.2 Write property test for image rotation
14. Implement file split and join operations
14.1 Write property test for file split
14.2 Write property test for file join
15. Implement file comparison functionality
15.1 Write property test for file comparison
16. Implement MainController and UI initialization
17. Implement PaneView UI component
18. Implement navigation key handlers
18.1 Write property test for directory navigation
18.2 Write property test for parent navigation
18.3 Write property test for pane switching
18.4 Write property test for cursor movement
18.5 Write property test for jump to first/last
18.6 Write property test for root navigation
19. Implement display mode switching
19.1 Write property test for display mode switching
19.2 Write property test for cursor preservation
20. Implement marking key handlers
20.1 Write property test for Shift+Space marking
20.2 Write property test for mark inversion
21. Implement wildcard marking dialog
22. Implement file operation handlers
22.1 Write property test for progress indicator
22.2 Write property test for operation cancellation
23. Implement directory creation handler
24. Implement sort mode cycling
25. Implement file mask filtering
25.1 Write property test for file mask filtering
26. Implement incremental search
27. Implement archive browsing
28. Implement compression handler
29. Implement text viewer UI
30. Implement image viewer UI
31. Implement registered folders
31.1 Write property test for registered folder navigation
32. Implement file execution
32.1 Write property test for file execution
33. Implement pattern-based rename
34. Implement file comparison UI
35. Implement file split and join UI handlers in MainController
36. Implement configuration loading
36.1 Write property test for configuration application
37. Implement session state management
38. Implement context menu UI in MainController
38.1 Write property test for context menu
39. Implement configuration program launcher in MainController
40. Final checkpoint - Ensure all tests pass

### IMPLEMENTED PBTS (0 total)