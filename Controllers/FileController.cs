using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;
using TWF.UI;
using Terminal.Gui;

namespace TWF.Controllers
{
    /// <summary>
    /// Specialized controller for high-level file system operations (Copy, Move, Delete, Rename, Create).
    /// </summary>
    public class FileController
    {
        private readonly FileOperations _fileOps;
        private readonly JobManager _jobManager;
        private readonly Configuration _config;
        private readonly ILogger _logger;

        // UI Callbacks
        private readonly Action<string> _setStatus;
        private readonly Action<string, IEnumerable<string>?, string?, int?, bool> _refreshPath;
        private readonly Func<PaneState> _getActivePane;
        private readonly Func<PaneState> _getInactivePane;
        private readonly Func<int> _getActiveTabIndex;
        private readonly Func<string, string, bool> _showConfirmation;
        private readonly Action<string, string> _showMessage;

        public FileController(
            FileOperations fileOps,
            JobManager jobManager,
            Configuration config,
            ILogger logger,
            Action<string> setStatus,
            Action<string, IEnumerable<string>?, string?, int?, bool> refreshPath,
            Func<PaneState> getActivePane,
            Func<PaneState> getInactivePane,
            Func<int> getActiveTabIndex,
            Func<string, string, bool> showConfirmation,
            Action<string, string> showMessage)
        {
            _fileOps = fileOps ?? throw new ArgumentNullException(nameof(fileOps));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _setStatus = setStatus;
            _refreshPath = refreshPath;
            _getActivePane = getActivePane;
            _getInactivePane = getInactivePane;
            _getActiveTabIndex = getActiveTabIndex;
            _showConfirmation = showConfirmation;
            _showMessage = showMessage;
        }

        /// <summary>
        /// Handles the copy operation from active to inactive pane.
        /// </summary>
        public void HandleCopyOperation()
        {
            var activePane = _getActivePane();
            var inactivePane = _getInactivePane();

            var filesToCopy = activePane.GetMarkedEntries();
            if (filesToCopy.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToCopy = new List<FileEntry> { currentEntry };
                }
            }

            if (filesToCopy.Count == 0)
            {
                _setStatus("No files to copy");
                return;
            }

            if (!CheckIfBusy(filesToCopy, "copying")) return;

            string destination = inactivePane.CurrentPath;
            ExecuteFileOperationWithProgress(
                "Copy",
                filesToCopy,
                destination,
                (files, dest, token, handler) => _fileOps.CopyAsync(files, dest, token, handler)
            );
        }

        /// <summary>
        /// Handles the move operation from active to inactive pane.
        /// </summary>
        public void HandleMoveOperation()
        {
            var activePane = _getActivePane();
            var inactivePane = _getInactivePane();

            var filesToMove = activePane.GetMarkedEntries();
            if (filesToMove.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToMove = new List<FileEntry> { currentEntry };
                }
            }

            if (filesToMove.Count == 0)
            {
                _setStatus("No files to move");
                return;
            }

            if (!CheckIfBusy(filesToMove, "moving")) return;

