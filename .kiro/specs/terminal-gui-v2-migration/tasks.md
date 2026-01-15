# Implementation Plan: Terminal.Gui v2 Migration

## Overview

This implementation plan guides the migration of TWF from Terminal.Gui v1.19.0 to v2.x through six distinct phases. Each phase builds on the previous one, enabling incremental progress and validation. The migration transforms the application from static, global-state-based patterns to instance-based, dependency-injected patterns while maintaining all existing functionality.

## Tasks

### Phase 1: Preparation and Environment Setup

- [x] 1. Set up migration environment
  - Create feature branch `feature/terminal-gui-v2-upgrade`
  - Tag current version as `pre-v2-upgrade` for rollback capability
  - Verify all existing tests pass on current v1 codebase
  - Document current test coverage metrics
  - _Requirements: 18.1, 18.2_

- [x] 2. Update Terminal.Gui package dependency
  - Update Terminal.Gui NuGet package to latest v2.x stable version
  - Update twf.csproj with new package version
  - Restore packages and verify build compiles (expect errors)
  - Document breaking changes from compiler errors
  - _Requirements: 12.1, 12.6_

- [x] 3. Audit Application static usage
  - Search codebase for all `Application.Init()` calls
  - Search codebase for all `Application.Run()` calls
  - Search codebase for all `Application.Shutdown()` calls
  - Search codebase for all `Application.Top` references
  - Search codebase for all `Application.Driver` references
  - Search codebase for all `Application.MainLoop` references
  - Search codebase for all `Application.RequestStop()` calls
  - Document findings in audit report with file locations and line numbers
  - _Requirements: 3.1, 3.2, 3.3_

- [x] 4. Identify all dialog invocations
  - List all dialog classes in UI/ directory
  - Identify dialogs with return values vs simple dialogs
  - Document dialog invocation locations in MainController
  - Document dialog invocation locations in service classes
  - Create dialog migration checklist (~40+ items)
  - _Requirements: 2.4, 2.5_

- [x] 5. Checkpoint - Review preparation phase
  - Ensure all tests pass, ask the user if questions arise.

### Phase 2: Core Application Lifecycle Migration

- [x] 6. Update MainController to implement IDisposable
  - Add `IDisposable` interface to MainController class
  - Add `private IApplication? _app;` field
  - Add `private bool _disposed;` field
  - Implement `Dispose()` method with proper disposal pattern
  - Implement `~MainController()` finalizer if needed
  - _Requirements: 1.4, 4.1_

- [x] 7. Migrate MainController.Initialize() method
  - Replace `Application.Init()` with `_app = Application.Create().Init()`
  - Store IApplication instance in `_app` field
  - Update configuration loading to work with instance-based pattern
  - Update key binding loading to work with instance-based pattern
  - Update custom function loading to work with instance-based pattern
  - Pass `_app` instance to services that need it (ConfigurationProvider, KeyBindingManager)
  - _Requirements: 1.1, 1.4_

- [x] 7.1 Write property test for application initialization
  - **Property 1: Application Lifecycle Initialization**
  - **Validates: Requirements 1.1**

- [x] 8. Migrate MainController.Run() method
  - Remove `Application.Top.Add(_mainWindow)` call
  - Replace `Application.Run()` with `_app.Run(_mainWindow)`
  - Update file list refresh timer to use `_app.MainLoop.AddTimeout`
  - Verify main window is displayed correctly
  - _Requirements: 1.2_

- [x] 8.1 Write property test for application run lifecycle
  - **Property 2: Application Lifecycle Disposal (partial - run aspect)**
  - **Validates: Requirements 1.2**

- [x] 9. Migrate MainController.Shutdown() method
  - Wrap session state saving in try-catch block
  - Replace `Application.Shutdown()` with `_app?.Dispose()`
  - Add disposal of `_mainWindow`
  - Implement proper error logging for disposal failures
  - Ensure `_disposed` flag is set
  - _Requirements: 1.3, 1.6, 17.1, 17.2_

