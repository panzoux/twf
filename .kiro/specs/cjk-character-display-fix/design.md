# Design Document

## Overview

This fix introduces proper character width calculation for CJK (double-width) characters throughout the application. The solution creates a centralized utility class for calculating display widths and updates all UI components to use it for proper alignment and formatting.

## Architecture

The fix introduces a new utility service and updates existing UI components:

- **CharacterWidthHelper**: New utility class for character width calculations
- **PaneView**: Updated to use character width for column alignment
- **TextViewerWindow**: Updated to use character width for text display
- **All Dialog Components**: Updated to use character width for text formatting

## Components and Interfaces

### CharacterWidthHelper (New Utility Class)

```csharp
public static class CharacterWidthHelper
{
    // Get display width of a single character
    public static int GetCharWidth(char c);
    
    // Get display width of a string
    public static int GetStringWidth(string text);
    
    // Pad string to specific display width
    public static string PadToWidth(string text, int targetWidth, char paddingChar = ' ');
    
    // Truncate string to fit within display width
    public static string TruncateToWidth(string text, int maxWidth, string ellipsis = "...");
    
    // Check if character is CJK
    private static bool IsCJKCharacter(char c);
    
    // Check if character is combining/zero-width
    private static bool IsZeroWidthCharacter(char c);
}
```

### PaneView Updates

```csharp
public class PaneView : View
{
    // Update RenderFileEntry to use CharacterWidthHelper
    private void RenderFileEntry(FileEntry entry, int y, bool isSelected);
    
    // Calculate proper column positions based on character widths
    private int CalculateColumnPosition(string text, int basePosition);
}
```

### TextViewerWindow Updates

```csharp
public class TextViewerWindow : Window
{
    // Update LoadContentAsText to handle CJK character widths
    private void LoadContentAsText();
    
    // Calculate line display width
    private int CalculateLineWidth(string line);
}
```

## Data Models

### Character Width Categories

```csharp
enum CharacterWidthCategory
{
    SingleWidth = 1,  // ASCII, Latin
    DoubleWidth = 2,  // CJK characters
    ZeroWidth = 0     // Combining marks, zero-width joiners
}
```

### CJK Unicode Ranges

The implementation will check these Unicode ranges for CJK characters:
- CJK Unified Ideographs: U+4E00 to U+9FFF
- CJK Extension A: U+3400 to U+4DBF
- CJK Extension B: U+20000 to U+2A6DF
- Hiragana: U+3040 to U+309F
- Katakana: U+30A0 to U+30FF
- Hangul Syllables: U+AC00 to U+D7AF
- Fullwidth Forms: U+FF00 to U+FFEF

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Width calculation consistency
*For any* string, the sum of individual character widths should equal the total string width
**Validates: Requirements 3.4**

### Property 2: CJK character width
*For any* character in CJK Unicode ranges, GetCharWidth should return 2
**Validates: Requirements 3.1**

### Property 3: ASCII character width
*For any* ASCII character (0x00-0x7F), GetCharWidth should return 1
**Validates: Requirements 3.2**

### Property 4: Padding correctness
*For any* string and target width, PadToWidth should produce a string with display width equal to target width
**Validates: Requirements 3.5**

### Property 5: Truncation correctness
*For any* string and max width, TruncateToWidth should produce a string with display width less than or equal to max width
**Validates: Requirements 3.5, 4.4**

### Property 6: Column alignment preservation
*For any* file entry with CJK characters, the date/time column should start at the same display position as entries without CJK characters
**Validates: Requirements 1.2, 1.3, 1.4, 1.5**

## Error Handling

- **Invalid Unicode**: Treat as single-width character
- **Null or empty strings**: Return width of 0
- **Negative target widths**: Throw ArgumentException
- **Surrogate pairs**: Handle correctly as single logical character

## Testing Strategy

### Unit Tests

- Test GetCharWidth with ASCII characters
- Test GetCharWidth with CJK characters
- Test GetCharWidth with combining characters
- Test GetStringWidth with mixed content
- Test PadToWidth with various inputs
- Test TruncateToWidth with various inputs
- Test edge cases (empty strings, null, surrogate pairs)

### Property-Based Tests

We will use CsCheck for property-based testing in C#.

Each property-based test will run a minimum of 100 iterations.

Property-based tests will be tagged with comments referencing the design document properties:
- Format: `**Feature: cjk-character-display-fix, Property {number}: {property_text}**`

### Integration Tests

- Test file pane display with CJK filenames
- Test text viewer with CJK content
- Test dialog display with CJK text
- Test column alignment with various filename lengths
