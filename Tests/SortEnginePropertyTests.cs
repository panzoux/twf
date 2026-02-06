using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Services;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for SortEngine
    /// </summary>
    public class SortEnginePropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 22: Sort mode changes file order
        /// Validates: Requirements 9.1
        /// 
        /// This property verifies that sorting by name produces an alphabetically ordered list
        /// where directories appear before files.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property SortByName_ProducesAlphabeticalOrderWithDirectoriesFirst(List<FileEntryData> fileData)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list is trivially sorted");
            }

            // Convert to FileEntry objects
            var entries = new List<FileEntry>();
            foreach (var fd in fileData)
            {
                if (fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                {
                    entries.Add(new FileEntry
                    {
                        Name = fd.Name,
                        Size = fd.Size,
                        LastModified = fd.LastModified,
                        IsDirectory = fd.IsDirectory,
                        Extension = fd.Extension ?? string.Empty,
                        FullPath = fd.Name,
                        Attributes = FileAttributes.Normal
                    });
                }
            }

            if (entries.Count == 0)
            {
                return true.ToProperty().Label("No valid entries to sort");
            }

            // Act: Sort by name ascending
            var sorted = SortEngine.Sort(entries, SortMode.NameAscending);

            // Assert: Verify the sort order
            bool isCorrectlySorted = true;
            string errorMessage = "";

            // Check that directories come before files
            int lastDirectoryIndex = -1;
            int firstFileIndex = -1;

            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i].IsDirectory)
                {
                    lastDirectoryIndex = i;
                }
                else if (firstFileIndex == -1)
                {
                    firstFileIndex = i;
                }
            }

            // If we have both directories and files, directories should come first
            if (lastDirectoryIndex >= 0 && firstFileIndex >= 0 && lastDirectoryIndex > firstFileIndex)
            {
                isCorrectlySorted = false;
                errorMessage = $"Directories should appear before files. Last directory at index {lastDirectoryIndex}, first file at index {firstFileIndex}";
            }

            // Check alphabetical order within directories
            if (isCorrectlySorted)
            {
                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    // Only compare consecutive entries of the same type (both dirs or both files)
                    if (sorted[i].IsDirectory == sorted[i + 1].IsDirectory)
                    {
                        int comparison = string.Compare(sorted[i].Name, sorted[i + 1].Name, StringComparison.OrdinalIgnoreCase);
                        if (comparison > 0)
                        {
                            isCorrectlySorted = false;
                            errorMessage = $"Names not in alphabetical order: '{sorted[i].Name}' should not come before '{sorted[i + 1].Name}'";
                            break;
                        }
                    }
                }
            }

            return isCorrectlySorted.ToProperty()
                .Label(errorMessage != "" ? errorMessage : "Sort order is correct");
        }

        /// <summary>
        /// Property: Sorting preserves all entries (no entries lost or added)
        /// </summary>
        [Property(MaxTest = 100)]
        public Property Sort_PreservesAllEntries(List<FileEntryData> fileData, SortMode sortMode)
        {
            if (fileData == null)
            {
                return true.ToProperty().Label("Null input handled");
            }

            // Convert to FileEntry objects
            var entries = new List<FileEntry>();
            foreach (var fd in fileData)
            {
                if (fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                {
                    entries.Add(new FileEntry
                    {
                        Name = fd.Name,
                        Size = fd.Size,
                        LastModified = fd.LastModified,
                        IsDirectory = fd.IsDirectory,
                        Extension = fd.Extension ?? string.Empty,
                        FullPath = fd.Name,
                        Attributes = FileAttributes.Normal
                    });
                }
            }

            // Act
            var sorted = SortEngine.Sort(entries, sortMode);

            // Assert: Same count
            var sameCount = sorted.Count == entries.Count;

            // Assert: All original entries are present
            bool allPresent = true;
            foreach (var e in entries)
            {
                bool found = false;
                foreach (var s in sorted)
                {
                    if (s.Name == e.Name && s.Size == e.Size)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    allPresent = false;
                    break;
                }
            }

            return (sameCount && allPresent).ToProperty()
                .Label($"Expected {entries.Count} entries, got {sorted.Count}. All entries preserved: {allPresent}");
        }

        /// <summary>
        /// Property: Sorting an empty list returns an empty list
        /// </summary>
        [Property(MaxTest = 100)]
        public Property Sort_EmptyList_ReturnsEmptyList(SortMode sortMode)
        {
            // Act
            var sorted = SortEngine.Sort(new List<FileEntry>(), sortMode);

            // Assert
            return (sorted.Count == 0).ToProperty()
                .Label("Empty list should remain empty after sorting");
        }

        /// <summary>
        /// Property: Sorting null returns empty list
        /// </summary>
        [Property(MaxTest = 100)]
        public Property Sort_NullList_ReturnsEmptyList(SortMode sortMode)
        {
            // Act
            var sorted = SortEngine.Sort(null!, sortMode);

            // Assert
            return (sorted != null && sorted.Count == 0).ToProperty()
                .Label("Null list should return empty list");
        }
    }

    /// <summary>
    /// Data class for generating FileEntry test data
    /// </summary>
    public class FileEntryData
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsDirectory { get; set; }
        public string? Extension { get; set; }

        /// <summary>
        /// Custom generator for FileEntryData
        /// </summary>
        public static Arbitrary<FileEntryData> Generator()
        {
            return Arb.From(
                from name in Arb.Generate<NonEmptyString>()
                from size in Arb.Generate<PositiveInt>()
                from date in Arb.Generate<DateTime>()
                from isDir in Arb.Generate<bool>()
                from ext in Arb.Generate<string>()
                select new FileEntryData
                {
                    Name = SanitizeFileName(name.Get),
                    Size = size.Get,
                    LastModified = date,
                    IsDirectory = isDir,
                    Extension = ext
                });
        }

        private static string SanitizeFileName(string name)
        {
            // Remove invalid filename characters
            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (!invalid.Contains(c))
                {
                    sb.Append(c);
                }
            }
            var sanitized = sb.ToString();
            
            // Ensure we have at least one character
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "file";
            }

            return sanitized;
        }
    }
}