- [x] 9.1 Write property test for application disposal
  - **Property 2: Application Lifecycle Disposal**
  - **Validates: Requirements 1.3, 4.1, 4.5, 4.6**

- [x] 9.2 Write property test for exception-safe disposal
  - **Property 3: Exception-Safe Disposal**
  - **Validates: Requirements 1.7, 4.5**

- [x] 10. Update Program.cs to use MainController disposal
  - Wrap MainController usage in try-finally block
  - Call `controller.Dispose()` in finally block
  - Ensure proper cleanup on all exit paths
  - _Requirements: 1.7, 4.1_

- [ ] 11. Update MainController color scheme initialization
  - Replace all `Application.Driver.MakeAttribute` with `_app?.Driver?.MakeAttribute`
  - Add null checks and fallback color attributes
  - Update status bar color scheme initialization
  - Update filename label color scheme initialization
  - Update pane color scheme initialization
  - _Requirements: 5.1, 3.7_

- [ ] 12. Checkpoint - Verify core application lifecycle
  - Ensure all tests pass, ask the user if questions arise.

### Phase 3: Dialog Execution Pattern Migration

- [ ] 13. Create dialog execution helper methods
  - Create `RunDialog(Dialog dialog)` helper method in MainController
  - Create `RunDialog<TResult>(Dialog dialog)` helper method for result-returning dialogs
  - Implement proper disposal in helper methods
  - Add error handling and logging
  - _Requirements: 2.1, 2.2, 2.3, 17.3_

- [ ] 14. Migrate simple dialogs (no return values)
  - Update MessageDialog invocations to use `_app?.Run(dialog)` pattern
  - Update ConfirmationDialog invocations
  - Update InfoDialog invocations
  - Ensure proper disposal after each dialog execution
  - _Requirements: 2.1, 2.3_

- [ ] 15. Migrate result-returning dialogs
  - Update FileCollisionDialog to use new pattern
  - Update CustomFunctionDialog to use new pattern
  - Update SortDialog to use new pattern
  - Update FileMaskDialog to use new pattern
  - Update RegisteredFolderDialog to use new pattern
  - Update HistoryDialog to use new pattern
  - Update TabSelectorDialog to use new pattern
  - Update JumpToPathDialog to use new pattern
  - Update SimpleRenameDialog to use new pattern
  - Update PatternRenameDialog to use new pattern
  - Update WildcardMarkingDialog to use new pattern
  - Ensure proper result retrieval and disposal
  - _Requirements: 2.2, 2.3_

- [ ] 15.1 Write property test for dialog execution and disposal
  - **Property 5: Dialog Execution and Disposal**
  - **Validates: Requirements 2.1, 2.3, 4.3**

- [ ] 15.2 Write property test for dialog result retrieval
  - **Property 6: Dialog Result Retrieval**
  - **Validates: Requirements 2.2**

- [ ] 16. Migrate file operation dialogs
  - Update FileOperationOptionsDialog invocations
  - Update SplitFileDialog invocations
  - Update CompareFilesDialog invocations
  - Update OperationProgressDialog invocations
  - Ensure proper disposal and error handling
  - _Requirements: 2.1, 2.2, 2.3_

- [ ] 17. Migrate viewer window dialogs
  - Update TextViewerWindow invocations to use `_app?.Run(window)` pattern
  - Update ImageViewerWindow invocations
  - Update HelpView invocations
  - Update JobManagerDialog invocations
  - Ensure proper disposal after window closes
  - _Requirements: 2.1, 2.3_

- [ ] 18. Update MacroExpander dialog invocations
  - Update any dialog calls in MacroExpander service
  - Pass `_app` instance or use alternative pattern
  - Ensure proper disposal
  - _Requirements: 2.5_

