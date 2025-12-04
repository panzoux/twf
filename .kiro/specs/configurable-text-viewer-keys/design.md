# Design Document

## Overview

This feature extends the TWF keybinding system to support mode-specific keybindings for the text viewer. The KeyBindingManager will be enhanced to handle multiple UI modes, and the TextViewerWindow will be refactored to use the KeyBindingManager instead of hardcoded key handlers.

## Architecture

### Current Architecture
```
TextViewerWindow
  └─> Hardcoded KeyPress handlers
      └─> Direct method calls (CycleEncoding, ShowSearchDialog, etc.)
```

### New Architecture
```
TextViewerWindow
  └─> KeyBindingManager (with TextViewer mode)
      └─> Action resolution
          └─> Method calls via action names
```

## Components and Interfaces

### 1. Enhanced KeyBindingManager

```csharp
public class KeyBindingManager
{
    private Dictionary<string, string> _normalModeBindings;
    private Dictionary<string, string> _textViewerModeBindings;
    
    public void LoadBindings(string configPath);
    public string? GetActionForKey(string keyString, UiMode mode);
}
```

### 2. TextViewerWindow Refactoring

```csharp
public class TextViewerWindow : Window
{
    private readonly KeyBindingManager _keyBindings;
    
    public TextViewerWindow(TextViewer textViewer, KeyBindingManager keyBindings);
    
    private void SetupKeyHandlers();
    private bool ExecuteTextViewerAction(string actionName);
    
    // Action methods
    private void GoToTop();
    private void GoToBottom();
    private void PageUp();
    private void PageDown();
    private void Close();
    private void Search();
    private void FindNext();
    private void FindPrevious();
    private void CycleEncoding();
}
```

### 3. Keybindings Configuration Format

```json
{
  "version": "1.0",
  "description": "TWF Key Bindings",
  "bindings": {
    "F": "EnterSearchMode",
    "Tab": "SwitchPane",
    ...
  },
  "textViewerBindings": {
    "Home": "TextViewer.GoToTop",
    "End": "TextViewer.GoToBottom",
    "PageUp": "TextViewer.PageUp",
    "PageDown": "TextViewer.PageDown",
    "Escape": "TextViewer.Close",
    "Enter": "TextViewer.Close",
    "F4": "TextViewer.Search",
    "F3": "TextViewer.FindNext",
    "Shift+F3": "TextViewer.FindPrevious",
    "Shift+E": "TextViewer.CycleEncoding"
  }
}
```

## Available TextViewer Actions

| Action Name | Description | Default Key |
|-------------|-------------|-------------|
| TextViewer.GoToFileTop | Scroll to first line of file | F5 |
| TextViewer.GoToFileBottom | Scroll to last line of file | F6 |
| TextViewer.GoToLineStart | Move cursor to start of line | Home |
| TextViewer.GoToLineEnd | Move cursor to end of line | End |
| TextViewer.PageUp | Scroll up one page | PageUp |
| TextViewer.PageDown | Scroll down one page | PageDown |
| TextViewer.Close | Close viewer | Esc, Enter |
| TextViewer.Search | Open search dialog | F4 |
| TextViewer.FindNext | Next search result | F3 |
| TextViewer.FindPrevious | Previous search result | Shift+F3 |
| TextViewer.CycleEncoding | Change encoding | Shift+E |

## Implementation Steps

1. **Update KeyBindingManager**
   - Add support for mode-specific bindings
   - Add `GetActionForKey(string key, UiMode mode)` method
   - Load textViewerBindings from config

2. **Update Configuration Model**
   - Add TextViewerBindings property to KeyBindingConfig
   - Update JSON deserialization

3. **Refactor TextViewerWindow**
   - Accept KeyBindingManager in constructor
   - Replace hardcoded key handlers with KeyBindingManager lookups
   - Implement ExecuteTextViewerAction method
   - Add all action methods

4. **Update MainController**
   - Pass KeyBindingManager to TextViewerWindow constructor

5. **Update Default Keybindings**
   - Add textViewerBindings section to keybindings.json
   - Document all available actions

## Error Handling

- Invalid action names: Log warning, ignore binding
- Missing textViewerBindings section: Use hardcoded defaults
- Invalid keybindings file: Fall back to all defaults
- Action execution errors: Log error, show status message

## Testing Strategy

### Unit Tests
- Test KeyBindingManager mode-specific binding resolution
- Test TextViewerWindow action execution
- Test configuration loading with textViewerBindings

### Integration Tests
- Test text viewer with custom keybindings
- Test fallback to defaults when config missing
- Test all TextViewer actions work correctly

## Backward Compatibility

- If textViewerBindings section is missing, use hardcoded defaults
- Existing keybindings.json files without textViewerBindings will continue to work
- No breaking changes to existing functionality
