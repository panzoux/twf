# Fix Verification: JumpToPath CPU Usage

## Issue
The `JumpToPathDialog` (Jump to Directory) was triggering a search operation on every key press event, even if the search text did not change (e.g., when pressing navigation keys like Up/Down arrows). This caused unnecessary CPU usage and potential lag.

## Fix
Implemented a check in `TriggerSearch` to compare the current sanitized query with the last processed query. If they are identical, the search is skipped.

## Verification Steps (Manual)
1. Open TWF.
2. Press the key binding for "Jump to Directory" (usually `g` or similar, check keybindings).
3. Type a path or partial name to trigger a search.
4. Press the **Up** and **Down** arrow keys to navigate the suggestion list.
5. **Observation:** 
   - Before fix: The list might flicker or you might see CPU spikes as it re-searches on every arrow key press.
   - After fix: The navigation should be smooth, and the search should NOT be re-triggered (no spinner reset or list refresh) when just moving the selection.
6. Type a new character.
7. **Observation:** The search should update correctly.

## Code Change
File: `UI/JumpToPathDialog.cs`
- Added `private string _lastSearchQuery = "INIT_VALUE";`
- Updated `TriggerSearch` to:
  ```csharp
  string cleanQuery = SanitizeInput(query);
  if (cleanQuery == _lastSearchQuery) return;
  _lastSearchQuery = cleanQuery;
  ```
