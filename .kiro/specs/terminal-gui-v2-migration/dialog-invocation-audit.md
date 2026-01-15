# Dialog Invocation Audit

**Date**: 2026-01-15  
**Terminal.Gui Version**: 2.0.0-develop.4819  
**Target Framework**: .NET 10.0

## Summary

This document identifies all dialog invocations in the TWF codebase and categorizes them by return value type for migration planning.

## Dialog Classification

### Simple Dialogs (No Return Values)
Dialogs that perform actions but don't return data to the caller.

1. **MessageDialog** - Simple message display
2. **HelpDialog** - Application help display
3. **FilePropertiesDialog** - File information display
4. **JobManagerDialog** - Background job management
5. **OperationProgressDialog** - Progress display (blocking)

### Result-Returning Dialogs
Dialogs that return data or user selections to the caller.

1. **ConfirmationDialog** - Returns: `bool Confirmed`
2. **ContextMenuDialog** - Returns: `MenuItem? SelectedItem`
3. **CustomFunctionDialog** - Returns: `CustomFunction? SelectedFunction`
4. **SortDialog** - Returns: `SortMode SelectedMode`, `bool IsOk`
5. **FileMaskDialog** - Returns: `string Mask`, `bool IsOk`
6. **SimpleRenameDialog** - Returns: `string NewName`, `bool IsOk`
7. **PatternRenameDialog** - Returns: `string Pattern`, `bool IsOk`
8. **WildcardMarkingDialog** - Returns: `string Pattern`, `bool IsOk`, `bool IsRegex`
9. **JumpToPathDialog** - Returns: `string Path`, `bool IsOk`
10. **RegisterFolderDialog** - Returns: `string Name`, `string Path`, `bool IsOk`
11. **CreateDirectoryDialog** - Returns: `string DirectoryName`, `bool IsOk`
12. **CreateNewFileDialog** - Returns: `string FileName`, `bool IsOk`
13. **DriveDialog** - Returns: `DriveInfo? SelectedDrive`
14. **HistoryDialog** - Returns: callback-based selection
15. **RegisteredFolderDialog** - Returns: callback-based navigation
16. **TabSelectorDialog** - Returns: `int SelectedTabIndex`, `bool IsJumped`
17. **CompressionOptionsDialog** - Returns: `string ArchiveName`, `string Format`, `bool IsOk`
18. **FileSplitOptionsDialog** - Returns: `long PartSize`, `string OutputDir`, `bool IsOk`
19. **FileJoinOptionsDialog** - Returns: `string OutputFile`, `bool IsOk`
20. **FileComparisonDialog** - Returns: `ComparisonCriteria Criteria`, `TimeSpan TimestampTolerance`, `bool IsOk`
21. **FileCollisionDialog** - Returns: `FileCollisionResult Result`
22. **RenameConflictDialog** - Returns: `string NewName`, `bool IsOk`

### Viewer Windows
Special dialogs that display content and handle their own interactions.

1. **TextViewerWindow** - Text file viewer
2. **ImageViewerWindow** - Image file viewer
3. **HelpView** - Help system viewer

## Dialog Invocation Locations

### Controllers/MainController.cs (Primary Location)

#### Simple Dialogs (5 invocations)
1. **Line 1333**: `JobManagerDialog` - ShowJobManager action
2. **Line 4339**: `MessageDialog` - ShowMessageDialog helper
3. **Line 4314**: `OperationProgressDialog` - ExecuteFileOperation helper
4. **Line 5458**: `OperationProgressDialog` - CompressFiles operation
5. **Line 5907**: `OperationProgressDialog` - ExtractArchive operation
6. **Line 6038**: `OperationProgressDialog` - SplitFile operation
7. **Line 6299**: `FilePropertiesDialog` - ShowFileProperties action

