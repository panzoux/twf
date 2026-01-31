# Advanced Jump Search Features

TWF provides high-performance, asynchronous jump dialogs for files and directories. These dialogs share a unified interface and advanced search capabilities.

## Shortcuts
- **`@`**: Jump to File (Recursive search)
- **`J`**: Jump to Directory (Bookmarks, History, and current directory)

## Shared UI Features
- **Smart Truncation**: Long paths are truncated in the middle to preserve both the starting path and the filename/extension.
- **Path Preview**: A 4-line preview at the bottom shows the full, un-truncated path of the selected item.
- **Search Status**: The top-right shows a spinner during search and a result count when finished.
- **High-Density Layout**: Dialogs are optimized for 66x20 terminal size.

## Navigation
When the search field is focused, you can control the suggestion list using:
- **`Up / Down Arrows`**: Move selection.
- **`PageUp / PageDown`**: Scroll the list by one page.
- **`Enter`**: Confirm selection and jump.

## Advanced Search Syntax

### Multiple Keyword Search (AND Logic)
You can type multiple keywords separated by spaces. An item is only shown if it matches **all** keywords (order-independent).
- Example: `src config` will find `src/app/config.json` and `infrastructure/src/config_helper.cs`.

### Escaping Spaces
If you need to search for a literal space in a filename, escape it with a backslash (`\ `).
- Example: `my\ folder docs` will find files containing "my folder" AND "docs".

### Environment Variables
You can type environment variables directly into the search field.
- Example: `%TEMP%\log` or `$HOME/docs`.

## Configuration (`config.json`)

Settings are located in the `"Navigation"` section:

```json
"Navigation": {
  "JumpToFileSearchDepth": 3,
  "JumpToFileMaxResults": 100,
  "JumpToPathSearchDepth": 2,
  "JumpToPathMaxResults": 100,
  "JumpIgnoreList": [ ".git", "node_modules", "obj", "bin" ]
}
```

### `JumpIgnoreList`
A list of folder names to skip entirely during the jump recursive search.
- **Performance**: Skipping folders like `node_modules` or `.git` drastically reduces CPU usage and keeps the UI responsive in large projects.
- **Default**: `[".git"]`

### `JumpToFileSearchDepth`
How many levels deep the recursive search should go for files. Default is `3`.

### `JumpToFileMaxResults`
Limits the number of results for file search to prevent UI lag. Default is `100`.

### `JumpToPathSearchDepth`
How many levels deep the recursive search should go for directories. Default is `2`.

### `JumpToPathMaxResults`
Limits the number of results for directory search to prevent UI lag. Default is `100`.
