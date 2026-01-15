# Application Static Usage Audit

**Date**: 2026-01-15  
**Terminal.Gui Version**: 2.0.0-develop.4819  
**Target Framework**: .NET 10.0

## Summary

This document audits all static `Application.*` usage patterns in the TWF codebase that need to be migrated to instance-based patterns for Terminal.Gui v2 compatibility.

## 1. Application.Init() Calls

**Total Occurrences**: 1

### Controllers/MainController.cs
- **Line 248**: `Application.Init();`
  - **Context**: Initialize() method
  - **Migration**: Replace with `_app = Application.Create().Init()`
  - **Priority**: HIGH - Core initialization

## 2. Application.Run() Calls

**Total Occurrences**: 47

### Controllers/MainController.cs (Main Run Loop)
- **Line 1732**: `Application.Run();`
  - **Context**: Run() method - main application loop
  - **Migration**: Replace with `_app.Run(_mainWindow)`
  - **Priority**: HIGH - Core application lifecycle

### Controllers/MainController.cs (Dialog Invocations - 28 occurrences)
- **Line 1333**: `Application.Run(new JobManagerDialog(_jobManager, _config));`
- **Line 1963**: `Application.Run(dialog);` - TabSelectorDialog
- **Line 2512**: `Application.Run(progressDialog);` - OperationProgressDialog
- **Line 2824**: `Application.Run(dialog);` - DriveDialog
- **Line 2850**: `Application.Run(dialog);` - CustomFunctionDialog
- **Line 2927**: `Application.Run(dialog);` - RegisterFolderDialog
- **Line 3070**: `Application.Run(dialog);` - HistoryDialog
- **Line 3385**: `Application.Run(viewerWindow);` - TextViewerWindow
- **Line 3439**: `Application.Run(viewerWindow);` - ImageViewerWindow
- **Line 3670**: `Application.Run(dialog);` - WildcardMarkingDialog
- **Line 4057**: `Application.Run(dialog);` - SimpleRenameDialog
- **Line 4126**: `Application.Run(dialog);` - PatternRenameDialog
- **Line 4206**: `Application.Run(dialog);` - FileComparisonDialog
- **Line 4252**: `Application.Run(dialog);` - ConfirmationDialog
- **Line 4314**: `Application.Run(progressDialog);` - OperationProgressDialog
- **Line 4327**: `Application.Run(dialog);` - FileCollisionDialog
- **Line 4339**: `Application.Run(new MessageDialog(title, message));`
- **Line 4355**: `Application.Run(dialog);` - CreateDirectoryDialog
- **Line 4444**: `Application.Run(dialog);` - CreateNewFileDialog
- **Line 4660**: `Application.Run(dialog);` - SortDialog
- **Line 4710**: `Application.Run(dialog);` - FileMaskDialog
- **Line 4845**: `Application.Run(dialog);` - JumpToPathDialog
- **Line 4903**: `Application.Run(dialog);` - HistoryDialog
- **Line 4958**: `Application.Run(helpView);` - HelpView
- **Line 5126**: `Application.Run(viewerWindow);` - ImageViewerWindow
- **Line 5186**: `Application.Run(viewerWindow);` - TextViewerWindow
- **Line 5241**: `Application.Run(viewerWindow);` - TextViewerWindow (hex mode)
- **Line 5338**: `Application.Run(dialog);` - CompressionOptionsDialog
- **Line 5458**: `Application.Run(progressDialog);` - OperationProgressDialog
- **Line 5907**: `Application.Run(progressDialog);` - OperationProgressDialog
- **Line 5923**: `Application.Run(dialog);` - FileSplitOptionsDialog
- **Line 6038**: `Application.Run(progressDialog);` - OperationProgressDialog
- **Line 6103**: `Application.Run(dialog);` - FileJoinOptionsDialog
- **Line 6142**: `Application.Run(dialog);` - ContextMenuDialog
- **Line 6299**: `Application.Run(dialog);` - FilePropertiesDialog
- **Line 6523**: `Application.Run(historyDialog);` - HistoryDialog

