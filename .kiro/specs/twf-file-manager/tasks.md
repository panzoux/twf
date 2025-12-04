# Implementation Plan

- [x] 1. Set up project structure and core infrastructure
  - Create .NET console application project with Terminal.Gui dependency
  - Set up project folders: Models, Controllers, Services, Providers, UI
  - Configure logging infrastructure
  - _Requirements: All_

- [x] 2. Implement core data models and enumerations
  - Create FileEntry model with display formatting methods
  - Create PaneState model for managing pane state
  - Define all enumerations (UiMode, DisplayMode, SortMode, ArchiveFormat, ViewMode, ActionType)
  - Create OperationResult and ProgressEventArgs models
  - _Requirements: 1, 2, 3, 9, 10, 12, 13, 14, 15_

- [x] 2.1 Write property test for FileEntry display formatting
  - **Property 9: Detail view shows required fields**
  - **Validates: Requirements 2.2**

- [x] 2.2 Write property test for FileEntry directory display
  - **Property 10: Directories show <DIR> in size column**
  - **Validates: Requirements 2.3**

- [x] 3. Implement FileSystemProvider for directory and file access
  - Create FileSystemProvider class with directory listing
  - Implement file metadata retrieval
  - Add error handling for access denied and file not found
  - _Requirements: 1, 2_

- [x] 4. Implement SortEngine for file list sorting
  - Create SortEngine with all sort modes (name, extension, size, date, unsorted)
  - Implement ascending and descending sort
  - Ensure directories appear before files in name sort
  - _Requirements: 9_

- [x] 4.1 Write property test for sort by name
  - **Property 22: Sort mode changes file order**
  - **Validates: Requirements 9.1**

- [x] 5. Implement MarkingEngine for file selection
  - Create MarkingEngine with toggle, range, and invert operations
  - Implement wildcard pattern matching
  - Implement regex pattern matching
  - _Requirements: 3, 4_

- [x] 5.1 Write property test for wildcard marking
  - **Property 16: Wildcard pattern marks matching files**
  - **Validates: Requirements 4.2**

- [x] 5.2 Write property test for mark toggle
  - **Property 12: Space toggles mark and moves cursor down**
  - **Validates: Requirements 3.1**

- [x] 5.3 Write property test for mark range
  - **Property 14: Ctrl+Space marks range**
  - **Validates: Requirements 3.3**

- [x] 6. Implement SearchEngine with Migemo support
  - Create SearchEngine with incremental search
  - Create IMigemoProvider interface
  - Implement MigemoProvider with DLL loading and dictionary check
  - Add fallback to standard search when Migemo unavailable
  - _Requirements: 11_

- [x] 6.1 Write property test for incremental search
  - **Property 24: Incremental search finds matching files**
  - **Validates: Requirements 11.2**

- [x] 7. Implement KeyBindingManager for customizable key mappings
  - Create KeyBindingManager with mode-specific bindings
  - Create ActionBinding model
  - Implement key binding file parser (AFXW.KEY format)
  - Support key redirects, function bindings, and command bindings
  - Handle modifier keys (SHIFT, CTRL, ALT)
  - _Requirements: 21_

- [x] 7.1 Write property test for custom key bindings





  - **Property 38: Custom key binding overrides default**
  - **Validates: Requirements 21.2**

- [x] 7.2 Write property test for modifier key bindings





  - **Property 39: Key binding with modifiers recognized**
  - **Validates: Requirements 21.5**

- [x] 7.3 Write property test for mode-specific bindings





  - **Property 40: Mode-specific bindings apply correctly**
  - **Validates: Requirements 21.6**

- [x] 8. Implement configuration models and providers






- [x] 8.1 Create Configuration and SessionState models


  - Create Configuration class with DisplaySettings, KeyBindings, RegisteredFolders, ExtensionAssociations, ArchiveSettings, ViewerSettings
  - Create SessionState class for persisting pane states
  - Create RegisteredFolder, DriveInfo, and MenuItem models
  - _Requirements: 22, 25, 16, 24_

- [x] 8.2 Implement ConfigurationProvider for settings management


  - Implement configuration file loading and saving (JSON format)
  - Implement session state persistence
  - Add validation and default value handling
  - _Requirements: 22, 25_

- [x] 8.3 Write property test for session state restoration


  - **Property 45: Session state restoration preserves paths**
  - **Property 46: Session state restoration preserves settings**
  - **Validates: Requirements 25.1, 25.2, 25.3**

