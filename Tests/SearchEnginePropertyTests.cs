using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Services;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for SearchEngine
    /// </summary>
    public class SearchEnginePropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 24: Incremental search finds matching files
        /// Validates: Requirements 11.2
        /// 
        /// This property verifies that incremental search moves the cursor to files
        /// whose names start with the search pattern (case-insensitive).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property IncrementalSearch_FindsMatchingFiles(List<FileEntryData> fileData, NonEmptyString searchPatternStr)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to search");
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
                return true.ToProperty().Label("No valid entries to search");
            }

            // Create a valid search pattern (sanitize it)
            string searchPattern = SanitizeSearchPattern(searchPatternStr.Get);

            // Arrange
            var engine = new SearchEngine();

            // Act: Find all matches
            var matches = engine.FindMatches(entries, searchPattern, useMigemo: false);

            // Assert: All matched files should start with the search pattern (case-insensitive)
            bool allMatchesValid = true;
            foreach (var index in matches)
            {
                if (index < 0 || index >= entries.Count || !entries[index].Name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                {
                    allMatchesValid = false;
                    break;
                }
            }

            // Assert: All files that start with the pattern should be in the matches
            var expectedMatches = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Name.StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                {
                    expectedMatches.Add(i);
                }
            }

            bool allExpectedFound = true;
            foreach (var expected in expectedMatches)
            {
                bool found = false;
                foreach (var m in matches)
                {
                    if (m == expected)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    allExpectedFound = false;
                    break;
                }
            }

            return (allMatchesValid && allExpectedFound).ToProperty()
                .Label($"Pattern: '{searchPattern}', Matches: {matches.Count}, Expected: {expectedMatches.Count}, All valid: {allMatchesValid}, All found: {allExpectedFound}");
        }

        /// <summary>
        /// Property: FindNext wraps around to the beginning when reaching the end
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FindNext_WrapsAroundToBeginning(List<FileEntryData> fileData, NonEmptyString searchPatternStr)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to search");
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
                return true.ToProperty().Label("No valid entries to search");
            }

            // Create a search pattern that matches at least one file
            // Use the first character of the first entry's name
            string searchPattern = entries[0].Name.Substring(0, 1);

            // Arrange
            var engine = new SearchEngine();

            // Find all matches
            var allMatches = engine.FindMatches(entries, searchPattern, useMigemo: false);

            if (allMatches.Count == 0)
            {
                return true.ToProperty().Label("No matches found - wrap around not applicable");
            }

            // Act: Start from the last match and find next
            int lastMatchIndex = allMatches[allMatches.Count - 1];
            int nextMatch = engine.FindNext(entries, searchPattern, lastMatchIndex, useMigemo: false);

            // Assert: Should wrap around to the first match
            if (allMatches.Count > 1)
            {
                // If there are multiple matches, next should be the first match
                bool wrappedCorrectly = nextMatch == allMatches[0];
                return wrappedCorrectly.ToProperty()
                    .Label($"Last match: {lastMatchIndex}, Next match: {nextMatch}, Expected: {allMatches[0]}");
            }
            else
            {
                // If there's only one match, it should return itself
                bool returnedSelf = nextMatch == lastMatchIndex;
                return returnedSelf.ToProperty()
                    .Label($"Single match at {lastMatchIndex}, returned: {nextMatch}");
            }
        }

        /// <summary>
        /// Property: FindPrevious wraps around to the end when reaching the beginning
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FindPrevious_WrapsAroundToEnd(List<FileEntryData> fileData, NonEmptyString searchPatternStr)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to search");
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
                return true.ToProperty().Label("No valid entries to search");
            }

            // Create a search pattern that matches at least one file
            string searchPattern = entries[0].Name.Substring(0, 1);

            // Arrange
            var engine = new SearchEngine();

            // Find all matches
            var allMatches = engine.FindMatches(entries, searchPattern, useMigemo: false);

            if (allMatches.Count == 0)
            {
                return true.ToProperty().Label("No matches found - wrap around not applicable");
            }

            // Act: Start from the first match and find previous
            int firstMatchIndex = allMatches[0];
            int prevMatch = engine.FindPrevious(entries, searchPattern, firstMatchIndex, useMigemo: false);

            // Assert: Should wrap around to the last match
            if (allMatches.Count > 1)
            {
                // If there are multiple matches, previous should be the last match
                bool wrappedCorrectly = prevMatch == allMatches[allMatches.Count - 1];
                return wrappedCorrectly.ToProperty()
                    .Label($"First match: {firstMatchIndex}, Previous match: {prevMatch}, Expected: {allMatches[allMatches.Count - 1]}");
            }
            else
            {
                // If there's only one match, it should return itself
                bool returnedSelf = prevMatch == firstMatchIndex;
                return returnedSelf.ToProperty()
                    .Label($"Single match at {firstMatchIndex}, returned: {prevMatch}");
            }
        }

        /// <summary>
        /// Property: Search is case-insensitive
        /// </summary>
        [Property(MaxTest = 100)]
        public Property Search_IsCaseInsensitive(List<FileEntryData> fileData)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to search");
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
                return true.ToProperty().Label("No valid entries to search");
            }

            // Use the first character of the first entry
            string firstChar = entries[0].Name.Substring(0, 1);

            // Arrange
            var engine = new SearchEngine();

            // Act: Search with lowercase, uppercase, and original
            var matchesLower = engine.FindMatches(entries, firstChar.ToLower(), useMigemo: false);
            var matchesUpper = engine.FindMatches(entries, firstChar.ToUpper(), useMigemo: false);
            var matchesOriginal = engine.FindMatches(entries, firstChar, useMigemo: false);

            // Assert: All should return the same matches
            bool sameLowerUpper = matchesLower.Count == matchesUpper.Count;
            if (sameLowerUpper)
            {
                foreach (var m in matchesLower)
                {
                    bool found = false;
                    foreach (var mu in matchesUpper)
                    {
                        if (m == mu) { found = true; break; }
                    }
                    if (!found) { sameLowerUpper = false; break; }
                }
            }

            bool sameLowerOriginal = matchesLower.Count == matchesOriginal.Count;
            if (sameLowerOriginal)
            {
                foreach (var m in matchesLower)
                {
                    bool found = false;
                    foreach (var mo in matchesOriginal)
                    {
                        if (m == mo) { found = true; break; }
                    }
                    if (!found) { sameLowerOriginal = false; break; }
                }
            }

            return (sameLowerUpper && sameLowerOriginal).ToProperty()
                .Label($"Lower: {matchesLower.Count}, Upper: {matchesUpper.Count}, Original: {matchesOriginal.Count}");
        }

        /// <summary>
        /// Property: Empty search pattern returns no matches
        /// </summary>
        [Property(MaxTest = 100)]
        public Property EmptyPattern_ReturnsNoMatches(List<FileEntryData> fileData)
        {
            // Convert to FileEntry objects
            var entries = new List<FileEntry>();
            if (fileData != null)
            {
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
            }

            // Arrange
            var engine = new SearchEngine();

            // Act: Search with empty pattern
            var matches = engine.FindMatches(entries, "", useMigemo: false);

            // Assert: Should return no matches
            return (matches.Count == 0).ToProperty()
                .Label("Empty pattern should return no matches");
        }

        /// <summary>
        /// Property: Migemo fallback works when Migemo is unavailable
        /// </summary>
        [Property(MaxTest = 100)]
        public Property MigemoUnavailable_FallsBackToStandardSearch(List<FileEntryData> fileData, NonEmptyString searchPatternStr)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to search");
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
                return true.ToProperty().Label("No valid entries to search");
            }

            string searchPattern = SanitizeSearchPattern(searchPatternStr.Get);

            // Arrange: Create engine without Migemo provider
            var engineWithoutMigemo = new SearchEngine();

            // Act: Search with Migemo flag set to true (should fall back to standard search)
            var matchesWithMigemoFlag = engineWithoutMigemo.FindMatches(entries, searchPattern, useMigemo: true);
            var matchesWithoutMigemoFlag = engineWithoutMigemo.FindMatches(entries, searchPattern, useMigemo: false);

            // Assert: Should produce the same results (fallback to standard search)
            bool sameResults = matchesWithMigemoFlag.Count == matchesWithoutMigemoFlag.Count;
            if (sameResults)
            {
                foreach (var m in matchesWithMigemoFlag)
                {
                    bool found = false;
                    foreach (var mo in matchesWithoutMigemoFlag)
                    {
                        if (m == mo) { found = true; break; }
                    }
                    if (!found) { sameResults = false; break; }
                }
            }

            return sameResults.ToProperty()
                .Label($"With Migemo flag: {matchesWithMigemoFlag.Count}, Without: {matchesWithoutMigemoFlag.Count}");
        }

        /// <summary>
        /// Helper method to sanitize search patterns
        /// </summary>
        private static string SanitizeSearchPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return "a";

            // Remove invalid characters
            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            var sb = new System.Text.StringBuilder();
            foreach (char c in pattern)
            {
                if (!invalid.Contains(c))
                {
                    sb.Append(c);
                }
            }
            var sanitized = sb.ToString();

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "a";

            // Take only the first few characters for a reasonable search pattern
            if (sanitized.Length > 5)
                sanitized = sanitized.Substring(0, 5);

            return sanitized;
        }
    }
}
