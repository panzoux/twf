using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Services;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for FileOperations service
    /// </summary>
    public class FileOperationsPropertyTests : IDisposable
    {
        private readonly string _testRoot;
        private readonly List<string> _createdDirectories;

        public FileOperationsPropertyTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "twf_test_" + Guid.NewGuid().ToString("N"));
            _createdDirectories = new List<string>();
            Directory.CreateDirectory(_testRoot);
            _createdDirectories.Add(_testRoot);
        }

        public void Dispose()
        {
            // Clean up test directories - sort by length descending manually to delete deepest first
            var sortedDirs = new List<string>(_createdDirectories);
            for (int i = 0; i < sortedDirs.Count; i++)
            {
                for (int j = i + 1; j < sortedDirs.Count; j++)
                {
                    if (sortedDirs[j].Length > sortedDirs[i].Length)
                    {
                        var temp = sortedDirs[i];
                        sortedDirs[i] = sortedDirs[j];
                        sortedDirs[j] = temp;
                    }
                }
            }

            foreach (var dir in sortedDirs)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 17: Copy operation transfers files
        /// Feature: twf-file-manager, Property 18: Copy preserves file attributes
        /// Validates: Requirements 5.1, 5.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property CopyOperation_TransfersFilesAndPreservesAttributes(
            NonEmptyString fileName,
            NonEmptyArray<byte> content)
        {
            // Arrange: Create source and destination directories
            var sourceDir = CreateTestDirectory("source");
            var destDir = CreateTestDirectory("dest");

            // Create a test file with specific content
            var sanitizedName = SanitizeFileName(fileName.Get);
            var sourceFile = Path.Combine(sourceDir, sanitizedName);
            File.WriteAllBytes(sourceFile, content.Get);
            
            var originalTimestamp = DateTime.UtcNow.AddDays(-1);
            File.SetLastWriteTimeUtc(sourceFile, originalTimestamp);

            var fileEntry = new FileEntry
            {
                FullPath = sourceFile,
                Name = sanitizedName,
                Size = content.Get.Length,
                LastModified = originalTimestamp,
                IsDirectory = false
            };

            var fileOps = new FileOperations();

            // Act: Copy the file
            var result = fileOps.CopyAsync(
                new List<FileEntry> { fileEntry },
                destDir,
                CancellationToken.None).Result;

            // Assert: File should exist in destination with same content and attributes
            var destFile = Path.Combine(destDir, sanitizedName);
            var fileExists = File.Exists(destFile);
            
            bool contentMatches = fileExists;
            if (fileExists)
            {
                var destContent = File.ReadAllBytes(destFile);
                if (destContent.Length != content.Get.Length)
                {
                    contentMatches = false;
                }
                else
                {
                    for (int i = 0; i < destContent.Length; i++)
                    {
                        if (destContent[i] != content.Get[i])
                        {
                            contentMatches = false;
                            break;
                        }
                    }
                }
            }
            var sizeMatches = fileExists && new FileInfo(destFile).Length == content.Get.Length;
            
            // Check timestamp preservation
            // Note: File copy operations preserve timestamps, but the property test validates
            // that the implementation attempts to preserve them. Due to file system precision
            // limitations and timing issues in tests, we verify the core copy functionality
            // (content and size) as the primary correctness criteria.
            var sourceTimestamp = File.GetLastWriteTimeUtc(sourceFile);
            var destTimestamp = fileExists ? File.GetLastWriteTimeUtc(destFile) : DateTime.MinValue;
            var timeDiff = Math.Abs((destTimestamp - sourceTimestamp).TotalSeconds);
            
            // Windows file systems have varying timestamp precision (FAT32: 2 seconds, NTFS: 100ns)
            // Allow up to 3 seconds difference to account for file system precision
            var timestampPreserved = fileExists && timeDiff < 3;

            return (result.Success && fileExists && contentMatches && sizeMatches && timestampPreserved)
                .ToProperty()
                .Label($"Copy should transfer file with preserved attributes. Success: {result.Success}, " +
                       $"Exists: {fileExists}, Content matches: {contentMatches}, " +
                       $"Size matches: {sizeMatches}, Timestamp preserved: {timestampPreserved}, " +
                       $"Time diff: {timeDiff:F3}s, Source: {sourceTimestamp:O}, Dest: {destTimestamp:O}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 19: Move operation relocates files
        /// Validates: Requirements 6.1, 6.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property MoveOperation_RelocatesFiles(
            NonEmptyString fileName,
            NonEmptyArray<byte> content)
        {
            // Arrange: Create source and destination directories
            var sourceDir = CreateTestDirectory("move_source");
            var destDir = CreateTestDirectory("move_dest");

            // Create a test file
            var sanitizedName = SanitizeFileName(fileName.Get);
            var sourceFile = Path.Combine(sourceDir, sanitizedName);
            File.WriteAllBytes(sourceFile, content.Get);

            var fileEntry = new FileEntry
            {
                FullPath = sourceFile,
                Name = sanitizedName,
                Size = content.Get.Length,
                IsDirectory = false
            };

            var fileOps = new FileOperations();

            // Act: Move the file
            var result = fileOps.MoveAsync(
                new List<FileEntry> { fileEntry },
                destDir,
                CancellationToken.None).Result;

            // Assert: File should exist in destination and not in source
            var destFile = Path.Combine(destDir, sanitizedName);
            var existsInDest = File.Exists(destFile);
            var notInSource = !File.Exists(sourceFile);
            
            bool contentMatches = existsInDest;
            if (existsInDest)
            {
                var destContent = File.ReadAllBytes(destFile);
                if (destContent.Length != content.Get.Length)
                {
                    contentMatches = false;
                }
                else
                {
                    for (int i = 0; i < destContent.Length; i++)
                    {
                        if (destContent[i] != content.Get[i])
                        {
                            contentMatches = false;
                            break;
                        }
                    }
                }
            }

            return (result.Success && existsInDest && notInSource && contentMatches)
                .ToProperty()
                .Label($"Move should relocate file from source to destination. Success: {result.Success}, " +
                       $"In dest: {existsInDest}, Not in source: {notInSource}, Content matches: {contentMatches}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 20: Delete operation removes files
        /// Validates: Requirements 7.3
        /// </summary>
        [Property(MaxTest = 100)]
        public Property DeleteOperation_RemovesFiles(
            NonEmptyString fileName,
            NonEmptyArray<byte> content)
        {
            // Arrange: Create a test directory and file
            var testDir = CreateTestDirectory("delete_test");
            var sanitizedName = SanitizeFileName(fileName.Get);
            var testFile = Path.Combine(testDir, sanitizedName);
            File.WriteAllBytes(testFile, content.Get);

            var fileEntry = new FileEntry
            {
                FullPath = testFile,
                Name = sanitizedName,
                Size = content.Get.Length,
                IsDirectory = false
            };

            var fileOps = new FileOperations();

            // Act: Delete the file
            var result = fileOps.DeleteAsync(
                new List<FileEntry> { fileEntry },
                CancellationToken.None).Result;

            // Assert: File should no longer exist
            var fileDeleted = !File.Exists(testFile);

            return (result.Success && fileDeleted)
                .ToProperty()
                .Label($"Delete should remove file. Success: {result.Success}, File deleted: {fileDeleted}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 21: Directory creation adds new folder
        /// Validates: Requirements 8.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property DirectoryCreation_AddsNewFolder(NonEmptyString dirName)
        {
            // Arrange: Create a test directory
            var testDir = CreateTestDirectory("create_test");
            var sanitizedName = SanitizeFileName(dirName.Get);

            var fileOps = new FileOperations();

            // Act: Create a new directory
            var result = fileOps.CreateDirectory(testDir, sanitizedName);

            // Assert: Directory should exist
            var newDir = Path.Combine(testDir, sanitizedName);
            var dirExists = Directory.Exists(newDir);

            if (dirExists)
            {
                _createdDirectories.Add(newDir);
            }

            return (result.Success && dirExists)
                .ToProperty()
                .Label($"CreateDirectory should add new folder. Success: {result.Success}, Exists: {dirExists}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 34: Pattern rename transforms filenames
        /// Validates: Requirements 18.3
        /// </summary>
        [Property(MaxTest = 100)]
        public Property PatternRename_TransformsFilenames(
            NonEmptyString fileName,
            NonEmptyArray<byte> content)
        {
            // Arrange: Create a test file with a predictable pattern
            var testDir = CreateTestDirectory("rename_test");
            var originalName = "test_" + SanitizeFileName(fileName.Get);
            var testFile = Path.Combine(testDir, originalName);
            File.WriteAllBytes(testFile, content.Get);

            var fileEntry = new FileEntry
            {
                FullPath = testFile,
                Name = originalName,
                Size = content.Get.Length,
                IsDirectory = false
            };

            var fileOps = new FileOperations();

            // Act: Rename using simple pattern replacement
            var result = fileOps.RenameAsync(
                new List<FileEntry> { fileEntry },
                "test_",
                "renamed_").Result;

            // Assert: File should be renamed
            var expectedName = originalName.Replace("test_", "renamed_");
            var renamedFile = Path.Combine(testDir, expectedName);
            var fileRenamed = File.Exists(renamedFile);
            var originalGone = !File.Exists(testFile);

            return (result.Success && fileRenamed && originalGone)
                .ToProperty()
                .Label($"Rename should transform filename. Success: {result.Success}, " +
                       $"Renamed exists: {fileRenamed}, Original gone: {originalGone}");
        }

        private string CreateTestDirectory(string name)
        {
            var dir = Path.Combine(_testRoot, name + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(dir);
            _createdDirectories.Add(dir);
            return dir;
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 36: File split creates multiple parts
        /// Validates: Requirements 20.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FileSplit_CreatesMultipleParts(
            NonEmptyString fileName,
            NonEmptyArray<byte> content,
            PositiveInt partSizeMultiplier)
        {
            // Arrange: Create a test file
            var testDir = CreateTestDirectory("split_test");
            var sanitizedName = SanitizeFileName(fileName.Get);
            var sourceFile = Path.Combine(testDir, sanitizedName);
            File.WriteAllBytes(sourceFile, content.Get);

            // Calculate part size (ensure it's reasonable and creates multiple parts)
            // Use a multiplier between 1 and content length to vary part sizes
            var maxPartSize = Math.Max(1, content.Get.Length / 2);
            var partSize = Math.Max(1, partSizeMultiplier.Get % maxPartSize);
            
            var outputDir = CreateTestDirectory("split_output");
            var fileOps = new FileOperations();

            // Act: Split the file
            var result = fileOps.SplitAsync(
                sourceFile,
                partSize,
                outputDir,
                CancellationToken.None).Result;

            // Assert: Multiple part files should be created
            var partFilesList = new List<string>(Directory.GetFiles(outputDir, "*.*"));
            partFilesList.Sort();
            var expectedPartCount = (int)Math.Ceiling((double)content.Get.Length / partSize);
            
            // Verify the total size of all parts equals the original
            long totalPartSize = 0;
            foreach (var f in partFilesList)
            {
                totalPartSize += new FileInfo(f).Length;
            }
            var sizesMatch = totalPartSize == content.Get.Length;
            
            // Verify correct number of parts
            var correctPartCount = partFilesList.Count == expectedPartCount;

            return (result.Success && partFilesList.Count > 0 && sizesMatch && correctPartCount)
                .ToProperty()
                .Label($"Split should create multiple parts with combined size equal to original. " +
                       $"Success: {result.Success}, Parts created: {partFilesList.Count}, " +
                       $"Expected parts: {expectedPartCount}, Total size matches: {sizesMatch}, " +
                       $"Original size: {content.Get.Length}, Part size: {partSize}, " +
                       $"Total part size: {totalPartSize}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 37: File join recreates original
        /// Validates: Requirements 20.4
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FileJoin_RecreatesOriginal(
            NonEmptyString fileName,
            NonEmptyArray<byte> content,
            PositiveInt partSizeMultiplier)
        {
            // Arrange: Create a test file and split it
            var testDir = CreateTestDirectory("join_test");
            var sanitizedName = SanitizeFileName(fileName.Get);
            var sourceFile = Path.Combine(testDir, sanitizedName);
            File.WriteAllBytes(sourceFile, content.Get);

            // Calculate part size
            var maxPartSize = Math.Max(1, content.Get.Length / 2);
            var partSize = Math.Max(1, partSizeMultiplier.Get % maxPartSize);
            
            var splitDir = CreateTestDirectory("join_split_output");
            var fileOps = new FileOperations();

            // Split the file
            var splitResult = fileOps.SplitAsync(
                sourceFile,
                partSize,
                splitDir,
                CancellationToken.None).Result;

            if (!splitResult.Success)
            {
                return false.ToProperty().Label("Split operation failed");
            }

            // Get all part files
            var partFiles = new List<string>(Directory.GetFiles(splitDir, "*.*"));
            partFiles.Sort();

            // Act: Join the parts back together
            var joinedFile = Path.Combine(testDir, "joined_" + sanitizedName);
            var joinResult = fileOps.JoinAsync(
                partFiles,
                joinedFile,
                CancellationToken.None).Result;

            // Assert: Joined file should be identical to original
            var joinedExists = File.Exists(joinedFile);
            
            bool contentMatches = joinedExists;
            if (joinedExists)
            {
                var destContent = File.ReadAllBytes(joinedFile);
                if (destContent.Length != content.Get.Length)
                {
                    contentMatches = false;
                }
                else
                {
                    for (int i = 0; i < destContent.Length; i++)
                    {
                        if (destContent[i] != content.Get[i])
                        {
                            contentMatches = false;
                            break;
                        }
                    }
                }
            }
            var sizeMatches = joinedExists && new FileInfo(joinedFile).Length == content.Get.Length;

            return (joinResult.Success && joinedExists && contentMatches && sizeMatches)
                .ToProperty()
                .Label($"Join should recreate original file. Success: {joinResult.Success}, " +
                       $"Exists: {joinedExists}, Content matches: {contentMatches}, " +
                       $"Size matches: {sizeMatches}, Original size: {content.Get.Length}, " +
                       $"Parts used: {partFiles.Count}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 35: File comparison marks matching files
        /// Validates: Requirements 19.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FileComparison_MarksBySize_MarksMatchingFiles(
            NonEmptyArray<NonEmptyString> leftFileNames,
            NonEmptyArray<NonEmptyString> rightFileNames,
            NonEmptyArray<PositiveInt> fileSizes)
        {
            // Arrange: Create two panes with files
            var leftPane = new PaneState();
            var rightPane = new PaneState();

            // Create files in left pane with various sizes
            for (int i = 0; i < Math.Min(leftFileNames.Get.Length, fileSizes.Get.Length); i++)
            {
                var sanitizedName = SanitizeFileName(leftFileNames.Get[i].Get);
                var size = fileSizes.Get[i].Get;
                
                leftPane.Entries.Add(new FileEntry
                {
                    Name = "left_" + sanitizedName,
                    FullPath = "/left/" + sanitizedName,
                    Size = size,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow
                });
            }

            // Create files in right pane, some with matching sizes
            for (int i = 0; i < Math.Min(rightFileNames.Get.Length, fileSizes.Get.Length); i++)
            {
                var sanitizedName = SanitizeFileName(rightFileNames.Get[i].Get);
                // Use same size for some files to create matches
                var size = i < fileSizes.Get.Length / 2 ? fileSizes.Get[i].Get : fileSizes.Get[0].Get;
                
                rightPane.Entries.Add(new FileEntry
                {
                    Name = "right_" + sanitizedName,
                    FullPath = "/right/" + sanitizedName,
                    Size = size,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow
                });
            }

            var fileOps = new FileOperations();

            // Act: Compare files by size
            var result = fileOps.CompareFiles(leftPane, rightPane, ComparisonCriteria.Size);

            // Assert: All marked files should have matching sizes in the other pane
            bool leftMarkedValid = true;
            foreach (var leftEntry in leftPane.Entries)
            {
                if (leftEntry.IsMarked)
                {
                    bool found = false;
                    foreach (var r in rightPane.Entries)
                    {
                        if (!r.IsDirectory && r.Size == leftEntry.Size) { found = true; break; }
                    }
                    if (!found) { leftMarkedValid = false; break; }
                }
            }

            bool rightMarkedValid = true;
            foreach (var rightEntry in rightPane.Entries)
            {
                if (rightEntry.IsMarked)
                {
                    bool found = false;
                    foreach (var l in leftPane.Entries)
                    {
                        if (!l.IsDirectory && l.Size == rightEntry.Size) { found = true; break; }
                    }
                    if (!found) { rightMarkedValid = false; break; }
                }
            }

            int leftMarkedCount = 0;
            foreach (var e in leftPane.Entries) if (e.IsMarked) leftMarkedCount++;
            
            int rightMarkedCount = 0;
            foreach (var e in rightPane.Entries) if (e.IsMarked) rightMarkedCount++;

            return (result.Success && leftMarkedValid && rightMarkedValid)
                .ToProperty()
                .Label($"Comparison by size should mark files with matching sizes. " +
                       $"Success: {result.Success}, Left marked: {leftMarkedCount}, " +
                       $"Right marked: {rightMarkedCount}, " +
                       $"Left valid: {leftMarkedValid}, Right valid: {rightMarkedValid}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 35: File comparison marks matching files
        /// Validates: Requirements 19.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FileComparison_MarksByTimestamp_MarksMatchingFiles(
            NonEmptyArray<NonEmptyString> leftFileNames,
            NonEmptyArray<NonEmptyString> rightFileNames)
        {
            // Arrange: Create two panes with files
            var leftPane = new PaneState();
            var rightPane = new PaneState();

            var baseTime = DateTime.UtcNow.AddDays(-10);

            // Create files in left pane with various timestamps
            for (int i = 0; i < leftFileNames.Get.Length; i++)
            {
                var sanitizedName = SanitizeFileName(leftFileNames.Get[i].Get);
                
                leftPane.Entries.Add(new FileEntry
                {
                    Name = "left_" + sanitizedName,
                    FullPath = "/left/" + sanitizedName,
                    Size = 1000,
                    IsDirectory = false,
                    LastModified = baseTime.AddHours(i)
                });
            }

            // Create files in right pane, some with matching timestamps (within tolerance)
            for (int i = 0; i < rightFileNames.Get.Length; i++)
            {
                var sanitizedName = SanitizeFileName(rightFileNames.Get[i].Get);
                // Use similar timestamp for some files to create matches (within 2 second tolerance)
                var timestamp = i < leftFileNames.Get.Length 
                    ? baseTime.AddHours(i).AddSeconds(1) // Within tolerance
                    : baseTime.AddHours(leftFileNames.Get.Length + i); // Outside tolerance
                
                rightPane.Entries.Add(new FileEntry
                {
                    Name = "right_" + sanitizedName,
                    FullPath = "/right/" + sanitizedName,
                    Size = 1000,
                    IsDirectory = false,
                    LastModified = timestamp
                });
            }

            var fileOps = new FileOperations();
            var tolerance = TimeSpan.FromSeconds(2);

            // Act: Compare files by timestamp
            var result = fileOps.CompareFiles(leftPane, rightPane, ComparisonCriteria.Timestamp, tolerance);

            // Assert: All marked files should have matching timestamps (within tolerance) in the other pane
            bool leftMarkedValid = true;
            foreach (var leftEntry in leftPane.Entries)
            {
                if (leftEntry.IsMarked)
                {
                    bool found = false;
                    foreach (var r in rightPane.Entries)
                    {
                        if (!r.IsDirectory && Math.Abs((r.LastModified - leftEntry.LastModified).TotalSeconds) <= tolerance.TotalSeconds)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) { leftMarkedValid = false; break; }
                }
            }

            bool rightMarkedValid = true;
            foreach (var rightEntry in rightPane.Entries)
            {
                if (rightEntry.IsMarked)
                {
                    bool found = false;
                    foreach (var l in leftPane.Entries)
                    {
                        if (!l.IsDirectory && Math.Abs((l.LastModified - rightEntry.LastModified).TotalSeconds) <= tolerance.TotalSeconds)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) { rightMarkedValid = false; break; }
                }
            }

            int leftMarkedCount = 0;
            foreach (var e in leftPane.Entries) if (e.IsMarked) leftMarkedCount++;
            
            int rightMarkedCount = 0;
            foreach (var e in rightPane.Entries) if (e.IsMarked) rightMarkedCount++;

            return (result.Success && leftMarkedValid && rightMarkedValid)
                .ToProperty()
                .Label($"Comparison by timestamp should mark files with matching timestamps. " +
                       $"Success: {result.Success}, Left marked: {leftMarkedCount}, " +
                       $"Right marked: {rightMarkedCount}, " +
                       $"Left valid: {leftMarkedValid}, Right valid: {rightMarkedValid}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 35: File comparison marks matching files
        /// Validates: Requirements 19.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FileComparison_MarksByName_MarksMatchingFiles(
            NonEmptyArray<NonEmptyString> fileNames)
        {
            // Arrange: Create two panes with files
            var leftPane = new PaneState();
            var rightPane = new PaneState();

            // Create files in left pane
            for (int i = 0; i < fileNames.Get.Length; i++)
            {
                var sanitizedName = SanitizeFileName(fileNames.Get[i].Get);
                
                leftPane.Entries.Add(new FileEntry
                {
                    Name = sanitizedName,
                    FullPath = "/left/" + sanitizedName,
                    Size = 1000 + i,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow.AddHours(i)
                });
            }

            // Create files in right pane, some with matching names
            for (int i = 0; i < fileNames.Get.Length; i++)
            {
                var sanitizedName = SanitizeFileName(fileNames.Get[i].Get);
                // Use same name for half the files to create matches
                var name = i < fileNames.Get.Length / 2 
                    ? sanitizedName 
                    : "different_" + sanitizedName;
                
                rightPane.Entries.Add(new FileEntry
                {
                    Name = name,
                    FullPath = "/right/" + name,
                    Size = 2000 + i,
                    IsDirectory = false,
                    LastModified = DateTime.UtcNow.AddHours(i + 100)
                });
            }

            var fileOps = new FileOperations();

            // Act: Compare files by name
            var result = fileOps.CompareFiles(leftPane, rightPane, ComparisonCriteria.Name);

            // Assert: All marked files should have matching names in the other pane
            bool leftMarkedValid = true;
            foreach (var leftEntry in leftPane.Entries)
            {
                if (leftEntry.IsMarked)
                {
                    bool found = false;
                    foreach (var r in rightPane.Entries)
                    {
                        if (!r.IsDirectory && string.Equals(r.Name, leftEntry.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) { leftMarkedValid = false; break; }
                }
            }

            bool rightMarkedValid = true;
            foreach (var rightEntry in rightPane.Entries)
            {
                if (rightEntry.IsMarked)
                {
                    bool found = false;
                    foreach (var l in leftPane.Entries)
                    {
                        if (!l.IsDirectory && string.Equals(l.Name, rightEntry.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) { rightMarkedValid = false; break; }
                }
            }

            int leftMarkedCount = 0;
            foreach (var e in leftPane.Entries) if (e.IsMarked) leftMarkedCount++;
            
            int rightMarkedCount = 0;
            foreach (var e in rightPane.Entries) if (e.IsMarked) rightMarkedCount++;

            return (result.Success && leftMarkedValid && rightMarkedValid)
                .ToProperty()
                .Label($"Comparison by name should mark files with matching names. " +
                       $"Success: {result.Success}, Left marked: {leftMarkedCount}, " +
                       $"Right marked: {rightMarkedCount}, " +
                       $"Left valid: {leftMarkedValid}, Right valid: {rightMarkedValid}");
        }

        private string SanitizeFileName(string name)
        {
            // Remove invalid characters and limit length
            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (!invalid.Contains(c)) sb.Append(c);
            }
            var sanitized = sb.ToString();
            
            // Ensure it's not empty and not too long
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "file";
            }
            
            // Exclude special directory names
            if (sanitized == "." || sanitized == "..")
            {
                sanitized = "file";
            }
            
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50);
            }

            return sanitized;
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 42: Progress indicator shows operation status
        /// Validates: Requirements 23.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ProgressIndicator_ShowsOperationStatus(
            NonEmptyArray<NonEmptyString> fileNames,
            NonEmptyArray<byte> content)
        {
            // Arrange: Create multiple test files
            var sourceDir = CreateTestDirectory("progress_source");
            var destDir = CreateTestDirectory("progress_dest");
            
            var fileEntries = new List<FileEntry>();
            
            // Create multiple files to ensure progress events are fired
            for (int i = 0; i < Math.Min(fileNames.Get.Length, 5); i++)
            {
                var sanitizedName = SanitizeFileName(fileNames.Get[i].Get) + $"_{i}";
                var sourceFile = Path.Combine(sourceDir, sanitizedName);
                File.WriteAllBytes(sourceFile, content.Get);
                
                fileEntries.Add(new FileEntry
                {
                    FullPath = sourceFile,
                    Name = sanitizedName,
                    Size = content.Get.Length,
                    IsDirectory = false
                });
            }
            
            var fileOps = new FileOperations();
            var progressEvents = new List<ProgressEventArgs>();
            
            // Subscribe to progress events
            EventHandler<ProgressEventArgs> progressHandler = (sender, e) =>
            {
                progressEvents.Add(e);
            };
            
            fileOps.ProgressChanged += progressHandler;
            
            // Act: Copy files (which should trigger progress events)
            var result = fileOps.CopyAsync(
                fileEntries,
                destDir,
                CancellationToken.None).Result;
            
            fileOps.ProgressChanged -= progressHandler;
            
            // Assert: Progress events should have been fired
            var progressEventsReceived = progressEvents.Count > 0;
            
            // Verify progress events contain required information
            bool allEventsHaveCurrentFile = true;
            bool allEventsHaveFileIndex = true;
            bool allEventsHaveTotalFiles = true;
            bool allEventsHavePercentComplete = true;

            foreach (var e in progressEvents)
            {
                if (string.IsNullOrEmpty(e.CurrentFile)) allEventsHaveCurrentFile = false;
                if (e.CurrentFileIndex <= 0 || e.CurrentFileIndex > e.TotalFiles) allEventsHaveFileIndex = false;
                if (e.TotalFiles != fileEntries.Count) allEventsHaveTotalFiles = false;
                if (e.PercentComplete < 0 || e.PercentComplete > 100) allEventsHavePercentComplete = false;
            }
            
            // Verify progress increases over time
            var progressIncreases = true;
            for (int i = 1; i < progressEvents.Count; i++)
            {
                if (progressEvents[i].BytesProcessed < progressEvents[i - 1].BytesProcessed)
                {
                    progressIncreases = false;
                    break;
                }
            }
            
            return (result.Success && progressEventsReceived && allEventsHaveCurrentFile && 
                    allEventsHaveFileIndex && allEventsHaveTotalFiles && allEventsHavePercentComplete && 
                    progressIncreases)
                .ToProperty()
                .Label($"Progress indicator should show operation status. Success: {result.Success}, " +
                       $"Events received: {progressEvents.Count}, Has current file: {allEventsHaveCurrentFile}, " +
                       $"Has file index: {allEventsHaveFileIndex}, Has total files: {allEventsHaveTotalFiles}, " +
                       $"Has percent: {allEventsHavePercentComplete}, Progress increases: {progressIncreases}");
        }
        
        /// <summary>
        /// Feature: twf-file-manager, Property 43: Operation cancellation stops processing
        /// Validates: Requirements 23.3
        /// </summary>
        [Property(MaxTest = 100)]
        public Property OperationCancellation_StopsProcessing(
            NonEmptyArray<NonEmptyString> fileNames,
            NonEmptyArray<byte> content)
        {
            // Arrange: Create multiple test files
            var sourceDir = CreateTestDirectory("cancel_source");
            var destDir = CreateTestDirectory("cancel_dest");
            
            var fileEntries = new List<FileEntry>();
            
            // Create enough files to ensure we can cancel mid-operation
            // Use at least 10 files to increase likelihood of catching cancellation
            var fileCount = Math.Max(10, Math.Min(fileNames.Get.Length, 20));
            
            for (int i = 0; i < fileCount; i++)
            {
                var sanitizedName = SanitizeFileName(fileNames.Get[i % fileNames.Get.Length].Get) + $"_{i}";
                var sourceFile = Path.Combine(sourceDir, sanitizedName);
                
                // Create larger files to give more time for cancellation
                var largeContent = new byte[content.Get.Length * 100];
                Array.Copy(content.Get, 0, largeContent, 0, Math.Min(content.Get.Length, largeContent.Length));
                File.WriteAllBytes(sourceFile, largeContent);
                
                fileEntries.Add(new FileEntry
                {
                    FullPath = sourceFile,
                    Name = sanitizedName,
                    Size = largeContent.Length,
                    IsDirectory = false
                });
            }
            
            var fileOps = new FileOperations();
            var cancellationTokenSource = new CancellationTokenSource();
            var progressEventCount = 0;
            
            // Subscribe to progress events and cancel after first few events
            EventHandler<ProgressEventArgs> progressHandler = (sender, e) =>
            {
                progressEventCount++;
                // Cancel after processing a few files
                if (progressEventCount >= 3)
                {
                    cancellationTokenSource.Cancel();
                }
            };
            
            fileOps.ProgressChanged += progressHandler;
            
            // Act: Start copy operation and cancel it mid-operation
            var result = fileOps.CopyAsync(
                fileEntries,
                destDir,
                cancellationTokenSource.Token).Result;
            
            fileOps.ProgressChanged -= progressHandler;
            
            // Assert: Operation should have been cancelled
            var wasCancelled = cancellationTokenSource.Token.IsCancellationRequested;
            
            // Not all files should have been processed (some should be skipped due to cancellation)
            var notAllFilesProcessed = result.FilesProcessed < fileEntries.Count;
            
            // Count how many files actually exist in destination
            var copiedFiles = Directory.GetFiles(destDir).Length;
            var fewerFilesCopied = copiedFiles < fileEntries.Count;
            
            // The operation should indicate it was cancelled (either in message or by not processing all files)
            var indicatesCancellation = result.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase) || 
                                       notAllFilesProcessed;
            
            return (wasCancelled && notAllFilesProcessed && fewerFilesCopied && indicatesCancellation)
                .ToProperty()
                .Label($"Cancellation should stop processing. Was cancelled: {wasCancelled}, " +
                       $"Not all processed: {notAllFilesProcessed} ({result.FilesProcessed}/{fileEntries.Count}), " +
                       $"Fewer copied: {fewerFilesCopied} ({copiedFiles}/{fileEntries.Count}), " +
                       $"Indicates cancellation: {indicatesCancellation}, Message: {result.Message}");
        }
    }
}
