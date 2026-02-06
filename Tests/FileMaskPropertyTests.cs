using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Providers;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for file mask filtering
    /// </summary>
    public class FileMaskPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 23: File mask filters displayed files
        /// Validates: Requirements 10.2
        /// 
        /// This property verifies that when a file mask is applied,
        /// only files matching the mask pattern are displayed in the file list.
        /// Directories are always included regardless of the mask.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FileMask_FiltersDisplayedFiles(List<FileEntryData> fileData, NonEmptyString maskStr)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to filter");
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
                return true.ToProperty().Label("No valid entries to filter");
            }

            // Create a valid file mask pattern (sanitize to avoid invalid patterns)
            string mask = SanitizeMask(maskStr.Get);

            // Arrange
            var provider = new FileSystemProvider();

            // Act
            var filtered = provider.ApplyFileMask(entries, mask);

            // Assert: Verify filtering rules
            // 1. All directories should be included
            bool allDirectoriesIncluded = true;
            foreach (var e in entries)
            {
                if (e.IsDirectory)
                {
                    bool found = false;
                    foreach (var f in filtered)
                    {
                        if (f.Name == e.Name) { found = true; break; }
                    }
                    if (!found) { allDirectoriesIncluded = false; break; }
                }
            }

            // 2. All filtered files (non-directories) should match at least one inclusion pattern
            //    and not match any exclusion pattern
            var patterns = mask.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var inclusionPatterns = new List<string>();
            var exclusionPatterns = new List<string>();
            foreach (var p in patterns)
            {
                if (p.StartsWith(":")) exclusionPatterns.Add(p.Substring(1));
                else inclusionPatterns.Add(p);
            }

            bool allFilesMatchRules = true;
            foreach (var file in filtered)
            {
                if (!file.IsDirectory)
                {
                    // Check if file matches inclusion patterns
                    bool matchesInclusion = inclusionPatterns.Count == 0;
                    if (!matchesInclusion)
                    {
                        foreach (var p in inclusionPatterns)
                        {
                            if (MatchesWildcard(file.Name, p)) { matchesInclusion = true; break; }
                        }
                    }

                    // Check if file doesn't match any exclusion patterns
                    bool matchesExclusion = false;
                    foreach (var p in exclusionPatterns)
                    {
                        if (MatchesWildcard(file.Name, p)) { matchesExclusion = true; break; }
                    }

                    if (!matchesInclusion || matchesExclusion)
                    {
                        allFilesMatchRules = false;
                        break;
                    }
                }
            }

            // 3. No files that should be excluded are in the filtered list
            bool noExcludedFiles = true;
            foreach (var file in filtered)
            {
                if (!file.IsDirectory)
                {
                    foreach (var p in exclusionPatterns)
                    {
                        if (MatchesWildcard(file.Name, p))
                        {
                            noExcludedFiles = false;
                            break;
                        }
                    }
                    if (!noExcludedFiles) break;
                }
            }

            bool result = allDirectoriesIncluded && allFilesMatchRules && noExcludedFiles;

            return result.ToProperty()
                .Label($"Mask: '{mask}', Original: {entries.Count}, Filtered: {filtered.Count}, " +
                       $"Dirs included: {allDirectoriesIncluded}, Files match: {allFilesMatchRules}, " +
                       $"No excluded: {noExcludedFiles}");
        }

        /// <summary>
        /// Sanitizes a mask pattern to ensure it's valid for testing
        /// </summary>
        private string SanitizeMask(string mask)
        {
            if (string.IsNullOrWhiteSpace(mask))
            {
                return "*.txt";
            }

            // Remove invalid characters that could cause issues
            mask = mask.Replace("\0", "").Replace("\n", "").Replace("\r", "").Replace("\t", " ");

            // Ensure the mask has at least some valid pattern
            bool allSpaces = true;
            foreach (char c in mask) if (c != ' ') { allSpaces = false; break; }
            if (string.IsNullOrWhiteSpace(mask) || allSpaces)
            {
                return "*.txt";
            }

            // Limit length to avoid extremely long patterns
            if (mask.Length > 100)
            {
                mask = mask.Substring(0, 100);
            }

            return mask.Trim();
        }

        /// <summary>
        /// Matches a filename against a wildcard pattern
        /// Simplified version for testing - matches the logic in FileSystemProvider
        /// </summary>
        private bool MatchesWildcard(string filename, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
            {
                return true;
            }

            try
            {
                // Convert wildcard pattern to regex
                // Escape special regex characters except * and ?
                string regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                return System.Text.RegularExpressions.Regex.IsMatch(
                    filename,
                    regexPattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
