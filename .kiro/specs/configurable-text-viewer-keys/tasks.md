q# Implementation Plan

- [x] 1. Update KeyBindingManager to support mode-specific bindings





  - Add _textViewerModeBindings dictionary
  - Add GetActionForKey(string key, UiMode mode) overload
  - Update LoadBindings to parse textViewerBindings section
  - Add fallback to default bindings when section missing
  - _Requirements: 1, 5_

- [x] 2. Update Configuration models





  - Add TextViewerBindings property to KeyBindingConfig class
  - Update JSON deserialization to handle textViewerBindings
  - Add validation for TextViewer action names
  - _Requirements: 5_

- [x] 3. Add TextViewer action methods to TextViewerWindow





  - Implement GoToTop() method
  - Implement GoToBottom() method  
  - Implement PageUp() method
  - Implement PageDown() method
  - Implement Close() method (rename CloseViewer)
  - Implement Search() method (rename ShowSearchDialog)
  - Implement FindNext() method (already exists)
  - Implement FindPrevious() method (already exists)
  - Implement CycleEncoding() method (already exists)
  - _Requirements: 2, 3, 4_

- [x] 4. Refactor TextViewerWindow to use KeyBindingManager





  - Add KeyBindingManager parameter to constructor
  - Add ExecuteTextViewerAction(string actionName) method
  - Update SetupKeyHandlers to use KeyBindingManager
  - Remove hardcoded key checks
  - Add key-to-string conversion (reuse from MainController)
  - _Requirements: 1_

- [x] 5. Update MainController to pass KeyBindingManager





  - Modify ViewFileAsText() to pass _keyBindings to TextViewerWindow
  - Ensure KeyBindingManager is available in MainController
  - _Requirements: 1_

- [x] 6. Update default keybindings.json




  - Add textViewerBindings section
  - Define all default TextViewer action bindings
  - Add comments documenting available actions
  - _Requirements: 5_

- [x] 7. Add error handling and logging





  - Log warnings for invalid TextViewer action names
  - Log info when falling back to default bindings
  - Handle action execution errors gracefully
  - _Requirements: 1, 5_

- [x] 8. Update documentation





  - Document textViewerBindings configuration format
  - List all available TextViewer actions
  - Provide examples of custom keybindings
  - Update CONFIGURATION.md
  - _Requirements: 5_

- [x] 9. Test configurable text viewer keybindings





  - Test with custom textViewerBindings
  - Test fallback when section missing
  - Test all TextViewer actions
  - Test invalid action names
  - Verify backward compatibility
  - _Requirements: 1, 2, 3, 4_
