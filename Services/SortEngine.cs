using TWF.Models;

namespace TWF.Services
{
    /// <summary>
    /// Handles sorting of file lists according to various criteria
    /// </summary>
    public class SortEngine
    {
        /// <summary>
        /// Sorts a list of file entries according to the specified sort mode
        /// </summary>
        /// <param name="entries">The list of file entries to sort</param>
        /// <param name="mode">The sort mode to apply</param>
        /// <returns>A new sorted list of file entries</returns>
        public static List<FileEntry> Sort(List<FileEntry> entries, SortMode mode)
        {
            if (entries == null || entries.Count == 0)
            {
                return new List<FileEntry>();
            }

            // Create a copy to avoid modifying the original list
            var sorted = new List<FileEntry>(entries);

            switch (mode)
            {
                case SortMode.Unsorted:
                    // Return as-is
                    break;

                case SortMode.NameAscending:
                    sorted.Sort((x, y) => CompareByName(x, y, true));
                    break;

                case SortMode.NameDescending:
                    sorted.Sort((x, y) => CompareByName(x, y, false));
                    break;

                case SortMode.ExtensionAscending:
                    sorted.Sort((x, y) => CompareByExtension(x, y, true));
                    break;

                case SortMode.ExtensionDescending:
                    sorted.Sort((x, y) => CompareByExtension(x, y, false));
                    break;

                case SortMode.SizeAscending:
                    sorted.Sort((x, y) => CompareBySize(x, y, true));
                    break;

                case SortMode.SizeDescending:
                    sorted.Sort((x, y) => CompareBySize(x, y, false));
                    break;

                case SortMode.DateAscending:
                    sorted.Sort((x, y) => CompareByDate(x, y, true));
                    break;

                case SortMode.DateDescending:
                    sorted.Sort((x, y) => CompareByDate(x, y, false));
                    break;

                default:
                    break;
            }

            return sorted;
        }

        private static int CompareByName(FileEntry x, FileEntry y, bool ascending)
        {
            // Directories always first
            if (x.IsDirectory != y.IsDirectory)
                return y.IsDirectory.CompareTo(x.IsDirectory); // true > false, so directory comes first

            return ascending 
                ? string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                : string.Compare(y.Name, x.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareByExtension(FileEntry x, FileEntry y, bool ascending)
        {
             // Directories always first
            if (x.IsDirectory != y.IsDirectory)
                return y.IsDirectory.CompareTo(x.IsDirectory);

            int result = ascending
                ? string.Compare(x.Extension, y.Extension, StringComparison.OrdinalIgnoreCase)
                : string.Compare(y.Extension, x.Extension, StringComparison.OrdinalIgnoreCase);

            if (result == 0)
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            
            return result;
        }

        private static int CompareBySize(FileEntry x, FileEntry y, bool ascending)
        {
             // Directories always first
            if (x.IsDirectory != y.IsDirectory)
                return y.IsDirectory.CompareTo(x.IsDirectory);

            int result = ascending
                ? x.Size.CompareTo(y.Size)
                : y.Size.CompareTo(x.Size);

            if (result == 0)
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);

            return result;
        }

        private static int CompareByDate(FileEntry x, FileEntry y, bool ascending)
        {
             // Directories always first
            if (x.IsDirectory != y.IsDirectory)
                return y.IsDirectory.CompareTo(x.IsDirectory);

            int result = ascending
                ? x.LastModified.CompareTo(y.LastModified)
                : y.LastModified.CompareTo(x.LastModified);

            if (result == 0)
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);

            return result;
        }
    }
}