#### Result-Returning Dialogs (35 invocations)
1. **Line 1963**: `TabSelectorDialog` - ShowTabSelector action
2. **Line 2512**: `OperationProgressDialog` - CopyFiles operation
3. **Line 2824**: `DriveDialog` - ChangeDrive action
4. **Line 2850**: `CustomFunctionDialog` - ShowCustomFunctions action
5. **Line 2927**: `RegisterFolderDialog` - RegisterFolder action
6. **Line 3070**: `HistoryDialog` - ShowHistory action
7. **Line 3670**: `WildcardMarkingDialog` - MarkByWildcard action
8. **Line 4057**: `SimpleRenameDialog` - RenameFile action
9. **Line 4126**: `PatternRenameDialog` - BatchRename action
10. **Line 4206**: `FileComparisonDialog` - CompareFiles action
11. **Line 4252**: `ConfirmationDialog` - ShowConfirmation helper
12. **Line 4327**: `FileCollisionDialog` - HandleFileCollision helper
13. **Line 4355**: `CreateDirectoryDialog` - CreateDirectory action
14. **Line 4444**: `CreateNewFileDialog` - CreateNewFile action
15. **Line 4660**: `SortDialog` - ShowSortDialog action
16. **Line 4710**: `FileMaskDialog` - SetFileMask action
17. **Line 4845**: `JumpToPathDialog` - JumpToPath action
18. **Line 4903**: `HistoryDialog` - ShowHistory action (duplicate)
19. **Line 5338**: `CompressionOptionsDialog` - CompressFiles operation
20. **Line 5923**: `FileSplitOptionsDialog` - SplitFile operation
21. **Line 6103**: `FileJoinOptionsDialog` - JoinFiles operation
22. **Line 6142**: `ContextMenuDialog` - ShowContextMenu action
23. **Line 6523**: `HistoryDialog` - ShowHistory action (duplicate)

#### Viewer Windows (6 invocations)
1. **Line 3385**: `TextViewerWindow` - ViewFile action
2. **Line 3439**: `ImageViewerWindow` - ViewFile action (image)
3. **Line 4958**: `HelpView` - ShowHelp action
4. **Line 5126**: `ImageViewerWindow` - QuickView action (image)
5. **Line 5186**: `TextViewerWindow` - QuickView action (text)
6. **Line 5241**: `TextViewerWindow` - QuickView action (hex mode)

### Services/MacroExpander.cs (1 invocation)
1. **Line 369**: Custom input dialog - ShowInputDialog method

### Services/CustomFunctionManager.cs (1 invocation)
1. **Line 327**: `MenuDialog` - ExecuteFunction method

### UI/FileOperationOptionsDialogs.cs (1 invocation)
1. **Line 306**: `RenameConflictDialog` - Nested dialog in FileCollisionDialog

## Migration Checklist

### Phase 3: Dialog Execution Pattern Migration

#### Task 14: Simple Dialogs (7 items)
- [ ] MessageDialog (1 invocation)
- [ ] HelpDialog (0 direct invocations - used via HelpView)
- [ ] FilePropertiesDialog (1 invocation)
- [ ] JobManagerDialog (1 invocation)
- [ ] OperationProgressDialog (4 invocations)

#### Task 15: Result-Returning Dialogs (22 items)
- [ ] ConfirmationDialog (1 invocation)
- [ ] ContextMenuDialog (1 invocation)
- [ ] CustomFunctionDialog (1 invocation)
- [ ] SortDialog (1 invocation)
- [ ] FileMaskDialog (1 invocation)
- [ ] SimpleRenameDialog (1 invocation)
- [ ] PatternRenameDialog (1 invocation)
- [ ] WildcardMarkingDialog (1 invocation)
- [ ] JumpToPathDialog (1 invocation)
- [ ] RegisterFolderDialog (1 invocation)
- [ ] CreateDirectoryDialog (1 invocation)
- [ ] CreateNewFileDialog (1 invocation)
- [ ] DriveDialog (1 invocation)
- [ ] HistoryDialog (3 invocations)
- [ ] RegisteredFolderDialog (0 direct invocations - used via RegisterFolderDialog)
- [ ] TabSelectorDialog (1 invocation)
- [ ] CompressionOptionsDialog (1 invocation)
- [ ] FileSplitOptionsDialog (1 invocation)
- [ ] FileJoinOptionsDialog (1 invocation)
- [ ] FileComparisonDialog (1 invocation)
- [ ] FileCollisionDialog (1 invocation)
- [ ] RenameConflictDialog (1 invocation - nested)

