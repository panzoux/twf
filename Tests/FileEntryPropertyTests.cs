using FsCheck;
using FsCheck.Xunit;
using TWF.Models;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for FileEntry model
    /// </summary>
    public class FileEntryPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 9: Detail view shows required fields
        /// Validates: Requirements 2.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property DetailView_ShowsRequiredFields(NonEmptyString name, PositiveInt size, DateTime lastModified)
        {
            // Sanitize name to avoid truncation issues (max 20 chars)
            string sanitizedName = name.Get;
            if (sanitizedName.Length > 20) sanitizedName = sanitizedName.Substring(0, 20);
            
            // Arrange: Create a file entry (not a directory)
            var fileEntry = new FileEntry
            {
                Name = sanitizedName,
                Size = size.Get,
                LastModified = lastModified,
                IsDirectory = false,
                Attributes = FileAttributes.Normal
            };

            // Act: Format for detail view
            var formatted = fileEntry.FormatForDisplay(DisplayMode.Details);

            // Assert: The formatted string should contain the file name, size, and timestamp
            var containsName = formatted.Contains(fileEntry.Name);
            var containsDate = formatted.Contains(lastModified.ToString("yyyy-MM-dd"));
            
            // For files, the size should be present (not <DIR>)
            var containsSize = !formatted.Contains("<DIR>");

            return (containsName && containsDate && containsSize).ToProperty()
                .Label($"Expected detail view to contain name, date, and size. Got: {formatted}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 10: Directories show <DIR> in size column
        /// Validates: Requirements 2.3
        /// </summary>
        [Property(MaxTest = 100)]
        public Property DirectoryDetailView_ShowsDirInSizeColumn(NonEmptyString name, DateTime lastModified)
        {
            // Sanitize name to avoid truncation issues (max 20 chars)
            string sanitizedName = name.Get;
            if (sanitizedName.Length > 20) sanitizedName = sanitizedName.Substring(0, 20);

            // Arrange: Create a directory entry
            var directoryEntry = new FileEntry
            {
                Name = sanitizedName,
                Size = 0, // Size is irrelevant for directories
                LastModified = lastModified,
                IsDirectory = true,
                Attributes = FileAttributes.Directory
            };

            // Act: Format for detail view
            var formatted = directoryEntry.FormatForDisplay(DisplayMode.Details);

            // Assert: The formatted string should contain <DIR> in the size column
            var containsDir = formatted.Contains("<DIR>");
            var containsName = formatted.Contains(directoryEntry.Name);

            return (containsDir && containsName).ToProperty()
                .Label($"Expected directory detail view to contain <DIR>. Got: {formatted}");
        }
    }
}
