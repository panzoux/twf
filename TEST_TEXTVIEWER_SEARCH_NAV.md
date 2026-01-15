# Test Plan: Text Viewer Search Navigation

## Objective
Verify that "Up" and "Down" keys find the previous and next matches respectively while in search mode in the Text Viewer.

## Pre-requisites
1.  Open TWF.
2.  Open a text file (e.g., `MANUAL.md` or any file with repeated text).

## Test Steps

1.  **Enter Search Mode:**
    *   Press `F4` or whatever key opens the search mode in Text Viewer (default might be `F4` or `/`).
    *   *Verify:* Status bar shows "Search:".

2.  **Type Search Pattern:**
    *   Type a common word (e.g., "file").
    *   *Verify:* The first occurrence is highlighted and jumped to.

3.  **Find Next:**
    *   Press `Down` arrow key.
    *   *Verify:* The view jumps to the *next* occurrence of the word "file".
    *   *Verify:* Status shows "(next found)".

4.  **Find Previous:**
    *   Press `Up` arrow key.
    *   *Verify:* The view jumps to the *previous* occurrence (back to the first one).
    *   *Verify:* Status shows "(previous found)".

5.  **Wrap Around / Not Found:**
    *   Keep pressing `Down` until no more matches.
    *   *Verify:* Status shows "(not found)" when end is reached (unless wrap-around is implemented in engine, but `FindNextAsync` implementation in `LargeFileEngine` stops at end).

6.  **Search History:**
    *   Press `Ctrl+K` to clear current input.
    *   Press `Ctrl+P` (Previous History).
    *   *Verify:* Previous search term appears.
    *   Press `Ctrl+N` (Next History).
    *   *Verify:* Newer search term appears (if any).
    *   Press `Up` / `Down` with a history term loaded.
    *   *Verify:* It searches for that history term's next/prev match.

7.  **Exit Search:**
    *   Press `Esc` or `Enter`.
    *   *Verify:* Search mode exits.