- [ ] 19. Update CustomFunctionManager dialog invocations
  - Update MenuDialog execution in CustomFunctionManager
  - Pass `_app` instance to CustomFunctionManager constructor
  - Update dialog execution to use instance-based pattern
  - Ensure proper disposal
  - _Requirements: 2.5_

- [ ] 19.1 Write property test for RequestStop functionality
  - **Property 7: RequestStop Functionality**
  - **Validates: Requirements 2.6**

- [ ] 20. Checkpoint - Verify all dialogs work correctly
  - Ensure all tests pass, ask the user if questions arise.

### Phase 4: Global State Elimination

- [ ] 21. Update ConfigurationProvider
  - Remove all `Application.Top != null` checks
  - Add `IApplication?` parameter to constructor
  - Store `_app` instance as field
  - Replace `Application.MainLoop.Invoke` with `_app?.MainLoop.Invoke`
  - Update error dialog invocations to use `_app?.Run(dialog)` pattern
  - Add null checks for `_app` before UI operations
  - _Requirements: 3.2, 3.3, 3.4, 3.6_

- [ ] 22. Update KeyBindingManager
  - Remove all `Application.Top != null` checks
  - Add `IApplication?` parameter to constructor
  - Store `_app` instance as field
  - Replace `Application.MainLoop.Invoke` with `_app?.MainLoop.Invoke`
  - Update error dialog invocations to use `_app?.Run(dialog)` pattern
  - Add null checks for `_app` before UI operations
  - _Requirements: 3.2, 3.3, 3.5, 3.6_

- [ ] 23. Update ViewerManager
  - Add `IApplication?` parameter to constructor if needed
  - Update any `Application.*` references to use `_app` instance
  - Update viewer window launching to use instance-based pattern
  - _Requirements: 3.6_

- [ ] 24. Update MacroExpander
  - Add `IApplication?` parameter to constructor if needed
  - Update any `Application.*` references to use `_app` instance
  - _Requirements: 3.6_

- [ ] 25. Update remaining service classes
  - Audit FileOperations for `Application.*` usage
  - Audit MarkingEngine for `Application.*` usage
  - Audit SearchEngine for `Application.*` usage
  - Audit ArchiveManager for `Application.*` usage
  - Update any found references to use `_app` instance or `View.App` property
  - _Requirements: 3.6_

- [ ] 26. Verify no static Application references remain
  - Run grep search for `Application.Init`
  - Run grep search for `Application.Top`
  - Run grep search for `Application.Driver` (excluding `View.App.Driver`)
  - Run grep search for `Application.MainLoop` (excluding `View.App.MainLoop`)
  - Document any remaining references and justify or fix
  - _Requirements: 1.5, 3.1, 3.2, 3.3_

- [ ] 27. Checkpoint - Verify global state elimination
  - Ensure all tests pass, ask the user if questions arise.

### Phase 5: Custom View Lifecycle Updates

- [ ] 28. Update PaneView
  - Replace `Application.Driver.MakeAttribute` with `App?.Driver?.MakeAttribute`
  - Add null checks and fallback attributes
  - Update `GetCursorColorAttribute()` method
  - Update `GetDirectoryColorAttribute()` method
  - Update `GetHighlightColorAttribute()` method
  - Update `GetInactiveCursorColorAttribute()` method
  - Test pane rendering with new pattern
  - _Requirements: 8.1, 5.1, 3.7_

- [ ] 29. Update TabBarView
  - Replace any `Application.*` references with `App` property
  - Update color scheme initialization if needed
  - Test tab bar rendering and interaction
  - _Requirements: 8.2_

- [ ] 30. Update TaskStatusView
  - Replace `Application.MainLoop.AddTimeout` with `App?.MainLoop.AddTimeout`
  - Replace `Application.Driver.MakeAttribute` with `App?.Driver?.MakeAttribute`
  - Update `InitializeColors()` method
  - Add null checks for `App` property
  - Test task status view updates and rendering
  - _Requirements: 8.3, 8.7_

