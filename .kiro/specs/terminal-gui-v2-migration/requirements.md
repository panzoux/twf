# Requirements Document: Terminal.Gui v2 Migration

## Introduction

This document specifies the requirements for migrating TWF (Two-pane Window Filer) from Terminal.Gui v1.19.0 to v2.x. Terminal.Gui v2 represents a fundamental architectural redesign with breaking changes that require systematic migration of application initialization, dialog execution, global state access, and resource management patterns. The migration will enable TrueColor support, improved performance, better testability, and alignment with modern .NET practices including PublishTrimmed and AOT compatibility.

## Glossary

- **TWF**: Two-pane Window Filer - a dual-pane file manager application
- **Terminal.Gui**: Cross-platform terminal UI toolkit for .NET
- **IApplication**: Instance-based application interface in Terminal.Gui v2
- **IRunnable**: Interface for views that can be run by the application
- **MainController**: Primary controller orchestrating the TWF application
- **PaneView**: Custom view displaying file lists in each pane
- **Dialog**: Modal window requiring user interaction
- **Global_State**: Static Application.* properties (Top, Driver, MainLoop)
- **TrueColor**: 24-bit RGB color support (16.7 million colors)
- **AOT**: Ahead-of-Time compilation for improved startup performance
- **PublishTrimmed**: .NET feature to reduce application size by removing unused code
- **Session_State**: Saved application state including paths, tabs, and settings
- **Property-Based_Test**: Test that validates properties across many generated inputs
- **View**: UI component in Terminal.Gui framework
- **ColorScheme**: Color configuration for UI components

## Requirements

### Requirement 1: Application Lifecycle Migration

**User Story:** As a developer, I want the application to use Terminal.Gui v2's instance-based pattern, so that the application follows modern .NET practices and eliminates global state dependencies.

#### Acceptance Criteria

1. WHEN the application starts, THE MainController SHALL create an IApplication instance using Application.Create().Init()
2. WHEN the application runs, THE MainController SHALL use the IApplication instance to run the main window
3. WHEN the application shuts down, THE MainController SHALL dispose the IApplication instance properly
4. THE MainController SHALL store the IApplication instance as a private field for lifecycle management
5. THE Application SHALL NOT use static Application.Init(), Application.Run(), or Application.Shutdown() methods
6. WHEN the application exits normally, THE System SHALL save session state before disposing resources
7. WHEN the application exits abnormally, THE System SHALL ensure proper resource disposal through try-finally blocks

### Requirement 2: Dialog Execution Pattern Migration

**User Story:** As a developer, I want all dialogs to use Terminal.Gui v2's execution pattern, so that dialog lifecycle is properly managed and results are correctly retrieved.

#### Acceptance Criteria

1. WHEN a dialog without return value is executed, THE System SHALL use app.Run(dialog) pattern
2. WHEN a dialog with return value is executed, THE System SHALL use IRunnable<TResult> pattern where appropriate
3. WHEN a dialog completes, THE System SHALL properly dispose the dialog instance
4. THE System SHALL migrate all ~40+ dialog invocations in MainController
5. THE System SHALL migrate all dialog invocations in service classes (MacroExpander, CustomFunctionManager)
6. WHEN Application.RequestStop() is called, THE System SHALL continue to function correctly (static method still available)
7. THE System SHALL maintain existing dialog functionality and user experience

### Requirement 3: Global State Elimination

**User Story:** As a developer, I want to eliminate all global state access, so that the application is more testable and supports multiple application contexts.

#### Acceptance Criteria

1. WHEN accessing the application driver, THE System SHALL use View.App.Driver instead of Application.Driver
2. WHEN accessing the top-level view, THE System SHALL use View.App.TopRunnableView instead of Application.Top
3. WHEN invoking main loop actions, THE System SHALL use View.App.MainLoop.Invoke instead of Application.MainLoop.Invoke
4. THE ConfigurationProvider SHALL NOT check Application.Top for null
5. THE KeyBindingManager SHALL NOT check Application.Top for null
6. THE System SHALL pass IApplication instance or use View.App property throughout the codebase
7. WHEN Application.Driver is null, THE System SHALL handle the case gracefully without crashes

