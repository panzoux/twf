namespace TWF.Services
{
    using TWF.Models;

    /// <summary>
    /// Manages archive operations and provider registration
    /// </summary>
    public class ArchiveManager
    {
        private readonly Dictionary<string, IArchiveProvider> _providers = new();

        public ArchiveManager()
        {
            // Register default providers for common formats
            RegisterProvider(new ZipArchiveProvider());
            RegisterProvider(new SevenZipArchiveProvider());
        }

        /// <summary>
        /// Registers an archive provider for its supported extensions
        /// </summary>
        public void RegisterProvider(IArchiveProvider provider)
        {
            foreach (var extension in provider.SupportedExtensions)
            {
                var normalizedExt = extension.ToLowerInvariant();
                if (!normalizedExt.StartsWith("."))
                {
                    normalizedExt = "." + normalizedExt;
                }
                _providers[normalizedExt] = provider;
            }
        }

        /// <summary>
        /// Checks if a file is a supported archive based on its extension
        /// </summary>
        public bool IsArchive(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return _providers.ContainsKey(extension);
        }

        /// <summary>
        /// Gets a list of all supported archive extensions
        /// </summary>
        public IEnumerable<string> GetSupportedArchiveExtensions()
        {
            var keys = new List<string>(_providers.Keys);
            keys.Sort();
            return keys;
        }

        /// <summary>
        /// Gets a list of all supported archive formats
        /// </summary>
        public List<ArchiveFormat> GetSupportedFormats()
        {
            var formats = new HashSet<ArchiveFormat>();
            foreach (var ext in _providers.Keys)
            {
                var format = ext.ToLowerInvariant() switch
                {
                    ".zip" => ArchiveFormat.ZIP,
                    ".tar" => ArchiveFormat.TAR,
                    ".tar.gz" => ArchiveFormat.TGZ,
                    ".tgz" => ArchiveFormat.TGZ,
                    ".7z" => ArchiveFormat.SevenZip,
                    ".rar" => ArchiveFormat.RAR,
                    ".lzh" => ArchiveFormat.LZH,
                    ".cab" => ArchiveFormat.CAB,
                    ".bz2" => ArchiveFormat.BZ2,
                    ".xz" => ArchiveFormat.XZ,
                    ".lzma" => ArchiveFormat.LZMA,
                    _ => (ArchiveFormat?)null
                };
                if (format.HasValue) formats.Add(format.Value);
            }
            
            var result = new List<ArchiveFormat>(formats);
            result.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
            return result;
        }

        /// <summary>
        /// Lists the contents of an archive file as a virtual folder
        /// </summary>
        public List<FileEntry> ListArchiveContents(string archivePath, string internalPath = "")
        {
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Archive file not found: {archivePath}");
            }

            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            
            if (!_providers.TryGetValue(extension, out var provider))
            {
                throw new NotSupportedException($"Archive format not supported: {extension}");
            }

            var flatEntries = provider.List(archivePath);
            return FilterAndGroupEntries(flatEntries, internalPath, archivePath);
        }

        private List<FileEntry> FilterAndGroupEntries(List<FileEntry> flatEntries, string internalPath, string archivePath)
        {
            var result = new List<FileEntry>();
            var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Normalize internalPath: remove leading/trailing slashes and ensure it ends with slash if not empty
            string prefix = string.IsNullOrEmpty(internalPath) ? "" : internalPath.Replace('\\', '/').Trim('/') + "/";

            foreach (var entry in flatEntries)
            {
                // FullPath in flatEntries is "archive.zip/path/in/zip/file.txt"
                // The actual internal path is what comes after "archive.zip/"
                string relativeToArchive = GetInternalPath(entry.FullPath, archivePath);
                
                if (!string.IsNullOrEmpty(prefix) && !relativeToArchive.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string remaining = relativeToArchive.Substring(prefix.Length);
                if (string.IsNullOrEmpty(remaining)) continue;

                int firstSlash = remaining.IndexOf('/');
                if (firstSlash >= 0)
                {
                    // It's in a sub-directory
                    string dirName = remaining.Substring(0, firstSlash);
                    if (directories.Add(dirName))
                    {
                        result.Add(CreateVirtualDirectory(dirName, Path.Combine(archivePath, prefix + dirName)));
                    }
                }
                else
                {
                    // It's a file at this level
                    entry.Name = remaining;
                    result.Add(entry);
                }
            }

            return result;
        }

        private string GetInternalPath(string fullPath, string archivePath)
        {
            // FullPath is archivePath + "/" + internalPath
            if (fullPath.StartsWith(archivePath, StringComparison.OrdinalIgnoreCase))
            {
                string internalPart = fullPath.Substring(archivePath.Length).TrimStart('\\', '/');
                return internalPart.Replace('\\', '/');
            }
            return fullPath;
        }

        private FileEntry CreateVirtualDirectory(string name, string fullPath)
        {
            return new FileEntry
            {
                Name = name,
                FullPath = fullPath,
                IsDirectory = true,
                IsVirtualFolder = true,
                LastModified = DateTime.MinValue,
                Size = 0,
                Extension = ""
            };
        }

        /// <summary>
        /// Lists the contents of an archive file asynchronously
        /// </summary>
        public Task<List<FileEntry>> ListArchiveContentsAsync(string archivePath, string internalPath = "", CancellationToken cancellationToken = default)
        {
            return Task.Run(() => 
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ListArchiveContents(archivePath, internalPath);
            }, cancellationToken);
        }

        /// <summary>
        /// Extracts specific entries from an archive file asynchronously
        /// </summary>
        public async Task<OperationResult> ExtractEntriesAsync(
            string archivePath, 
            List<string> entryNames, 
            string destination, 
            IProgress<(string CurrentFile, string CurrentFullPath, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            if (!_providers.TryGetValue(extension, out var provider))
            {
                return new OperationResult { Success = false, Message = "Unsupported archive format" };
            }

            return await provider.ExtractEntries(archivePath, entryNames, destination, progress, cancellationToken);
        }

        /// <summary>
        /// Deletes specific entries from an archive file asynchronously
        /// </summary>
        public async Task<OperationResult> DeleteEntriesAsync(
            string archivePath, 
            List<string> entryNames, 
            CancellationToken cancellationToken = default)
        {
            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            if (!_providers.TryGetValue(extension, out var provider))
            {
                return new OperationResult { Success = false, Message = "Unsupported archive format" };
            }

            return await provider.DeleteEntries(archivePath, entryNames, cancellationToken);
        }

        /// <summary>
        /// Extracts an archive to a destination directory
        /// </summary>
        public async Task<OperationResult> ExtractAsync(
            string archivePath, 
            string destination, 
            IProgress<(string CurrentFile, string CurrentFullPath, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(archivePath))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Archive file not found: {archivePath}",
                    Errors = new List<string> { "File not found" }
                };
            }

            var extension = Path.GetExtension(archivePath).ToLowerInvariant();
            
            if (!_providers.TryGetValue(extension, out var provider))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Archive format not supported: {extension}",
                    Errors = new List<string> { "Unsupported format" }
                };
            }

            return await provider.Extract(archivePath, destination, progress, cancellationToken);
        }

        /// <summary>
        /// Compresses files into an archive
        /// </summary>
        public async Task<OperationResult> CompressAsync(
            List<FileEntry> sources, 
            string archivePath, 
            ArchiveFormat format, 
            int compressionLevel,
            IProgress<(string CurrentFile, string CurrentFullPath, int ProcessedFiles, int TotalFiles, long ProcessedBytes, long TotalBytes)>? progress,
            CancellationToken cancellationToken = default)
        {
            // Determine extension from format
            var extension = format switch
            {
                ArchiveFormat.ZIP => ".zip",
                ArchiveFormat.TAR => ".tar",
                ArchiveFormat.TGZ => ".tar.gz",
                ArchiveFormat.SevenZip => ".7z",
                ArchiveFormat.RAR => ".rar",
                ArchiveFormat.LZH => ".lzh",
                ArchiveFormat.CAB => ".cab",
                ArchiveFormat.BZ2 => ".bz2",
                ArchiveFormat.XZ => ".xz",
                ArchiveFormat.LZMA => ".lzma",
                _ => ".zip"
            };

            if (!_providers.TryGetValue(extension, out var provider))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = $"Archive format not supported: {format}",
                    Errors = new List<string> { "Unsupported format" }
                };
            }

            // Convert FileEntry list to path list
            var sourcePaths = new List<string>(sources.Count);
            foreach (var source in sources)
            {
                sourcePaths.Add(source.FullPath);
            }

            return await provider.Compress(sourcePaths, archivePath, compressionLevel, progress, cancellationToken);
        }
    }
}
