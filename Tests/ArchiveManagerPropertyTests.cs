namespace TWF.Tests
{
    using FsCheck;
    using FsCheck.Xunit;
    using TWF.Models;
    using TWF.Services;
    using System.IO.Compression;

    /// <summary>
    /// Property-based tests for ArchiveManager
    /// </summary>
    public class ArchiveManagerPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 25: Archive displays as virtual folder
        /// Validates: Requirements 12.1
        /// 
        /// For any supported archive file, pressing Enter should display its contents as a navigable folder structure
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ArchiveDisplaysAsVirtualFolder()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 10)),  // Number of files in archive
                (fileCount) =>
                {
                    // Create a temporary archive with random files
                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    var archivePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.zip");

                    try
                    {
                        Directory.CreateDirectory(tempDir);

                        // Create test files
                        var createdFiles = new List<string>();
                        var nameComponents = new[] { "file", "document", "test", "data", "readme" };
                        for (int i = 0; i < fileCount; i++)
                        {
                            var fileName = $"{nameComponents[i % nameComponents.Length]}_{i}.txt";
                            var filePath = Path.Combine(tempDir, fileName);
                            File.WriteAllText(filePath, $"Test content {i}");
                            createdFiles.Add(fileName);
                        }

                        // Create archive
                        ZipFile.CreateFromDirectory(tempDir, archivePath);

                        // Test: List archive contents
                        var manager = new ArchiveManager();
                        var contents = manager.ListArchiveContents(archivePath);

                        // Property: All created files should appear in the virtual folder
                        var allFilesPresent = createdFiles.All(fileName =>
                            contents.Any(entry => entry.Name == fileName));

                        // Property: All entries should be marked as virtual folder entries
                        var allMarkedAsVirtual = contents.All(entry => entry.IsVirtualFolder);

                        // Property: Number of entries should match number of files created
                        var correctCount = contents.Count == fileCount;

                        return (allFilesPresent && allMarkedAsVirtual && correctCount).ToProperty();
                    }
                    finally
                    {
                        // Cleanup
                        try
                        {
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                            if (File.Exists(archivePath))
                                File.Delete(archivePath);
                        }
                        catch { }
                    }
                });
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 26: Archive extraction creates files
        /// Validates: Requirements 12.4
        /// 
        /// For any archive file, extracting should create all contained files in the destination directory
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ArchiveExtractionCreatesFiles()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 10)),  // Number of files in archive
                (fileCount) =>
                {
                    // Create a temporary archive with random files
                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    var archivePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.zip");
                    var extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                    try
                    {
                        Directory.CreateDirectory(tempDir);

                        // Create test files with specific content
                        var createdFiles = new Dictionary<string, string>();
                        for (int i = 0; i < fileCount; i++)
                        {
                            var fileName = $"testfile_{i}.txt";
                            var content = $"Test content for file {i} - {Guid.NewGuid()}";
                            var filePath = Path.Combine(tempDir, fileName);
                            File.WriteAllText(filePath, content);
                            createdFiles[fileName] = content;
                        }

                        // Create archive
                        ZipFile.CreateFromDirectory(tempDir, archivePath);

                        // Test: Extract archive
                        var manager = new ArchiveManager();
                        var result = manager.ExtractAsync(archivePath, extractDir, CancellationToken.None).Result;

                        // Property: Extraction should succeed
                        if (!result.Success)
                            return false.ToProperty();

                        // Property: All files should be extracted
                        var allFilesExtracted = createdFiles.Keys.All(fileName =>
                            File.Exists(Path.Combine(extractDir, fileName)));

                        // Property: Extracted files should have identical content
                        var contentMatches = createdFiles.All(kvp =>
                        {
                            var extractedPath = Path.Combine(extractDir, kvp.Key);
                            if (!File.Exists(extractedPath))
                                return false;
                            var extractedContent = File.ReadAllText(extractedPath);
                            return extractedContent == kvp.Value;
                        });

                        // Property: Number of processed files should match
                        var correctCount = result.FilesProcessed == fileCount;

                        return (allFilesExtracted && contentMatches && correctCount).ToProperty();
                    }
                    finally
                    {
                        // Cleanup
                        try
                        {
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                            if (File.Exists(archivePath))
                                File.Delete(archivePath);
                            if (Directory.Exists(extractDir))
                                Directory.Delete(extractDir, true);
                        }
                        catch { }
                    }
                });
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 27: Compression creates archive
        /// Validates: Requirements 13.2
        /// 
        /// For any set of marked files, compressing should create an archive file containing all selected files
        /// </summary>
        [Property(MaxTest = 100)]
        public Property CompressionCreatesArchive()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 10)),  // Number of files to compress
                (fileCount) =>
                {
                    // Create temporary files to compress
                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    var archivePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.zip");

                    try
                    {
                        Directory.CreateDirectory(tempDir);

                        // Create test files
                        var createdFiles = new List<FileEntry>();
                        var fileContents = new Dictionary<string, string>();
                        
                        for (int i = 0; i < fileCount; i++)
                        {
                            var fileName = $"testfile_{i}.txt";
                            var content = $"Test content for file {i} - {Guid.NewGuid()}";
                            var filePath = Path.Combine(tempDir, fileName);
                            File.WriteAllText(filePath, content);
                            
                            createdFiles.Add(new FileEntry
                            {
                                FullPath = filePath,
                                Name = fileName,
                                Size = content.Length,
                                IsDirectory = false
                            });
                            
                            fileContents[fileName] = content;
                        }

                        // Test: Compress files
                        var manager = new ArchiveManager();
                                                var result = manager.CompressAsync(
                                                    createdFiles,
                                                    archivePath,
                                                    ArchiveFormat.ZIP,
                                                    5,
                                                    null,
                                                    CancellationToken.None).Result;
                        // Property: Compression should succeed
                        if (!result.Success)
                            return false.ToProperty();

                        // Property: Archive file should be created
                        if (!File.Exists(archivePath))
                            return false.ToProperty();

                        // Property: Archive should contain all files
                        var archiveContents = manager.ListArchiveContents(archivePath);
                        var allFilesInArchive = createdFiles.All(file =>
                            archiveContents.Any(entry => entry.Name == file.Name));

                        // Property: Number of processed files should match
                        var correctCount = result.FilesProcessed == fileCount;

                        // Property: Archive should be valid (can be extracted)
                        var extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        try
                        {
                            var extractResult = manager.ExtractAsync(archivePath, extractDir, CancellationToken.None).Result;
                            var canExtract = extractResult.Success;

                            // Verify extracted content matches original
                            var contentMatches = fileContents.All(kvp =>
                            {
                                var extractedPath = Path.Combine(extractDir, kvp.Key);
                                if (!File.Exists(extractedPath))
                                    return false;
                                var extractedContent = File.ReadAllText(extractedPath);
                                return extractedContent == kvp.Value;
                            });

                            return (allFilesInArchive && correctCount && canExtract && contentMatches).ToProperty();
                        }
                        finally
                        {
                            if (Directory.Exists(extractDir))
                                Directory.Delete(extractDir, true);
                        }
                    }
                    finally
                    {
                        // Cleanup
                        try
                        {
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                            if (File.Exists(archivePath))
                                File.Delete(archivePath);
                        }
                        catch { }
                    }
                });
        }
    }
}