### Requirement 4: Resource Management and Disposal

**User Story:** As a developer, I want proper resource management using IDisposable pattern, so that the application prevents memory leaks and resource exhaustion.

#### Acceptance Criteria

1. THE MainController SHALL implement IDisposable pattern for IApplication instance
2. WHEN custom views are created, THE System SHALL implement IDisposable where resources need cleanup
3. WHEN dialogs are created, THE System SHALL ensure proper disposal after execution
4. THE System SHALL use using statements or try-finally blocks for disposable resources
5. WHEN the application exits, THE System SHALL dispose all resources in correct order
6. THE System SHALL NOT leave any undisposed IApplication, Window, or Dialog instances
7. WHEN running property-based tests, THE System SHALL properly dispose all test resources

### Requirement 5: Color System Migration

**User Story:** As a developer, I want to migrate color configurations to Terminal.Gui v2's color system, so that colors are ANSI-compliant and support TrueColor.

#### Acceptance Criteria

1. WHEN creating color attributes, THE System SHALL use Application.Driver.MakeAttribute with View.App.Driver
2. THE System SHALL replace any Color.Brown references with Color.Yellow for ANSI compliance
3. WHEN TrueColor is available, THE System SHALL support 24-bit RGB color values
4. THE System SHALL maintain existing color configuration file compatibility
5. WHEN Application.Driver is unavailable, THE System SHALL defer color scheme initialization
6. THE System SHALL update all ColorScheme assignments in custom views (PaneView, TabBarView, TaskStatusView, TextViewerWindow, ImageViewerWindow, HelpView)
7. THE System SHALL update all ColorScheme assignments in dialog classes

### Requirement 6: Event Handler Pattern Migration

**User Story:** As a developer, I want event handlers to use standard EventHandler<T> pattern, so that the code follows .NET conventions and is more maintainable.

#### Acceptance Criteria

1. WHEN custom events are defined, THE System SHALL use EventHandler<T> pattern
2. THE System SHALL update event handler signatures to match Terminal.Gui v2 patterns
3. THE System SHALL maintain existing event functionality and behavior
4. WHEN events are raised, THE System SHALL pass correct event arguments
5. THE System SHALL update event subscriptions and unsubscriptions correctly

### Requirement 7: Key Handling Migration

**User Story:** As a developer, I want key handling to use Terminal.Gui v2's enhanced Key class, so that keyboard input is processed correctly and consistently.

#### Acceptance Criteria

1. WHEN checking for key combinations, THE System SHALL use Key.WithCtrl, Key.WithAlt, Key.WithShift fluent API
2. THE KeyBindingManager SHALL update key comparison logic for v2 Key class
3. THE System SHALL maintain all existing keyboard shortcuts and bindings
4. WHEN custom key handlers are defined, THE System SHALL use v2 key event patterns
5. THE System SHALL load and apply keybindings.json configuration correctly

### Requirement 8: Custom View Lifecycle Updates

**User Story:** As a developer, I want custom views to follow Terminal.Gui v2 lifecycle patterns, so that views are properly initialized, updated, and disposed.

#### Acceptance Criteria

1. THE PaneView SHALL update to use View.App property for application access
2. THE TabBarView SHALL update to use View.App property for application access
3. THE TaskStatusView SHALL update to use View.App property for application access
4. THE TextViewerWindow SHALL update to use View.App property for application access
5. THE ImageViewerWindow SHALL update to use View.App property for application access
6. THE HelpView SHALL update to use View.App property for application access
7. WHEN views use Application.MainLoop.AddTimeout, THE System SHALL update to use View.App.MainLoop.AddTimeout
8. WHEN views use Application.MainLoop.Invoke, THE System SHALL update to use View.App.MainLoop.Invoke
9. THE System SHALL implement IDisposable in custom views that manage resources