- [x] 9. Implement ListProvider for generic list data





  - Create ListProvider class
  - Implement GetDriveList for drive enumeration
  - Implement GetJumpList for registered folders
  - Implement GetHistoryList for directory/search/command history
  - Implement GetContextMenu for context menu generation
  - _Requirements: 16, 24_

- [x] 10. Implement FileOperations for file system operations





  - Create FileOperations class with async methods
  - Implement CopyAsync with progress reporting
  - Implement MoveAsync with progress reporting
  - Implement DeleteAsync with confirmation
  - Implement CreateDirectory
  - Implement RenameAsync with pattern support
  - Add cancellation token support
  - _Requirements: 5, 6, 7, 8, 18_

- [x] 10.1 Write property test for copy operation


  - **Property 17: Copy operation transfers files**
  - **Property 18: Copy preserves file attributes**
  - **Validates: Requirements 5.1, 5.2**

- [x] 10.2 Write property test for move operation


  - **Property 19: Move operation relocates files**
  - **Validates: Requirements 6.1, 6.2**

- [x] 10.3 Write property test for delete operation


  - **Property 20: Delete operation removes files**
  - **Validates: Requirements 7.3**

- [x] 10.4 Write property test for directory creation


  - **Property 21: Directory creation adds new folder**
  - **Validates: Requirements 8.2**

- [x] 10.5 Write property test for pattern rename


  - **Property 34: Pattern rename transforms filenames**
  - **Validates: Requirements 18.3**

- [x] 11. Implement ArchiveManager and archive providers





  - Create IArchiveProvider interface
  - Create ArchiveManager with provider registration
  - Implement archive detection by extension
  - Implement ListArchiveContents for virtual folders
  - Implement ExtractAsync and CompressAsync
  - Add support for ZIP format using System.IO.Compression
  - _Requirements: 12, 13_

- [x] 11.1 Write property test for archive as virtual folder


  - **Property 25: Archive displays as virtual folder**
  - **Validates: Requirements 12.1**

- [x] 11.2 Write property test for archive extraction


  - **Property 26: Archive extraction creates files**
  - **Validates: Requirements 12.4**

- [x] 11.3 Write property test for compression


  - **Property 27: Compression creates archive**
  - **Validates: Requirements 13.2**

- [x] 12. Implement ViewerManager and text viewer





  - Create ViewerManager class
  - Create TextViewer with encoding support
  - Implement file loading with multiple encodings (UTF-8, UTF-16, ASCII, Latin1)
  - Implement encoding cycling
  - Implement search functionality in text viewer
  - _Requirements: 14_

- [x] 12.1 Write property test for text viewer


  - **Property 28: Text viewer displays file contents**
  - **Validates: Requirements 14.2**

- [x] 12.2 Write property test for encoding change


  - **Property 29: Encoding change updates display**
  - **Validates: Requirements 14.3**

- [x] 13. Implement image viewer





  - Create ImageViewer class
  - Implement image loading with ImageSharp or System.Drawing
  - Implement view modes (original size, fit to window, fit to screen)
  - Implement rotation (90 degree increments)
  - Implement flip (horizontal and vertical)
  - Implement zoom functionality
  - _Requirements: 15_

- [x] 13.1 Write property test for image viewer


  - **Property 30: Image viewer displays image**
  - **Validates: Requirements 15.2**

- [x] 13.2 Write property test for image rotation


  - **Property 31: Image rotation transforms display**
  - **Validates: Requirements 15.5**

- [x] 14. Implement file split and join operations





  - Add SplitAsync method to FileOperations
  - Add JoinAsync method to FileOperations
  - Implement split part file naming convention
  - Add validation for join operation
  - _Requirements: 20_

- [x] 14.1 Write property test for file split


  - **Property 36: File split creates multiple parts**
  - **Validates: Requirements 20.2**

- [x] 14.2 Write property test for file join


  - **Property 37: File join recreates original**
  - **Validates: Requirements 20.4**

- [x] 15. Implement file comparison functionality





  - Add comparison methods to FileOperations
  - Implement comparison by size
  - Implement comparison by timestamp with tolerance
  - Implement comparison by name
  - _Requirements: 19_

- [x] 15.1 Write property test for file comparison


  - **Property 35: File comparison marks matching files**
  - **Validates: Requirements 19.2**

- [x] 16. Implement MainController and UI initialization







  - Create MainController class
  - Initialize Terminal.Gui application
  - Create main window with two panes
  - Create status bar and message area
  - Set up initial pane states
  - Wire up all dependencies
  - _Requirements: All_

