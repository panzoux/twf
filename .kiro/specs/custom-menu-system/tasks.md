# Implementation Plan

- [x] 1. Create MenuFile and MenuItemDefinition models





  - Create Models/MenuFile.cs with MenuFile class
  - Add Version property (string, default "1.0")
  - Add Menus property (List<MenuItemDefinition>)
  - Create MenuItemDefinition class with Name, Function, Menu properties
  - Add IsSeparator property (checks if Name == "-----")
  - Add IsSelectable property (not separator and has Function or Menu)
  - _Requirements: 1.2, 1.3, 4.1, 4.2_

- [x] 2. Extend CustomFunction model to support Menu property





  - Add Menu property (string?, nullable) to CustomFunction class
  - Add IsMenuType property (checks if Menu is not null/empty)
  - Update JSON serialization to include Menu property
  - _Requirements: 2.1, 2.2_

- [x] 3. Create MenuManager service





  - Create Services/MenuManager.cs
  - Add constructor that takes config directory path
  - Implement LoadMenuFile(string menuFilePath) method
  - Implement ParseMenuFile(string jsonContent) method
  - Add dictionary to cache loaded menu files
  - Handle relative and absolute paths
  - Add error handling for file not found
  - Add error handling for invalid JSON
  - _Requirements: 1.1, 1.4, 1.5, 2.4, 2.5_

- [x] 4. Create MenuDialog UI component





  - Create UI/MenuDialog.cs extending Dialog
  - Add ListView for displaying menu items
  - Add SelectedItem property to store selection
  - Format menu items (separators as "─────────")
  - Set dialog size and position
  - _Requirements: 5.1_

- [x] 5. Implement keyboard navigation in MenuDialog





  - Override ProcessKey to handle Up/Down arrows
  - Implement GetNextSelectableIndex helper method
  - Skip separators when navigating up/down
  - Wrap around at top/bottom of list
  - _Requirements: 5.2, 5.3, 4.3_

- [x] 6. Implement letter jump navigation in MenuDialog




  - Handle letter key presses in ProcessKey
  - Implement JumpToNextMatch(char letter) method
  - Find next item starting with the pressed letter
  - Wrap around to beginning if no match found after current position
  - Update selection to matched item
  - _Requirements: 5.4_

- [x] 7. Implement menu item selection and execution




  - Handle Enter key to confirm selection
  - Handle Escape key to cancel
  - Set SelectedItem when Enter is pressed
  - Close dialog on Enter or Escape
  - Return null for SelectedItem on Escape
  - _Requirements: 5.5, 5.6_

- [x] 8. Update CustomFunctionManager to handle menu-type functions





  - Check if custom function IsMenuType before execution
  - If menu type, load menu file using MenuManager
  - Display MenuDialog with loaded menu items
  - Get selected menu item from dialog
  - Execute selected item (Function or Menu)
  - _Requirements: 2.3, 3.1, 3.2, 3.3, 3.4_

- [x] 9. Implement menu item execution logic





  - If menu item has Function property, look up and execute custom function
  - If menu item has Menu property, execute built-in action
  - Handle empty Menu property (do nothing)
  - Apply macro expansion to Function before execution
  - Log execution and errors
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 6.1, 6.2, 6.3_

- [x] 10. Integrate MenuManager into MainController





  - Add MenuManager instance to MainController
  - Initialize MenuManager with config directory path
  - Pass MenuManager to CustomFunctionManager
  - _Requirements: 2.3_

- [x] 11. Create example menu files





  - Create example menu file in config directory
  - Add file_operations.json with common file operations
  - Add text_viewer_options.json with viewer actions
  - Document menu file format
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [x] 12. Update documentation





  - Document menu file format in CUSTOM_FUNCTIONS.md
  - Add examples of menu-type custom functions
  - Explain Function vs Menu properties
  - Document separator syntax
  - Add keyboard navigation instructions
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ]* 13. Write unit tests for menu system
  - Test MenuFile parsing with valid JSON
  - Test MenuFile parsing with invalid JSON
  - Test IsSeparator detection
  - Test IsSelectable detection
  - Test GetNextSelectableIndex with various scenarios
  - Test JumpToNextMatch with various letters
  - Test menu item execution logic

- [ ] 14. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
