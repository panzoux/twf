using System.Text.RegularExpressions;
using TWF.Models;

namespace TWF.Services
{
    /// <summary>
    /// Manages file marking and wildcard/regex pattern matching
    /// </summary>
    public class MarkingEngine
    {
        /// <summary>
        /// Toggles the mark state of an entry at the specified index
        /// </summary>
        public void ToggleMark(PaneState pane, int index)
        {
            if (index < 0 || index >= pane.Entries.Count)
                return;

            if (pane.MarkedIndices.Contains(index))
            {
                pane.MarkedIndices.Remove(index);
            }
            else
            {
                pane.MarkedIndices.Add(index);
            }
        }

        /// <summary>
        /// Marks all entries in a range from startIndex to endIndex (inclusive)
        /// </summary>
        public void MarkRange(PaneState pane, int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex < 0 || 
                startIndex >= pane.Entries.Count || endIndex >= pane.Entries.Count)
                return;

            // Ensure startIndex is less than or equal to endIndex
            int start = Math.Min(startIndex, endIndex);
            int end = Math.Max(startIndex, endIndex);

            for (int i = start; i <= end; i++)
            {
                pane.MarkedIndices.Add(i);
            }
        }

        /// <summary>
        /// Inverts the mark state of all entries in the pane
        /// </summary>
        public void InvertMarks(PaneState pane)
        {
            var newMarkedIndices = new HashSet<int>();

            for (int i = 0; i < pane.Entries.Count; i++)
            {
                if (!pane.MarkedIndices.Contains(i))
                {
                    newMarkedIndices.Add(i);
                }
            }

            pane.MarkedIndices = newMarkedIndices;
        }

        /// <summary>
        /// Clears all marks in the pane
        /// </summary>
        public void ClearMarks(PaneState pane)
        {
            pane.MarkedIndices.Clear();
        }

        /// <summary>
        /// Marks all files matching the wildcard pattern
        /// Supports multiple patterns separated by spaces
        /// Patterns starting with colon (:) are exclusion patterns
        /// </summary>
        public void MarkByWildcard(PaneState pane, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;

            // Split pattern by spaces to support multiple patterns
            var patterns = pattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var inclusionPatterns = new List<string>();
            var exclusionPatterns = new List<string>();

            foreach (var p in patterns)
            {
                if (p.StartsWith(":"))
                {
                    exclusionPatterns.Add(p.Substring(1));
                }
                else
                {
                    inclusionPatterns.Add(p);
                }
            }

            // Mark files matching inclusion patterns
            for (int i = 0; i < pane.Entries.Count; i++)
            {
                var entry = pane.Entries[i];
                bool shouldMark = false;

                // Check inclusion patterns
                if (inclusionPatterns.Count > 0)
                {
                    foreach (var incPattern in inclusionPatterns)
                    {
                        if (MatchesWildcard(entry.Name, incPattern))
                        {
                            shouldMark = true;
                            break;
                        }
                    }
                }

                // Check exclusion patterns
                if (shouldMark && exclusionPatterns.Count > 0)
                {
                    foreach (var excPattern in exclusionPatterns)
                    {
                        if (MatchesWildcard(entry.Name, excPattern))
                        {
                            shouldMark = false;
                            break;
                        }
                    }
                }

                if (shouldMark)
                {
                    pane.MarkedIndices.Add(i);
                }
            }
        }

        /// <summary>
        /// Marks all files matching the regex pattern
        /// Pattern should start with m/ to indicate regex mode
        /// </summary>
        public void MarkByRegex(PaneState pane, string regexPattern)
        {
            if (string.IsNullOrWhiteSpace(regexPattern))
                return;

            // Remove m/ prefix if present
            string pattern = regexPattern;
            if (pattern.StartsWith("m/"))
            {
                pattern = pattern.Substring(2);
            }

            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);

                for (int i = 0; i < pane.Entries.Count; i++)
                {
                    if (MatchesRegex(pane.Entries[i].Name, pattern))
                    {
                        pane.MarkedIndices.Add(i);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, do nothing
            }
        }

        /// <summary>
        /// Checks if a filename matches a wildcard pattern
        /// Supports * (any characters) and ? (single character)
        /// </summary>
        public bool MatchesWildcard(string filename, string pattern)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(pattern))
                return false;

            // Convert wildcard pattern to regex
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            try
            {
                return Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a filename matches a regex pattern
        /// </summary>
        public bool MatchesRegex(string filename, string regexPattern)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(regexPattern))
                return false;

            try
            {
                return Regex.IsMatch(filename, regexPattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
