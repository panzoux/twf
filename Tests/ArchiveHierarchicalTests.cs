using Xunit;
using TWF.Services;
using TWF.Models;
using TWF.Infrastructure;
using System.IO.Compression;

namespace TWF.Tests
{
    public class ArchiveHierarchicalTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly ArchiveManager _archiveManager;

        public ArchiveHierarchicalTests()
        {
            LoggingConfiguration.Initialize();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"twf_hierarchical_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            _archiveManager = new ArchiveManager();
            _archiveManager.RegisterProvider(new ZipArchiveProvider());
        }

        [Fact]
        public void ListArchiveContents_Root_ShowsTopLevelDirsAndFiles()
        {
            // Arrange
            var zipPath = Path.Combine(_testDirectory, "hierarchical.zip");
            CreateTestZip(zipPath);

            // Act
            // List root (internalPath = "")
            var entries = _archiveManager.ListArchiveContents(zipPath, "");

            // Assert
            // Structure:
            // file1.txt
            // folder1/file2.txt
            // folder1/subfolder/file3.txt
            // folder2/file4.txt

            // Expected at root:
            // file1.txt (File)
            // folder1 (Dir)
            // folder2 (Dir)

            Assert.Equal(3, entries.Count);
            Assert.Contains(entries, e => e.Name == "file1.txt" && !e.IsDirectory);
            Assert.Contains(entries, e => e.Name == "folder1" && e.IsDirectory && e.IsVirtualFolder);
            Assert.Contains(entries, e => e.Name == "folder2" && e.IsDirectory && e.IsVirtualFolder);
        }

        [Fact]
        public void ListArchiveContents_SubDir_ShowsContent()
        {
            // Arrange
            var zipPath = Path.Combine(_testDirectory, "hierarchical.zip");
            CreateTestZip(zipPath);

            // Act
            // List folder1 (internalPath = "folder1")
            var entries = _archiveManager.ListArchiveContents(zipPath, "folder1");

            // Assert
            // Expected in folder1:
            // file2.txt (File)
            // subfolder (Dir)

            Assert.Equal(2, entries.Count);
            Assert.Contains(entries, e => e.Name == "file2.txt" && !e.IsDirectory);
            Assert.Contains(entries, e => e.Name == "subfolder" && e.IsDirectory && e.IsVirtualFolder);
        }

        [Fact]
        public void ListArchiveContents_DeepSubDir_ShowsContent()
        {
            // Arrange
            var zipPath = Path.Combine(_testDirectory, "hierarchical.zip");
            CreateTestZip(zipPath);

            // Act
            // List folder1/subfolder
            var entries = _archiveManager.ListArchiveContents(zipPath, "folder1/subfolder");

            // Assert
            // Expected in folder1/subfolder:
            // file3.txt

            Assert.Single(entries);
            Assert.Contains(entries, e => e.Name == "file3.txt" && !e.IsDirectory);
        }

        private void CreateTestZip(string zipPath)
        {
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Create entries with hierarchical names
                CreateEntry(archive, "file1.txt", "content1");
                CreateEntry(archive, "folder1/file2.txt", "content2");
                CreateEntry(archive, "folder1/subfolder/file3.txt", "content3");
                CreateEntry(archive, "folder2/file4.txt", "content4");
            }
        }

        private void CreateEntry(ZipArchive archive, string entryName, string content)
        {
            var entry = archive.CreateEntry(entryName);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try { Directory.Delete(_testDirectory, true); } catch { }
            }
        }
    }
}
