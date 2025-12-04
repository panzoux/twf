# Design Document

## Overview

TWF (Two-pane Window Filer) is architected as a modular, event-driven terminal application built on the Terminal.Gui framework for .NET. The system follows a Model-View-Controller (MVC) pattern with clear separation between UI components, business logic, and data access layers. The architecture supports extensibility through plugin systems for archive formats and image viewers, while maintaining a responsive user interface through asynchronous operations for long-running tasks.

The core design principles are:
- **Keyboard-first interaction**: All operations accessible via keyboard shortcuts
- **Configurability**: Extensive customization through configuration files
- **Extensibility**: Plugin architecture for formats and viewers
- **Performance**: Efficient file system operations with caching
- **Reliability**: Robust error handling and operation cancellation

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Terminal.Gui Layer                       │
│  (Window, ListView, Label, TextField, Dialog components)    │
└─────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────┐
│                    UI Controller Layer                       │
│  - MainController (orchestrates UI and operations)          │
│  - KeyBindingManager (maps keys to actions)                 │
│  - ViewModeManager (handles display modes)                  │
└─────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────┐
│                   Business Logic Layer                       │
│  - FileOperations (copy, move, delete, rename)              │
│  - MarkingEngine (file selection and wildcard matching)     │
│  - SortEngine (file list sorting)                           │
│  - SearchEngine (incremental search)                        │
│  - ArchiveManager (archive operations)                      │
│  - ViewerManager (text and image viewing)                   │
└─────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────┐
│                      Data Access Layer                       │
│  - FileSystemProvider (directory and file access)           │
│  - ArchiveProvider (virtual folder for archives)            │
│  - ListProvider (generic list data for jumplists, drives)   │
│  - ConfigurationProvider (settings persistence)             │
│  - HistoryProvider (session state persistence)              │
└─────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────┐
│                      Plugin Layer                            │
│  - ArchiveDllLoader (loads archive format DLLs)             │
│  - ImageFormatProvider (built-in image format support)      │
│  - MigemoProvider (optional Japanese search support)        │
└─────────────────────────────────────────────────────────────┘
```

### Component Interaction Flow

1. **User Input**: User presses a key → Terminal.Gui captures event
2. **Key Mapping**: KeyBindingManager translates key to action
3. **Action Dispatch**: MainController routes action to appropriate handler
4. **Business Logic**: Handler executes operation (e.g., FileOperations.Copy)
5. **Data Access**: Operation interacts with FileSystemProvider or ArchiveProvider
6. **UI Update**: Controller updates UI components (ListView, StatusBar)
7. **State Persistence**: ConfigurationProvider saves state if needed

## Components and Interfaces

### Core Components

#### 1. MainController
**Responsibility**: Orchestrates the entire application, manages UI state, and coordinates between components.

```csharp
public class MainController
{
    // UI Components
    private Window mainWindow;
    private ListView leftPane;
    private ListView rightPane;
    private Label pathLabel;
    private Label statusBar;
    private TextField inputField;
    
    // State
    private PaneState leftState;
    private PaneState rightState;
    private UiMode currentMode;
    private bool leftPaneActive;
    
    // Dependencies
    private KeyBindingManager keyBindings;
    private FileOperations fileOps;
    private MarkingEngine markingEngine;
    private ConfigurationProvider config;
    
    public void Initialize();
    public void Run();
    public void HandleKeyPress(KeyEvent keyEvent);
    public void RefreshPanes();
    public void SwitchPane();
    public void SetStatus(string message);
}
```

#### 2. PaneState
**Responsibility**: Maintains the state of a single file pane.

```csharp
public class PaneState
{
    public string CurrentPath { get; set; }
    public List<FileEntry> Entries { get; set; }
    public HashSet<int> MarkedIndices { get; set; }
    public int CursorPosition { get; set; }
    public int ScrollOffset { get; set; }
    public string FileMask { get; set; }
    public SortMode SortMode { get; set; }
    public DisplayMode DisplayMode { get; set; }
    
