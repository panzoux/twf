# Refactoring, Optimization, and UX Logic Log (2026-01-24)

This document summarizes the major refactoring, performance optimizations, and logic improvements completed.

## 1. Complete LINQ Removal
**Goal:** Optimize executable size and improve performance by removing all dependencies on `System.Linq`.

- **Strategy**: A hybrid approach was used.
    - Simple operations (`Any`, `Count`, `Contains`, `Sum`) were replaced with imperative `foreach` loops.
    - Complex chains (`Where().Select().ToList()`) were refactored into direct iterations.
    - Sorting logic was moved from `OrderBy`/`ThenBy` to `List<T>.Sort()` with custom `Comparison<T>` delegates.
    - Deduplication was moved from `.Distinct()` to `HashSet<T>` logic.
- **Project Configuration**:
    - `ImplicitUsings` for `System.Linq` was disabled in `twf.csproj`.
    - Explicit `<Using Remove="System.Linq" />` was added.
    - The `Tests/` directory was excluded from the main project build to maintain LINQ-free core code.

## 2. Search Performance Optimization (Prepared Search Pattern)
**Issue:** Search mode (especially `JumpToFile`) was performing hundreds of redundant Migemo expansions and regex compilations per keystroke.

- **Pattern Introduced**: `PreparedQuery`
- **Implementation**:
    - `SearchEngine.Prepare(string query)` creates a `PreparedQuery` object.
    - Migemo expansion and `Regex` compilation (with `RegexOptions.Compiled`) happen **exactly once** during preparation.
    - High-frequency loops now use `preparedQuery.IsMatch(filename)`.
- **Result**: Massive speedup in search performance and reduced CPU usage.

## 3. Unified Marking Logic and Key Bindings
**Goal:** Centralize marking behavior to adhere to DRY principles and provide consistent user feedback.

- **Key Bindings Added**:
    - `Ctrl+K`: Clear Marks.
    - `Shift+A`: Invert Marks.
- **Refactoring**:
    - Implemented a unified `ExecuteMarkingOperation(MarkingAction action)` in `MainController.cs`.
    - Consolidated `MarkAll`, `ClearMarks`, and `InvertMarks` logic.
    - Refactored `RefreshAndClearMarks` to reuse the unified clear logic.
- **Standardized Behavior**:
    - **Parent Directory Protection**: All marking operations (`MarkAll`, `Invert`) now consistently skip the `..` entry.
    - **Status Feedback**: Every marking operation now updates the status bar with the result (e.g., "Marks inverted (12 marked)").

## 4. Post-Refactoring Bug Fixes
- **NRE in Dialogs**: Moved `ApplyColors()` to end of constructors and added `Application.Driver` null checks.
- **Key Interception**: Added `Application.Current == Application.Top` check to `HandleKeyPress` to prevent main window from stealing keys from modal dialogs.
- **Invisible Focus**: Updated `ColorScheme` to use highlight colors for `Focus` and `HotFocus`. Explicitly applied colors to buttons and text fields to ensure visible selection.

## 5. Pane Synchronization and Swapping Logic
**Goal**: Align synchronization actions with functional intent and unify implementation.

- **Actions Renamed/Added**:
    - `SyncOppositePaneWithCurrentPane`: Synchronizes the inactive pane's path to match the active pane.
    - `SyncCurrentPaneWithOppositePane`: Synchronizes the active pane's path to match the inactive pane.
- **Key Bindings Updated**:
    - `o`: `SyncCurrentPaneWithOppositePane`
    - `Shift+O`: `SyncOppositePaneWithCurrentPane`
    - `Ctrl+O`: `SwapPanes`
- **Refactoring**:
    - Implemented a unified `SyncPanePaths(PaneState source, PaneState target)` helper method in `MainController.cs` to centralize the synchronization logic.
    - Removed redundant code blocks from `ExecuteAction`.

## 6. Removal of Internal Image Viewer and System.Drawing
**Goal**: Reduce binary size and dependency complexity by removing the high-color incompatible internal viewer in favor of an external dedicated tool.

- **Dependency Removed**: `System.Drawing.Common` (removed from `twf.csproj`).
- **Configuration Refactored**:
    - Removed legacy `ImageViewerPath`, `DefaultImageViewMode`, and `ViewMode` enum.
    - Image viewing is now exclusively handled by a custom function named `"ImageViewer"`.
    - `ShowVersionInfo` (F1) now provides instructions on how to configure this function.
- **Text Viewer Enhancements**:
    - Implemented heuristic encoding auto-detection in `LargeFileEngine`.
    - Added support for BOM check, strict UTF-8 state-machine validation, and Japanese (Shift-JIS/EUC-JP) scoring.
    - Removed hardcoded `Encoding.UTF8` overrides in `MainController` to enable auto-detection for all files.
    - Removed redundant `DefaultTextEncoding` configuration; the first item in `EncodingPriority` now acts as the default.
    - Added detailed debug logging for the detection process (visible in Debug output).
    - Made the encoding cycle (F7) and detection order configurable via `EncodingPriority` in `config.json`.
