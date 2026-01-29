# Binary Viewer Search Implementation

## Overview
This document describes the implementation of the advanced Hybrid Search system for the TWF Binary (Hex) Viewer. The system allows users to search across three data domains simultaneously: Address Space (offsets), Hex Byte Space, and ASCII Space.

## Features

### 1. Multi-Domain Hybrid Search
The search query is evaluated against multiple contexts in a single pass:
- **Address Search**: Finds partial or full matches in the hex representation of the file offset (e.g., searching "1A0" finds the row at `000001A0`).
- **Hex Byte Search**: Finds sequences of hex bytes. Supports spaced or continuous input (e.g., `41 42` or `4142`).
- **ASCII Search**: If the query is not a valid hex string, it automatically searches the ASCII representation of the file bytes.

### 2. Context-Aware Highlighting
Visual feedback is precisely targeted based on the match type:
- **Address Match**: Highlights the match within the Offset column only.
- **Data Match**: Highlights the match within both the Hex and ASCII columns.

### 3. Cross-Row Highlighting (Wrapping)
Matches that span across the 16-byte row boundaries are handled naturally:
- Highlighting is calculated using absolute byte offsets.
- Continuous matches wrap from the end of one line to the beginning of the next.
- All visible occurrences of the pattern on the screen are highlighted (passive highlighting).

### 4. High-Performance Asynchronous Engine
- Uses an `async` search loop in `LargeFileEngine`.
- Memory-efficient scanning using 1MB buffers with overlap handling for cross-buffer matches.
- Minimal allocations in tight loops (avoids LINQ and redundant string generation).

## Technical Implementation

### Data Models
Introduced `HexSearchResult` and `HexMatchType` to track the nature of search hits:
```csharp
public enum HexMatchType { None, Address, Data }
public struct HexSearchResult {
    public long Offset;
    public HexMatchType MatchType;
}
```

### Engine Logic (`LargeFileEngine.cs`)
- **`FindNextHexAsync`**: Orchestrates the dual-space scan.
- **`IsValidHexQuery`**: Determines if a query should be treated as hex bytes or ASCII text.
- **`IsByteInMatch`**: Optimized helper for the UI to determine if a specific byte should be highlighted based on intersection with match ranges.

### UI Logic (`VirtualFileView.cs` & `TextViewerWindow.cs`)
- **`DrawHexMode`**: Refactored to iterate through absolute offsets and apply conditional attributes to the Terminal.Gui driver.
- **Coordinate Translation**: Automatically converts between byte offsets (internal) and row indices (UI).

## Usage
1. Open a file in the Text Viewer (**V**).
2. Switch to Binary Mode (**F8** or **B**).
3. Start Search (**F4** or **/**).
4. Enter an address (e.g., `000045`), hex bytes (e.g., `EB FE`), or text (e.g., `MZ`).
5. Use **n** / **N** to navigate matches.