- [ ] 31. Update TextViewerWindow
  - Replace `Application.MainLoop.Invoke` with `App?.MainLoop.Invoke`
  - Replace `Application.MainLoop.AddTimeout` with `App?.MainLoop.AddTimeout`
  - Replace `Application.MainLoop.RemoveTimeout` with `App?.MainLoop.RemoveTimeout`
  - Replace `Application.Driver.MakeAttribute` with `App?.Driver?.MakeAttribute`
  - Update color scheme initialization with null checks
  - Test text viewer functionality (open, navigate, search, close)
  - _Requirements: 8.4, 8.7, 8.8_

- [ ] 32. Update ImageViewerWindow
  - Replace `Application.Driver` checks with `App?.Driver` checks
  - Replace `Application.Driver.MakeAttribute` with `App?.Driver?.MakeAttribute`
  - Update color scheme initialization with null checks
  - Test image viewer functionality
  - _Requirements: 8.5_

- [ ] 33. Update HelpView
  - Replace any `Application.*` references with `App` property
  - Update color scheme initialization if needed
  - Test help view display and navigation
  - _Requirements: 8.6_

- [ ] 34. Update MessageLogView
  - Replace `Application.Driver.MakeAttribute` with `App?.Driver?.MakeAttribute`
  - Update color scheme initialization
  - _Requirements: 8.9_

- [ ] 35. Update VirtualFileView
  - Replace any `Application.*` references with `App` property
  - Update color scheme initialization if needed
  - _Requirements: 8.9_

- [ ] 36. Implement IDisposable in views with resources
  - Review TextViewerWindow for disposable resources (timers, file handles)
  - Review ImageViewerWindow for disposable resources
  - Review TaskStatusView for disposable resources (timers)
  - Implement IDisposable pattern where needed
  - Ensure proper cleanup in Dispose() methods
  - _Requirements: 4.2, 8.9_

- [ ] 36.1 Write property test for view disposal
  - **Property 2: Application Lifecycle Disposal (view aspect)**
  - **Validates: Requirements 4.2**

- [ ] 37. Update all dialog color schemes
  - Update CustomFunctionDialog color scheme initialization
  - Update DriveDialog color scheme initialization
  - Update FileMaskDialog color scheme initialization
  - Update HistoryDialog color scheme initialization
  - Update JobManagerDialog color scheme initialization
  - Update JumpToPathDialog color scheme initialization
  - Update MenuDialog color scheme initialization
  - Update RegisteredFolderDialog color scheme initialization
  - Update SortDialog color scheme initialization
  - Update TabSelectorDialog color scheme initialization
  - Update WildcardMarkingDialog color scheme initialization
  - Update SystemDialogs color scheme initialization
  - Replace `Application.Driver.MakeAttribute` with `App?.Driver?.MakeAttribute`
  - Add null checks and fallback attributes
  - _Requirements: 5.6, 5.7_

- [ ] 38. Checkpoint - Verify all views render correctly
  - Ensure all tests pass, ask the user if questions arise.

### Phase 6: Testing, Validation, and Polish

- [ ] 39. Run and fix all existing unit tests
  - Run full test suite
  - Fix any test failures related to v2 migration
  - Update test setup/teardown for IApplication disposal
  - Ensure all tests properly dispose resources
  - _Requirements: 13.1, 13.8_

- [ ] 39.1 Write property test for session state round-trip
  - **Property 4: Session State Round-Trip**
  - **Validates: Requirements 1.6, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9**

- [ ] 39.2 Write property test for configuration file compatibility
  - **Property 8: Configuration File Compatibility**
  - **Validates: Requirements 9.1, 9.2, 9.3, 5.4**

- [ ] 39.3 Write property test for TrueColor support
  - **Property 9: Color System TrueColor Support**
  - **Validates: Requirements 5.3, 14.1, 14.2**

- [ ] 39.4 Write property test for color system fallback
  - **Property 10: Color System Fallback**
  - **Validates: Requirements 14.3, 14.4**