- [x] 17. Implement PaneView UI component





  - Create PaneView class for displaying file lists
  - Implement rendering for different display modes
  - Handle visual indicators for marked files
  - Implement cursor highlighting
  - _Requirements: 1, 2, 3_

- [x] 18. Implement navigation key handlers





  - Handle Enter key for directory navigation
  - Handle Backspace for parent directory
  - Handle Tab for pane switching
  - Handle arrow keys for cursor movement
  - Handle Ctrl+PageUp/PageDown for first/last entry
  - Handle Home for root directory navigation
  - _Requirements: 1_

- [x] 18.1 Write property test for directory navigation


  - **Property 1: Directory navigation updates pane contents**
  - **Validates: Requirements 1.1**

- [x] 18.2 Write property test for parent navigation

  - **Property 2: Backspace navigates to parent**
  - **Validates: Requirements 1.2**

- [x] 18.3 Write property test for pane switching

  - **Property 3: Tab toggles pane focus**
  - **Validates: Requirements 1.3**

- [x] 18.4 Write property test for cursor movement

  - **Property 4: Arrow keys move cursor**
  - **Validates: Requirements 1.4**

- [x] 18.5 Write property test for jump to first/last

  - **Property 5: Ctrl+PageUp moves to first entry**
  - **Property 6: Ctrl+PageDown moves to last entry**
  - **Validates: Requirements 1.5, 1.6**

- [x] 18.6 Write property test for root navigation

  - **Property 7: Home navigates to drive root**
  - **Validates: Requirements 1.7**

- [x] 19. Implement display mode switching





  - Handle number keys 1-8 for display mode switching
  - Implement display mode rendering for each mode
  - Preserve cursor position and scroll offset on mode change
  - _Requirements: 2_

- [x] 19.1 Write property test for display mode switching


  - **Property 8: Number keys switch display modes**
  - **Validates: Requirements 2.1**

- [x] 19.2 Write property test for cursor preservation


  - **Property 11: Display mode change preserves cursor position**
  - **Validates: Requirements 2.6**

- [x] 20. Implement marking key handlers





  - Handle Space for mark toggle and cursor down
  - Handle Shift+Space for mark toggle and cursor up
  - Handle Ctrl+Space for range marking
  - Handle Home/backtick for mark inversion
  - Update UI to show marked files visually
  - _Requirements: 3_

- [x] 20.1 Write property test for Shift+Space marking


  - **Property 13: Shift+Space toggles mark and moves cursor up**
  - **Validates: Requirements 3.2**

- [x] 20.2 Write property test for mark inversion


  - **Property 15: Home inverts all marks**
  - **Validates: Requirements 3.4**

- [x] 21. Implement wildcard marking dialog





  - Handle @ key to show wildcard input dialog
  - Parse wildcard patterns with exclusions
  - Apply patterns to mark files
  - Support regex patterns with m/ syntax
  - _Requirements: 4_

- [x] 22. Implement file operation handlers





  - Handle C key for copy operation
  - Handle M key for move operation
  - Handle D key for delete operation with confirmation
  - Show progress dialog during operations
  - Handle Escape for operation cancellation
  - _Requirements: 5, 6, 7, 23_

- [x] 22.1 Write property test for progress indicator

  - **Property 42: Progress indicator shows operation status**
  - **Validates: Requirements 23.2**

- [x] 22.2 Write property test for operation cancellation

  - **Property 43: Operation cancellation stops processing**
  - **Validates: Requirements 23.3**

- [x] 23. Implement directory creation handler





  - Handle J key to show directory name input
  - Create directory in current path
  - Position cursor on new directory
  - Handle Escape for cancellation
  - _Requirements: 8_

- [x] 24. Implement sort mode cycling





  - Handle S key to cycle through sort modes
  - Update pane display with new sort order
  - Show current sort mode in status area
  - _Requirements: 9_

- [x] 25. Implement file mask filtering





  - Handle colon key to show file mask input
  - Parse multiple masks with spaces
  - Support exclusion patterns with colon prefix
  - Apply mask to filter file list
  - _Requirements: 10_

- [x] 25.1 Write property test for file mask filtering


  - **Property 23: File mask filters displayed files**
  - **Validates: Requirements 10.2**

- [x] 26. Implement incremental search





  - Handle F key to enter search mode
  - Update cursor position as characters are typed
  - Handle Space to mark and find next
  - Handle arrow keys for next/previous match
  - Support Migemo if available
  - Handle Escape/Enter to exit search
  - _Requirements: 11_

