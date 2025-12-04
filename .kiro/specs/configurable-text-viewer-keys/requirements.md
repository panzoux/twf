# Requirements Document

## Introduction

This feature adds support for configurable keybindings in the TWF text viewer, allowing users to customize all text viewer keyboard shortcuts through the keybindings.json configuration file. Currently, text viewer keys are hardcoded in the TextViewerWindow class.

## Glossary

- **Text Viewer**: The built-in file viewer for displaying text files with encoding support
- **Keybinding**: A mapping between a keyboard key/combination and an action
- **Mode-Specific Binding**: A keybinding that only applies in a specific UI mode (e.g., TextViewer mode)
- **Action**: A named function that can be triggered by a keybinding
- **KeyBindingManager**: The service that manages and resolves keybindings

## Requirements

### Requirement 1

**User Story:** As a user, I want to customize text viewer keybindings, so that I can use my preferred keyboard shortcuts when viewing files.

#### Acceptance Criteria

1. WHEN the keybindings.json file contains TextViewer mode bindings THEN the system SHALL use those bindings in the text viewer
2. WHEN a TextViewer keybinding is defined THEN the system SHALL override the default hardcoded binding
3. WHEN no custom TextViewer binding is defined for a key THEN the system SHALL use the default hardcoded binding
4. WHEN the keybindings file is invalid THEN the system SHALL fall back to default bindings and log an error
5. WHEN the application starts THEN the system SHALL load TextViewer keybindings along with normal mode bindings

### Requirement 2

**User Story:** As a user, I want standard text viewer navigation actions, so that I can efficiently browse file contents.

#### Acceptance Criteria

1. WHEN the user triggers TextViewer.GoToTop THEN the system SHALL scroll to the first line of the file
2. WHEN the user triggers TextViewer.GoToBottom THEN the system SHALL scroll to the last line of the file
3. WHEN the user triggers TextViewer.PageUp THEN the system SHALL scroll up one page
4. WHEN the user triggers TextViewer.PageDown THEN the system SHALL scroll down one page
5. WHEN the user triggers TextViewer.Close THEN the system SHALL close the viewer and return to the file list

### Requirement 3

**User Story:** As a user, I want text viewer search actions, so that I can find content within files.

#### Acceptance Criteria

1. WHEN the user triggers TextViewer.Search THEN the system SHALL display the search dialog
2. WHEN the user triggers TextViewer.FindNext THEN the system SHALL jump to the next search match
3. WHEN the user triggers TextViewer.FindPrevious THEN the system SHALL jump to the previous search match
4. WHEN no search has been performed THEN FindNext and FindPrevious SHALL display a message prompting to search first

### Requirement 4

**User Story:** As a user, I want text viewer encoding actions, so that I can view files in different character encodings.

#### Acceptance Criteria

1. WHEN the user triggers TextViewer.CycleEncoding THEN the system SHALL switch to the next encoding and reload the file
2. WHEN cycling through encodings THEN the system SHALL display the current encoding name in the status bar
3. WHEN encoding change fails THEN the system SHALL keep the previous content and display an error message

### Requirement 5

**User Story:** As a developer, I want a clear keybinding configuration format, so that I can easily customize text viewer keys.

#### Acceptance Criteria

1. WHEN defining TextViewer keybindings THEN the configuration SHALL use the format "TextViewer.ActionName"
2. WHEN the keybindings.json is read THEN the system SHALL support a "textViewerBindings" section
3. WHEN documenting keybindings THEN the system SHALL provide examples of all available TextViewer actions
4. WHEN a TextViewer action name is invalid THEN the system SHALL log a warning and ignore that binding