- [ ] 39.5 Write property test for key binding preservation
  - **Property 11: Key Binding Preservation**
  - **Validates: Requirements 7.2, 7.3, 7.5**

- [ ] 39.6 Write property test for event handler invocation
  - **Property 12: Event Handler Invocation**
  - **Validates: Requirements 6.4, 6.5**

- [ ] 39.7 Write property test for clipboard operations
  - **Property 13: Clipboard Operations**
  - **Validates: Requirements 15.1, 15.2, 15.3**

- [ ] 39.8 Write property test for error handling and logging
  - **Property 14: Error Handling and Logging**
  - **Validates: Requirements 17.1, 17.2, 17.3**

- [ ] 39.9 Write property test for view disposal in tests
  - **Property 15: View Disposal in Tests**
  - **Validates: Requirements 4.7**

- [ ] 40. Run all property-based tests
  - Execute all property tests with minimum 100 iterations
  - Fix any property test failures
  - Verify resource disposal in all tests
  - _Requirements: 13.2, 13.8_

- [ ] 41. Perform integration testing
  - Test application startup and shutdown workflow
  - Test file navigation and operations workflow
  - Test dialog workflows (open, interact, close)
  - Test viewer windows workflow (text viewer, image viewer)
  - Test tab management workflow (create, switch, close)
  - Test custom functions workflow
  - Test keyboard bindings workflow
  - Test session state save and restore workflow
  - _Requirements: 13.7_

- [ ] 42. Execute manual testing checklist
  - Test application starts without errors
  - Test main window displays correctly
  - Test file navigation (up, down, enter, backspace)
  - Test drive selection (Alt+F1, Alt+F2)
  - Test file operations (F5 copy, F6 move, F8 delete)
  - Test marking (Insert, +, -, *)
  - Test search (Ctrl+S incremental search)
  - Test text viewer (F3)
  - Test image viewer (if applicable)
  - Test custom functions (F2 menu)
  - Test keyboard bindings (all configured keys)
  - Test tab management (Ctrl+T, Ctrl+W, Ctrl+Tab)
  - Test task status view (Ctrl+O)
  - Test help system (F1)
  - Test configuration loads correctly
  - Test session state restores correctly
  - Test application exits cleanly
  - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7_

- [ ] 43. Validate performance improvements
  - Measure CPU usage while idle (compare to v1 baseline)
  - Measure CPU usage during file list refresh
  - Measure memory usage over time (check for leaks)
  - Measure dialog open time (should be <100ms)
  - Measure tab switch time (should be <100ms)
  - Measure frame render time (should be <16ms)
  - Document performance metrics
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7_

- [ ] 44. Test PublishTrimmed compatibility
  - Build with PublishTrimmed enabled
  - Verify no trimming warnings
  - Run application and verify functionality
  - Measure published size reduction
  - _Requirements: 12.1, 12.2, 12.3, 12.5_

- [ ] 45. Verify backward compatibility
  - Test with existing v1 config.json files
  - Test with existing v1 keybindings.json files
  - Test with existing v1 custom_functions.json files
  - Test with existing v1 session state files
  - Verify all settings load and apply correctly
  - _Requirements: 18.4, 18.5_

- [ ] 46. Update documentation
  - Update code comments referencing v1 patterns
  - Document IApplication lifecycle pattern
  - Document dialog execution patterns
  - Document migration decisions and rationales
  - Update README with v2 requirements and features
  - _Requirements: 19.1, 19.2, 19.3, 19.4, 19.5_

- [ ] 47. Final checkpoint - Migration complete
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- All tasks are required for comprehensive migration with full test coverage
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at phase boundaries
- Property tests validate universal correctness properties with minimum 100 iterations
- Unit tests validate specific examples and edge cases
- Manual testing validates user experience and performance
- The migration is designed to be incremental with validation at each phase
- Rollback to v1 is possible at any point by switching to `main-v1-stable` branch
