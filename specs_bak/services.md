# TWF Rust Port - Services Specification

## Overview

This document specifies the service layer for the Rust port of TWF. Services encapsulate the core business logic of the application, including file operations, search functionality, archive management, and job scheduling.

## Service Architecture

The service layer follows a modular design where each service handles a specific domain of functionality:

```
┌─────────────────────┐
│   FileOperations    │ ← Handles copy, move, delete operations
├─────────────────────┤
│     SearchEngine    │ ← Handles file search and filtering
├─────────────────────┤
│    ArchiveManager   │ ← Handles archive creation and extraction
├─────────────────────┤
│      JobManager     │ ← Manages background operations
├─────────────────────┤
│    HistoryManager   │ ← Tracks navigation history
├─────────────────────┤
│   MarkingEngine     │ ← Handles file marking operations
├─────────────────────┤
│     SortEngine      │ ← Handles file sorting
├─────────────────────┤
│    ViewerManager    │ ← Manages file viewing
├─────────────────────┤
│  CustomFunctionMgr  │ ← Executes custom functions/macros
└─────────────────────┘
```

## FileOperations Service

Handles all file system operations with progress reporting and error handling.

### Interface
```rust
pub trait FileOperationsService {
    /// Copy files to destination directory
    async fn copy_async(
        &self,
        sources: Vec<FileEntry>,
        destination: &Path,
        options: CopyOptions,
        progress_callback: Option<Box<dyn Fn(ProgressUpdate) -> bool>>,
    ) -> Result<OperationResult, FileOperationError>;

    /// Move files to destination directory
    async fn move_async(
        &self,
        sources: Vec<FileEntry>,
        destination: &Path,
        options: MoveOptions,
        progress_callback: Option<Box<dyn Fn(ProgressUpdate) -> bool>>,
    ) -> Result<OperationResult, FileOperationError>;

    /// Delete files/directories
    async fn delete_async(
        &self,
        entries: Vec<FileEntry>,
        options: DeleteOptions,
        progress_callback: Option<Box<dyn Fn(ProgressUpdate) -> bool>>,
    ) -> Result<OperationResult, FileOperationError>;

    /// Create a new directory
    fn create_directory(&self, path: &Path, name: &str) -> Result<OperationResult, FileOperationError>;

    /// Rename files using pattern-based transformations
    async fn rename_async(
        &self,
        entries: Vec<FileEntry>,
        pattern: &str,
        replacement: &str,
    ) -> Result<OperationResult, FileOperationError>;
}
```

### Implementation Details
- Uses async/await for non-blocking operations
- Implements proper cancellation support
- Handles file collisions with customizable strategies (overwrite, skip, rename, cancel)
- Reports progress with byte-level accuracy
- Implements cross-volume move detection and handling
- Provides detailed error reporting

### Options Structures
```rust
pub struct CopyOptions {
    pub collision_strategy: CollisionStrategy,
    pub preserve_attributes: bool,
    pub preserve_timestamps: bool,
    pub buffer_size: usize,
}

pub struct MoveOptions {
    pub collision_strategy: CollisionStrategy,
    pub preserve_attributes: bool,
    pub buffer_size: usize,
}

pub struct DeleteOptions {
    pub force_delete_readonly: bool,
    pub send_to_recycle_bin: bool,
}
```

## SearchEngine Service

Provides advanced search and filtering capabilities.

### Interface
```rust
pub trait SearchEngineService {
    /// Search for files in a directory using pattern
    async fn search_async(
        &self,
        directory: &Path,
        pattern: &str,
        options: SearchOptions,
    ) -> Result<Vec<FileEntry>, SearchError>;

    /// Filter files based on criteria
    fn filter(
        &self,
        entries: &[FileEntry],
        criteria: &FilterCriteria,
    ) -> Vec<FileEntry>;

    /// Perform incremental search (Migemo support)
    fn incremental_search(
        &self,
        entries: &[FileEntry],
        query: &str,
    ) -> Vec<FileEntry>;
}
```

### Implementation Details
- Supports wildcards (*, ?) and regular expressions
- Implements Migemo for Japanese incremental search
- Provides case-sensitive and insensitive search options
- Supports content-based search (with limitations for performance)
- Implements search result caching
- Provides fuzzy matching capabilities

## ArchiveManager Service