- [x] 27. Implement archive browsing





  - Handle Enter on archive files to show virtual folder
  - Display archive contents in pane
  - Handle Backspace to exit virtual folder
  - Handle Shift+Enter for extraction
  - _Requirements: 12_

- [x] 28. Implement compression handler





  - Handle P key to show compression dialog
  - Select archive format
  - Compress marked files or cursor file
  - Place archive in opposite pane
  - Show compression ratio
  - _Requirements: 13_

- [x] 29. Implement text viewer UI





  - Create text viewer window
  - Display file contents with line numbers
  - Handle Shift+E for encoding cycling
  - Handle F4 for search
  - Handle Escape/Enter to close
  - _Requirements: 14_

- [x] 30. Implement image viewer UI




  - Create image viewer window
  - Display image with current view mode
  - Handle Home/End for view mode switching
  - Handle Q/K for rotation
  - Handle G/U for flipping
  - Handle arrow keys for scrolling
  - Handle Escape to close
  - _Requirements: 15_

- [x] 31. Implement registered folders





  - Handle I key to show registered folder dialog
  - Handle Shift+B to register current directory
  - Handle Shift+M to move to registered folder
  - Load and save registered folders from configuration
  - _Requirements: 16_

- [x] 31.1 Write property test for registered folder navigation


  - **Property 32: Registered folder navigation changes path**
  - **Validates: Requirements 16.2**

- [x] 32. Implement file execution
  - Handle Enter on executable files
  - Handle Enter on files with extension associations
  - Handle Shift+Enter for editor
  - Handle Ctrl+Enter for Explorer association
  - Show error messages on failure
  - _Requirements: 17_

- [x] 32.1 Write property test for file execution
  - **Property 33: File execution launches program**
  - **Validates: Requirements 17.1, 17.2**

- [x] 33. Implement pattern-based rename
  - Handle Shift+R to show rename dialog
  - Parse search and replacement patterns
  - Support regex patterns (s/ and tr/)
  - Preview new names
  - Apply rename on confirmation
  - _Requirements: 18_

- [x] 34. Implement file comparison UI
  - Handle W key to show comparison dialog
  - Select comparison criteria (size, timestamp, name)
  - Call FileOperations.CompareFiles to mark matching files
  - Display results to user
  - _Requirements: 19_

- [x] 35. Implement file split and join UI handlers in MainController





  - Add HandleFileSplit method to show split dialog and call FileOperations.SplitAsync
  - Add HandleFileJoin method to show join dialog and call FileOperations.JoinAsync
  - Wire up Shift+W key binding in HandleKeyPress to detect split vs join based on file type
  - Show progress dialog during split/join operations
  - Display status messages on completion or error
  - _Requirements: 20_

- [x] 36. Implement configuration loading
  - Load configuration on startup
  - Apply display settings
  - Load key bindings
  - Load registered folders
  - Load extension associations
  - _Requirements: 22, 25_

- [x] 36.1 Write property test for configuration application






  - **Property 41: Configuration changes apply**
  - **Validates: Requirements 22.2**

- [x] 37. Implement session state management
  - Save session state on exit
  - Restore directory paths on startup
  - Restore file masks on startup
  - Restore sort modes on startup
  - Respect configuration setting for state saving
  - _Requirements: 25_

- [x] 38. Implement context menu UI in MainController




  - Add HandleContextMenu method to show context menu dialog
  - Wire up backtick (`) key binding in HandleKeyPress to trigger context menu
  - Call ListProvider.GetContextMenu to generate menu items based on current selection
  - Create Terminal.Gui dialog to display menu items with keyboard navigation
  - Execute selected operation by routing to existing handlers (copy, move, delete, etc.)
  - Handle Escape to close menu without action
  - _Requirements: 24_

- [x] 38.1 Write property test for context menu


  - **Property 44: Context menu shows applicable operations**
  - **Validates: Requirements 24.3**

- [x] 39. Implement configuration program launcher in MainController



  - Add HandleLaunchConfigurationProgram method to MainController
  - Get configuration program path from Configuration.ConfigurationProgramPath (default: "notepad.exe")
  - Get configuration file path from ConfigurationProvider
  - Launch external program using Process.Start with configuration file path as argument
  - Display success message in status bar when program launches
  - Display error message in status bar if program not found or launch fails
  - Handle exceptions gracefully (file not found, access denied, etc.)
  - _Requirements: 22.1_

- [x] 40. Final checkpoint - Ensure all tests pass





  - Run all unit tests and property tests
  - Fix any failing tests
  - Verify all 46 correctness properties are validated
  - Ensure all tests pass, ask the user if questions arise.
  - _Requirements: All_
