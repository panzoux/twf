# Terminal.Gui v2 Breaking Changes Audit

**Date**: 2026-01-15  
**Terminal.Gui Version**: 2.0.0-develop.4819  
**Target Framework**: .NET 10.0

## Summary

- **Total Compilation Errors**: ~200+ errors
- **Affected Files**: ~40+ files across UI, Controllers, Utilities, Services

## Breaking Changes by Category

### 1. Missing Types (CS0246 errors)

#### Core UI Types
- `Dialog` - Base class for dialogs no longer exists or moved
- `Window` - Window class missing or moved
- `View` - View class missing or moved
- `Label` - Label control missing or moved
- `TextField` - TextField control missing or moved
- `ListView` - ListView control missing or moved
- `TextView` - TextView control missing or moved
- `RadioGroup` - RadioGroup control missing or moved

#### Layout Types
- `Rect` - Rectangle type missing or moved
- `Pos` - Position type likely changed
- `Dim` - Dimension type likely changed

#### Input Types
- `Key` - Key type missing or moved
- `KeyEvent` - KeyEvent type missing or moved

#### Graphics Types
- `Color` - Color type missing or moved (CS0234 - namespace issue)
- `Attribute` - Attribute type missing or moved (CS0234 - namespace issue)

### 2. Namespace Changes (CS0234 errors)

Types that exist but are in different namespaces:
- `Terminal.Gui.Color` - Color moved to different namespace
- `Terminal.Gui.Attribute` - Attribute moved to different namespace

### 3. Affected Files by Component

#### UI Components (30+ files)
- UI/CustomFunctionDialog.cs
- UI/FileActionDialogs.cs
- UI/FileMaskDialog.cs
- UI/DriveDialog.cs
- UI/FileOperationOptionsDialogs.cs
- UI/HelpView.cs
- UI/HistoryDialog.cs
- UI/ImageViewerWindow.cs
- UI/JumpToPathDialog.cs
- UI/JobManagerDialog.cs
- UI/MenuDialog.cs
- UI/MessageLogView.cs
- UI/OperationProgressDialog.cs
- UI/PaneView.cs
- UI/PatternRenameDialog.cs
- UI/RegisteredFolderDialog.cs
- UI/SimpleRenameDialog.cs
- UI/SortDialog.cs
- UI/SystemDialogs.cs
- UI/TabBarView.cs
- UI/TabSelectorDialog.cs
- UI/TaskStatusView.cs
- UI/TextViewerWindow.cs
- UI/VirtualFileView.cs
- UI/WildcardMarkingDialog.cs

#### Controllers
- Controllers/MainController.cs (multiple errors)

#### Utilities
- Utilities/ColorHelper.cs
- Utilities/KeyHelper.cs

### 4. Application Static Methods

From previous grep search, these need to be migrated:
- `Application.Init()` → `Application.Create().Init()`
- `Application.Run()` → `app.Run(window)`
- `Application.Shutdown()` → `app.Dispose()`
- `Application.Top` → `app.TopRunnableView` or `View.App`
- `Application.Driver` → `app.Driver` or `View.App.Driver`
- `Application.MainLoop` → `app.MainLoop` or `View.App.MainLoop`
- `Application.RequestStop()` → Still static (unchanged)

### 5. Dialog Invocations (~40+ locations)

All `Application.Run(dialog)` calls need to be updated to use instance-based pattern.

## Next Steps

1. Research v2 namespace structure
2. Update using statements
3. Migrate Application initialization
4. Update dialog patterns
5. Fix color and attribute references
6. Update event handlers

## Notes

- Most errors are due to namespace changes and missing using statements
- Core architecture changes (Application.Init → Application.Create().Init())
- Dialog pattern changes (need IRunnable<T> for result-returning dialogs)
- Color system moved to different namespace
- Event handling patterns changed

## References

- Terminal.Gui v2 Documentation: https://gui-cs.github.io/Terminal.Gui/docs/newinv2
- NuGet Package: https://www.nuget.org/packages/Terminal.Gui/2.0.0-develop.4819