    public void LoadDirectory(string path);
    public void ApplyFileMask(string mask);
    public void ApplySort(SortMode mode);
    public FileEntry GetCurrentEntry();
    public List<FileEntry> GetMarkedEntries();
}
```

#### 3. FileEntry
**Responsibility**: Represents a file or directory with its metadata.

```csharp
public class FileEntry
{
    public string FullPath { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public FileAttributes Attributes { get; set; }
    public bool IsDirectory { get; set; }
    public bool IsArchive { get; set; }
    public bool IsVirtualFolder { get; set; }
    
    public string FormatForDisplay(DisplayMode mode);
}
```

#### 4. KeyBindingManager
**Responsibility**: Maps key events to actions based on configuration.

```csharp
public class KeyBindingManager
{
    private Dictionary<KeyCode, ActionBinding> normalModeBindings;
    private Dictionary<KeyCode, ActionBinding> viewerModeBindings;
    private Dictionary<KeyCode, ActionBinding> inputModeBindings;
    
    public void LoadBindings(string configPath);
    public ActionBinding GetBinding(KeyCode key, UiMode mode);
    public void SetBinding(KeyCode key, ActionBinding action, UiMode mode);
}

public class ActionBinding
{
    public ActionType Type { get; set; } // Function, KeyRedirect, Command
    public string Target { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
}
```

#### 5. FileOperations
**Responsibility**: Executes file system operations with progress reporting.

```csharp
public class FileOperations
{
    public event EventHandler<ProgressEventArgs> ProgressChanged;
    public event EventHandler<ErrorEventArgs> ErrorOccurred;
    
    public async Task<OperationResult> CopyAsync(
        List<FileEntry> sources, 
        string destination, 
        CancellationToken cancellationToken);
        
    public async Task<OperationResult> MoveAsync(
        List<FileEntry> sources, 
        string destination, 
        CancellationToken cancellationToken);
        
    public async Task<OperationResult> DeleteAsync(
        List<FileEntry> entries, 
        CancellationToken cancellationToken);
        
    public OperationResult CreateDirectory(string path, string name);
    
    public async Task<OperationResult> RenameAsync(
        List<FileEntry> entries, 
        string pattern, 
        string replacement);
}
```

#### 6. MarkingEngine
**Responsibility**: Manages file marking and wildcard matching.

```csharp
public class MarkingEngine
{
    public void ToggleMark(PaneState pane, int index);
    public void MarkRange(PaneState pane, int startIndex, int endIndex);
    public void InvertMarks(PaneState pane);
    public void ClearMarks(PaneState pane);
    public void MarkByWildcard(PaneState pane, string pattern);
    public void MarkByRegex(PaneState pane, string regexPattern);
    public bool MatchesWildcard(string filename, string pattern);
    public bool MatchesRegex(string filename, string regexPattern);
}
```

#### 7. ArchiveManager
**Responsibility**: Handles archive operations and virtual folder navigation.

```csharp
public class ArchiveManager
{
    private Dictionary<string, IArchiveProvider> providers;
    
    public void RegisterProvider(string extension, IArchiveProvider provider);
    public bool IsArchive(string path);
    public List<FileEntry> ListArchiveContents(string archivePath);
    public async Task<OperationResult> ExtractAsync(
        string archivePath, 
        string destination, 
        CancellationToken cancellationToken);
    public async Task<OperationResult> CompressAsync(
        List<FileEntry> sources, 
        string archivePath, 
        ArchiveFormat format, 
        CancellationToken cancellationToken);
}

public interface IArchiveProvider
{
    string[] SupportedExtensions { get; }
    List<FileEntry> List(string archivePath);
    Task<OperationResult> Extract(string archivePath, string destination);
    Task<OperationResult> Compress(List<string> sources, string archivePath);
}
```

#### 8. ViewerManager
**Responsibility**: Manages text and image viewers.

```csharp
public class ViewerManager
{
    public void OpenTextViewer(string filePath, Encoding encoding);
    public void OpenImageViewer(string filePath);
    public void CloseCurrentViewer();
}

public class TextViewer
{
    public string FilePath { get; set; }
    public Encoding CurrentEncoding { get; set; }
    public int CurrentLine { get; set; }
    