### Requirement 9: Configuration and Session State Preservation

**User Story:** As a user, I want my configuration and session state to be preserved during migration, so that I don't lose my settings, history, or workspace layout.

#### Acceptance Criteria

1. WHEN the application starts, THE System SHALL load existing config.json without modification
2. WHEN the application starts, THE System SHALL load existing keybindings.json without modification
3. WHEN the application starts, THE System SHALL load existing custom_functions.json without modification
4. WHEN the application starts, THE System SHALL restore session state from previous v1 sessions
5. WHEN the application exits, THE System SHALL save session state in compatible format
6. THE System SHALL preserve all tab states including paths, masks, sort modes, and display modes
7. THE System SHALL preserve history for both left and right panes across all tabs
8. THE System SHALL preserve registered folders and their configurations
9. THE System SHALL preserve task panel height and expansion state

### Requirement 10: Functional Equivalence

**User Story:** As a user, I want all existing features to work identically after migration, so that my workflow is not disrupted.

#### Acceptance Criteria

1. THE System SHALL support all file operations (copy, move, delete, rename, create)
2. THE System SHALL support all navigation operations (directory change, drive change, history, registered folders)
3. THE System SHALL support all marking operations (mark, unmark, invert, wildcard marking)
4. THE System SHALL support all search operations (incremental search, file content search)
5. THE System SHALL support all viewer operations (text viewer, image viewer, hex mode)
6. THE System SHALL support all custom functions and menu operations
7. THE System SHALL support all keyboard bindings and shortcuts
8. THE System SHALL support all display modes (brief, detailed, full)
9. THE System SHALL support all sorting modes (name, extension, size, date)
10. THE System SHALL support tab management (create, close, switch, rename)
11. THE System SHALL support archive browsing (zip file navigation)
12. THE System SHALL support background job management
13. THE System SHALL support task status view with log display

### Requirement 11: Performance Requirements

**User Story:** As a user, I want the application to be more responsive and use less CPU, so that I can work efficiently without system slowdown.

#### Acceptance Criteria

1. WHEN the application is idle, THE System SHALL use less CPU than v1 baseline
2. WHEN rendering the UI, THE System SHALL complete frame updates within 16ms (60 FPS)
3. WHEN loading large directories, THE System SHALL display results progressively
4. WHEN switching tabs, THE System SHALL respond within 100ms
5. WHEN opening dialogs, THE System SHALL display within 100ms
6. THE System SHALL NOT introduce memory leaks compared to v1
7. WHEN running for extended periods, THE System SHALL maintain stable memory usage

### Requirement 12: Build and Deployment Requirements

**User Story:** As a developer, I want the application to be PublishTrimmed and AOT compatible, so that deployment size is minimized and startup is fast.

#### Acceptance Criteria

1. WHEN building with PublishTrimmed, THE System SHALL compile without warnings
2. WHEN building with PublishTrimmed, THE System SHALL run without runtime errors
3. THE System SHALL NOT use reflection in ways incompatible with trimming
4. THE System SHALL NOT use dynamic code generation incompatible with AOT
5. WHEN published as trimmed, THE System SHALL be significantly smaller than untrimmed build
6. THE System SHALL maintain compatibility with .NET 8.0 runtime

### Requirement 13: Testing Requirements

**User Story:** As a developer, I want comprehensive test coverage to validate the migration, so that regressions are caught before release.

#### Acceptance Criteria

1. WHEN running unit tests, THE System SHALL pass all existing unit tests
2. WHEN running property-based tests, THE System SHALL pass all existing property tests
3. THE System SHALL maintain test coverage equal to or greater than v1
4. WHEN testing dialogs, THE System SHALL verify correct execution and disposal
5. WHEN testing views, THE System SHALL verify correct lifecycle and rendering
6. WHEN testing file operations, THE System SHALL verify correct behavior and error handling
7. THE System SHALL include integration tests for critical user workflows
8. WHEN running tests, THE System SHALL properly dispose all test resources

