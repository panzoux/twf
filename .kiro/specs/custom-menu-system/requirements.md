# Requirements Document

## Introduction

This feature adds support for hierarchical custom menus in TWF, allowing users to organize related custom functions into submenus. Custom functions can reference menu files that contain multiple menu items, creating a more organized and navigable interface for complex workflows.

## Glossary

- **Custom Function**: A user-defined action that executes commands or built-in actions
- **Menu File**: A JSON file containing a list of menu items
- **Menu Item**: An entry in a menu that can execute a function or be a separator
- **Separator**: A visual divider in menus (displayed as "-----")
- **Menu Dialog**: A dialog box that displays menu items for user selection
- **Keyboard Navigation**: Using arrow keys and alphabet keys to navigate menus

## Requirements

### Requirement 1

**User Story:** As a user, I want to create menu files that group related functions, so that I can organize my custom functions hierarchically.

#### Acceptance Criteria

1. WHEN a menu file is created in the config directory THEN the system SHALL load it when referenced by a custom function
2. WHEN a menu file contains a Version field THEN the system SHALL validate the version format
3. WHEN a menu file contains a Menus array THEN the system SHALL parse all menu items
4. WHEN a menu file is invalid JSON THEN the system SHALL log an error and skip that menu
5. WHEN a menu file does not exist THEN the system SHALL log an error and treat the custom function as a regular function

### Requirement 2

**User Story:** As a user, I want to reference menu files from custom functions, so that I can create submenu structures.

#### Acceptance Criteria

1. WHEN a custom function has a Menu property THEN the system SHALL treat it as a menu reference instead of a command
2. WHEN a custom function has both Menu and Command properties THEN the system SHALL prioritize the Menu property
3. WHEN a menu-type custom function is executed THEN the system SHALL open a menu dialog
4. WHEN a menu file path is relative THEN the system SHALL look for it in the config directory
5. WHEN a menu file path is absolute THEN the system SHALL use the absolute path

### Requirement 3

**User Story:** As a user, I want menu items to execute functions or actions, so that I can perform operations from menus.

#### Acceptance Criteria

1. WHEN a menu item has a Function property THEN the system SHALL execute that custom function when selected
2. WHEN a menu item has a Menu property THEN the system SHALL execute that built-in action when selected
3. WHEN a menu item Function references a custom function THEN the system SHALL execute that custom function
4. WHEN a menu item Menu references a built-in action THEN the system SHALL execute that action
5. WHEN a menu item has an empty Menu property THEN the system SHALL treat it as non-selectable

### Requirement 4

**User Story:** As a user, I want visual separators in menus, so that I can group related menu items.

#### Acceptance Criteria

1. WHEN a menu item Name is "-----" THEN the system SHALL display it as a separator
2. WHEN a menu item is a separator THEN the system SHALL make it non-selectable
3. WHEN navigating with arrow keys THEN the system SHALL skip over separators
4. WHEN a separator has an empty Menu property THEN the system SHALL not execute any action
5. WHEN displaying a separator THEN the system SHALL render it as a horizontal line

### Requirement 5

**User Story:** As a user, I want to navigate menus with keyboard, so that I can quickly select menu items.

#### Acceptance Criteria

1. WHEN the menu dialog opens THEN the system SHALL focus on the first selectable item
2. WHEN the user presses Up arrow THEN the system SHALL move to the previous selectable item
3. WHEN the user presses Down arrow THEN the system SHALL move to the next selectable item
4. WHEN the user presses a letter key THEN the system SHALL jump to the next item starting with that letter
5. WHEN the user presses Enter THEN the system SHALL execute the selected menu item
6. WHEN the user presses Escape THEN the system SHALL close the menu dialog without executing anything

### Requirement 6

**User Story:** As a user, I want menu items to support macro expansion, so that menu actions can use file context.

#### Acceptance Criteria

1. WHEN a menu item Function contains macros THEN the system SHALL expand them before execution
2. WHEN a menu item is executed THEN the system SHALL have access to current file context
3. WHEN macro expansion fails THEN the system SHALL log an error and display a message
4. WHEN a menu item executes a custom function THEN that function SHALL have access to macro expansion
5. WHEN a menu item executes a built-in action THEN that action SHALL execute normally

### Requirement 7

**User Story:** As a developer, I want clear menu file format documentation, so that I can create menu files easily.

#### Acceptance Criteria

1. WHEN documenting menu files THEN the system SHALL provide the JSON schema
2. WHEN documenting menu files THEN the system SHALL provide examples
3. WHEN documenting menu files THEN the system SHALL explain all properties
4. WHEN documenting menu files THEN the system SHALL show separator syntax
5. WHEN documenting menu files THEN the system SHALL explain the difference between Function and Menu properties