    public void LoadFile(string path, Encoding encoding);
    public void CycleEncoding();
    public void Search(string pattern);
    public void ScrollTo(int line);
}

public class ImageViewer
{
    public string FilePath { get; set; }
    public ViewMode ViewMode { get; set; }
    public int Rotation { get; set; }
    public bool FlipHorizontal { get; set; }
    public bool FlipVertical { get; set; }
    
    public void LoadImage(string path);
    public void Rotate(int degrees);
    public void Flip(FlipDirection direction);
    public void Zoom(double factor);
    public void SetViewMode(ViewMode mode);
}
```

**Note**: Image viewing will use built-in .NET image libraries (System.Drawing or ImageSharp) for terminal-compatible rendering. Susie plugin support is considered a future enhancement.

#### 9. SearchEngine
**Responsibility**: Handles incremental search with optional Migemo support.

```csharp
public class SearchEngine
{
    private IMigemoProvider migemoProvider;
    
    public SearchEngine(IMigemoProvider migemoProvider = null)
    {
        this.migemoProvider = migemoProvider;
    }
    
    public List<int> FindMatches(List<FileEntry> entries, string searchPattern, bool useMigemo);
    public int FindNext(List<FileEntry> entries, string searchPattern, int currentIndex, bool useMigemo);
    public int FindPrevious(List<FileEntry> entries, string searchPattern, int currentIndex, bool useMigemo);
}

public interface IMigemoProvider
{
    bool IsAvailable { get; }
    string ExpandPattern(string romajiPattern);
}

public class MigemoProvider : IMigemoProvider
{
    public bool IsAvailable => CheckMigemoDllExists();
    
    public string ExpandPattern(string romajiPattern)
    {
        // Converts romaji input to regex pattern matching hiragana/katakana/kanji
        // Example: "nihon" -> "(nihon|にほん|ニホン|日本)"
    }
    
