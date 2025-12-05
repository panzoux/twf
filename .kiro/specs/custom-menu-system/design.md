# Design Document

## Overview

This feature extends the custom function system to support hierarchical menus by allowing custom functions to reference menu files. Menu files contain lists of menu items that can execute functions, actions, or act as visual separators. A new menu dialog component provides keyboard navigation for menu selection.

## Architecture

The menu system integrates with the existing custom function infrastructure:

- **MenuFile Model**: Represents the structure of a menu file
- **MenuManager Service**: Loads and manages menu files
- **MenuDialog UI Component**: Displays menu items and handles selection
- **CustomFunctionManager**: Extended to handle menu-type custom functions
- **MainController**: Orchestrates menu display and execution

## Components and Interfaces

### MenuFile Model

```csharp
public class MenuFile
{
    public string Version { get; set; } = "1.0";
    public List<MenuItemDefinition> Menus { get; set; } = new();
}

public class MenuItemDefinition
{
    public string Name { get; set; } = "";
    public string? Function { get; set; }  // Custom function name
    public string? Menu { get; set; }      // Built-in action name
    
    public bool IsSeparator => Name == "-----";
    public bool IsSelectable => !IsSeparator && (!string.IsNullOrEmpty(Function) || !string.IsNullOrEmpty(Menu));
}
```

### MenuManager Service

```csharp
public class MenuManager
{
    private readonly string _configDirectory;
    private readonly Dictionary<string, MenuFile> _loadedMenus;
    
    public MenuFile? LoadMenuFile(string menuFilePath);
    public bool IsMenuFile(CustomFunction function);
    private MenuFile ParseMenuFile(string jsonContent);
}
```

### MenuDialog UI Component

```csharp
public class MenuDialog : Dialog
{
    private readonly List<MenuItemDefinition> _menuItems;
    private readonly ListView _menuList;
    private int _selectedIndex;
    
    public MenuItemDefinition? SelectedItem { get; private set; }
    
    private void HandleKeyPress(KeyEvent keyEvent);
    private void JumpToNextMatch(char letter);
    private int GetNextSelectableIndex(int currentIndex, int direction);
}
```

### CustomFunction Extension

```csharp
public class CustomFunction
{
    public string Name { get; set; } = "";
    public string? Command { get; set; }
    public string? Menu { get; set; }  // NEW: Path to menu file
    public string Description { get; set; } = "";
    
    public bool IsMenuType => !string.IsNullOrEmpty(Menu);
}
```

## Data Models

### Menu File Format

```json
{
  "Version": "1.0",
  "Menus": [
    {
      "Name": "Open in Notepad",
      "Function": "Open in Notepad"
    },
    {
      "Name": "-----",
      "Menu": ""
    },
    {
      "Name": "View as Text",
      "Menu": "ViewFileAsText"
    },
    {
      "Name": "View as Binary",
      "Menu": "ViewFileAsHex"
    }
  ]
}
```

### Custom Function with Menu Reference

```json
{
  "Name": "File Operations",
  "Menu": "file_operations.json",
  "Description": "Show file operations menu"
}
```

## Error Handling

- **Menu file not found**: Log error, treat as regular custom function
- **Invalid JSON**: Log error with file path and line number
- **Missing required fields**: Use defaults (empty strings)
- **Circular menu references**: Detect and prevent infinite loops
- **Function not found**: Display error message in status bar
- **Action not found**: Display error message in status bar

## Testing Strategy

### Unit Tests

- Test MenuFile parsing with valid JSON
- Test MenuFile parsing with invalid JSON
- Test separator detection
- Test selectable item detection
- Test menu item execution (Function vs Menu)
- Test keyboard navigation logic
- Test letter jump functionality

### Integration Tests

- Test loading menu files from config directory
- Test executing custom functions from menus
- Test executing built-in actions from menus
- Test menu dialog display and navigation
- Test macro expansion in menu items
- Test error handling for missing files

## Implementation Notes

### Menu Dialog Navigation

- Use ListView for menu display
- Override key handling for Up/Down/Letter navigation
- Skip separators when navigating
- Highlight selected item
- Display separators as horizontal lines

### Execution Flow

1. User triggers menu-type custom function
2. MenuManager loads the referenced menu file
3. MenuDialog displays menu items
4. User navigates and selects an item
5. System determines if item is Function or Menu type
6. System executes the appropriate action
7. Dialog closes

### File Location

Menu files are located in the same directory as config.json:
- Windows: `%APPDATA%\TWF\`
- Relative paths are resolved from this directory
- Absolute paths are used as-is