#### Task 17: Viewer Windows (6 items)
- [ ] TextViewerWindow (3 invocations)
- [ ] ImageViewerWindow (2 invocations)
- [ ] HelpView (1 invocation)

#### Task 18: MacroExpander Dialog (1 item)
- [ ] Custom input dialog in MacroExpander.ShowInputDialog

#### Task 19: CustomFunctionManager Dialog (1 item)
- [ ] MenuDialog in CustomFunctionManager.ExecuteFunction

## Total Dialog Invocations: 47

### By Category
- Simple Dialogs: 7
- Result-Returning Dialogs: 34
- Viewer Windows: 6

### By Location
- MainController.cs: 44
- MacroExpander.cs: 1
- CustomFunctionManager.cs: 1
- FileOperationOptionsDialogs.cs: 1 (nested)

## Migration Strategy

### 1. Create Helper Methods (Task 13)
```csharp
// In MainController
private void RunDialog(Dialog dialog)
{
    try
    {
        _app?.Run(dialog);
    }
    finally
    {
        dialog?.Dispose();
    }
}

private TResult? RunDialog<TResult>(Dialog dialog, Func<Dialog, TResult> resultExtractor)
{
    try
    {
        _app?.Run(dialog);
        return resultExtractor(dialog);
    }
    finally
    {
        dialog?.Dispose();
    }
}
```

### 2. Update Simple Dialogs (Task 14)
Replace:
```csharp
Application.Run(new MessageDialog(title, message));
```

With:
```csharp
RunDialog(new MessageDialog(title, message));
```

### 3. Update Result-Returning Dialogs (Task 15)
Replace:
```csharp
var dialog = new SortDialog(activePane.SortMode, paneTitle);
Application.Run(dialog);
if (dialog.IsOk) { ... }
```

With:
```csharp
var dialog = new SortDialog(activePane.SortMode, paneTitle);
RunDialog(dialog);
if (dialog.IsOk) { ... }
```

### 4. Update Viewer Windows (Task 17)
Replace:
```csharp
var viewerWindow = new TextViewerWindow(...);
Application.Run(viewerWindow);
```

With:
```csharp
var viewerWindow = new TextViewerWindow(...);
RunDialog(viewerWindow);
```

### 5. Update Service Classes (Tasks 18-19)
Pass `_app` instance to services:
```csharp
// In MainController constructor
_macroExpander = new MacroExpander(_app);
_customFunctionManager = new CustomFunctionManager(..., _app);
```

Then in services:
```csharp
_app?.Run(dialog);
```

## Notes

- All dialogs inherit from `Dialog` base class
- Most dialogs use `IsOk` property to indicate user confirmation
- Some dialogs use callbacks instead of return values (HistoryDialog, RegisteredFolderDialog)
- OperationProgressDialog is used in multiple contexts (copy, compress, extract, split)
- Nested dialogs (RenameConflictDialog in FileCollisionDialog) need special handling
- Viewer windows are technically dialogs but have more complex lifecycle
- Application.RequestStop() calls within dialogs remain unchanged (static method)

## Next Steps

1. Complete Phase 1 Task 5: Checkpoint review
2. Begin Phase 2: Core Application Lifecycle Migration
3. Implement dialog helper methods in MainController
4. Systematically migrate all 47 dialog invocations
5. Test each dialog type after migration
6. Verify proper disposal of all dialogs
