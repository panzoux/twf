using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Services;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for MarkingEngine
    /// </summary>
    public class MarkingEnginePropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 16: Wildcard pattern marks matching files
        /// Validates: Requirements 4.2
        /// 
        /// This property verifies that when a wildcard pattern is applied,
        /// all marked files match at least one of the patterns (when multiple patterns are provided).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property WildcardPattern_MarksMatchingFiles(List<FileEntryData> fileData, NonEmptyString patternStr)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to mark");
            }

            // Convert to FileEntry objects
            var entries = fileData
                .Where(fd => fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                .Select(fd => new FileEntry
                {
                    Name = fd.Name,
                    Size = fd.Size,
                    LastModified = fd.LastModified,
                    IsDirectory = fd.IsDirectory,
                    Extension = fd.Extension ?? string.Empty,
                    FullPath = fd.Name,
                    Attributes = FileAttributes.Normal
                })
                .ToList();

            if (entries.Count == 0)
            {
                return true.ToProperty().Label("No valid entries to mark");
            }

            // Create a valid wildcard pattern (without spaces to avoid multi-pattern complexity)
            string pattern = SanitizePattern(patternStr.Get).Replace(" ", "");

            // Arrange
            var pane = new PaneState { Entries = entries };
            var engine = new MarkingEngine();

            // Act
            engine.MarkByWildcard(pane, pattern);
            var markedFiles = pane.GetMarkedEntries();

            // Assert: All marked files should match the pattern
            // Since we're using a single pattern (no spaces), all marked files should match it
            bool allMatch = markedFiles.All(f => engine.MatchesWildcard(f.Name, pattern));

            return allMatch.ToProperty()
                .Label($"Pattern: '{pattern}', Marked: {markedFiles.Count}/{entries.Count}, All match: {allMatch}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 12: Space toggles mark and moves cursor down
        /// Validates: Requirements 3.1
        /// 
        /// This property verifies that toggling a mark changes the mark state.
        /// Note: Cursor movement is handled by the UI controller, not the MarkingEngine.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ToggleMark_ChangesMarkState(List<FileEntryData> fileData, NonNegativeInt indexGen)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to mark");
            }

            // Convert to FileEntry objects
            var entries = fileData
                .Where(fd => fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                .Select(fd => new FileEntry
                {
                    Name = fd.Name,
                    Size = fd.Size,
                    LastModified = fd.LastModified,
                    IsDirectory = fd.IsDirectory,
                    Extension = fd.Extension ?? string.Empty,
                    FullPath = fd.Name,
                    Attributes = FileAttributes.Normal
                })
                .ToList();

            if (entries.Count == 0)
            {
                return true.ToProperty().Label("No valid entries to mark");
            }

            // Get a valid index
            int index = indexGen.Get % entries.Count;

            // Arrange
            var pane = new PaneState { Entries = entries };
            var engine = new MarkingEngine();

            // Get initial state
            bool wasMarked = pane.MarkedIndices.Contains(index);

            // Act: Toggle mark
            engine.ToggleMark(pane, index);

            // Assert: Mark state should be inverted
            bool isNowMarked = pane.MarkedIndices.Contains(index);
            bool stateChanged = wasMarked != isNowMarked;

            // Act: Toggle again
            engine.ToggleMark(pane, index);

            // Assert: Should return to original state
            bool isBackToOriginal = pane.MarkedIndices.Contains(index) == wasMarked;

            return (stateChanged && isBackToOriginal).ToProperty()
                .Label($"Index: {index}, Initial: {wasMarked}, After toggle: {isNowMarked}, After second toggle: {pane.MarkedIndices.Contains(index)}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 14: Ctrl+Space marks range
        /// Validates: Requirements 3.3
        /// 
        /// This property verifies that marking a range marks all entries
        /// between the start and end indices (inclusive).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property MarkRange_MarksAllEntriesInRange(List<FileEntryData> fileData, NonNegativeInt startGen, NonNegativeInt endGen)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to mark");
            }

            // Convert to FileEntry objects
            var entries = fileData
                .Where(fd => fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                .Select(fd => new FileEntry
                {
                    Name = fd.Name,
                    Size = fd.Size,
                    LastModified = fd.LastModified,
                    IsDirectory = fd.IsDirectory,
                    Extension = fd.Extension ?? string.Empty,
                    FullPath = fd.Name,
                    Attributes = FileAttributes.Normal
                })
                .ToList();

            if (entries.Count == 0)
            {
                return true.ToProperty().Label("No valid entries to mark");
            }

            // Get valid indices
            int startIndex = startGen.Get % entries.Count;
            int endIndex = endGen.Get % entries.Count;

            // Arrange
            var pane = new PaneState { Entries = entries };
            var engine = new MarkingEngine();

            // Act
            engine.MarkRange(pane, startIndex, endIndex);

            // Assert: All entries in the range should be marked
            int expectedStart = Math.Min(startIndex, endIndex);
            int expectedEnd = Math.Max(startIndex, endIndex);

            bool allInRangeMarked = true;
            for (int i = expectedStart; i <= expectedEnd; i++)
            {
                if (!pane.MarkedIndices.Contains(i))
                {
                    allInRangeMarked = false;
                    break;
                }
            }

            // Also verify that the correct number of marks exist (at least the range size)
            int rangeSize = expectedEnd - expectedStart + 1;
            bool hasEnoughMarks = pane.MarkedIndices.Count >= rangeSize;

            return (allInRangeMarked && hasEnoughMarks).ToProperty()
                .Label($"Range: [{expectedStart}, {expectedEnd}], Marked count: {pane.MarkedIndices.Count}, All in range marked: {allInRangeMarked}");
        }

        /// <summary>
        /// Property: Invert marks should flip all mark states
        /// </summary>
        [Property(MaxTest = 100)]
        public Property InvertMarks_FlipsAllMarkStates(List<FileEntryData> fileData)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to mark");
            }

            // Convert to FileEntry objects
            var entries = fileData
                .Where(fd => fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                .Select(fd => new FileEntry
                {
                    Name = fd.Name,
                    Size = fd.Size,
                    LastModified = fd.LastModified,
                    IsDirectory = fd.IsDirectory,
                    Extension = fd.Extension ?? string.Empty,
                    FullPath = fd.Name,
                    Attributes = FileAttributes.Normal
                })
                .ToList();

            if (entries.Count == 0)
            {
                return true.ToProperty().Label("No valid entries to mark");
            }

            // Arrange
            var pane = new PaneState { Entries = entries };
            var engine = new MarkingEngine();

            // Mark some random entries
            for (int i = 0; i < entries.Count; i += 2)
            {
                pane.MarkedIndices.Add(i);
            }

            var originalMarked = new HashSet<int>(pane.MarkedIndices);

            // Act
            engine.InvertMarks(pane);

            // Assert: Previously marked should be unmarked, previously unmarked should be marked
            bool correctInversion = true;
            for (int i = 0; i < entries.Count; i++)
            {
                bool wasMarked = originalMarked.Contains(i);
                bool isNowMarked = pane.MarkedIndices.Contains(i);

                if (wasMarked == isNowMarked)
                {
                    correctInversion = false;
                    break;
                }
            }

            // Total count should be complementary
            int expectedCount = entries.Count - originalMarked.Count;
            bool correctCount = pane.MarkedIndices.Count == expectedCount;

            return (correctInversion && correctCount).ToProperty()
                .Label($"Original marked: {originalMarked.Count}, After invert: {pane.MarkedIndices.Count}, Expected: {expectedCount}");
        }

        /// <summary>
        /// Property: Wildcard matching should be case-insensitive
        /// </summary>
        [Property(MaxTest = 100)]
        public Property WildcardMatch_IsCaseInsensitive(NonEmptyString filename)
        {
            var engine = new MarkingEngine();
            string name = SanitizeFileName(filename.Get);

            // Create pattern from the filename
            string pattern = name.ToLower() + "*";

            // Test with uppercase filename
            bool matchesUpper = engine.MatchesWildcard(name.ToUpper(), pattern);
            bool matchesLower = engine.MatchesWildcard(name.ToLower(), pattern);
            bool matchesOriginal = engine.MatchesWildcard(name, pattern);

            return (matchesUpper && matchesLower && matchesOriginal).ToProperty()
                .Label($"Pattern: '{pattern}', Filename: '{name}', Upper: {matchesUpper}, Lower: {matchesLower}, Original: {matchesOriginal}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 13: Shift+Space toggles mark and moves cursor up
        /// Validates: Requirements 3.2
        /// 
        /// This property verifies that toggling a mark with Shift+Space changes the mark state.
        /// Note: The cursor movement is handled by the UI controller (MainController.ToggleMarkAndMoveUp),
        /// so this test focuses on the mark toggle behavior of the MarkingEngine.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ShiftSpaceToggleMark_ChangesMarkState(List<FileEntryData> fileData, NonNegativeInt indexGen)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to mark");
            }

            // Convert to FileEntry objects
            var entries = fileData
                .Where(fd => fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                .Select(fd => new FileEntry
                {
                    Name = fd.Name,
                    Size = fd.Size,
                    LastModified = fd.LastModified,
                    IsDirectory = fd.IsDirectory,
                    Extension = fd.Extension ?? string.Empty,
                    FullPath = fd.Name,
                    Attributes = FileAttributes.Normal
                })
                .ToList();

            if (entries.Count == 0)
            {
                return true.ToProperty().Label("No valid entries to mark");
            }

            // Get a valid index
            int index = indexGen.Get % entries.Count;

            // Arrange
            var pane = new PaneState { Entries = entries };
            var engine = new MarkingEngine();

            // Get initial state
            bool wasMarked = pane.MarkedIndices.Contains(index);

            // Act: Toggle mark (simulating Shift+Space behavior)
            engine.ToggleMark(pane, index);

            // Assert: Mark state should be inverted
            bool isNowMarked = pane.MarkedIndices.Contains(index);
            bool stateChanged = wasMarked != isNowMarked;

            // Act: Toggle again
            engine.ToggleMark(pane, index);

            // Assert: Should return to original state
            bool isBackToOriginal = pane.MarkedIndices.Contains(index) == wasMarked;

            return (stateChanged && isBackToOriginal).ToProperty()
                .Label($"Index: {index}, Initial: {wasMarked}, After toggle: {isNowMarked}, After second toggle: {pane.MarkedIndices.Contains(index)}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 15: Home inverts all marks
        /// Validates: Requirements 3.4
        /// 
        /// This property verifies that pressing Home (or backtick) inverts all mark states.
        /// Previously marked files become unmarked, and previously unmarked files become marked.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property HomeInvertsAllMarks(List<FileEntryData> fileData)
        {
            // Filter out null or invalid entries
            if (fileData == null || fileData.Count == 0)
            {
                return true.ToProperty().Label("Empty list - no files to mark");
            }

            // Convert to FileEntry objects
            var entries = fileData
                .Where(fd => fd != null && !string.IsNullOrWhiteSpace(fd.Name))
                .Select(fd => new FileEntry
                {
                    Name = fd.Name,
                    Size = fd.Size,
                    LastModified = fd.LastModified,
                    IsDirectory = fd.IsDirectory,
                    Extension = fd.Extension ?? string.Empty,
                    FullPath = fd.Name,
                    Attributes = FileAttributes.Normal
                })
                .ToList();

            if (entries.Count == 0)
            {
                return true.ToProperty().Label("No valid entries to mark");
            }

            // Arrange
            var pane = new PaneState { Entries = entries };
            var engine = new MarkingEngine();

            // Mark some random entries (every other entry)
            for (int i = 0; i < entries.Count; i += 2)
            {
                pane.MarkedIndices.Add(i);
            }

            var originalMarked = new HashSet<int>(pane.MarkedIndices);

            // Act: Invert marks (simulating Home key behavior)
            engine.InvertMarks(pane);

            // Assert: Previously marked should be unmarked, previously unmarked should be marked
            bool correctInversion = true;
            for (int i = 0; i < entries.Count; i++)
            {
                bool wasMarked = originalMarked.Contains(i);
                bool isNowMarked = pane.MarkedIndices.Contains(i);

                if (wasMarked == isNowMarked)
                {
                    correctInversion = false;
                    break;
                }
            }

            // Total count should be complementary
            int expectedCount = entries.Count - originalMarked.Count;
            bool correctCount = pane.MarkedIndices.Count == expectedCount;

            return (correctInversion && correctCount).ToProperty()
                .Label($"Original marked: {originalMarked.Count}, After invert: {pane.MarkedIndices.Count}, Expected: {expectedCount}");
        }

        /// <summary>
        /// Helper method to sanitize pattern strings
        /// </summary>
        private static string SanitizePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return "*.txt";

            // Remove invalid characters but keep wildcards
            var invalid = Path.GetInvalidFileNameChars().Where(c => c != '*' && c != '?').ToArray();
            var sanitized = new string(pattern.Where(c => !invalid.Contains(c)).ToArray());

            if (string.IsNullOrWhiteSpace(sanitized))
                return "*.txt";

            // Ensure pattern has at least one wildcard
            if (!sanitized.Contains('*') && !sanitized.Contains('?'))
                sanitized += "*";

            return sanitized;
        }

        /// <summary>
        /// Helper method to sanitize filenames
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "file";

            return sanitized;
        }
    }
}
