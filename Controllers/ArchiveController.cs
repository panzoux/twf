using System;
using System.Collections.Generic;
using System.IO;
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
    /// Specialized controller for archive-related operations including extraction, compression, and virtual folder navigation.
    /// </summary>
    public class ArchiveController
    {
        private readonly ArchiveManager _archiveManager;
        private readonly JobManager _jobManager;
        private readonly Configuration _config;
        private readonly ILogger _logger;

        // UI Callbacks to interact with MainController/UI
        private readonly Action<string> _setStatus;
        private readonly Action<string, IEnumerable<string>?, string?, int?, bool> _refreshPath;
        private readonly Func<PaneState> _getActivePane;
        private readonly Func<PaneState> _getInactivePane;
        private readonly Func<int> _getActiveTabIndex;
        private readonly Func<string, string, bool> _showConfirmation;
        private readonly Action<PaneState, string?, IEnumerable<string>?, int?, bool> _loadPaneDirectory;
        private readonly Func<PaneState, CancellationTokenSource> _setupLoadingCts;
        private readonly Action<PaneState, CancellationTokenSource> _finalizeLoadingCts;
        private readonly Action _refreshPanes;
        private readonly Action<PaneState> _updatePaneStats;
        private readonly Func<List<FileEntry>, SortMode, List<FileEntry>> _sortAction;

        public ArchiveController(
            ArchiveManager archiveManager,
            JobManager jobManager,
            Configuration config,
            ILogger logger,
            Action<string> setStatus,
            Action<string, IEnumerable<string>?, string?, int?, bool> refreshPath,
            Func<PaneState> getActivePane,
            Func<PaneState> getInactivePane,
            Func<int> getActiveTabIndex,
            Func<string, string, bool> showConfirmation,
            Action<PaneState, string?, IEnumerable<string>?, int?, bool> loadPaneDirectory,
            Func<PaneState, CancellationTokenSource> setupLoadingCts,
            Action<PaneState, CancellationTokenSource> finalizeLoadingCts,
            Action refreshPanes,
            Action<PaneState> updatePaneStats,
            Func<List<FileEntry>, SortMode, List<FileEntry>> sortAction)
        {
            _archiveManager = archiveManager ?? throw new ArgumentNullException(nameof(archiveManager));
            _jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _setStatus = setStatus;
            _refreshPath = refreshPath;
            _getActivePane = getActivePane;
            _getInactivePane = getInactivePane;
            _getActiveTabIndex = getActiveTabIndex;
            _showConfirmation = showConfirmation;
            _loadPaneDirectory = loadPaneDirectory;
            _setupLoadingCts = setupLoadingCts;
            _finalizeLoadingCts = finalizeLoadingCts;
            _refreshPanes = refreshPanes;
            _updatePaneStats = updatePaneStats;
            _sortAction = sortAction;
        }

        /// <summary>
        /// Handles the extraction of the currently selected archive.
        /// </summary>
        public void HandleExtraction()
        {
            var activePane = _getActivePane();
            var inactivePane = _getInactivePane();
            var currentEntry = activePane.GetCurrentEntry();
            
            if (currentEntry == null) return;
            
            if (!_archiveManager.IsArchive(currentEntry.FullPath))
            {
                _setStatus("Not an archive file");
                return;
            }
            
            try
            {
                _logger.LogDebug($"Extracting archive: {currentEntry.FullPath}");
                string destination = inactivePane.CurrentPath;

                // Safety check: Peek into archive to find real conflicts in the destination
                if (Directory.Exists(destination))
                {
                    try
                    {
                        var archiveEntries = _archiveManager.ListArchiveContents(currentEntry.FullPath, "");
                        string firstConflict = "";
                        foreach (var entry in archiveEntries)
                        {
                            string checkPath = Path.Combine(destination, entry.Name);
                            if (File.Exists(checkPath) || Directory.Exists(checkPath))
                            {
                                firstConflict = checkPath;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(firstConflict))
                        {
                            if (!_showConfirmation("Overwrite Warning", $"The item '{firstConflict}' already exists in the destination. Overwrite existing files?"))
                            {
                                _setStatus("Extraction cancelled");
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to peek into archive for conflict check");
                    }
                }

                if (!_showConfirmation("Extract Archive", $"Extract '{currentEntry.Name}' to '{destination}'?"))
                {
                    _setStatus("Extraction cancelled");
                    return;
                }
                
                int tabIndex = _getActiveTabIndex();
                _jobManager.StartJob(
                    name: "Extract",
                    description: currentEntry.Name,
                    tabId: tabIndex,
                    tabName: $"Tab {tabIndex + 1}",
                    action: async (job, token, jobProgress) => 
                    {
                        lock (job.RelatedPaths) { job.RelatedPaths.Add(currentEntry.FullPath); }

                        bool initialRefreshDone = false;
                        var progressHandler = new Progress<(string CurrentFile, string CurrentFullPath, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>(report =>
                        {
                            double percent = 0;
                            if (report.TotalFiles > 0)
                                percent = (double)report.ProcessedFiles / report.TotalFiles * 100;
                            
                            string progressInfo = report.TotalFiles > 0 ? $"{report.ProcessedFiles}/{report.TotalFiles}" : "";

                            if (!string.IsNullOrEmpty(report.CurrentFullPath))
                            {
                                lock (job.RelatedPaths) 
                                { 
                                    job.RelatedPaths.Add(report.CurrentFullPath); 
                                    var relative = Path.GetRelativePath(destination, report.CurrentFullPath);
                                    if (!relative.StartsWith(".."))
                                    {
                                        var parts = relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                                        string currentRelPath = "";
                                        foreach (var part in parts)
                                        {
                                            currentRelPath = string.IsNullOrEmpty(currentRelPath) ? part : Path.Combine(currentRelPath, part);
                                            var fullPath = Path.Combine(destination, currentRelPath);
                                            job.RelatedPaths.Add(fullPath);
                                        }
                                    }
                                }
                            }

                            jobProgress.Report(new JobProgress { 
                                Percent = percent, 
                                Message = $"Extracting {report.CurrentFile}",
                                CurrentOperationDetail = progressInfo,
                                CurrentItemFullPath = report.CurrentFullPath
                            });

                            if (!initialRefreshDone)
                            {
                                initialRefreshDone = true;
                                Application.MainLoop.Invoke(() => _refreshPath(destination, null, null, null, false));
                            }
                        });

                        try
                        {
                            var result = await _archiveManager.ExtractAsync(currentEntry.FullPath, destination, progressHandler, token);
                            if (result.Success)
                            {
                                _setStatus($"Extracted {result.FilesProcessed} file(s) from {currentEntry.Name}");
                                Application.MainLoop.Invoke(() => _refreshPath(destination, null, null, null, false));
                            }
                            else
                            {
                                _setStatus($"Extraction failed: {result.Message}");
                            }
                        }
                        catch (OperationCanceledException) { _setStatus("Extraction cancelled"); }
                        catch (Exception ex) { ErrorHelper.Handle(ex, "Extraction failed"); }
                    }
                );
            }
            catch (Exception ex) { ErrorHelper.Handle(ex, "Error extracting archive"); }
        }

        /// <summary>
        /// Opens an archive file as a virtual folder
        /// </summary>
        public void OpenArchiveAsVirtualFolder(string archivePath)
        {
            _ = OpenArchiveAsVirtualFolderAsync(archivePath);
        }

        private async Task OpenArchiveAsVirtualFolderAsync(string archivePath)
        {
            var activePane = _getActivePane();
            string originalPath = activePane.CurrentPath;
            var cts = _setupLoadingCts(activePane);
            
            try
            {
                _logger.LogDebug($"Opening archive async: {archivePath}");
                
                // Get archive contents BEFORE changing pane state
                var archiveEntries = await _archiveManager.ListArchiveContentsAsync(archivePath, "", cts.Token);
                
                if (cts.Token.IsCancellationRequested) return;

                Application.MainLoop.Invoke(() => 
                {
                    UpdatePaneWithArchive(activePane, archivePath, originalPath, archiveEntries);
                });
            }
            catch (Exception ex)
            {
                HandleArchiveOpenError(activePane, archivePath, ex);
            }
            finally
            {
                _finalizeLoadingCts(activePane, cts);
            }
        }

        private void UpdatePaneWithArchive(PaneState pane, string archivePath, string parentPath, List<FileEntry> entries)
        {
            pane.VirtualFolderParentPath = parentPath;
            pane.VirtualFolderArchivePath = archivePath;
            pane.VirtualFolderInternalPath = "";
            pane.IsInVirtualFolder = true;
            
            UpdateVirtualFolderPath(pane);
            
            pane.Entries = _sortAction(entries, pane.SortMode);
            pane.CursorPosition = 0;
            pane.ScrollOffset = 0;
            _updatePaneStats(pane);
            
            _refreshPanes();
            _logger.LogDebug($"Viewing archive: {Path.GetFileName(archivePath)} ({entries.Count} entries)");
        }

        private void HandleArchiveOpenError(PaneState pane, string archivePath, Exception ex)
        {
            _logger.LogError(ex, $"Failed to open archive: {archivePath}");
            Application.MainLoop.Invoke(() => 
            {
                ErrorHelper.Handle(ex, "Error opening archive");
                _refreshPanes();
            });
        }

        public void NavigateUpInVirtualFolder(PaneState pane)
        {
            if (string.IsNullOrEmpty(pane.VirtualFolderInternalPath)) return;

            string currentDirName = Path.GetFileName(pane.VirtualFolderInternalPath);
            int lastSlash = pane.VirtualFolderInternalPath.LastIndexOf('/');
            if (lastSlash >= 0)
                pane.VirtualFolderInternalPath = pane.VirtualFolderInternalPath.Substring(0, lastSlash);
            else
                pane.VirtualFolderInternalPath = "";
            
            _loadPaneDirectory(pane, currentDirName, null, null, false);
            _refreshPanes();
            _logger.LogDebug($"Navigated up in archive to: {pane.VirtualFolderInternalPath}");
        }

        public void NavigateIntoVirtualDirectory(PaneState pane, string dirName)
        {
            if (string.IsNullOrEmpty(pane.VirtualFolderInternalPath))
                pane.VirtualFolderInternalPath = dirName;
            else
                pane.VirtualFolderInternalPath = pane.VirtualFolderInternalPath.TrimEnd('/') + "/" + dirName;
            
            _loadPaneDirectory(pane, null, null, null, false);
            _refreshPanes();
            _logger.LogDebug($"Navigated into archive directory: {pane.VirtualFolderInternalPath}");
        }

        public void UpdateVirtualFolderPath(PaneState pane)
        {
            if (!pane.IsInVirtualFolder || string.IsNullOrEmpty(pane.VirtualFolderArchivePath)) return;

            string archiveName = Path.GetFileName(pane.VirtualFolderArchivePath);
            if (string.IsNullOrEmpty(pane.VirtualFolderInternalPath))
                pane.CurrentPath = $"[{archiveName}]";
            else
                pane.CurrentPath = $"[{archiveName}]/{pane.VirtualFolderInternalPath.Replace('/', Path.DirectorySeparatorChar)}";
        }

        public void ExitVirtualFolder(PaneState pane)
        {
            if (pane.VirtualFolderParentPath == null) return;

            _logger.LogDebug($"Exiting archive, returning to: {pane.VirtualFolderParentPath}");
            
            string parentPath = pane.VirtualFolderParentPath;
            string? archiveName = !string.IsNullOrEmpty(pane.VirtualFolderArchivePath) 
                ? Path.GetFileName(pane.VirtualFolderArchivePath) : null;
            
            pane.IsInVirtualFolder = false;
            pane.VirtualFolderArchivePath = null;
            pane.VirtualFolderParentPath = null;
            pane.VirtualFolderInternalPath = null;
            
            pane.CurrentPath = parentPath;
            _loadPaneDirectory(pane, archiveName, null, null, false);
            _refreshPanes();
            _logger.LogDebug($"Exited archive");
        }

        public void HandleCompressionOperation()
        {
            var activePane = _getActivePane();
            var inactivePane = _getInactivePane();
            
            // Get files to compress (marked files or current file)
            var filesToCompress = activePane.GetMarkedEntries();
            if (filesToCompress.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToCompress = new List<FileEntry> { currentEntry };
                }
            }
            
            if (filesToCompress.Count == 0)
            {
                _setStatus("No files to compress");
                return;
            }
            
            try
            {
                // Show compression dialog to select format and archive name
                var (archiveFormat, archiveName, compressionLevel, confirmed) = ShowCompressionDialog(filesToCompress);
                
                if (!confirmed)
                {
                    _setStatus("Compression cancelled");
                    return;
                }
                
                // Determine the full archive path in the opposite pane
                var archivePath = Path.Combine(inactivePane.CurrentPath, archiveName);
                
                // Safety check: if target archive already exists, ask for overwrite confirmation
                if (File.Exists(archivePath))
                {
                    if (!_showConfirmation("Overwrite Warning", $"Archive '{archiveName}' already exists. Overwrite?"))
                    {
                        _setStatus("Compression cancelled");
                        return;
                    }
                }

                // Create a unique temporary filename to avoid collisions and allow coloring
                string tempPath = archivePath + $".{Guid.NewGuid():N}.tmp";

                // Calculate original size for compression ratio
                long originalSize = 0;
                foreach (var f in filesToCompress)
                {
                    if (!f.IsDirectory) originalSize += f.Size;
                }
                
                int tabIndex = _getActiveTabIndex();
                // Execute compression as a background job
                _jobManager.StartJob(
                    name: "Compress",
                    description: Path.GetFileName(archivePath),
                    tabId: tabIndex,
                    tabName: $"Tab {tabIndex + 1}",
                    action: async (job, token, jobProgress) => 
                    {
                        // Add temp path to related paths for coloring
                        lock (job.RelatedPaths) { job.RelatedPaths.Add(tempPath); }

                        string? destDir = Path.GetDirectoryName(archivePath);
                        bool initialRefreshDone = false;

                        var progressHandler = new Progress<(string CurrentFile, string CurrentFullPath, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>(report =>
                        {
                            double percent = 0;
                            if (report.TotalBytes > 0)
                                percent = (double)report.ProcessedBytes / report.TotalBytes * 100;
                            else if (report.TotalFiles > 0)
                                percent = (double)report.ProcessedFiles / report.TotalFiles * 100;
                            
                            // Report to JobManager
                            string sizeInfo = report.TotalBytes > 0 
                                ? $"{report.ProcessedBytes / 1048576.0:F1}MB / {report.TotalBytes / 1048576.0:F1}MB"
                                : $"{report.ProcessedFiles}/{report.TotalFiles}";

                            // Update related paths for coloring
                            if (!string.IsNullOrEmpty(report.CurrentFullPath))
                            {
                                lock (job.RelatedPaths) { job.RelatedPaths.Add(report.CurrentFullPath); }
                            }

                            jobProgress.Report(new JobProgress { 
                                Percent = percent, 
                                Message = $"Compressing {report.CurrentFile}",
                                CurrentOperationDetail = sizeInfo,
                                CurrentItemFullPath = report.CurrentFullPath
                            });

                            // Force an initial refresh once we have progress (meaning file exists)
                            if (!initialRefreshDone && !string.IsNullOrEmpty(destDir))
                            {
                                initialRefreshDone = true;
                                Application.MainLoop.Invoke(() => _refreshPath(destDir, null, null, null, false));
                            }
                        });

                        try
                        {
                            var result = await _archiveManager.CompressAsync(filesToCompress, tempPath, archiveFormat, compressionLevel, progressHandler, token);
                            
                            if (result.Success)
                            {
                                // Atomic move to final destination
                                if (File.Exists(archivePath)) File.Delete(archivePath);
                                File.Move(tempPath, archivePath);
                                _setStatus($"Compressed to {archiveName}");
                                if (!string.IsNullOrEmpty(destDir))
                                {
                                    Application.MainLoop.Invoke(() => _refreshPath(destDir, null, archiveName, null, false));
                                }
                            }
                            else if (result.Message != "Compression cancelled by user")
                            {
                                var error = result.Message + (result.Errors.Count > 0 ? ": " + result.Errors[0] : "");
                                throw new Exception(error);
                            }
                        }
                        catch (OperationCanceledException) { try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { } }
                        catch (Exception ex) 
                        { 
                            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                            ErrorHelper.Handle(ex, "Compression failed"); 
                        }
                    }
                );
            }
            catch (Exception ex) { ErrorHelper.Handle(ex, "Error in compression operation"); }
        }

        private (ArchiveFormat format, string archiveName, int level, bool confirmed) ShowCompressionDialog(List<FileEntry> filesToCompress)
        {
            var defaultName = filesToCompress.Count == 1 
                ? Path.GetFileNameWithoutExtension(filesToCompress[0].Name) 
                : "archive";

            var supportedFormats = _archiveManager.GetSupportedFormats();
            var dialog = new CompressionOptionsDialog(filesToCompress.Count, defaultName, supportedFormats);
            Application.Run(dialog);

            if (dialog.IsOk)
            {
                return (dialog.SelectedFormat, dialog.ArchiveName, dialog.SelectedCompressionLevel, true);
            }

            return (ArchiveFormat.ZIP, string.Empty, 5, false);
        }

        public void HandleArchiveCopyOut(PaneState activePane, PaneState inactivePane, List<FileEntry> filesToCopy)
        {
            int tabId = _getActiveTabIndex();
            string tabName = $"Tab {tabId + 1}";
            string archivePath = activePane.VirtualFolderArchivePath!;
            string destination = inactivePane.CurrentPath;

            var entryNames = new List<string>(filesToCopy.Count);
            foreach (var f in filesToCopy)
            {
                entryNames.Add(Path.GetRelativePath(archivePath, f.FullPath));
            }

            _jobManager.StartJob(
                $"Extract from {Path.GetFileName(archivePath)}",
                $"Extracting {filesToCopy.Count} items",
                tabId,
                tabName,
                async (job, token, jobProgress) => 
                {
                    var progressHandler = new Progress<(string CurrentFile, string CurrentFullPath, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>(report =>
                    {
                        double percent = 0;
                        if (report.TotalFiles > 0)
                            percent = (double)report.ProcessedFiles / report.TotalFiles * 100;
                        
                        jobProgress.Report(new JobProgress { 
                            Percent = percent, 
                            Message = $"Extracting {report.CurrentFile}"
                        });
                    });

                    var result = await _archiveManager.ExtractEntriesAsync(archivePath, entryNames, destination, progressHandler, token);
                    
                    if (!result.Success && result.Message != "Operation cancelled by user")
                    {
                         throw new Exception(result.Message);
                    }

                    Application.MainLoop.Invoke(() => _loadPaneDirectory(inactivePane, null, null, null, false));
                },
                archivePath,
                destination);
                
            _setStatus("Extraction started in background");
        }

        public void HandleArchiveDelete(PaneState activePane, List<FileEntry> filesToDelete)
        {
            int tabId = _getActiveTabIndex();
            string tabName = $"Tab {tabId + 1}";
            string archivePath = activePane.VirtualFolderArchivePath!;

            var entryNames = new List<string>(filesToDelete.Count);
            foreach (var f in filesToDelete)
            {
                entryNames.Add(Path.GetRelativePath(archivePath, f.FullPath));
            }

            _jobManager.StartJob(
                $"Delete from {Path.GetFileName(archivePath)}",
                $"Deleting {filesToDelete.Count} items",
                tabId,
                tabName,
                async (job, token, progress) => 
                {
                    var result = await _archiveManager.DeleteEntriesAsync(archivePath, entryNames, token);
                    
                    if (!result.Success && result.Message != "Operation cancelled by user")
                    {
                         throw new Exception(result.Message);
                    }

                    Application.MainLoop.Invoke(() => _loadPaneDirectory(activePane, null, null, null, false));
                },
                archivePath);
                
            _setStatus("Archive deletion started in background");
        }
    }
}
