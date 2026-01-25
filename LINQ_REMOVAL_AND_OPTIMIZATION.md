# LINQ Removal and Search Optimization Summary

This document summarizes the major refactoring and optimization work completed on 2026-01-24.

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

## 2. Post-Refactoring Bug Fixes
The massive refactoring introduced several regressions that were subsequently resolved:

### Bug 1: NullReferenceException in Dialogs
- **Cause**: `ApplyColors()` was being called before UI sub-controls (like `_nameField`) were initialized.
- **Fix**: 
    - Moved `ApplyColors()` calls to the end of constructors in all dialog classes.
    - Added null checks for `Application.Driver` in all `ApplyColors` methods to prevent crashes during early initialization.

### Bug 2 & 3: Non-functional Keys and RadioButtons
- **Cause**: `MainController.HandleKeyPress` was attached to the main window and panes, intercepting and consuming global keys even when modal dialogs were open.
- **Fix**: Added a top-level focus check in `HandleKeyPress`:
  ```csharp
  if (Application.Current != Application.Top) return;
  ```
  This ensures that global shortcuts only fire when the main application is active, allowing unbound keys to bubble up correctly to modal dialogs.

### Bug 4: Invisible Focus in Dialogs
- **Cause**: The `ColorScheme` applied to dialogs used the same colors for `Normal` and `Focus` states.
- **Fix**: 
    - Updated `ColorScheme` logic to use `HighlightForegroundColor` and `HighlightBackgroundColor` for `Focus` and `HotFocus` states.
    - Promoted local button variables to private fields in various dialogs to allow explicit `ColorScheme` application.
    - Set specific colors according to user requirements:
        - **Buttons**: Black/Gray (Normal), White/DarkGray (Focus).
        - **Hotkeys**: Cyan/Gray (Normal), Yellow/DarkGray (Focus).
        - **TextBox**: White/DarkGray.

## 3. Search Performance Optimization (Prepared Search Pattern)
**Issue:** Search mode (especially `JumpToFile`) was performing hundreds of redundant Migemo expansions and regex compilations per keystroke (once per file checked).

- **Pattern Introduced**: `PreparedQuery`
- **Implementation**:
    - `SearchEngine.Prepare(string query)` creates a `PreparedQuery` object.
    - Migemo expansion and `Regex` compilation (with `RegexOptions.Compiled`) happen **exactly once** during preparation.
    - The high-frequency search loops in `JumpToFileDialog` and `JumpToPathDialog` now use `preparedQuery.IsMatch(filename)`.
- **Result**: Significant reduction in CPU usage and elimination of redundant Migemo library calls, resulting in "super fast" search performance even in directories with thousands of files.

## Files Modified:
- `Controllers/MainController.cs` (Extensive refactoring)
- `Services/SearchEngine.cs` (PreparedQuery implementation)
- `UI/*.cs` (All dialogs updated for colors, focus, and NRE fixes)
- `twf.csproj` (Configuration changes)
- Various `Services/` and `Providers/` (LINQ removal)
