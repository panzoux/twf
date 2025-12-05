# Implementation Plan

- [x] 1. Extend TextViewer service with byte access methods


  - Add GetBytes(int startOffset, int length) method to read raw bytes
  - Add GetAllBytes() method for complete file access
  - Handle file read errors gracefully
  - _Requirements: 3.1, 3.2, 3.3_



- [ ] 2. Add hex mode state and formatting to TextViewerWindow
  - Add _isHexMode boolean field to track current mode
  - Implement FormatHexLine(int offset, byte[] bytes) method
  - Implement FormatHexByte(byte b) helper method
  - Implement FormatAsciiChar(byte b) helper method


  - Handle lines with fewer than 16 bytes (end of file)
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ] 3. Implement hex content loading
  - Create LoadContentAsHex() method

  - Read file bytes in chunks for memory efficiency
  - Format each 16-byte chunk as a hex line
  - Build complete hex display content
  - _Requirements: 2.1, 3.4_



- [ ] 4. Refactor text content loading
  - Rename LoadContent() to LoadContentAsText()
  - Keep existing text display logic
  - _Requirements: 1.3_

- [x] 5. Implement mode toggle action


  - Add ToggleHexMode() action method
  - Store current scroll position before toggle
  - Switch between LoadContentAsText() and LoadContentAsHex()
  - Restore scroll position after toggle


  - Update status bar to show current mode
  - Log mode changes
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_


- [ ] 6. Register ToggleHexMode action in ExecuteTextViewerAction
  - Add case for "TextViewer.ToggleHexMode"
  - Call ToggleHexMode() method
  - Handle action execution errors
  - _Requirements: 1.1, 4.3_



- [ ] 7. Update keybindings configuration
  - Add "B": "TextViewer.ToggleHexMode" to textViewerBindings in keybindings.json
  - Document the hex mode toggle action
  - _Requirements: 4.1, 4.2_

- [ ] 8. Add error handling for hex mode
  - Handle file read errors during hex display
  - Handle memory constraints for large files
  - Display error messages in status bar
  - Log errors appropriately
  - _Requirements: 3.4, 4.4_

- [ ] 9. Update documentation
  - Document hex mode feature in CONFIGURATION.md
  - Add examples of hex display format
  - Document the ToggleHexMode action
  - _Requirements: 4.1_




- [ ]* 10. Write unit tests for hex formatting
  - Test FormatHexLine with full 16-byte lines
  - Test FormatHexLine with partial lines (< 16 bytes)
  - Test ASCII character conversion for printable characters
  - Test ASCII character conversion for non-printable characters (should be '.')
  - Test offset formatting (8-digit hex)
  - Test empty byte array handling
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [ ]* 11. Write property-based tests
  - **Property 1: Mode toggle consistency** - _Requirements: 1.1, 1.3_
  - **Property 2: Byte preservation** - _Requirements: 3.1, 3.2_
  - **Property 3: Hex format correctness** - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
  - **Property 4: Non-printable character handling** - _Requirements: 2.6_

- [ ] 12. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
