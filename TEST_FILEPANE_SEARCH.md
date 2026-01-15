# Test Plan: File Pane Search History & Navigation

## Objective
Verify that File Pane incremental search supports "Up/Down" for match navigation and "Ctrl+P/Ctrl+N" for search history navigation.

## Pre-requisites
1.  Open TWF.
2.  Navigate to a folder with multiple files (e.g., source root).

## Test Steps

1.  **Search & Save History:**
    *   Press `F` to enter search mode.
    *   Type a pattern (e.g., "json").
    *   *Verify:* Cursor jumps to matching file.
    *   Press `Enter`.
    *   *Verify:* Search mode exits. Pattern "json" should be saved to history.

2.  **Verify History (Ctrl+P):**
    *   Press `F` to enter search mode.
    *   Press `Ctrl+P`.
    *   *Verify:* Input changes to "json" (or last used pattern).
    *   *Verify:* Cursor jumps to matching file for "json".
    *   Press `Ctrl+K` to clear.
    *   *Verify:* Input cleared.

3.  **Verify Multiple History Items:**
    *   (If cleared) Type "md" and press `Enter`.
    *   Press `F`.
    *   Press `Ctrl+P`. *Verify:* "md".
    *   Press `Ctrl+P`. *Verify:* "json".
    *   Press `Ctrl+N`. *Verify:* "md".
    *   Press `Ctrl+N`. *Verify:* Empty (cleared).

4.  **Verify Match Navigation (Up/Down):**
    *   Press `F`.
    *   Type a pattern with multiple matches (e.g. "t" or ".").
    *   Press `Down`. *Verify:* Moves to next match.
    *   Press `Up`. *Verify:* Moves to previous match.

5.  **Exit:**
    *   Press `Esc`.
