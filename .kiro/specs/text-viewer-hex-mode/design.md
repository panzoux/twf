# Design Document

## Overview

This feature adds hexadecimal view mode to the text viewer by introducing a view mode toggle and hex formatting logic. The implementation will extend the existing TextViewerWindow class with a new display mode that formats file bytes as hexadecimal values with ASCII representation.

## Architecture

The hex mode feature integrates into the existing text viewer architecture:

- **TextViewerWindow**: Extended with hex mode toggle and display logic
- **TextViewer**: Provides raw byte access for hex formatting
- **KeyBindingManager**: Handles the ToggleHexMode action binding

## Components and Interfaces

### TextViewerWindow Extensions

```csharp
public class TextViewerWindow : Window
{
    private bool _isHexMode = false;
    
    // New action method
    private void ToggleHexMode();
    
    // New display methods
    private void LoadContentAsText();
    private void LoadContentAsHex();
    private string FormatHexLine(int offset, byte[] bytes);
}
```

### TextViewer Extensions

```csharp
public class TextViewer
{
    // New method to get raw bytes
    public byte[] GetBytes(int startOffset, int length);
    public byte[] GetAllBytes();
}
```

## Data Models

### Hex Display Format

Each line in hex mode follows this format:
```
AAAAAAAA  HH HH HH HH HH HH HH HH  HH HH HH HH HH HH HH HH  |CCCCCCCCCCCCCCCC|
```

Where:
- `AAAAAAAA`: 8-digit hexadecimal offset (e.g., 00000000, 00000010)
- `HH`: Two-digit hexadecimal byte value (00-FF)
- Extra space after 8th byte for visual grouping
- `C`: ASCII character (printable) or `.` (non-printable)
- Pipe characters `|` enclose the ASCII column

### View Mode State

```csharp
enum ViewMode
{
    Text,
    Hex
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Mode toggle consistency
*For any* text viewer instance, toggling hex mode twice should return to the original display mode
**Validates: Requirements 1.1, 1.3**

### Property 2: Byte preservation
*For any* file content, the bytes displayed in hex mode should match the actual file bytes when read directly
**Validates: Requirements 3.1, 3.2**

### Property 3: Hex format correctness
*For any* line in hex mode, the format should match the pattern: 8-digit offset, 16 hex bytes (with space after 8th), and 16 ASCII characters enclosed in pipes
**Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**

### Property 4: Non-printable character handling
*For any* byte value less than 0x20 or greater than 0x7E, the ASCII column should display a dot (.)
**Validates: Requirements 2.6**

### Property 5: Scroll position preservation
*For any* scroll position before mode toggle, the viewer should maintain approximately the same position after toggle
**Validates: Requirements 1.4**

## Error Handling

- **File read errors**: Display error message and remain in current mode
- **Memory constraints**: For very large files, read and format in chunks
- **Invalid byte access**: Handle gracefully with error logging
- **Mode toggle failures**: Log error and keep current mode active

## Testing Strategy

### Unit Tests

- Test hex line formatting with various byte patterns
- Test ASCII character conversion (printable vs non-printable)
- Test offset calculation for different file positions
- Test mode toggle state transitions
- Test empty file handling

### Property-Based Tests

We will use CsCheck for property-based testing in C#.

Each property-based test will run a minimum of 100 iterations.

Property-based tests will be tagged with comments referencing the design document properties:
- Format: `**Feature: text-viewer-hex-mode, Property {number}: {property_text}**`

### Integration Tests

- Test hex mode with various file types (text, binary, empty)
- Test keybinding integration
- Test with different encodings
- Test scroll position preservation across mode changes