### Services/MacroExpander.cs
- **Line 369**: `Application.Run(dialog);`
  - **Context**: ShowInputDialog method
  - **Migration**: Pass IApplication instance to service

### Services/CustomFunctionManager.cs
- **Line 327**: `Application.Run(menuDialog);`
  - **Context**: ExecuteFunction method
  - **Migration**: Pass IApplication instance to service

### UI/FileOperationOptionsDialogs.cs
- **Line 306**: `Application.Run(inputDialog);`
  - **Context**: RenameConflictDialog in FileCollisionDialog
  - **Migration**: Use parent dialog's App property

## 3. Application.Shutdown() Calls

**Total Occurrences**: 1

### Controllers/MainController.cs
- **Line 1832**: `Application.Shutdown();`
  - **Context**: Shutdown() method
  - **Migration**: Replace with `_app?.Dispose()`
  - **Priority**: HIGH - Core cleanup

## 4. Application.Top References

**Total Occurrences**: 6

### Controllers/MainController.cs
- **Line 1698**: `Application.Top.Add(_mainWindow);`
  - **Context**: Run() method
  - **Migration**: Remove - use `_app.Run(_mainWindow)` instead
  - **Priority**: HIGH - Core application lifecycle

### Providers/ConfigurationProvider.cs
- **Line 107**: `if (Application.Top != null)`
- **Line 194**: `if (Application.Top != null)`
- **Line 249**: `if (Application.Top != null)`
  - **Context**: Error dialog checks
  - **Migration**: Pass IApplication instance to provider
  - **Priority**: MEDIUM - Error handling

### Services/KeyBindingManager.cs
- **Line 70**: `if (Application.Top != null)`
- **Line 198**: `if (Application.Top != null)`
  - **Context**: Error dialog checks
  - **Migration**: Pass IApplication instance to service
  - **Priority**: MEDIUM - Error handling

## 5. Application.Driver References

**Total Occurrences**: 60+

### Controllers/MainController.cs
- **Line 159**: `int windowHeight = Application.Driver?.Rows ?? 0;`
- **Line 189**: `Application.Driver.MakeAttribute(foregroundColor, backgroundColor)`
- **Line 197**: `Application.Driver.MakeAttribute(topSeparatorFg, topSeparatorBg)`
- **Line 206**: `Application.Driver.MakeAttribute(labelFg, labelBg)`
- **Line 215**: `Application.Driver.MakeAttribute(foregroundColor, backgroundColor)` (multiple)
- **Line 227**: `Application.Driver.MakeAttribute(foregroundColor, backgroundColor)` (multiple)
- **Line 481**: `Application.Driver.MakeAttribute(Color.White, Color.Black)`
- **Line 520**: `Application.Driver.MakeAttribute(...)` (multiple in CreateMainWindow)
- **Line 649**: `int windowWidth = Math.Max(40, Application.Driver.Cols);`
  - **Context**: Various color scheme and layout operations
  - **Migration**: Replace with `_app?.Driver` or `View.App?.Driver`
  - **Priority**: HIGH - Core rendering

### UI Components (Multiple Files)
- **PaneView.cs**: Lines 355, 374, 380, 387, 391, 404, 407, 415
- **TextViewerWindow.cs**: Lines 127, 131, 132, 148, 152, 167, 171
- **TaskStatusView.cs**: Lines 286, 290, 294
- **ImageViewerWindow.cs**: Lines 83, 88, 103, 108
- **TabSelectorDialog.cs**: Lines 245, 246, 251, 253
- **RegisteredFolderDialog.cs**: Lines 267, 268, 269, 270, 276, 285
- **HistoryDialog.cs**: Lines 201, 202, 203, 204, 212
- **JobManagerDialog.cs**: Lines 40, 41, 74
- **MessageLogView.cs**: Lines 20, 21
- **SystemDialogs.cs**: Line 126
- **WildcardMarkingDialog.cs**: Line 45
  - **Context**: Color scheme initialization in views/dialogs
  - **Migration**: Replace with `App?.Driver` property
  - **Priority**: MEDIUM - View rendering

