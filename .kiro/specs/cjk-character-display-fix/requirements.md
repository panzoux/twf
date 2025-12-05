# Requirements Document

## Introduction

This feature fixes display alignment issues when CJK (Chinese, Japanese, Korean) characters are present in file names or file content. CJK characters are double-width characters that occupy two character cells in terminal display, but the current implementation treats them as single-width, causing misalignment of columns and UI elements.

## Glossary

- **CJK Characters**: Chinese, Japanese, and Korean characters that occupy two character cells in terminal display
- **Double-Width Character**: A character that occupies two character cells in monospace terminal display
- **Single-Width Character**: A character that occupies one character cell (ASCII, Latin characters)
- **Character Width**: The number of terminal cells a character occupies when displayed
- **File Pane**: The left and right panels displaying directory contents

## Requirements

### Requirement 1

**User Story:** As a user with CJK filenames, I want proper column alignment in file panes, so that file information is displayed correctly.

#### Acceptance Criteria

1. WHEN displaying a filename containing CJK characters THEN the system SHALL calculate the display width correctly
2. WHEN displaying file information columns THEN the system SHALL align columns based on character display width
3. WHEN displaying the file size column THEN the system SHALL position it correctly regardless of CJK characters in the filename
4. WHEN displaying the date/time column THEN the system SHALL position it correctly regardless of CJK characters in the filename
5. WHEN displaying the DIR indicator THEN the system SHALL position it correctly regardless of CJK characters in the filename

### Requirement 2

**User Story:** As a user viewing files with CJK content, I want proper text display in the text viewer, so that content is readable and properly formatted.

#### Acceptance Criteria

1. WHEN displaying CJK characters in text viewer THEN the system SHALL calculate line lengths correctly
2. WHEN displaying CJK characters in text viewer THEN the system SHALL handle line wrapping based on display width
3. WHEN displaying mixed ASCII and CJK text THEN the system SHALL calculate total display width correctly
4. WHEN displaying CJK characters in hex mode THEN the system SHALL show the correct byte representation

### Requirement 3

**User Story:** As a developer, I want a utility function for character width calculation, so that all UI components can handle CJK characters consistently.

#### Acceptance Criteria

1. WHEN calculating character width THEN the system SHALL return 2 for CJK characters
2. WHEN calculating character width THEN the system SHALL return 1 for ASCII and Latin characters
3. WHEN calculating character width THEN the system SHALL return 0 for combining characters and zero-width characters
4. WHEN calculating string display width THEN the system SHALL sum the widths of all characters
5. WHEN padding strings for display THEN the system SHALL account for character display widths

### Requirement 4

**User Story:** As a user, I want consistent CJK character handling across all UI components, so that the application works correctly with international filenames.

#### Acceptance Criteria

1. WHEN displaying file lists THEN the system SHALL use character width calculation for all formatting
2. WHEN displaying dialogs THEN the system SHALL use character width calculation for text alignment
3. WHEN displaying status bars THEN the system SHALL use character width calculation for positioning
4. WHEN truncating long filenames THEN the system SHALL account for character display widths
