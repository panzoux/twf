using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SevenZip;
using TWF.Models;
using Microsoft.Extensions.Logging;
using TWF.Infrastructure;

namespace TWF.Services
{
    /// <summary>
    /// Archive provider for multiple formats using SevenZipSharp (7z, LZH, RAR, TAR, etc.)
    /// </summary>
    public class SevenZipArchiveProvider : IArchiveProvider
    {
        private readonly ILogger<SevenZipArchiveProvider> _logger;

        public SevenZipArchiveProvider()
        {
            _logger = LoggingConfiguration.GetLogger<SevenZipArchiveProvider>();
        }

        public string[] SupportedExtensions => new[] { ".7z", ".lzh", ".rar", ".tar", ".bz2", ".gz", ".xz", ".cab", ".lzma" };

        public List<FileEntry> List(string archivePath)
        {
            var entries = new List<FileEntry>();
            try
            {
                using (var extractor = new SevenZipExtractor(archivePath))
                {
                    foreach (var entry in extractor.ArchiveFileData)
                    {
                        if (entry.IsDirectory) continue;

                        entries.Add(new FileEntry
                        {
                            FullPath = Path.Combine(archivePath, entry.FileName),
                            Name = Path.GetFileName(entry.FileName),
                            Extension = Path.GetExtension(entry.FileName),
                            Size = (long)entry.Size,
                            LastModified = entry.LastWriteTime,
                            Attributes = FileAttributes.Normal,
                            IsDirectory = false,
                            IsArchive = false,
                            IsVirtualFolder = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list archive: {Path}", archivePath);
                throw new InvalidOperationException($"Failed to list archive: {ex.Message}", ex);
            }
            return entries;
        }

        public async Task<TWF.Models.OperationResult> Extract(string archivePath, string destination, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var result = new TWF.Models.OperationResult { Success = true };
            try
            {
                Directory.CreateDirectory(destination);
                using (var extractor = new SevenZipExtractor(archivePath))
                {
                    await Task.Run(() => extractor.ExtractArchive(destination), cancellationToken);
                }
                result.Message = "Extracted archive successfully";
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = "Extraction cancelled";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Extraction failed: {ex.Message}";
                _logger.LogError(ex, "Extraction failed for {Path}", archivePath);
            }
            result.Duration = DateTime.Now - startTime;
            return result;
        }

        public Task<TWF.Models.OperationResult> ExtractEntries(string archivePath, List<string> entryNames, string destination, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TWF.Models.OperationResult { Success = false, Message = "Partial extraction not yet implemented" });
        }

        public Task<TWF.Models.OperationResult> DeleteEntries(string archivePath, List<string> entryNames, CancellationToken cancellationToken)
        {
            return Task.FromResult(new TWF.Models.OperationResult { Success = false, Message = "Entry deletion not supported" });
        }

        public async Task<TWF.Models.OperationResult> Compress(List<string> sources, string archivePath, int compressionLevel, IProgress<(string CurrentFile, string CurrentFullPath, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>? progress, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            var result = new TWF.Models.OperationResult { Success = true };
            try
            {
                var szLevel = compressionLevel switch
                {
                    0 => SevenZip.CompressionLevel.None,
                    1 => SevenZip.CompressionLevel.Low,
                    <= 3 => SevenZip.CompressionLevel.Fast,
                    <= 5 => SevenZip.CompressionLevel.Normal,
                    <= 7 => SevenZip.CompressionLevel.High,
                    _ => SevenZip.CompressionLevel.Ultra
                };

                // Determine format based on extension
                var ext = Path.GetExtension(archivePath).ToLowerInvariant();
                var format = ext switch
                {
                    ".7z" => OutArchiveFormat.SevenZip,
                    ".tar" => OutArchiveFormat.Tar,
                    ".bz2" => OutArchiveFormat.BZip2,
                    ".gz" => OutArchiveFormat.GZip,
                    ".xz" => OutArchiveFormat.XZ,
                    _ => OutArchiveFormat.SevenZip
                };

                // Note: SevenZipSharp compression support for LZH/RAR/CAB is limited or read-only in many 7z.dll versions.
                // RAR is definitely write-protected (proprietary).
                // LZH is often read-only in modern 7z binaries.

                var compressor = new SevenZipCompressor
                {
                    CompressionLevel = szLevel,
                    ArchiveFormat = format
                };

                var files = new List<string>();
                foreach (var source in sources)
                {
                    if (File.Exists(source)) files.Add(source);
                    else if (Directory.Exists(source))
                    {
                        files.AddRange(Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories));
                    }
                }

                int total = files.Count;
                int processed = 0;

                compressor.FileCompressionStarted += (s, e) => {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        e.Cancel = true;
                        return;
                    }
                    processed++;
                    progress?.Report((e.FileName, e.FileName, processed, total, 0, 0));
                };

                compressor.Compressing += (s, e) => {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        e.Cancel = true;
                    }
                };

                await Task.Run(() => compressor.CompressFiles(archivePath, files.ToArray()), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                     throw new OperationCanceledException();
                }

                result.FilesProcessed = total;
                result.Message = $"Compressed {total} files into {ext} archive";
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.Message = "Compression cancelled";
                if (File.Exists(archivePath)) File.Delete(archivePath);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Compression failed: {ex.Message}";
                _logger.LogError(ex, "Compression failed for {Path}", archivePath);
                if (File.Exists(archivePath)) File.Delete(archivePath);
            }
            result.Duration = DateTime.Now - startTime;
            return result;
        }
    }
}