    private bool CheckMigemoDllExists()
    {
        // Check if migemo.dll and dictionary files exist
    }
}
```

#### 10. ListProvider
**Responsibility**: Provides generic list data for various UI components.

```csharp
public class ListProvider
{
    public List<DriveInfo> GetDriveList();
    public List<RegisteredFolder> GetJumpList();
    public List<string> GetHistoryList(HistoryType type);
    public List<MenuItem> GetContextMenu(FileEntry entry, bool hasMarkedFiles);
}

public class DriveInfo
{
    public string DriveLetter { get; set; }
    public string VolumeLabel { get; set; }
    public DriveType DriveType { get; set; }
    public long TotalSize { get; set; }
    public long FreeSpace { get; set; }
}

public class RegisteredFolder
{
    public string Name { get; set; }
    public string Path { get; set; }
    public int SortOrder { get; set; }
}

public enum HistoryType
{
    DirectoryHistory,
    SearchHistory,
    CommandHistory
}
```

#### 11. ConfigurationProvider
**Responsibility**: Loads and saves application configuration.

```csharp
public class ConfigurationProvider
{
    public Configuration LoadConfiguration(string configPath);
    public void SaveConfiguration(Configuration config, string configPath);
    public SessionState LoadSessionState();
    public void SaveSessionState(SessionState state);
}

public class Configuration
{
    public DisplaySettings Display { get; set; }
    public KeyBindings KeyBindings { get; set; }
    public List<RegisteredFolder> RegisteredFolders { get; set; }
    public Dictionary<string, string> ExtensionAssociations { get; set; }
    public ArchiveSettings Archive { get; set; }
    public ViewerSettings Viewer { get; set; }
    public bool SaveSessionState { get; set; }
}

public class SessionState
{
    public string LeftPath { get; set; }
    public string RightPath { get; set; }
    public string LeftMask { get; set; }
    public string RightMask { get; set; }
    public SortMode LeftSort { get; set; }
    public SortMode RightSort { get; set; }
}
```

## Data Models

### Enumerations

```csharp
public enum UiMode
{
    Normal,
    InputField,
    Confirmation,
    TextViewer,
    ImageViewer,
    Menu
}

public enum DisplayMode
{
    NameOnly,
    Details,
    Thumbnail,
    Icon,
    OneColumn,
    TwoColumns,
    ThreeColumns,
    FourColumns,
    FiveColumns,
    SixColumns,
    SevenColumns,
    EightColumns
}

public enum SortMode
{
    Unsorted,
    NameAscending,
    NameDescending,
    ExtensionAscending,
    ExtensionDescending,
    SizeAscending,
    SizeDescending,
    DateAscending,
    DateDescending
}

public enum ArchiveFormat
{
    LZH,
    ZIP,
    TAR,
    TGZ,
    CAB,
    RAR,
    SevenZip,
    BZ2,
    XZ,
    LZMA
}

public enum ViewMode
{
    OriginalSize,
    FitToWindow,
    FitToScreen,
    FixedZoom
}

public enum ActionType
{
    Function,      // Execute a built-in function
    KeyRedirect,   // Redirect to another key
    Command        // Execute a shell command
}
```

### Operation Results

```csharp
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public List<string> Errors { get; set; }
    public TimeSpan Duration { get; set; }
}

public class ProgressEventArgs : EventArgs
{
    public string CurrentFile { get; set; }
    public int CurrentFileIndex { get; set; }
    public int TotalFiles { get; set; }
    public long BytesProcessed { get; set; }
    public long TotalBytes { get; set; }
    public double PercentComplete { get; set; }
}
```

## Correctness Properties


*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Property 1: Directory navigation updates pane contents
*For any* directory entry, when Enter is pressed on that entry, the active pane should display the contents of that directory
**Validates: Requirements 1.1**

Property 2: Backspace navigates to parent
*For any* non-root directory, when Backspace is pressed, the active pane should display the parent directory's contents
**Validates: Requirements 1.2**

Property 3: Tab toggles pane focus
*For any* application state, pressing Tab should switch focus from left to right or right to left pane
**Validates: Requirements 1.3**

Property 4: Arrow keys move cursor
*For any* file list with multiple entries, pressing down arrow should move cursor to the next entry, and pressing up arrow should move cursor to the previous entry
**Validates: Requirements 1.4**

Property 5: Ctrl+PageUp moves to first entry
*For any* non-empty file list, pressing Ctrl+PageUp should position the cursor at the first entry
**Validates: Requirements 1.5**

Property 6: Ctrl+PageDown moves to last entry
*For any* non-empty file list, pressing Ctrl+PageDown should position the cursor at the last entry
**Validates: Requirements 1.6**

Property 7: Home navigates to drive root
*For any* path on a drive, pressing Home should navigate to the root directory of that drive
**Validates: Requirements 1.7**

Property 8: Number keys switch display modes
*For any* file list, pressing number keys 1-8 should switch to the corresponding display mode
**Validates: Requirements 2.1**

Property 9: Detail view shows required fields
*For any* file entry in detail view, the formatted display string should contain file name, size, and timestamp
**Validates: Requirements 2.2**

Property 10: Directories show <DIR> in size column
*For any* directory entry in detail view, the size column should display "<DIR>"
**Validates: Requirements 2.3**

Property 11: Display mode change preserves cursor position
*For any* file list, changing display mode should preserve the cursor position and scroll offset
**Validates: Requirements 2.6**

Property 12: Space toggles mark and moves cursor down
*For any* file entry, pressing Space should toggle its mark state and move the cursor to the next entry
**Validates: Requirements 3.1**

Property 13: Shift+Space toggles mark and moves cursor up
*For any* file entry, pressing Shift+Space should toggle its mark state and move the cursor to the previous entry
**Validates: Requirements 3.2**

Property 14: Ctrl+Space marks range
*For any* file list with a previous mark, pressing Ctrl+Space should mark all entries between the previous mark and the cursor position
**Validates: Requirements 3.3**

Property 15: Home inverts all marks
*For any* file list, pressing Home should invert the mark state of all entries
**Validates: Requirements 3.4**

Property 16: Wildcard pattern marks matching files
*For any* wildcard pattern and file list, applying the pattern should mark all files whose names match the pattern
**Validates: Requirements 4.2**

Property 17: Copy operation transfers files
*For any* set of marked files, executing copy should create identical files in the destination directory
**Validates: Requirements 5.1, 5.2**

Property 18: Copy preserves file attributes
*For any* file being copied, the destination file should have the same size, content, and timestamp as the source
**Validates: Requirements 5.1**

Property 19: Move operation relocates files
*For any* set of marked files, executing move should transfer files to the destination and remove them from the source
**Validates: Requirements 6.1, 6.2**

Property 20: Delete operation removes files
*For any* set of marked files, confirming deletion should remove all selected files from the file system
**Validates: Requirements 7.3**

Property 21: Directory creation adds new folder
*For any* valid directory name, creating a directory should add a new folder entry to the current path
**Validates: Requirements 8.2**

Property 22: Sort mode changes file order
*For any* file list, changing the sort mode should reorder the entries according to the selected criteria
**Validates: Requirements 9.1**

Property 23: File mask filters displayed files
*For any* file mask pattern, only files matching the pattern should be displayed in the file list
**Validates: Requirements 10.2**

Property 24: Incremental search finds matching files
*For any* search string, the cursor should move to the first file whose name starts with that string (or matches via Migemo if enabled)
**Validates: Requirements 11.2**

Property 25: Archive displays as virtual folder
*For any* supported archive file, pressing Enter should display its contents as a navigable folder structure
**Validates: Requirements 12.1**

Property 26: Archive extraction creates files
*For any* archive file, extracting should create all contained files in the destination directory
**Validates: Requirements 12.4**

Property 27: Compression creates archive
*For any* set of marked files, compressing should create an archive file containing all selected files
**Validates: Requirements 13.2**

Property 28: Text viewer displays file contents
*For any* text file, opening in the viewer should display all lines of the file
**Validates: Requirements 14.2**

Property 29: Encoding change updates display
*For any* text file, cycling through encodings should update the displayed text according to the selected encoding
**Validates: Requirements 14.3**

Property 30: Image viewer displays image
*For any* supported image file, opening in the viewer should display the image
**Validates: Requirements 15.2**

Property 31: Image rotation transforms display
*For any* displayed image, rotating should change the image orientation by 90 degrees
**Validates: Requirements 15.5**

Property 32: Registered folder navigation changes path
*For any* registered folder, selecting it should change the active pane to that directory path
**Validates: Requirements 16.2**

Property 33: File execution launches program
*For any* executable file or file with extension association, pressing Enter should execute or open the file
**Validates: Requirements 17.1, 17.2**

Property 34: Pattern rename transforms filenames
*For any* set of marked files and rename pattern, applying the pattern should rename files according to the transformation rules
**Validates: Requirements 18.3**

Property 35: File comparison marks matching files
*For any* comparison criteria, files matching the criteria should be marked
**Validates: Requirements 19.2**

Property 36: File split creates multiple parts
*For any* file and split size, splitting should create multiple part files whose combined size equals the original
**Validates: Requirements 20.2**

Property 37: File join recreates original
*For any* set of split file parts, joining should recreate a file identical to the original
**Validates: Requirements 20.4**

Property 38: Custom key binding overrides default
*For any* custom key binding defined in the configuration file, pressing that key should execute the custom action instead of the default
**Validates: Requirements 21.2**

Property 39: Key binding with modifiers recognized
*For any* key binding with SHIFT, CTRL, or ALT modifiers, the system should recognize and execute the modified key combination
**Validates: Requirements 21.5**

Property 40: Mode-specific bindings apply correctly
*For any* UI mode (Normal, Image Viewer, Text Viewer), the system should apply the key bindings defined for that specific mode
**Validates: Requirements 21.6**

Property 41: Configuration changes apply
*For any* configuration setting change, saving should update the application behavior according to the new setting
**Validates: Requirements 22.2**

Property 42: Progress indicator shows operation status
*For any* long-running file operation, the system should display progress information including current file and percentage complete
**Validates: Requirements 23.2**

Property 43: Operation cancellation stops processing
*For any* in-progress file operation, pressing Escape should cancel the operation and stop further processing
**Validates: Requirements 23.3**

Property 44: Context menu shows applicable operations
*For any* file selection state, the context menu should display operations that are valid for the current selection
**Validates: Requirements 24.3**

Property 45: Session state restoration preserves paths
*For any* application session, restarting should restore the last used directory paths for both panes
**Validates: Requirements 25.1**

Property 46: Session state restoration preserves settings
*For any* application session, restarting should restore the last used file mask and sort mode
**Validates: Requirements 25.2, 25.3**

## Error Handling

### Error Categories

The system handles errors in the following categories:

1. **File System Errors**
   - File not found
   - Access denied (permissions)
   - Disk full
   - Path too long
   - File in use by another process
   - Invalid filename characters

2. **Archive Errors**
   - Unsupported archive format
   - Corrupted archive file
   - Archive DLL not found
   - Extraction failure
   - Compression failure

3. **Configuration Errors**
   - Invalid configuration file format
   - Missing configuration file
   - Invalid key binding syntax
   - Invalid color values
   - Invalid font specifications

4. **Operation Errors**
   - Source and destination are the same
   - Circular directory copy/move
   - Insufficient disk space
   - Operation cancelled by user
   - Network path unavailable

5. **Plugin Errors**
   - Archive DLL not found
   - Archive DLL initialization failure
   - Incompatible DLL version
   - DLL execution error
   - Migemo dictionary not found (non-critical)

### Error Handling Strategy

#### 1. Graceful Degradation
- When an archive DLL fails to load, continue with built-in archive support
- When Migemo is unavailable, fall back to standard incremental search
- When a configuration file is invalid, use default settings
- When a custom key binding is malformed, fall back to default binding
- When image format is unsupported, display file info instead of preview

#### 2. User Notification
- Display error messages in the status bar for minor errors
- Show modal dialogs for critical errors requiring user decision
- Log detailed error information to a log file for debugging

#### 3. Operation Recovery
- For batch operations, offer options to skip, retry, or cancel
- Maintain operation state to allow resumption after errors
- Rollback partial operations when possible (e.g., failed move operations)

#### 4. Error Message Format
```
[ERROR] Operation: <operation_name>
File: <file_path>
Reason: <error_description>
Options: [Skip] [Retry] [Cancel All]
```

#### 5. Logging
- Log all errors to `twf_errors.log` with timestamp
- Include stack traces for unexpected exceptions
- Rotate log files when they exceed 10MB

### Critical Error Handling

For unrecoverable errors (e.g., Terminal.Gui initialization failure):
1. Display error message to console
2. Write error to log file if possible
3. Exit gracefully with appropriate error code

## Testing Strategy

### Unit Testing

Unit tests verify individual components in isolation using mocks for dependencies.

**Test Coverage Areas:**
- **MarkingEngine**: Test wildcard matching, regex matching, mark toggling, range marking
- **SortEngine**: Test all sort modes with various file lists
- **SearchEngine**: Test incremental search with and without Migemo
- **ListProvider**: Test drive list, jump list, and history list generation
- **FileEntry**: Test display formatting for different modes
- **KeyBindingManager**: Test key mapping, binding loading, mode-specific bindings
- **ConfigurationProvider**: Test configuration loading, saving, validation
- **FileOperations**: Test operation logic with mocked file system

**Example Unit Tests:**
```csharp
[Test]
public void MarkingEngine_WildcardMatch_MatchesPattern()
{
    var engine = new MarkingEngine();
    Assert.IsTrue(engine.MatchesWildcard("test.txt", "*.txt"));
    Assert.IsFalse(engine.MatchesWildcard("test.doc", "*.txt"));
}

[Test]
public void SortEngine_SortByName_OrdersAlphabetically()
{
    var entries = CreateTestEntries();
    var sorted = SortEngine.Sort(entries, SortMode.NameAscending);
    Assert.AreEqual("aaa.txt", sorted[0].Name);
    Assert.AreEqual("zzz.txt", sorted[sorted.Count - 1].Name);
}

[Test]
public void SearchEngine_WithoutMigemo_FindsExactMatch()
{
    var engine = new SearchEngine();
    var entries = CreateTestEntries();
    var matches = engine.FindMatches(entries, "test", useMigemo: false);
    Assert.IsTrue(matches.All(i => entries[i].Name.StartsWith("test", StringComparison.OrdinalIgnoreCase)));
}

[Test]
public void ListProvider_GetDriveList_ReturnsAllDrives()
{
    var provider = new ListProvider();
    var drives = provider.GetDriveList();
    Assert.IsTrue(drives.Count > 0);
    Assert.IsTrue(drives.All(d => !string.IsNullOrEmpty(d.DriveLetter)));
}

[Test]
public void KeyBindingManager_CustomBinding_OverridesDefault()
{
    var manager = new KeyBindingManager();
    manager.LoadBindings("test_bindings.key");
    var binding = manager.GetBinding(KeyCode.C, UiMode.Normal);
    Assert.AreEqual("CustomCopyCommand", binding.Target);
}
```

### Property-Based Testing

Property-based tests verify universal properties across many randomly generated inputs using a property testing library (e.g., FsCheck for .NET).

**Property Test Configuration:**
- Minimum 100 iterations per property test
- Use appropriate generators for file paths, filenames, patterns
- Tag each test with the property number from the design document

**Example Property Tests:**

```csharp
// Feature: twf-file-manager, Property 16: Wildcard pattern marks matching files
[Property(Arbitrary = new[] { typeof(FileGenerators) })]
public Property WildcardPattern_MarksMatchingFiles(List<FileEntry> files, string pattern)
{
    var pane = new PaneState { Entries = files };
    var engine = new MarkingEngine();
    
    engine.MarkByWildcard(pane, pattern);
    var markedFiles = pane.GetMarkedEntries();
    
    return markedFiles.All(f => engine.MatchesWildcard(f.Name, pattern)).ToProperty();
}

// Feature: twf-file-manager, Property 22: Sort mode changes file order
[Property]
public Property SortByName_ProducesAlphabeticalOrder(List<FileEntry> files)
{
    var sorted = SortEngine.Sort(files, SortMode.NameAscending);
    
    for (int i = 0; i < sorted.Count - 1; i++)
    {
        if (string.Compare(sorted[i].Name, sorted[i + 1].Name, StringComparison.Ordinal) > 0)
            return false.ToProperty();
    }
    
    return true.ToProperty();
}

// Feature: twf-file-manager, Property 38: Custom key binding overrides default
[Property]
public Property CustomKeyBinding_OverridesDefault(KeyCode key, string customAction)
{
    var manager = new KeyBindingManager();
    manager.SetBinding(key, new ActionBinding { Type = ActionType.Function, Target = customAction }, UiMode.Normal);
    
    var binding = manager.GetBinding(key, UiMode.Normal);
    
    return (binding.Target == customAction).ToProperty();
}

// Feature: twf-file-manager, Property 46: Session state restoration preserves settings
[Property]
public Property SessionRestore_PreservesFileMaskAndSort(string leftMask, string rightMask, SortMode leftSort, SortMode rightSort)
{
    var state = new SessionState
    {
        LeftMask = leftMask,
        RightMask = rightMask,
        LeftSort = leftSort,
        RightSort = rightSort
    };
    
    var provider = new ConfigurationProvider();
    provider.SaveSessionState(state);
    var restored = provider.LoadSessionState();
    
    return (restored.LeftMask == leftMask &&
            restored.RightMask == rightMask &&
            restored.LeftSort == leftSort &&
            restored.RightSort == rightSort).ToProperty();
}
```

**Property Test Generators:**
```csharp
public class FileGenerators
{
    public static Arbitrary<FileEntry> FileEntry()
    {
        return Arb.From(
            from name in Arb.Generate<NonEmptyString>()
            from size in Arb.Generate<PositiveInt>()
            from date in Arb.Generate<DateTime>()
            select new FileEntry
            {
                Name = name.Get,
                Size = size.Get,
                LastModified = date,
                IsDirectory = false
            });
    }
    
