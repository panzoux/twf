# Custom Functions with Macro Support

TWF now supports custom user-defined functions with powerful macro expansion! Execute external commands with context from the file manager.

## Quick Start

### Method 1: Direct Keybinding (Fastest)

1. Define a function in `custom_functions.json`
2. Bind a key to it in `keybindings.json` using the function name
3. Press the key to execute instantly!

### Method 2: Function Dialog

1. Press `Shift+F` to open the custom functions dialog
2. Select a function from the list
3. The command will be executed with macros expanded

## Complete Example

**custom_functions.json:**
```json
{
  "version": "1.0",
  "functions": [
    {
      "name": "Open in Notepad",
      "command": "notepad \"$P\\$F\"",
      "description": "Open current file in Notepad"
    },
    {
      "name": "Open in VS Code",
      "command": "code \"$P\"",
      "description": "Open current directory in VS Code"
    }
  ]
}
```

**keybindings.json:**
```json
{
  "bindings": {
    ":": "Open in Notepad",
    "Shift+N": "Open in VS Code",
    "Shift+F": "ShowCustomFunctionsDialog"
  }
}
```

Now pressing `:` opens the current file in Notepad instantly!

## Configuration File

Custom functions are defined in `custom_functions.json`:

**Location:**
- Windows: `%APPDATA%\TWF\custom_functions.json`
- Linux/Mac: `~/.config/TWF/custom_functions.json`

**Format:**
```json
{
  "version": "1.0",
  "functions": [
    {
      "name": "Open in Notepad",
      "command": "notepad \"$P\\$F\"",
      "description": "Open current file in Notepad"
    },
    {
      "name": "Copy to Other Pane",
      "command": "cmd /c copy \"$P\\$F\" \"$O\\\"",
      "description": "Copy current file to other pane"
    }
  ]
}
```

## Macro Reference

### File/Path Macros

| Macro | Description | Example |
|-------|-------------|---------|
| `$$` | Literal `$` character | `$` |
| `$F` | Current filename | `document.txt` |
| `$W` | Filename without extension | `document` |
| `$E` | File extension | `.txt` |
| `$P` | Active pane path (no trailing `\`) | `C:\Users\John\Documents` |
| `$O` | Other pane path | `D:\Backup` |
| `$L` | Left pane path | `C:\Projects` |
| `$R` | Right pane path | `D:\Data` |

**Lowercase variants** (`$f`, `$w`, `$p`, `$o`, `$l`, `$r`) return short (8.3) filenames when available.

### Sort State Macros

| Macro | Description |
|-------|-------------|
| `$SP` | Active pane sort mode |
| `$SO` | Other pane sort mode |
| `$SL` | Left pane sort mode |
| `$SR` | Right pane sort mode |

### Marked Files Macros

| Macro | Description | Example Output |
|-------|-------------|----------------|
| `$MS` | Marked filenames (space-separated, quoted) | `"file1.txt" "file2.txt"` |
| `$MF` | Marked full paths (space-separated, quoted) | `"C:\path\file1.txt" "C:\path\file2.txt"` |
| `$MO` | Other pane marked files (full paths) | `"D:\other\file.txt"` |
| `$ML` | Left pane marked files | `"C:\left\file.txt"` |
| `$MR` | Right pane marked files | `"D:\right\file.txt"` |

**Notes:**
- If no files are marked, uses current file
- Uppercase `M` (`$MO`, `$ML`, `$MR`): Cancels command if no marked files
- Lowercase `m` (`$mO`, `$mL`, `$mR`): Returns empty string if no marked files
- Lowercase second letter (`$Ms`, `$Mf`, etc.): Returns short filenames (unquoted)

### File Mask Macros

| Macro | Description |
|-------|-------------|
| `$*P` | Active pane file mask |
| `$*O` | Other pane file mask |
| `$*L` | Left pane file mask |
| `$*R` | Right pane file mask |

### Input Dialog Macro

| Macro | Description | Example |
|-------|-------------|---------|
| `$I"prompt"` | Show input dialog | `$I"Enter destination"` |
| `$I5"prompt"` | Input dialog with width (1-9, 0=10) | `$I5"File name"` |

**Example:**
```json
{
  "name": "Custom Copy",
  "command": "cmd /c copy \"$P\\$F\" \"$I\"Destination path\"\"",
  "description": "Copy file to custom location"
}
```

### Environment Variable Macro

| Macro | Description |
|-------|-------------|
| `$V"varname"` | Environment variable value |
| `$V"twf"` | TWF executable directory |
| `$~` | Same as `$V"twf"` |

### Special Macros

| Macro | Description |
|-------|-------------|
| `$#XX` | ASCII character (XX = hex code) |
| `$"` | Literal quote character |

## Example Functions

### Open Command Prompt Here
```json
{
  "name": "Open Command Prompt Here",
  "command": "cmd /k cd /d \"$P\"",
  "description": "Open command prompt in current directory"
}
```

### Batch Rename
```json
{
  "name": "Batch Rename",
  "command": "cmd /c ren \"$P\\$F\" \"$I\"New filename\"\"",
  "description": "Rename current file (prompts for new name)"
}
```

### Copy Marked Files
```json
{
  "name": "Copy Marked to Folder",
  "command": "cmd /c for %f in ($MF) do copy %f \"$I\"Destination folder\"\\\"",
  "description": "Copy all marked files to a custom folder"
}
```

### Open in VS Code
```json
{
  "name": "Open in VS Code",
  "command": "code \"$P\"",
  "description": "Open current directory in Visual Studio Code"
}
```

### Compare Files
```json
{
  "name": "Compare with Other Pane",
  "command": "fc \"$P\\$F\" \"$O\\$F\"",
  "description": "Compare current file with same filename in other pane"
}
```

## Keybindings

### Option 1: Direct Key Binding (Recommended)

Bind a key directly to a custom function by using the function name:

```json
{
  "bindings": {
    ":": "Open in Notepad",
    "Shift+N": "Open in VS Code",
    "Ctrl+C": "Copy to Other Pane"
  }
}
```

The function name must match exactly (case-insensitive) with a function defined in `custom_functions.json`.

### Option 2: Show Function Dialog

Open a dialog to select from all available functions:

```json
{
  "bindings": {
    "Shift+F": "ShowCustomFunctionsDialog"
  }
}
```

## How Direct Keybindings Work

When you press a key, TWF:
1. Looks up the action in `keybindings.json`
2. Checks if it's a built-in action (like `ShowFileMaskDialog`)
3. If not found, searches for a custom function with that name
4. Executes the custom function if found

This means you can bind any custom function to any key!

## Tips

1. **Quote paths**: Always quote paths with spaces: `"$P\\$F"`
2. **Test commands**: Test commands in cmd.exe first before adding to custom functions
3. **Use $I for flexibility**: Input dialogs make functions reusable
4. **Marked files**: Use `$MF` to operate on multiple files at once
5. **Cancel on no marks**: Use uppercase `$MO` to prevent accidental execution
6. **Direct keybindings**: Bind frequently-used functions to keys for quick access

## Troubleshooting

**Q: My function doesn't work**  
A: Check that paths are properly quoted and use `\\` for path separators in JSON

**Q: How do I see what command was executed?**  
A: Check the TWF logs for the expanded command

**Q: Can I use PowerShell instead of cmd?**  
A: Yes! Use `powershell -Command "your command here"`

**Q: Input dialog was cancelled, what happens?**  
A: The entire command is cancelled, nothing executes
