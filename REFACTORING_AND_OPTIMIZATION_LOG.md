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

## Files Modified:
- `Controllers/MainController.cs` (Refactoring & marking logic)
- `Services/SearchEngine.cs` (PreparedQuery pattern)
- `Models/Enumerations.cs` (Added MarkingAction enum)
- `UI/*.cs` (Dialog updates for colors and focus)
- `keybindings.json` (New shortcuts)
- `twf.csproj` (LINQ removal configuration)
