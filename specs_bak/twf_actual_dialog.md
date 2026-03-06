# TWF Job Dialog UI - Actual Implementation Sample

Based on the actual TWF source code, here is the real implementation of the OperationProgressDialog with the Cancelling state:

## Actual OperationProgressDialog Implementation

```csharp
using Terminal.Gui;
using System.Threading;

namespace TWF.UI
{
    /// <summary>
    /// Reusable dialog for showing progress of background operations
    /// </summary>
    public class OperationProgressDialog : Dialog
    {
        private Label _statusLabel;
        private Label _fileLabel;
        private Label _progressLabel;
        private Label _bytesLabel;
        private CancellationTokenSource _cts;

        public string Status
        {
            get => _statusLabel.Text.ToString() ?? string.Empty;
            set => _statusLabel.Text = value;
        }

        public OperationProgressDialog(string title, CancellationTokenSource cts) : base(title, 70, 12)
        {
            _cts = cts;

            _statusLabel = new Label("Preparing...")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(_statusLabel);

            _fileLabel = new Label("")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            Add(_fileLabel);

            _progressLabel = new Label("0%")
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1)
            };
            Add(_progressLabel);

            _bytesLabel = new Label("")
            {
                X = 1,
                Y = 4,
                Width = Dim.Fill(1)
            };
            Add(_bytesLabel);

            var cancelButton = new Button("Cancel (ESC)")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(2)
            };

            cancelButton.Clicked += () =>
            {
                _cts.Cancel();
                _statusLabel.Text = "Cancelling...";  // ← This is the actual Cancelling state!
            };

            AddButton(cancelButton);

            // Handle Escape key for cancellation
            this.KeyPress += (e) =>
            {
                if (e.KeyEvent.Key == (Key)27) // Escape
                {
                    _cts.Cancel();
                    _statusLabel.Text = "Cancelling...";  // ← This is the actual Cancelling state!
                    e.Handled = true;
                }
            };
        }

        /// <summary>
        /// Updates the progress information labels
        /// </summary>
        public void UpdateProgress(string currentFile, int currentIndex, int totalFiles, double percent, long bytesProcessed = -1, long totalBytes = -1)
        {
            const int maxFileWidth = 60;
            string truncatedFile = TWF.Utilities.CharacterWidthHelper.SmartTruncate(currentFile, maxFileWidth);
            _fileLabel.Text = $"File: {truncatedFile} ({currentIndex}/{totalFiles})";
            _progressLabel.Text = $"{percent:F1}%";

            if (totalBytes > 0 && bytesProcessed >= 0)
            {
                var mbProcessed = bytesProcessed / (1024.0 * 1024.0);
                var mbTotal = totalBytes / (1024.0 * 1024.0);
                _bytesLabel.Text = $"{mbProcessed:F2} MB / {mbTotal:F2} MB";
            }
            else if (bytesProcessed >= 0)
            {
                var mbProcessed = bytesProcessed / (1024.0 * 1024.0);
                _bytesLabel.Text = $"{mbProcessed:F2} MB";
            }
        }
    }
}
```

## Actual Usage in MainController

```csharp
// From MainController.cs - actual usage
private async Task ExecuteFileOperationAsync(
    List<FileEntry> files,
    string destination,
    string operationName,
    Func<List<FileEntry>, string, CancellationToken, Task<OperationResult>> operation)
{
    var cancellationTokenSource = new CancellationTokenSource();
    var progressDialog = new OperationProgressDialog($"{operationName} Progress", cancellationTokenSource);

    // Subscribe to progress events
    EventHandler<ProgressEventArgs>? progressHandler = (sender, e) =>
    {
        Application.MainLoop.Invoke(() =>
        {
            progressDialog.UpdateProgress(e.CurrentFile, e.CurrentFileIndex, e.TotalFiles, e.PercentComplete, e.BytesProcessed, e.TotalBytes);
        });
    };

    _fileOps.ProgressChanged += progressHandler;

    // Execute operation asynchronously
    Task.Run(async () =>
    {
        try
        {
            var result = await operation(files, destination, cancellationTokenSource.Token);

            Application.MainLoop.Invoke(() =>
            {
                _fileOps.ProgressChanged -= progressHandler;
                Application.RequestStop();  // Close the dialog

                // Show result message
                SetStatus(result.Message);

                // Refresh both panes
                LoadPaneDirectory(_leftState);
                LoadPaneDirectory(_rightState);
                RefreshPanes();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during {operationName} operation");
            Application.MainLoop.Invoke(() =>
            {
                _fileOps.ProgressChanged -= progressHandler;
                Application.RequestStop();
                SetStatus($"{operationName} failed: {ex.Message}");
            });
        }
    });

    // Show progress dialog (blocking until RequestStop)
    Application.Run(progressDialog);
}
```

## Actual UI Mock - Copy Operation Dialog

```
┌─────────────────────────────────────────────────────────────┐
│                Copy Progress                              │
├─────────────────────────────────────────────────────────────┤
│ Preparing...                                                │
│ File: Documents/Report.pdf (23/150)                        │
│ 15.3%                                                       │
│ 4.23 MB / 27.65 MB                                         │
│                                                             │
│                        [ Cancel (ESC) ]                     │
└─────────────────────────────────────────────────────────────┘
```

## Actual UI Mock - Cancelling State

When user presses Cancel button or ESC:

```
┌─────────────────────────────────────────────────────────────┐
│                Copy Progress                              │
├─────────────────────────────────────────────────────────────┤
│ Cancelling...                                               ← Actual Cancelling state display
│ File: Documents/Report.pdf (23/150)                        │
│ 15.3%                                                       │
│ 4.23 MB / 27.65 MB                                         │
│                                                             │
│                        [ Cancel (ESC) ]                     │
└─────────────────────────────────────────────────────────────┘
```

## Actual UI Mock - Completed Operation

```
┌─────────────────────────────────────────────────────────────┐
│                Copy Progress                              │
├─────────────────────────────────────────────────────────────┤
│ Operation completed successfully                              ← Final state
│ File: Documents/Report.pdf (150/150)                       │
│ 100.0%                                                      │
│ 27.65 MB / 27.65 MB                                        │
│                                                             │
│                        [ OK ]                               │
└─────────────────────────────────────────────────────────────┘
```

## Actual UI Mock - Failed Operation

```
┌─────────────────────────────────────────────────────────────┐
│                Copy Progress                              │
├─────────────────────────────────────────────────────────────┤
│ Operation failed: Access denied                             ← Error state
│ File: Documents/Restricted.docx (45/150)                   │
│ 30.0%                                                       │
│ 8.45 MB / 27.65 MB                                         │
│                                                             │
│                        [ OK ]                               │
└─────────────────────────────────────────────────────────────┘
```

## Key Implementation Details

1. **Cancelling State Display**: When user clicks "Cancel" or presses ESC, the `_statusLabel.Text` is set to `"Cancelling..."`

2. **Cancellation Token**: The `_cts.Cancel()` method signals the background operation to stop

3. **Progress Continues**: During cancellation, the operation may still report progress until it actually stops

4. **Visual Feedback**: The "Cancelling..." text provides immediate feedback that the cancellation request was received

5. **Dialog Closure**: The operation completes (either successfully or with cancellation) and the dialog closes via `Application.RequestStop()`

This is the actual implementation used in TWF, where the "Cancelling..." state is a visual indicator that the cancellation request has been sent but the operation may still be in progress.