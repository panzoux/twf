namespace TWF.Services
{
    using System.IO.Compression;
    using TWF.Models;
    using Microsoft.Extensions.Logging;
    using TWF.Infrastructure;

    /// <summary>
    /// Archive provider for ZIP format using System.IO.Compression
    /// </summary>
    public class ZipArchiveProvider : IArchiveProvider
    {
        private readonly ILogger<ZipArchiveProvider> _logger;

        public ZipArchiveProvider()
        {
            _logger = LoggingConfiguration.GetLogger<ZipArchiveProvider>();
        }

        public string[] SupportedExtensions => new[] { ".zip" };

        public List<FileEntry> List(string archivePath)
        {
            var entries = new List<FileEntry>();

            try
            {
                using var archive = ZipFile.OpenRead(archivePath);
                foreach (var entry in archive.Entries)
                {
                    // Skip directory entries (they end with /)
                    if (entry.FullName.EndsWith("/"))
                        continue;

                    entries.Add(new FileEntry
                    {
                        FullPath = Path.Combine(archivePath, entry.FullName),
                        Name = entry.Name,
                        Extension = Path.GetExtension(entry.Name),
                        Size = entry.Length,
                        LastModified = entry.LastWriteTime.DateTime,
                        Attributes = FileAttributes.Normal,
                        IsDirectory = false,
                        IsArchive = false,
                        IsVirtualFolder = true
                    });
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to list archive contents: {ex.Message}", ex);
            }

            return entries;
        }

        public async Task<OperationResult> Extract(string archivePath, string destination, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var result = new OperationResult { Success = true };

            try
            {
                // Ensure destination directory exists
                Directory.CreateDirectory(destination);

                using var archive = ZipFile.OpenRead(archivePath);
                var totalEntries = archive.Entries.Count;
                var processedEntries = 0;

                foreach (var entry in archive.Entries)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Success = false;
                        result.Message = "Extraction cancelled by user";
                        break;
                    }

                    // Skip directory entries
                    if (entry.FullName.EndsWith("/"))
                        continue;

                    var destinationPath = Path.Combine(destination, entry.FullName);
                    
                    // Ensure the directory exists
                    var directoryPath = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Extract the file
                    await Task.Run(() => entry.ExtractToFile(destinationPath, overwrite: true), cancellationToken);
                    
                    processedEntries++;
                }

                result.FilesProcessed = processedEntries;
                result.Message = $"Extracted {processedEntries} files from archive";
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = "Extraction cancelled by user";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Extraction failed: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            result.Duration = DateTime.Now - startTime;
            return result;
        }

        public async Task<OperationResult> ExtractEntries(string archivePath, List<string> entryNames, string destination, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var result = new OperationResult { Success = true };

            try
            {
                _logger.LogDebug("ExtractEntries started: archive={ArchivePath}, destination={Destination}, entryCount={Count}", archivePath, destination, entryNames.Count);
                Directory.CreateDirectory(destination);

                using var archive = ZipFile.OpenRead(archivePath);
                var processedEntries = 0;
                
                // Normalize targets: forward slashes and no trailing slash
                var targets = entryNames.Select(n => n.Replace('\\', '/').TrimEnd('/')).ToList();
                _logger.LogDebug("Normalized targets: {Targets}", string.Join(", ", targets));

                foreach (var entry in archive.Entries)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Success = false;
                        result.Message = "Operation cancelled by user";
                        _logger.LogInformation("Extraction cancelled by user");
                        break;
                    }

                    // Normalize entry name for comparison (no trailing slash)
                    string normalizedEntryName = entry.FullName.Replace('\\', '/').TrimEnd('/');

                    // Find which target matches this entry
                    string? matchingTarget = targets.FirstOrDefault(t => 
                        normalizedEntryName.Equals(t, StringComparison.OrdinalIgnoreCase) || 
                        normalizedEntryName.StartsWith(t + "/", StringComparison.OrdinalIgnoreCase));

                    if (matchingTarget == null || entry.FullName.EndsWith("/")) continue;

                    // Calculate destination path relative to the target's parent
                    // e.g. If target is "Tests/a.txt", we strip "Tests/" to get "a.txt"
                    string relativePath;
                    int lastSlash = matchingTarget.LastIndexOf('/');
                    if (lastSlash >= 0)
                    {
                        string prefixToStrip = matchingTarget.Substring(0, lastSlash + 1);
                        relativePath = entry.FullName.Substring(prefixToStrip.Length);
                    }
                    else
                    {
                        relativePath = entry.FullName;
                    }

                    var destinationPath = Path.Combine(destination, relativePath);
                    var directoryPath = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    _logger.LogDebug("Extracting entry: {EntryName} -> {DestPath} (Stripped relative: {Relative})", 
                        entry.FullName, destinationPath, relativePath);
                    
                    await Task.Run(() => entry.ExtractToFile(destinationPath, overwrite: true), cancellationToken);
                    processedEntries++;
                }

                result.FilesProcessed = processedEntries;
                result.Message = $"Extracted {processedEntries} items from archive";
                _logger.LogInformation("Extraction completed: {Message}", result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Extraction failed: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "Extraction failed for {ArchivePath}", archivePath);
            }

            result.Duration = DateTime.Now - startTime;
            return result;
        }

        public async Task<OperationResult> DeleteEntries(string archivePath, List<string> entryNames, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var result = new OperationResult { Success = true };

            try
            {
                _logger.LogDebug("DeleteEntries started: archive={ArchivePath}, entryCount={Count}", archivePath, entryNames.Count);
                using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
                var processedEntries = 0;
                
                // Normalize targets: forward slashes and no trailing slash
                var targets = entryNames.Select(n => n.Replace('\\', '/').TrimEnd('/')).ToList();
                _logger.LogDebug("Normalized targets: {Targets}", string.Join(", ", targets));

                var toDelete = new List<ZipArchiveEntry>();
                foreach (var entry in archive.Entries)
                {
                    // Normalize entry name for comparison (no trailing slash)
                    string normalizedEntryName = entry.FullName.Replace('\\', '/').TrimEnd('/');

                    bool matches = targets.Any(t => 
                        normalizedEntryName.Equals(t, StringComparison.OrdinalIgnoreCase) || 
                        normalizedEntryName.StartsWith(t + "/", StringComparison.OrdinalIgnoreCase));
                    
                    if (matches)
                    {
                        toDelete.Add(entry);
                    }
                }

                _logger.LogDebug("Identified {Count} entries to delete in archive", toDelete.Count);

                foreach (var entry in toDelete)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.Success = false;
                        result.Message = "Operation cancelled by user";
                        _logger.LogInformation("Deletion cancelled by user");
                        break;
                    }

                    _logger.LogDebug("Deleting entry: {EntryName}", entry.FullName);
                    await Task.Run(() => entry.Delete(), cancellationToken);
                    processedEntries++;
                }

                result.FilesProcessed = processedEntries;
                result.Message = $"Deleted {processedEntries} items from archive";
                _logger.LogInformation("Deletion completed: {Message}", result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Deletion failed: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "Deletion failed for {ArchivePath}", archivePath);
            }

            result.Duration = DateTime.Now - startTime;
            return result;
        }

        public async Task<OperationResult> Compress(List<string> sources, string archivePath, IProgress<(string CurrentFile, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>? progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CompressAsync started. Token CanBeCanceled: {CanBeCanceled}", cancellationToken.CanBeCanceled);
            var startTime = DateTime.Now;
            var result = new OperationResult { Success = true };
            string tempArchivePath = archivePath + ".tmp";

            try
            {
                // Clean up any existing temp file
                if (File.Exists(tempArchivePath)) File.Delete(tempArchivePath);

                // First pass: Count total files and bytes for progress reporting
                int totalFiles = 0;
                long totalBytes = 0;
                foreach (var sourcePath in sources)
                {
                    if (File.Exists(sourcePath)) 
                    {
                        totalFiles++;
                        try { totalBytes += new FileInfo(sourcePath).Length; } catch {}
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        try {
                            var dirInfo = new DirectoryInfo(sourcePath);
                            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                            {
                                totalFiles++;
                                totalBytes += file.Length;
                            }
                        } catch { }
                    }
                }

                // Use explicit FileStream with async support
                using (var fs = new FileStream(tempArchivePath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, true))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    var processedFiles = 0;
                    long processedBytes = 0;
                    DateTime lastReportTime = DateTime.MinValue;
                    byte[] buffer = new byte[81920]; // Reusable 80KB buffer

                    // Local function to report progress with throttling (500ms)
                    void ReportProgress(string currentFile, bool force = false)
                    {
                        if (progress == null) return;
                        var now = DateTime.Now;
                        if (force || (now - lastReportTime).TotalMilliseconds > 500) 
                        {
                            progress.Report((currentFile, processedFiles, totalFiles, processedBytes, totalBytes));
                            lastReportTime = now;
                        }
                    }

                    foreach (var sourcePath in sources)
                    {
                        if (cancellationToken.IsCancellationRequested) 
                        {
                            _logger.LogInformation("Cancellation detected in source loop");
                            break;
                        }

                        if (File.Exists(sourcePath))
                        {
                            var entryName = Path.GetFileName(sourcePath);
                            ReportProgress(entryName, true);
                            
                            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                            try { entry.LastWriteTime = new DateTimeOffset(new FileInfo(sourcePath).LastWriteTime); } catch { }
                            
                            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                            using (var entryStream = entry.Open())
                            {
                                int bytesRead;
                                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                                {
                                    await entryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                    processedBytes += bytesRead;
                                    ReportProgress(entryName);
                                }
                            }
                            processedFiles++;
                            ReportProgress(entryName, true);
                        }
                        else if (Directory.Exists(sourcePath))
                        {
                            var dirInfo = new DirectoryInfo(sourcePath);
                            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                            {
                                if (cancellationToken.IsCancellationRequested) break;

                                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(sourcePath) ?? "", file.FullName);
                                ReportProgress(relativePath, true);

                                var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                                try { entry.LastWriteTime = new DateTimeOffset(file.LastWriteTime); } catch { }

                                using (var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                                using (var entryStream = entry.Open())
                                {
                                    int bytesRead;
                                    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                                    {
                                        await entryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                                        processedBytes += bytesRead;
                                        ReportProgress(relativePath);
                                    }
                                }
                                processedFiles++;
                                ReportProgress(relativePath, true);
                            }
                        }
                    }
                } // Archive and FileStream are disposed here

                // Check cancellation *after* resource disposal but before move
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                // Atomic move to final destination
                if (File.Exists(archivePath)) File.Delete(archivePath);
                File.Move(tempArchivePath, archivePath);

                result.FilesProcessed = totalFiles; // Approx
                result.Message = $"Compressed files into archive";
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Caught OperationCanceledException in Compress");
                result.Success = false;
                result.Message = "Compression cancelled by user";
                try 
                { 
                    if (File.Exists(tempArchivePath)) File.Delete(tempArchivePath); 
                } 
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup temporary file after cancellation: {Path}", tempArchivePath);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Compression failed: {ex.Message}";
                result.Errors.Add(ex.Message);
                try 
                { 
                    if (File.Exists(tempArchivePath)) File.Delete(tempArchivePath); 
                } 
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup temporary file after failure: {Path}", tempArchivePath);
                }
            }

            result.Duration = DateTime.Now - startTime;
            return result;
        }
    }
}
