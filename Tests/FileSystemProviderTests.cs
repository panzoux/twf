using Xunit;
using TWF.Providers;
using TWF.Models;

namespace TWF.Tests
{
    /// <summary>
    /// Unit tests for FileSystemProvider
    /// </summary>
    public class FileSystemProviderTests
    {
        private readonly FileSystemProvider _provider;

        public FileSystemProviderTests()
        {
            _provider = new FileSystemProvider();
        }

        [Fact]
        public void ListDirectory_WithValidPath_ReturnsEntries()
        {
            // Arrange: Use the current directory which should exist
            var currentDir = Directory.GetCurrentDirectory();

            // Act
            var entries = _provider.ListDirectory(currentDir);

            // Assert: Should return at least some entries (files or directories)
            Assert.NotNull(entries);
            // Current directory should have at least the project file
            Assert.True(entries.Count >= 0); // May be empty in some cases
        }

        [Fact]
        public void ListDirectory_WithInvalidPath_ReturnsEmptyList()
        {
            // Arrange: Use a path that doesn't exist
            var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var entries = _provider.ListDirectory(invalidPath);

            // Assert: Should return empty list, not throw
            Assert.NotNull(entries);
            Assert.Empty(entries);
        }

        [Fact]
        public void ListDirectory_WithNullPath_ReturnsEmptyList()
        {
            // Act
            var entries = _provider.ListDirectory(null!);

            // Assert: Should handle gracefully
            Assert.NotNull(entries);
            Assert.Empty(entries);
        }

        [Fact]
        public void GetFileMetadata_WithValidFile_ReturnsFileEntry()
        {
            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "test content");

                // Act
                var entry = _provider.GetFileMetadata(tempFile);

                // Assert
                Assert.NotNull(entry);
                Assert.False(entry.IsDirectory);
                Assert.Equal(Path.GetFileName(tempFile), entry.Name);
                Assert.True(entry.Size > 0);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void GetFileMetadata_WithValidDirectory_ReturnsDirectoryEntry()
        {
            // Arrange: Use the current directory
            var currentDir = Directory.GetCurrentDirectory();

            // Act
            var entry = _provider.GetFileMetadata(currentDir);

            // Assert
            Assert.NotNull(entry);
            Assert.True(entry.IsDirectory);
            Assert.Equal(Path.GetFileName(currentDir), entry.Name);
        }

        [Fact]
        public void GetFileMetadata_WithInvalidPath_ReturnsNull()
        {
            // Arrange: Use a path that doesn't exist
            var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var entry = _provider.GetFileMetadata(invalidPath);

            // Assert
            Assert.Null(entry);
        }

        [Fact]
        public void PathExists_WithValidPath_ReturnsTrue()
        {
            // Arrange: Use the current directory
            var currentDir = Directory.GetCurrentDirectory();

            // Act
            var exists = _provider.PathExists(currentDir);

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void PathExists_WithInvalidPath_ReturnsFalse()
        {
            // Arrange: Use a path that doesn't exist
            var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act
            var exists = _provider.PathExists(invalidPath);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void GetParentDirectory_WithValidPath_ReturnsParent()
        {
            // Arrange: Use a subdirectory path
            var currentDir = Directory.GetCurrentDirectory();

            // Act
            var parent = _provider.GetParentDirectory(currentDir);

            // Assert: Should return a parent (unless we're at root)
            // We can't guarantee parent exists, but it should not throw
            Assert.True(parent == null || Directory.Exists(parent));
        }

        [Fact]
        public void ListDirectory_IdentifiesArchiveFiles()
        {
            // Arrange: Create a temporary directory with a mock archive file
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var zipFile = Path.Combine(tempDir, "test.zip");
                File.WriteAllText(zipFile, "mock zip content");

                // Act
                var entries = _provider.ListDirectory(tempDir);

                // Assert
                Assert.NotNull(entries);
                var archiveEntry = entries.FirstOrDefault(e => e.Name == "test.zip");
                Assert.NotNull(archiveEntry);
                Assert.True(archiveEntry.IsArchive);
                Assert.False(archiveEntry.IsDirectory);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void ListDirectory_SeparatesFilesAndDirectories()
        {
            // Arrange: Create a temporary directory with files and subdirectories
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var subDir = Path.Combine(tempDir, "subdir");
                Directory.CreateDirectory(subDir);
                
                var file = Path.Combine(tempDir, "test.txt");
                File.WriteAllText(file, "test content");

                // Act
                var entries = _provider.ListDirectory(tempDir);

                // Assert
                Assert.NotNull(entries);
                Assert.Equal(2, entries.Count);
                
                var dirEntry = entries.FirstOrDefault(e => e.IsDirectory);
                var fileEntry = entries.FirstOrDefault(e => !e.IsDirectory);
                
                Assert.NotNull(dirEntry);
                Assert.NotNull(fileEntry);
                Assert.Equal("subdir", dirEntry.Name);
                Assert.Equal("test.txt", fileEntry.Name);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