Handles archive creation, extraction, and browsing.

### Interface
```rust
pub trait ArchiveManagerService {
    /// Create a new archive from files
    async fn create_archive_async(
        &self,
        files: Vec<&Path>,
        archive_path: &Path,
        format: ArchiveFormat,
        options: ArchiveOptions,
    ) -> Result<OperationResult, ArchiveError>;

    /// Extract archive to destination
    async fn extract_archive_async(
        &self,
        archive_path: &Path,
        destination: &Path,
        options: ExtractOptions,
    ) -> Result<OperationResult, ArchiveError>;

    /// List contents of an archive
    fn list_archive_contents(
        &self,
        archive_path: &Path,
    ) -> Result<Vec<FileEntry>, ArchiveError>;

    /// Check if a file is an archive
    fn is_archive(&self, path: &Path) -> bool;

    /// Get virtual file system for archive browsing
    fn get_virtual_fs(
        &self,
        archive_path: &Path,
    ) -> Result<Box<dyn VirtualFileSystem>, ArchiveError>;
}
```

### Implementation Details
- Supports ZIP, TAR, TAR.GZ, and 7Z formats
- Provides streaming operations for large archives
- Implements virtual file system for archive browsing
- Handles password-protected archives
- Provides progress reporting for extraction
- Implements archive validation

## JobManager Service

Manages background operations with progress tracking and cancellation.

### Interface
```rust
pub trait JobManagerService {
    /// Submit a new job for execution
    fn submit_job(&self, job: Job) -> JobId;

    /// Cancel a running job
    fn cancel_job(&self, job_id: JobId) -> Result<(), JobError>;

    /// Get status of a job
    fn get_job_status(&self, job_id: JobId) -> Option<JobStatus>;

    /// Get all active jobs
    fn get_active_jobs(&self) -> Vec<JobStatus>;

    /// Set maximum concurrent jobs
    fn set_max_concurrent_jobs(&self, max: usize);

    /// Set job update interval
    fn set_update_interval(&self, interval: Duration);
}
```

### Implementation Details
- Limits concurrent operations based on configuration
- Provides progress updates with configurable frequency
- Implements job prioritization
- Handles job queuing when limits are reached
- Provides job statistics and history
- Implements automatic cleanup of completed jobs

## HistoryManager Service

Tracks navigation history for each pane.

### Interface
```rust
pub trait HistoryManagerService {
    /// Navigate forward in history
    fn forward(&mut self, pane: PaneType) -> Option<&Path>;

    /// Navigate backward in history
    fn backward(&mut self, pane: PaneType) -> Option<&Path>;

    /// Add current path to history
    fn add(&mut self, pane: PaneType, path: &Path);

    /// Clear history for a pane
    fn clear(&mut self, pane: PaneType);

    /// Get current position in history
    fn current(&self, pane: PaneType) -> Option<&Path>;

    /// Check if forward navigation is possible
    fn can_forward(&self, pane: PaneType) -> bool;

    /// Check if backward navigation is possible
    fn can_backward(&self, pane: PaneType) -> bool;

    /// Get history entries for a pane
    fn get_history(&self, pane: PaneType) -> Vec<&Path>;
}
```

### Implementation Details
- Maintains separate histories for left and right panes
- Respects maximum history size configuration
- Prevents duplicate consecutive entries
- Provides history serialization for session persistence
- Implements history navigation with proper bounds checking

## MarkingEngine Service

Handles file marking operations.

### Interface
```rust
pub trait MarkingEngineService {
    /// Mark files matching a pattern
    fn mark_by_pattern(&self, entries: &mut [FileEntry], pattern: &str);

    /// Unmark files matching a pattern
    fn unmark_by_pattern(&self, entries: &mut [FileEntry], pattern: &str);

    /// Toggle mark on a specific file
    fn toggle_mark(&self, entry: &mut FileEntry);

    /// Mark all files in a list
    fn mark_all(&self, entries: &mut [FileEntry]);

    /// Unmark all files in a list
    fn unmark_all(&self, entries: &mut [FileEntry]);

    /// Get marked files from a list
    fn get_marked(&self, entries: &[FileEntry]) -> Vec<&FileEntry>;

    /// Apply marking rules to a list of files
    fn apply_rules(&self, entries: &mut [FileEntry], rules: &[MarkingRule]);
}
```

