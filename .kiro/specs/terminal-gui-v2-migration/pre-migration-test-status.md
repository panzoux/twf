# Pre-Migration Test Status

**Date**: 2026-01-15  
**Branch**: terminalgui2  
**Tag**: pre-v2-upgrade  
**Terminal.Gui Version**: 1.19.0

## Test Summary

- **Total Tests**: 170
- **Passed**: 162 (95.3%)
- **Failed**: 8 (4.7%)

## Failing Tests (Pre-Existing Issues)

These failures exist in the v1 codebase before migration begins:

### 1. TextViewerWindowTests.TextViewerWindow_ToggleHexMode
- **Error**: File access conflict (temp file locked)
- **Type**: Resource cleanup issue
- **Impact**: Low - test infrastructure issue

### 2. TextViewerWindowTests.TextViewerWindow_CanBeCreated
- **Error**: File access conflict (temp file locked)
- **Type**: Resource cleanup issue
- **Impact**: Low - test infrastructure issue

### 3. MainControllerTests.MainController_CreateDirectory_CreatesDirectoryAndPositionsCursor
- **Error**: Assertion failure (Expected: 1, Actual: 0)
- **Type**: Logic issue
- **Impact**: Medium - directory creation cursor positioning

### 4. FileEntryPropertyTests.DirectoryDetailView_ShowsDirInSizeColumn
- **Error**: Property test failure - <DIR> not found in expected location
- **Type**: Display formatting issue
- **Impact**: Low - cosmetic issue with long directory names

### 5. RegisteredFolderPropertyTests.RegisteredFolderNavigation_ChangesPath
- **Error**: File access conflict (config.json locked)
- **Type**: Concurrent access issue
- **Impact**: Medium - test needs better isolation

### 6. ViewerManagerPropertyTests.TextViewer_SearchFindsMatchingLines
- **Error**: Property test failure - search with newline character
- **Type**: Search logic issue with special characters
- **Impact**: Medium - edge case in search functionality

### 7. ViewerManagerPropertyTests.TextViewer_DisplaysFileContents
- **Error**: Property test failure - line count mismatch
- **Type**: Line parsing issue
- **Impact**: Medium - text viewer line handling

### 8. NavigationPropertyTests.DirectoryNavigation_UpdatesPaneContents
- **Error**: Property test failure - entries not loaded
- **Type**: Directory loading issue
- **Impact**: Medium - navigation test flakiness

## Notes

- Most failures are test infrastructure issues (file locking, resource cleanup)
- Some are edge cases in property-based tests (special characters, newlines)
- None are critical blocking issues for the v2 migration
- These issues should be tracked separately and fixed independently

## Migration Strategy

- Proceed with v2 migration despite these failures
- Monitor if v2 changes affect these tests (positively or negatively)
- Fix test infrastructure issues as part of Phase 6 (Testing & Validation)
- Ensure no new test failures are introduced during migration