### Requirement 14: TrueColor Support

**User Story:** As a user, I want TrueColor (24-bit RGB) support, so that I can use more vibrant and accurate colors in the interface.

#### Acceptance Criteria

1. WHEN the terminal supports TrueColor, THE System SHALL enable 24-bit color rendering
2. THE System SHALL allow RGB color values in configuration files
3. WHEN TrueColor is unavailable, THE System SHALL fall back to 256-color or 16-color mode
4. THE System SHALL detect terminal color capabilities automatically
5. THE System SHALL render gradients and smooth color transitions when TrueColor is available

### Requirement 15: Copy/Paste Functionality

**User Story:** As a user, I want copy/paste functionality, so that I can transfer text between TWF and other applications.

#### Acceptance Criteria

1. WHEN text is selected in text viewer, THE System SHALL support copying to clipboard
2. WHEN clipboard contains text, THE System SHALL support pasting into text fields
3. THE System SHALL use Terminal.Gui v2's clipboard integration
4. WHEN clipboard operations fail, THE System SHALL handle errors gracefully
5. THE System SHALL support clipboard operations across different terminal emulators

### Requirement 16: Migration Validation

**User Story:** As a developer, I want validation checkpoints during migration, so that I can verify each phase before proceeding.

#### Acceptance Criteria

1. WHEN Phase 2 completes, THE System SHALL build without errors
2. WHEN Phase 2 completes, THE System SHALL initialize and display main window
3. WHEN Phase 3 completes, THE System SHALL execute all dialogs correctly
4. WHEN Phase 4 completes, THE System SHALL have zero Application.Top or Application.Driver references
5. WHEN Phase 5 completes, THE System SHALL render all custom views correctly
6. WHEN Phase 6 completes, THE System SHALL pass all automated tests
7. WHEN Phase 6 completes, THE System SHALL pass manual testing checklist

### Requirement 17: Error Handling and Logging

**User Story:** As a developer, I want comprehensive error handling and logging, so that issues can be diagnosed and fixed quickly.

#### Acceptance Criteria

1. WHEN migration errors occur, THE System SHALL log detailed error information
2. WHEN disposal fails, THE System SHALL log the error and continue cleanup
3. WHEN dialog execution fails, THE System SHALL log the error and show user-friendly message
4. THE System SHALL maintain existing logging levels and configuration
5. WHEN running in debug mode, THE System SHALL log lifecycle events (init, run, dispose)
6. THE System SHALL NOT expose sensitive information in logs

### Requirement 18: Backward Compatibility

**User Story:** As a user, I want to be able to roll back to v1 if needed, so that I have a safety net during migration.

#### Acceptance Criteria

1. THE System SHALL maintain v1 branch as main-v1-stable
2. THE System SHALL tag v1 releases separately from v2 releases
3. THE System SHALL document rollback procedure
4. THE System SHALL maintain configuration file compatibility between v1 and v2
5. WHEN rolling back to v1, THE System SHALL load session state saved by v2

### Requirement 19: Documentation Updates

**User Story:** As a developer, I want updated documentation reflecting v2 changes, so that future maintenance is easier.

#### Acceptance Criteria

1. THE System SHALL update code comments referencing v1 patterns
2. THE System SHALL document new IApplication lifecycle pattern
3. THE System SHALL document new dialog execution patterns
4. THE System SHALL document migration decisions and rationales
5. THE System SHALL update README with v2 requirements and features

### Requirement 20: Future Enhancement Readiness

**User Story:** As a developer, I want the migration to enable future enhancements, so that new features can be added easily.

#### Acceptance Criteria

1. THE System SHALL use architecture patterns that support theme customization
2. THE System SHALL use architecture patterns that support view arrangement (movable/resizable windows)
3. THE System SHALL use architecture patterns that support Sixel image rendering
4. THE System SHALL use architecture patterns that support enhanced help system
5. THE System SHALL document extension points for future features