## 6. Application.MainLoop References

**Total Occurrences**: 40+

### Controllers/MainController.cs
- **Line 615**: `Application.MainLoop.AddTimeout(...)` - Spinner/refresh timer
- **Line 634**: `Application.MainLoop.Invoke(...)` - Job manager updates (2 occurrences)
- **Line 1459**: `Application.MainLoop.Invoke(...)` - Archive loading
- **Line 1501**: `Application.MainLoop.Invoke(...)` - Status bar update
- **Line 1516**: `Application.MainLoop.Invoke(...)` - Status bar update
- **Line 1564**: `Application.MainLoop.Invoke(...)` - Batch updates
- **Line 1587**: `Application.MainLoop.Invoke(...)` - Batch updates
- **Line 1629**: `Application.MainLoop.Invoke(...)` - Error handling
- **Line 1638**: `Application.MainLoop.Invoke(...)` - Status bar update
- **Line 1707**: `Application.MainLoop.AddTimeout(...)` - File list refresh timer
- **Line 2113**: `Application.MainLoop.Invoke(...)` - Pane refresh
- **Line 2359**: `Application.MainLoop.Invoke(...)` - Status bar update
- **Line 2383**: `Application.MainLoop.Invoke(...)` - Archive entries update
- **Line 2397**: `Application.MainLoop.Invoke(...)` - Error handling
- **Line 2418**: `Application.MainLoop.Invoke(...)` - Status bar update
- **Line 2472**: `Application.MainLoop.Invoke(...)` - RequestStop
- **Line 2502**: `Application.MainLoop.Invoke(...)` - RequestStop
- **Line 4272**: `Application.MainLoop.Invoke(...)` - Progress updates
- **Line 4287**: `Application.MainLoop.Invoke(...)` - Operation completion
- **Line 4304**: `Application.MainLoop.Invoke(...)` - Error handling
- **Line 4324**: `Application.MainLoop.Invoke(...)` - File collision dialog
- **Line 4854**: `Application.MainLoop.Invoke(...)` - Path verification (multiple)
  - **Context**: Async operations, timers, UI updates
  - **Migration**: Replace with `_app?.MainLoop` or `View.App?.MainLoop`
  - **Priority**: HIGH - Core async operations

### UI/TextViewerWindow.cs
- **Line 60**: `Application.MainLoop.Invoke(...)` - Indexing completed
- **Line 68**: `Application.MainLoop.AddTimeout(...)` - Status update timer
- **Line 96**: `Application.MainLoop.RemoveTimeout(...)` - Timer cleanup
- **Line 447**: `Application.MainLoop.Invoke(...)` - Search highlight
- **Line 509**: `Application.MainLoop.Invoke(...)` - Search highlight
  - **Context**: File viewer async operations
  - **Migration**: Replace with `App?.MainLoop`
  - **Priority**: MEDIUM - Viewer functionality

### UI/TaskStatusView.cs
- **Line 84**: `Application.MainLoop.AddTimeout(...)` - Refresh timer
  - **Context**: Task status updates
  - **Migration**: Replace with `App?.MainLoop`
  - **Priority**: MEDIUM - Task panel updates

### UI/JobManagerDialog.cs
- **Line 96**: `Application.MainLoop.AddTimeout(...)` - Refresh timer
  - **Context**: Job list updates
  - **Migration**: Replace with `App?.MainLoop`
  - **Priority**: MEDIUM - Job manager updates

### Services/KeyBindingManager.cs
- **Line 72**: `Application.MainLoop.Invoke(...)` - Error dialog (2 occurrences)
- **Line 200**: `Application.MainLoop.Invoke(...)` - Error dialog
  - **Context**: Error handling
  - **Migration**: Pass IApplication instance to service
  - **Priority**: MEDIUM - Error handling

### Services/CustomFunctionManager.cs
- **Line 94**: `Application.MainLoop.Invoke(...)` - Error dialog (2 occurrences)
- **Line 106**: `Application.MainLoop.Invoke(...)` - Error dialog
  - **Context**: Error handling
  - **Migration**: Pass IApplication instance to service
  - **Priority**: MEDIUM - Error handling

