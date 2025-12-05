# Implementation Plan

- [ ] 1. Create CharacterWidthHelper utility class
  - Create new file Utilities/CharacterWidthHelper.cs
  - Implement GetCharWidth(char c) method with CJK Unicode range checks
  - Implement IsCJKCharacter(char c) helper method
  - Implement IsZeroWidthCharacter(char c) helper method
  - Handle surrogate pairs correctly
  - _Requirements: 3.1, 3.2, 3.3_

- [ ] 2. Implement string width calculation methods
  - Implement GetStringWidth(string text) method
  - Sum character widths for entire string
  - Handle null and empty strings
  - _Requirements: 3.4_

- [ ] 3. Implement string padding method
  - Implement PadToWidth(string text, int targetWidth, char paddingChar) method
  - Calculate current display width
  - Add padding characters to reach target width
  - Handle cases where text is already wider than target
  - _Requirements: 3.5_

- [ ] 4. Implement string truncation method
  - Implement TruncateToWidth(string text, int maxWidth, string ellipsis) method
  - Calculate display width while iterating characters
  - Truncate when max width is reached
  - Add ellipsis if truncated
  - Ensure ellipsis fits within max width
  - _Requirements: 3.5, 4.4_

- [ ] 5. Update PaneView to use CharacterWidthHelper
  - Import CharacterWidthHelper
  - Update RenderFileEntry method to calculate column positions using GetStringWidth
  - Fix filename column width calculation
  - Fix size column positioning
  - Fix date/time column positioning
  - Fix DIR indicator positioning
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 4.1_

- [ ] 6. Update TextViewerWindow to use CharacterWidthHelper
  - Import CharacterWidthHelper
  - Update LoadContentAsText to calculate line widths correctly
  - Update line number column width calculation
  - Handle CJK characters in file content
  - _Requirements: 2.1, 2.2, 2.3, 4.1_

- [ ] 7. Update dialog components to use CharacterWidthHelper
  - Identify dialogs with text alignment issues
  - Update text formatting to use GetStringWidth
  - Update padding to use PadToWidth
  - Update truncation to use TruncateToWidth
  - _Requirements: 4.2_

- [ ] 8. Update status bar components to use CharacterWidthHelper
  - Update status bar text positioning
  - Use GetStringWidth for calculating positions
  - _Requirements: 4.3_

- [ ] 9. Write unit tests for CharacterWidthHelper

  - Test GetCharWidth with ASCII characters (should return 1)
  - Test GetCharWidth with CJK characters (should return 2)
  - Test GetCharWidth with combining characters (should return 0)
  - Test GetStringWidth with mixed ASCII and CJK
  - Test GetStringWidth with empty and null strings
  - Test PadToWidth with various inputs
  - Test TruncateToWidth with various inputs
  - Test surrogate pair handling
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ]* 10. Write property-based tests
  - **Property 1: Width calculation consistency** - _Requirements: 3.4_
  - **Property 2: CJK character width** - _Requirements: 3.1_
  - **Property 3: ASCII character width** - _Requirements: 3.2_
  - **Property 4: Padding correctness** - _Requirements: 3.5_
  - **Property 5: Truncation correctness** - _Requirements: 3.5, 4.4_

- [ ] 11. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
