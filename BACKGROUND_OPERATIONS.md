# Background Operations System

TWF now supports non-blocking background file operations. This allows you to perform long-running tasks like copying thousands of files while continuing to browse and manage files in other tabs or panes.

## Key Features

- **Non-blocking Execution**: Copy, Move, and Delete operations run in background threads.
- **Task & Log Pane**: A unified status and log area at the bottom of the screen.
- **Job Manager**: A dedicated dialog to monitor, manage, and cancel active jobs.
- **Multi-tab Support**: Each tab tracks its own busy status, indicated by a `~` in the tab bar.
- **Safe Collision Handling**: File collisions (Overwrite/Skip) pause only the specific background job, requesting user input without freezing the rest of the application.

## User Interface

### 1. Task Status View (Log Pane)
Located at the bottom of the main window, this view replaces the old status bar.
- **Collapsed Mode**: Displays a single line with a busy spinner and the progress of the most prominent active job.
- **Expanded Mode**: Shows a scrollable history of all operation logs.

### 2. Job Manager Dialog
Accessed via `Ctrl+J`, this dialog provides a detailed list of all background jobs and allows for manual cancellation.

### 3. Busy Indicators
Tabs with active background operations are marked with a `~` (e.g., `[1:Projects]~*`).

## Key Bindings

| Key | Action | Description |
| :--- | :--- | :--- |
| **Ctrl+J** | `ShowJobManager` | Open the Job Manager dialog. |
| **Ctrl+L** | `ToggleTaskPanel` | Toggle between collapsed and expanded log panel. |
| **Ctrl+Up** | `ResizeTaskPanelUp` | Increase the height of the expanded log panel. |
| **Ctrl+Down** | `ResizeTaskPanelDown` | Decrease the height of the expanded log panel. |
| **Alt+Up** | `ScrollTaskPanelUp` | Scroll up in the log history. |
| **Alt+Down** | `ScrollTaskPanelDown` | Scroll down in the log history. |

## Technical Architecture

### Core Components

1.  **`JobManager` (Service)**: The central registry for all background tasks. It handles job submission, cancellation tokens, and progress reporting.
2.  **`BackgroundJob` (Model)**: Represents a single unit of work, containing its status, progress, and context (Tab ID, Name).
3.  **`TaskStatusView` (UI)**: A custom `Terminal.Gui` view that subscribes to `JobManager` events to provide real-time updates and log persistence.
4.  **`FileOperations` (Service)**: Updated to be fully asynchronous and use `IProgress<T>` for thread-safe UI updates.

### Adding New Background Tasks
To run a new operation in the background, use the `_jobManager.StartJob` method in the `MainController`:

```csharp
_jobManager.StartJob(
    "Job Name",
    "Detailed Description",
    tabId,
    tabName,
    async (token, progress) => {
        // Your async logic here
        // Use progress.Report(new JobProgress { ... }) for updates
    });
```

### Feature Details

   1. CPU Optimization (Throttling):
       * The background job progress updates and the UI spinner are now synchronized to a configurable interval
         (TaskPanelUpdateIntervalMs), defaulting to 300ms.
       * 100ms Minimum: A strict minimum of 100ms is enforced. If the configuration is set lower, TWF will log a
         warning and automatically use 100ms to prevent excessive CPU usage.
       * A warning message will appear in the Task Panel on boot if the setting was too low.

   2. Concurrency Control:
       * Implemented a limit on simultaneous background operations using MaxConcurrentJobs (default: 4).
       * If more jobs are started, they will stay in a Pending state and automatically begin running as previous
         jobs complete.

   3. Boot-time Enforcement:
       * As these settings are critical for system stability and performance, they are loaded only at startup.
       * I have updated the ReloadConfiguration logic and documentation to inform you that changing these
         specific settings requires a restart.

   4. Configuration Settings sample (config.json):
   ```json
    "Display": {
    "TaskPanelUpdateIntervalMs": 300,
    "MaxConcurrentJobs": 4
    }
    ```
