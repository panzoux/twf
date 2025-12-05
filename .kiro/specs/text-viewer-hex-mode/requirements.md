# Requirements Document

## Introduction

This feature adds hexadecimal view mode to the TWF text viewer, allowing users to toggle between normal text display and hexadecimal representation of file contents. This is useful for viewing binary files, examining file encoding issues, and debugging file content problems.

## Glossary

- **Text Viewer**: The built-in file viewer for displaying file contents
- **Hex Mode**: Display mode showing file contents as hexadecimal byte values
- **Text Mode**: Normal display mode showing file contents as text
- **Byte**: An 8-bit unit of data, displayed as two hexadecimal digits (00-FF)

## Requirements

### Requirement 1

**User Story:** As a user, I want to toggle between text and hex view modes, so that I can examine file contents in different representations.

#### Acceptance Criteria

1. WHEN the user presses the "B" key in the text viewer THEN the system SHALL toggle between text mode and hex mode
2. WHEN switching to hex mode THEN the system SHALL display file contents as hexadecimal bytes
3. WHEN switching to text mode THEN the system SHALL display file contents as normal text
4. WHEN the view mode changes THEN the system SHALL preserve the current scroll position
5. WHEN the view mode changes THEN the system SHALL update the status bar to indicate the current mode

### Requirement 2

**User Story:** As a user, I want hex mode to display bytes in a readable format, so that I can easily interpret the file contents.

#### Acceptance Criteria

1. WHEN displaying in hex mode THEN the system SHALL show 16 bytes per line
2. WHEN displaying hex bytes THEN the system SHALL format each byte as two uppercase hexadecimal digits
3. WHEN displaying hex lines THEN the system SHALL show the byte offset at the start of each line in 8-digit hexadecimal format
4. WHEN displaying hex lines THEN the system SHALL separate bytes with spaces and add extra space after 8 bytes for readability
5. WHEN displaying hex lines THEN the system SHALL show an ASCII representation of printable characters in a separate column enclosed in pipe characters (|)
6. WHEN displaying non-printable characters in ASCII column THEN the system SHALL show a dot (.) character

### Requirement 3

**User Story:** As a user, I want hex mode to work with all file types, so that I can examine any file in hexadecimal format.

#### Acceptance Criteria

1. WHEN viewing a binary file in hex mode THEN the system SHALL display all bytes correctly
2. WHEN viewing a text file in hex mode THEN the system SHALL display the encoded byte values
3. WHEN viewing an empty file in hex mode THEN the system SHALL display an appropriate message
4. WHEN viewing a large file in hex mode THEN the system SHALL handle it efficiently without loading the entire file into memory

### Requirement 4

**User Story:** As a user, I want the hex mode toggle to be configurable, so that I can customize the keybinding.

#### Acceptance Criteria

1. WHEN the keybindings.json contains a TextViewer.ToggleHexMode binding THEN the system SHALL use that key
2. WHEN no custom binding is defined THEN the system SHALL use "B" as the default key
3. WHEN the hex mode action is triggered THEN the system SHALL log the mode change
4. WHEN hex mode toggle fails THEN the system SHALL display an error message and remain in the current mode