    public static Arbitrary<string> WildcardPattern()
    {
        return Arb.From(
            from pattern in Arb.Generate<NonEmptyString>()
            select pattern.Get.Replace("/", "").Replace("\\", "") + "*");
    }
}
```

### Integration Testing

Integration tests verify component interactions with real file system operations in a controlled test environment.

**Test Areas:**
- File operations (copy, move, delete) with real files in temp directories
- Archive operations with actual archive files
- Configuration loading and saving with real config files
- Key binding loading from actual .KEY files

**Example Integration Tests:**
```csharp
[Test]
public async Task FileOperations_CopyFiles_CreatesIdenticalFiles()
{
    var testDir = CreateTestDirectory();
    var sourceFile = CreateTestFile(testDir, "source.txt", "test content");
    var destDir = CreateTestDirectory();
    
    var fileOps = new FileOperations();
    var result = await fileOps.CopyAsync(
        new List<FileEntry> { new FileEntry { FullPath = sourceFile } },
        destDir,
        CancellationToken.None);
    
    Assert.IsTrue(result.Success);
    Assert.IsTrue(File.Exists(Path.Combine(destDir, "source.txt")));
    Assert.AreEqual(
        File.ReadAllText(sourceFile),
        File.ReadAllText(Path.Combine(destDir, "source.txt")));
}

[Test]
public void ArchiveManager_ListContents_ReturnsAllEntries()
{
    var testArchive = CreateTestArchive("test.zip", new[] { "file1.txt", "file2.txt" });
    var manager = new ArchiveManager();
    
    var contents = manager.ListArchiveContents(testArchive);
    
    Assert.AreEqual(2, contents.Count);
    Assert.IsTrue(contents.Any(e => e.Name == "file1.txt"));
    Assert.IsTrue(contents.Any(e => e.Name == "file2.txt"));
}
```

### Manual Testing Checklist

Critical user workflows to verify manually:
- [ ] Navigate through directory tree using keyboard
- [ ] Mark files with Space and wildcard patterns
- [ ] Copy/move/delete files with progress display
- [ ] Browse archive contents as virtual folders
- [ ] View text files with different encodings
- [ ] View images with zoom and rotation
- [ ] Use registered folders for quick navigation
- [ ] Customize key bindings and verify they work
- [ ] Split and join large files
- [ ] Rename files with pattern transformations
- [ ] Verify session state restoration on restart
- [ ] Test error handling for permission denied scenarios
- [ ] Test operation cancellation with Escape key