### Providers/ConfigurationProvider.cs
- **Line 109**: `Application.MainLoop.Invoke(...)` - Error dialog (3 occurrences)
- **Line 195**: `Application.MainLoop.Invoke(...)` - Error dialog
- **Line 250**: `Application.MainLoop.Invoke(...)` - Error dialog
  - **Context**: Error handling
  - **Migration**: Pass IApplication instance to provider
  - **Priority**: MEDIUM - Error handling

## 7. Application.RequestStop() Calls

**Total Occurrences**: 60+

All `Application.RequestStop()` calls remain **UNCHANGED** in Terminal.Gui v2 - this is still a static method.

### Affected Files
- UI/WildcardMarkingDialog.cs (2)
- UI/TextViewerWindow.cs (2)
- UI/TabSelectorDialog.cs (2)
- UI/SystemDialogs.cs (8)
- UI/SortDialog.cs (3)
- UI/SimpleRenameDialog.cs (2)
- UI/RegisteredFolderDialog.cs (2)
- UI/PatternRenameDialog.cs (2)
- UI/MenuDialog.cs (4)
- UI/JumpToPathDialog.cs (2)
- UI/JobManagerDialog.cs (1)
- UI/ImageViewerWindow.cs (1)
- UI/HistoryDialog.cs (2)
- UI/HelpView.cs (2)
- UI/FileOperationOptionsDialogs.cs (15+)
- Controllers/MainController.cs (multiple in dialog handlers)

**No migration needed** - Application.RequestStop() remains static in v2.

## 8. Application.Resized Event

**Total Occurrences**: 1

### Controllers/MainController.cs
- **Line 596**: `Application.Resized += (e) => { ... }`
  - **Context**: Terminal resize handler
  - **Migration**: Replace with `_app.Resized += ...` or handle via View lifecycle
  - **Priority**: MEDIUM - Layout updates

## Migration Priority Summary

### Phase 2 (Core Application Lifecycle) - HIGH PRIORITY
1. **Application.Init()** → `_app = Application.Create().Init()` (1 occurrence)
2. **Application.Run()** (main loop) → `_app.Run(_mainWindow)` (1 occurrence)
3. **Application.Shutdown()** → `_app?.Dispose()` (1 occurrence)
4. **Application.Top.Add()** → Remove, use `_app.Run()` (1 occurrence)
5. **Application.Driver** in MainController → `_app?.Driver` (15+ occurrences)
6. **Application.MainLoop** in MainController → `_app?.MainLoop` (25+ occurrences)

### Phase 3 (Dialog Execution Pattern) - HIGH PRIORITY
1. **Application.Run(dialog)** → `_app?.Run(dialog)` (47 occurrences)

### Phase 4 (Global State Elimination) - MEDIUM PRIORITY
1. **Application.Top** checks in services → Pass `_app` instance (5 occurrences)
2. **Application.MainLoop.Invoke** in services → Use `_app?.MainLoop` (10+ occurrences)
3. **Application.Driver** in UI components → Use `App?.Driver` (45+ occurrences)
4. **Application.MainLoop** in UI components → Use `App?.MainLoop` (15+ occurrences)

### No Migration Needed
1. **Application.RequestStop()** - Remains static in v2 (60+ occurrences)

## Notes

- Total static Application references: ~200+
- Most critical: Application.Init(), Application.Run(), Application.Shutdown()
- Dialog invocations are the largest category requiring migration
- Services need IApplication instance passed via constructor
- UI components can use View.App property once in the view hierarchy
- Application.RequestStop() is the only static method that remains unchanged

## Next Steps

1. Complete Phase 1 Task 4: Identify all dialog invocations
2. Begin Phase 2: Core Application Lifecycle Migration
3. Update MainController to store IApplication instance
4. Migrate all dialog invocations to use instance-based pattern
5. Update services to accept IApplication parameter
6. Update UI components to use App property instead of static Application
