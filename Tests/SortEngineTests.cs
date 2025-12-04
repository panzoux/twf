using Xunit;
using TWF.Models;
using TWF.Services;

namespace TWF.Tests
{
    /// <summary>
    /// Unit tests for SortEngine
    /// </summary>
    public class SortEngineTests
    {
        [Fact]
        public void Sort_ByNameAscending_DirectoriesBeforeFiles()
        {
            // Arrange
            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "file1.txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now },
                new FileEntry { Name = "dirB", IsDirectory = true, Size = 0, LastModified = DateTime.Now },
                new FileEntry { Name = "file2.txt", IsDirectory = false, Size = 200, LastModified = DateTime.Now },
                new FileEntry { Name = "dirA", IsDirectory = true, Size = 0, LastModified = DateTime.Now }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.NameAscending);

            // Assert
            Assert.Equal(4, sorted.Count);
            Assert.True(sorted[0].IsDirectory);
            Assert.Equal("dirA", sorted[0].Name);
            Assert.True(sorted[1].IsDirectory);
            Assert.Equal("dirB", sorted[1].Name);
            Assert.False(sorted[2].IsDirectory);
            Assert.Equal("file1.txt", sorted[2].Name);
            Assert.False(sorted[3].IsDirectory);
            Assert.Equal("file2.txt", sorted[3].Name);
        }

        [Fact]
        public void Sort_ByNameDescending_AlphabeticallyReversed()
        {
            // Arrange
            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "aaa.txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now },
                new FileEntry { Name = "zzz.txt", IsDirectory = false, Size = 200, LastModified = DateTime.Now },
                new FileEntry { Name = "mmm.txt", IsDirectory = false, Size = 150, LastModified = DateTime.Now }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.NameDescending);

            // Assert
            Assert.Equal("zzz.txt", sorted[0].Name);
            Assert.Equal("mmm.txt", sorted[1].Name);
            Assert.Equal("aaa.txt", sorted[2].Name);
        }

        [Fact]
        public void Sort_ByExtensionAscending_GroupsByExtension()
        {
            // Arrange
            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "file1.txt", Extension = ".txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now },
                new FileEntry { Name = "file2.doc", Extension = ".doc", IsDirectory = false, Size = 200, LastModified = DateTime.Now },
                new FileEntry { Name = "file3.txt", Extension = ".txt", IsDirectory = false, Size = 150, LastModified = DateTime.Now }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.ExtensionAscending);

            // Assert
            Assert.Equal(".doc", sorted[0].Extension);
            Assert.Equal(".txt", sorted[1].Extension);
            Assert.Equal(".txt", sorted[2].Extension);
        }

        [Fact]
        public void Sort_BySizeAscending_OrdersBySize()
        {
            // Arrange
            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "large.txt", IsDirectory = false, Size = 1000, LastModified = DateTime.Now },
                new FileEntry { Name = "small.txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now },
                new FileEntry { Name = "medium.txt", IsDirectory = false, Size = 500, LastModified = DateTime.Now }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.SizeAscending);

            // Assert
            Assert.Equal(100, sorted[0].Size);
            Assert.Equal(500, sorted[1].Size);
            Assert.Equal(1000, sorted[2].Size);
        }

        [Fact]
        public void Sort_BySizeDescending_OrdersBySizeReversed()
        {
            // Arrange
            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "large.txt", IsDirectory = false, Size = 1000, LastModified = DateTime.Now },
                new FileEntry { Name = "small.txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now },
                new FileEntry { Name = "medium.txt", IsDirectory = false, Size = 500, LastModified = DateTime.Now }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.SizeDescending);

            // Assert
            Assert.Equal(1000, sorted[0].Size);
            Assert.Equal(500, sorted[1].Size);
            Assert.Equal(100, sorted[2].Size);
        }

        [Fact]
        public void Sort_ByDateAscending_OrdersByDate()
        {
            // Arrange
            var date1 = new DateTime(2020, 1, 1);
            var date2 = new DateTime(2021, 1, 1);
            var date3 = new DateTime(2022, 1, 1);

            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "newest.txt", IsDirectory = false, Size = 100, LastModified = date3 },
                new FileEntry { Name = "oldest.txt", IsDirectory = false, Size = 100, LastModified = date1 },
                new FileEntry { Name = "middle.txt", IsDirectory = false, Size = 100, LastModified = date2 }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.DateAscending);

            // Assert
            Assert.Equal(date1, sorted[0].LastModified);
            Assert.Equal(date2, sorted[1].LastModified);
            Assert.Equal(date3, sorted[2].LastModified);
        }

        [Fact]
        public void Sort_ByDateDescending_OrdersByDateReversed()
        {
            // Arrange
            var date1 = new DateTime(2020, 1, 1);
            var date2 = new DateTime(2021, 1, 1);
            var date3 = new DateTime(2022, 1, 1);

            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "newest.txt", IsDirectory = false, Size = 100, LastModified = date3 },
                new FileEntry { Name = "oldest.txt", IsDirectory = false, Size = 100, LastModified = date1 },
                new FileEntry { Name = "middle.txt", IsDirectory = false, Size = 100, LastModified = date2 }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.DateDescending);

            // Assert
            Assert.Equal(date3, sorted[0].LastModified);
            Assert.Equal(date2, sorted[1].LastModified);
            Assert.Equal(date1, sorted[2].LastModified);
        }

        [Fact]
        public void Sort_Unsorted_ReturnsOriginalOrder()
        {
            // Arrange
            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "zzz.txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now },
                new FileEntry { Name = "aaa.txt", IsDirectory = false, Size = 200, LastModified = DateTime.Now },
                new FileEntry { Name = "mmm.txt", IsDirectory = false, Size = 150, LastModified = DateTime.Now }
            };

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.Unsorted);

            // Assert
            Assert.Equal("zzz.txt", sorted[0].Name);
            Assert.Equal("aaa.txt", sorted[1].Name);
            Assert.Equal("mmm.txt", sorted[2].Name);
        }

        [Fact]
        public void Sort_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var entries = new List<FileEntry>();

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.NameAscending);

            // Assert
            Assert.Empty(sorted);
        }

        [Fact]
        public void Sort_NullList_ReturnsEmptyList()
        {
            // Act
            var sorted = SortEngine.Sort(null!, SortMode.NameAscending);

            // Assert
            Assert.NotNull(sorted);
            Assert.Empty(sorted);
        }

        [Fact]
        public void Sort_DoesNotModifyOriginalList()
        {
            // Arrange
            var entries = new List<FileEntry>
            {
                new FileEntry { Name = "zzz.txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now },
                new FileEntry { Name = "aaa.txt", IsDirectory = false, Size = 200, LastModified = DateTime.Now }
            };
            var originalFirstName = entries[0].Name;

            // Act
            var sorted = SortEngine.Sort(entries, SortMode.NameAscending);

            // Assert
            Assert.Equal(originalFirstName, entries[0].Name); // Original list unchanged
            Assert.NotEqual(originalFirstName, sorted[0].Name); // Sorted list is different
        }
    }
}