- **External Editor Enhancements**:
    - Refactored `Alt+E` handler (`HandleExecuteFileWithEditor`) to prioritize a custom function named `"Editor"`.
    - Added `ExternalEditorIsGui` root-level configuration to support non-blocking execution for GUI editors.
    - Implemented pane-level locking with a visual overlay ("Editor is running...") and manual `Esc` unlock override for non-blocking mode.
    - Standardized path cleaning (quote removal) across all external execution paths.
- **UI & Modes Cleanup**:
    - Deleted `UI/ImageViewerWindow.cs` and `Tests/ImageViewerWindowTests.cs`.
    - Removed `UiMode.ImageViewer` and associated mode-specific key bindings (`imageViewerBindings`).
    - Updated documentation (`CONFIGURATION.md`) to reflect the new system.
- **Key Handling Improvements**:
    - Refactored `KeyHelper.cs` into modular private methods to adhere to the 30-line method limit and improve readability.
    - Simplified character mapping logic to rely on generic Unicode conversion for special keys (like `¥` and `\`), ensuring cross-platform stability.
    - Cleaned up non-functional key bindings to prevent confusion on systems with ambiguous hardware mapping.

## Files Modified:
- `Controllers/MainController.cs` (Refactoring & marking logic)
- `Services/SearchEngine.cs` (PreparedQuery pattern)
- `Models/Enumerations.cs` (Added MarkingAction enum)
- `UI/*.cs` (Dialog updates for colors and focus)
- `keybindings.json` (New shortcuts)
- `twf.csproj` (LINQ removal configuration)

---

Documentation: Dialog Conversion to Status Updates and Logging

  Goals

  The primary goal of this conversion was to improve user experience by eliminating unnecessary dialog
  interruptions while preserving important error information. Specifically:

   1. Reduce User Interruption: Eliminate popup dialogs that halt user workflow unnecessarily
   2. Maintain Information Visibility: Ensure error messages remain accessible to users
   3. Improve Workflow Continuity: Allow users to continue working without interruption
   4. Consistent Error Reporting: Establish a standardized approach for error messaging

  Overview of UI Components

  TaskStatusView
   - Purpose: Non-modal view for displaying status logs and task progress
   - Location: Bottom of the main window, expandable/collapsible
   - Features:
     - Colored status tags ([OK], [FAIL], [WARN])
     - Scrollable log entries with timestamps
     - Support for different log levels
     - Background job monitoring
     - Persistent log display

  SetStatus Method
   - Function: Wrapper method that sends messages to TaskStatusView
   - Behavior: Automatically timestamps messages and adds them to the task panel
   - Usage: Primary method for sending status updates to users

  MessageBox/Dialog Components
   - MessageDialog: Modal dialog box that interrupts user workflow
   - MessageBox: Various modal dialogs for confirmations and error reporting
   - Issue: Blocks user interaction until dismissed

  Cases Handled

  Converted ShowMessageDialog Calls

  1. Extraction Errors
   - Before: Modal dialog showing detailed extraction errors
   - After: [ERROR] Extraction Errors: <detailed message> logged to task panel
   - Context: Archive extraction failures with multiple possible error messages
   - Benefit: Users can continue browsing while seeing error details in task panel

  2. Compression Errors
   - Before: Modal dialog showing detailed compression errors
   - After: [ERROR] Compression Errors: <detailed message> logged to task panel
   - Context: Archive creation failures with multiple possible error messages
   - Benefit: Workflow continues uninterrupted while errors are logged

  3. Split Errors
   - Before: Modal dialog showing detailed file split errors
   - After: [ERROR] Split Errors: <detailed message> logged to task panel
   - Context: File splitting operation failures
   - Benefit: Users can continue file operations while error details are preserved

  4. Join Errors
   - Before: Modal dialog showing detailed file join errors
   - After: [ERROR] Join Errors: <detailed message> logged to task panel
   - Context: File joining operation failures
   - Benefit: Seamless workflow continuation with error visibility

  Error Message Format

  All converted error messages now follow the format:

   1 [ERROR] <Operation Type> Errors: <detailed error information>

  This format provides:
   - Visual indication of severity via the [ERROR] tag
   - Clear identification of the operation type
   - Preservation of detailed error information
   - Consistency across all error types

  Implementation Details

  Before Conversion

   1 SetStatus($"Operation failed: {result.Message}");
   2 ShowMessageDialog("Operation Errors", detailedErrorMsg);

  After Conversion

   1 SetStatus($"Operation failed: {result.Message}");
   2 SetStatus($"[ERROR] Operation Errors: {detailedErrorMsg}");

  Benefits Achieved

   1. Enhanced User Experience: No more disruptive modal dialogs during operations
   2. Information Retention: All error details preserved in task panel
   3. Workflow Continuity: Users can continue working without interruption
   4. Visual Clarity: Color-coded error indicators in task panel
   5. Consistent Behavior: Standardized error reporting across operations
   6. Reduced Cognitive Load: Less context switching between dialogs and main interface

  Future Considerations

  This conversion establishes a pattern that can be extended to other non-critical MessageBox calls throughout
  the application, further improving the user experience while maintaining robust error reporting capabilities.