### Implementation Details
- Supports wildcard patterns for marking
- Implements regular expression matching
- Provides bulk marking operations
- Tracks total size of marked files
- Implements marking persistence across directory changes

## SortEngine Service

Handles file sorting operations.

### Interface
```rust
pub trait SortEngineService {
    /// Sort files based on the specified mode
    fn sort(&self, entries: &mut [FileEntry], mode: SortMode);

    /// Sort files with custom comparison function
    fn sort_by<F>(&self, entries: &mut [FileEntry], compare_fn: F)
    where
        F: FnMut(&FileEntry, &FileEntry) -> std::cmp::Ordering;

    /// Get sort key for a file entry
    fn get_sort_key(&self, entry: &FileEntry, mode: SortMode) -> SortKey;
}
```

### Implementation Details
- Implements all sort modes (name, size, date, type)
- Handles case-insensitive sorting
- Properly sorts directories vs files
- Implements natural sorting for numeric filenames
- Optimizes sorting for large directories

## ViewerManager Service

Manages file viewing capabilities.

### Interface
```rust
pub trait ViewerManagerService {
    /// Open a file in the appropriate viewer
    fn open_file(&self, path: &Path) -> Result<ViewerHandle, ViewerError>;

    /// Check if a file can be viewed
    fn can_view(&self, path: &Path) -> bool;

    /// Get viewer type for a file
    fn get_viewer_type(&self, path: &Path) -> ViewerType;

    /// Get text content of a file (for text viewer)
    fn get_text_content(&self, path: &Path) -> Result<String, ViewerError>;

    /// Get image data of a file (for image viewer)
    fn get_image_data(&self, path: &Path) -> Result<ImageData, ViewerError>;

    /// Get binary preview of a file
    fn get_binary_preview(&self, path: &Path, max_size: usize) -> Result<Vec<u8>, ViewerError>;
}
```

### Implementation Details
- Supports text files with syntax highlighting
- Supports image files with zoom and pan
- Implements binary file preview
- Provides file type detection
- Handles large files efficiently
- Implements content caching

## CustomFunctionManager Service

Executes custom functions and macros.

### Interface
```rust
pub trait CustomFunctionManagerService {
    /// Load custom functions from configuration
    fn load_functions(&mut self, config_path: &Path) -> Result<(), ConfigError>;

    /// Execute a custom function by name
    async fn execute_function(
        &self,
        function_name: &str,
        context: FunctionContext,
    ) -> Result<FunctionResult, FunctionError>;

    /// Expand macros in a string
    fn expand_macros(&self, input: &str, context: &FunctionContext) -> String;

    /// Register a built-in action
    fn register_builtin_action(
        &mut self,
        name: &str,
        handler: Box<dyn Fn(FunctionContext) -> Result<FunctionResult, FunctionError>>,
    );
}
```

### Implementation Details
- Parses and validates custom function definitions
- Implements macro expansion with context variables
- Provides built-in actions for common operations
- Supports parameterized functions
- Implements function composition
- Handles errors gracefully

## Service Dependencies

Services may depend on other services or providers:

- `FileOperationsService` depends on `FileSystemProvider`
- `SearchEngineService` depends on `FileSystemProvider`
- `ArchiveManagerService` depends on `FileSystemProvider`
- `JobManagerService` may coordinate with other services
- `CustomFunctionManagerService` may use other services

## Error Handling

Each service defines its own error types that implement proper error chaining:

```rust
#[derive(Debug, thiserror::Error)]
pub enum FileOperationError {
    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),
    
    #[error("Permission denied: {path}")]
    PermissionDenied { path: PathBuf },
    
    #[error("Disk full")]
    DiskFull,
    
    #[error("Operation cancelled")]
    Cancelled,
}
```

## Configuration Integration

Services receive configuration through their constructors or via a configuration provider:

```rust
impl FileOperationsService {
    pub fn new(config: Arc<RwLock<Configuration>>) -> Self {
        Self { config }
    }
}
```

## Async Patterns

Long-running operations use async/await with proper cancellation:

```rust
async fn operation_with_cancellation(
    &self,
    cancel_token: CancellationToken,
) -> Result<(), ServiceError> {
    tokio::select! {
        result = self.do_work() => result,
        _ = cancel_token.cancelled() => Err(ServiceError::Cancelled),
    }
}
```