            string destination = inactivePane.CurrentPath;
            ExecuteFileOperationWithProgress(
                "Move",
                filesToMove,
                destination,
                (files, dest, token, handler) => _fileOps.MoveAsync(files, dest, token, handler)
            );
        }

        /// <summary>
        /// Handles the delete operation for marked or current files.
        /// </summary>
        public void HandleDeleteOperation()
        {
            var activePane = _getActivePane();
            var filesToDelete = activePane.GetMarkedEntries();
            if (filesToDelete.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToDelete = new List<FileEntry> { currentEntry };
                }
            }

            if (filesToDelete.Count == 0)
            {
                _setStatus("No files to delete");
                return;
            }

            if (!CheckIfBusy(filesToDelete, "deletion")) return;

            // Preview message
            var previewList = new List<string>();
            int limit = Math.Min(3, filesToDelete.Count);
            for (int i = 0; i < limit; i++)
            {
                var f = filesToDelete[i];
                previewList.Add(f.IsDirectory ? f.Name + "/" : f.Name);
            }
            var fileList = string.Join(", \n", previewList);
            if (filesToDelete.Count > 3) fileList += $"\n and {filesToDelete.Count - 3} more";

            int fileCount = 0; int dirCount = 0;
            foreach (var e in filesToDelete)
            {
                if (e.IsDirectory) dirCount++; else fileCount++;
            }

            string deleteMessage;
            if (fileCount > 0 && dirCount > 0)
                deleteMessage = $"Delete {fileCount} files and {dirCount} directories?\n\n{fileList}";
            else if (dirCount > 0)
                deleteMessage = $"Delete {dirCount} directories and their contents?\n\n{fileList}";
            else
                deleteMessage = $"Delete {fileCount} files?\n\n{fileList}";

            if (!_showConfirmation("Delete", deleteMessage))
            {
                _setStatus("Delete cancelled");
                return;
            }

            ExecuteFileOperationWithProgress(
                "Delete",
                filesToDelete,
                string.Empty,
                (files, _, token, handler) => _fileOps.DeleteAsync(files, token)
            );
        }

        /// <summary>
        /// Handles simple renaming of a single file.
        /// </summary>
        public void HandleSimpleRename()
        {
            try
            {
                var activePane = _getActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry == null)
                {
                    _setStatus("No file selected");
                    return;
                }

                string? newName = SimpleRenameDialog.Show(currentEntry.Name, _config.Display);
                if (string.IsNullOrWhiteSpace(newName) || newName == currentEntry.Name) return;

                if (!CheckIfBusy(new List<FileEntry> { currentEntry }, "renaming")) return;

                string oldPath = currentEntry.FullPath;
                string newPath = Path.Combine(Path.GetDirectoryName(oldPath) ?? "", newName);

                if (Path.Exists(newPath))
                {
                    _setStatus($"Already exists: {newName}");
                    return;
                }

                File.Move(oldPath, newPath);
                _setStatus($"Renamed to: {newName}");
                _refreshPath(Path.GetDirectoryName(oldPath) ?? string.Empty, null, newName, activePane.ScrollOffset, false);
            }
            catch (Exception ex)
            {
                ErrorHelper.Handle(ex, "Error in simple rename");
            }
        }

        /// <summary>
        /// Handles batch renaming using a pattern.
        /// </summary>
        public void HandlePatternRename()
        {
            var activePane = _getActivePane();
            var filesToRename = activePane.GetMarkedEntries();
            if (filesToRename.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null) filesToRename = new List<FileEntry> { currentEntry };
            }

            if (filesToRename.Count == 0)
            {
                _setStatus("No files to rename");
                return;
            }

            if (!CheckIfBusy(filesToRename, "renaming")) return;

            var renameResult = PatternRenameDialog.Show(filesToRename, _config.Display);
            if (renameResult == null)
            {
                _setStatus("Rename cancelled");
                return;
            }

            ExecuteFileOperationWithProgress(
                "Rename",
                filesToRename,
                string.Empty,
                (files, _, token, handler) => _fileOps.RenameAsync(files, renameResult.Value.Pattern, renameResult.Value.Replacement, token, handler)
            );
        }

        /// <summary>
        /// Handles directory creation dialog and logic.
        /// </summary>
        public void HandleCreateDirectory()
        {
            try
            {
                string? directoryName = CreateDirectoryDialog.Show(_config.Display);
                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    if (directoryName != null) _setStatus("No directory name entered");
                    else _setStatus("Directory creation cancelled");
                    return;
                }

                var activePane = _getActivePane();
                var result = _fileOps.CreateDirectory(activePane.CurrentPath, directoryName);
                if (result.Success)
                {
                    _setStatus($"Directory created: {directoryName}");
                    _refreshPath(activePane.CurrentPath, null, directoryName, null, false);
                }
                else
                {
                    _setStatus($"Failed to create directory: {result.Message}");
                }
            }
            catch (Exception ex) { ErrorHelper.Handle(ex, "Error creating directory"); }
        }

        /// <summary>
        /// Handles new file creation dialog and logic.
        /// </summary>
        public void HandleEditNewFile()
        {
            try
            {
                string? fileName = CreateNewFileDialog.Show(_config.Display);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    if (fileName != null) _setStatus("No file name entered");
                    else _setStatus("File creation cancelled");
                    return;
                }

                var activePane = _getActivePane();
                var invalidChars = Path.GetInvalidFileNameChars();
                if (fileName.IndexOfAny(invalidChars) >= 0)
                {
                    _setStatus("Invalid file name: contains invalid characters");
                    return;
                }

                string fullPath = Path.Combine(activePane.CurrentPath, fileName);
                if (File.Exists(fullPath))
                {
                    _setStatus($"File already exists: {fileName}");
                    return;
                }

                File.Create(fullPath).Dispose();
                _setStatus($"Created: {fileName}");
                _refreshPath(activePane.CurrentPath, null, fileName, null, false);
                
                // Note: The logic to open the editor remains in MainController as it requires 
                // launching external processes which is currently better handled there or via ExternalAppLauncher.
            }
            catch (Exception ex) { ErrorHelper.Handle(ex, "Error creating file"); }
        }

        private void ExecuteFileOperationWithProgress(
            string operationName,
            List<FileEntry> files,
            string destination,
            Func<List<FileEntry>, string, CancellationToken, Func<string, Task<FileCollisionResult>>?, Task<OperationResult>> operation)
        {
            int tabId = _getActiveTabIndex();
            string tabName = $"Tab {tabId + 1}";
            string sourceDir = _getActivePane().CurrentPath;

            _jobManager.StartJob(
                name: operationName,
                description: $"{operationName} {files.Count} items",
                tabId: tabId,
                tabName: tabName,
                action: async (job, token, jobProgress) =>
                {
                    var progressHandler = new Progress<ProgressEventArgs>(e =>
                    {
                        double percent = e.PercentComplete;
                        jobProgress.Report(new JobProgress
                        {
                            Percent = percent,
                            Message = $"{operationName}ing {e.CurrentFile}",
                            CurrentOperationDetail = $"Item {e.CurrentFileIndex}/{e.TotalFiles}",
                            CurrentItemFullPath = e.SourcePath
                        });
                    });

                    _fileOps.ProgressChanged += (s, e) => ((IProgress<ProgressEventArgs>)progressHandler).Report(e);

                    try
                    {
                        var result = await operation(files, destination, token, HandleCollision);
                        _setStatus(result.Message);
                        Application.MainLoop.Invoke(() =>
                        {
                            _refreshPath(sourceDir, null, null, null, false);
                            if (!string.IsNullOrEmpty(destination)) _refreshPath(destination, null, null, null, false);
                        });
                    }
                    catch (OperationCanceledException) { _setStatus($"{operationName} cancelled"); }
                    catch (Exception ex) { ErrorHelper.Handle(ex, $"{operationName} failed"); }
                },
                sourceDir,
                destination);

            _setStatus($"{operationName} operation started in background");
        }

        private Task<FileCollisionResult> HandleCollision(string destPath)
        {
            var tcs = new TaskCompletionSource<FileCollisionResult>();
            Application.MainLoop.Invoke(() =>
            {
                var result = FileCollisionDialog.Show(Path.GetFileName(destPath), _config.Display);
                tcs.SetResult(result);
            });
            return tcs.Task;
        }

        private bool CheckIfBusy(List<FileEntry> entries, string operationName)
        {
            var busyPaths = new HashSet<string>(_jobManager.GetBusyPaths(), StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (busyPaths.Contains(entry.FullPath))
                {
                    string message = entries.Count == 1
                        ? $"'{entry.Name}' is currently being used by a background job."
                        : $"One or more items (including '{entry.Name}') are currently being used by a background job.";

                    return _showConfirmation("Safety Warning", $"{message}\n\nDo you want to proceed with {operationName} anyway?");
                }
            }
            return true;
        }
    }
}
