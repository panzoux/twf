using TWF.Models;

namespace TWF.Services
{
    /// <summary>
    /// Handles incremental search with optional Migemo support for Japanese text
    /// </summary>
    public class SearchEngine
    {
        private readonly IMigemoProvider? _migemoProvider;

        public SearchEngine(IMigemoProvider? migemoProvider = null)
        {
            _migemoProvider = migemoProvider;
        }

        /// <summary>
        /// Finds all entries matching the search pattern
        /// </summary>
        /// <param name="entries">List of file entries to search</param>
        /// <param name="searchPattern">Pattern to search for</param>
        /// <param name="useMigemo">Whether to use Migemo expansion if available</param>
        /// <returns>List of indices of matching entries</returns>
        public List<int> FindMatches(List<FileEntry> entries, string searchPattern, bool useMigemo)
        {
            if (entries == null || entries.Count == 0 || string.IsNullOrWhiteSpace(searchPattern))
            {
                return new List<int>();
            }

            var matches = new List<int>();
            string effectivePattern = searchPattern;

            // Use Migemo expansion if requested and available
            if (useMigemo && _migemoProvider?.IsAvailable == true)
            {
                effectivePattern = _migemoProvider.ExpandPattern(searchPattern);
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (MatchesPattern(entries[i].Name, effectivePattern, useMigemo))
                {
                    matches.Add(i);
                }
            }

            return matches;
        }

        /// <summary>
        /// Finds the next entry matching the search pattern starting from the current index
        /// </summary>
        /// <param name="entries">List of file entries to search</param>
        /// <param name="searchPattern">Pattern to search for</param>
        /// <param name="currentIndex">Current cursor position</param>
        /// <param name="useMigemo">Whether to use Migemo expansion if available</param>
        /// <returns>Index of next match, or -1 if no match found</returns>
        public int FindNext(List<FileEntry> entries, string searchPattern, int currentIndex, bool useMigemo)
        {
            if (entries == null || entries.Count == 0 || string.IsNullOrWhiteSpace(searchPattern))
            {
                return -1;
            }

            string effectivePattern = searchPattern;

            // Use Migemo expansion if requested and available
            if (useMigemo && _migemoProvider?.IsAvailable == true)
            {
                effectivePattern = _migemoProvider.ExpandPattern(searchPattern);
            }

            // Search from current index + 1 to end
            for (int i = currentIndex + 1; i < entries.Count; i++)
            {
                if (MatchesPattern(entries[i].Name, effectivePattern, useMigemo))
                {
                    return i;
                }
            }

            // Wrap around: search from beginning to current index
            for (int i = 0; i <= currentIndex; i++)
            {
                if (MatchesPattern(entries[i].Name, effectivePattern, useMigemo))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds the previous entry matching the search pattern starting from the current index
        /// </summary>
        /// <param name="entries">List of file entries to search</param>
        /// <param name="searchPattern">Pattern to search for</param>
        /// <param name="currentIndex">Current cursor position</param>
        /// <param name="useMigemo">Whether to use Migemo expansion if available</param>
        /// <returns>Index of previous match, or -1 if no match found</returns>
        public int FindPrevious(List<FileEntry> entries, string searchPattern, int currentIndex, bool useMigemo)
        {
            if (entries == null || entries.Count == 0 || string.IsNullOrWhiteSpace(searchPattern))
            {
                return -1;
            }

            string effectivePattern = searchPattern;

            // Use Migemo expansion if requested and available
            if (useMigemo && _migemoProvider?.IsAvailable == true)
            {
                effectivePattern = _migemoProvider.ExpandPattern(searchPattern);
            }

            // Search backwards from current index - 1 to beginning
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                if (MatchesPattern(entries[i].Name, effectivePattern, useMigemo))
                {
                    return i;
                }
            }

            // Wrap around: search from end to current index
            for (int i = entries.Count - 1; i >= currentIndex; i--)
            {
                if (MatchesPattern(entries[i].Name, effectivePattern, useMigemo))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Checks if a filename matches the search pattern
        /// </summary>
        private bool MatchesPattern(string filename, string pattern, bool isMigemoPattern)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            // If it's a Migemo pattern, it's already a regex pattern
            if (isMigemoPattern && _migemoProvider?.IsAvailable == true)
            {
                try
                {
                    return System.Text.RegularExpressions.Regex.IsMatch(
                        filename,
                        pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                catch
                {
                    // If regex fails, fall back to simple matching
                    return filename.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Standard incremental search: check if filename starts with pattern
            return filename.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Interface for Migemo provider that converts romaji to Japanese text patterns
    /// </summary>
    public interface IMigemoProvider
    {
        /// <summary>
        /// Gets whether Migemo is available (DLL and dictionary files exist)
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Expands a romaji pattern to a regex pattern matching hiragana/katakana/kanji
        /// </summary>
        /// <param name="romajiPattern">Romaji input pattern</param>
        /// <returns>Regex pattern that matches Japanese text equivalents</returns>
        string ExpandPattern(string romajiPattern);
    }

    /// <summary>
    /// Migemo provider implementation that loads migemo.dll for Japanese text search
    /// </summary>
    public class MigemoProvider : IMigemoProvider
    {
        private const string MigemoDllName = "migemo.dll";
        private const string MigemoDictPath = "dict";
        private bool? _isAvailable;

        public bool IsAvailable
        {
            get
            {
                if (_isAvailable.HasValue)
                {
                    return _isAvailable.Value;
                }

                _isAvailable = CheckMigemoDllExists();
                return _isAvailable.Value;
            }
        }

        /// <summary>
        /// Expands a romaji pattern to a regex pattern matching Japanese text
        /// </summary>
        /// <param name="romajiPattern">Romaji input (e.g., "nihon")</param>
        /// <returns>Regex pattern (e.g., "(nihon|にほん|ニホン|日本)")</returns>
        public string ExpandPattern(string romajiPattern)
        {
            if (!IsAvailable || string.IsNullOrWhiteSpace(romajiPattern))
            {
                return romajiPattern;
            }

            // TODO: Actual Migemo DLL integration would go here
            // For now, return the original pattern as a fallback
            // Real implementation would use P/Invoke to call migemo.dll functions
            // Example: migemo_query(migemo, romajiPattern)
            
            // This is a placeholder that would be replaced with actual DLL calls:
            // 1. Load migemo.dll
            // 2. Initialize with dictionary path
            // 3. Call migemo_query to expand the pattern
            // 4. Return the expanded regex pattern

            return romajiPattern;
        }

        /// <summary>
        /// Checks if migemo.dll and dictionary files exist
        /// </summary>
        private bool CheckMigemoDllExists()
        {
            try
            {
                // Check if migemo.dll exists in the application directory
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string dllPath = Path.Combine(appDir, MigemoDllName);
                
                if (!File.Exists(dllPath))
                {
                    return false;
                }

                // Check if dictionary directory exists
                string dictPath = Path.Combine(appDir, MigemoDictPath);
                if (!Directory.Exists(dictPath))
                {
                    return false;
                }

                // Check for at least one dictionary file
                var dictFiles = Directory.GetFiles(dictPath, "*.dat");
                return dictFiles.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
