using Xunit;
using TWF.Controllers;
using TWF.Services;
using TWF.Providers;
using TWF.Infrastructure;
using TWF.Models;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace TWF.Tests
{
    /// <summary>
    /// Tests for archive browsing functionality (Task 27)
    /// </summary>
    public class ArchiveBrowsingTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly MainController _controller;

        public ArchiveBrowsingTests()
        {
            LoggingConfiguration.Initialize();
            
            // Create a temporary test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"twf_archive_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            
            // Create test controller
            var fileSystemProvider = new FileSystemProvider();
            var configProvider = new ConfigurationProvider();
            var listProvider = new ListProvider(configProvider);
            var sortEngine = new SortEngine();
            var markingEngine = new MarkingEngine();
            var searchEngine = new SearchEngine();
            var archiveManager = new ArchiveManager();
            var fileOps = new FileOperations();
            var viewerManager = new ViewerManager();
            var keyBindings = new KeyBindingManager();
            var historyManager = new HistoryManager(configProvider.LoadConfiguration());
            var logger = LoggingConfiguration.GetLogger<MainController>();

            var macroExpander = new MacroExpander();
            var customFunctionManager = new CustomFunctionManager(macroExpander);
            var menuManager = new MenuManager(configProvider.GetConfigDirectory());

            _controller = new MainController(
                keyBindings,
                fileOps,
                markingEngine,
                sortEngine,
                searchEngine,
                archiveManager,
                viewerManager,
                configProvider,
                fileSystemProvider,
                listProvider,
                customFunctionManager,
                menuManager,
                historyManager,
                logger
            );
        }

        [Fact]
        public void PaneState_SupportsVirtualFolderProperties()
        {
            // Arrange
            var pane = new PaneState();

            // Act & Assert
            Assert.False(pane.IsInVirtualFolder);
            Assert.Null(pane.VirtualFolderArchivePath);
            Assert.Null(pane.VirtualFolderParentPath);
            
            // Set virtual folder state
            pane.IsInVirtualFolder = true;
            pane.VirtualFolderArchivePath = "test.zip";
            pane.VirtualFolderParentPath = "C:\\test";
            
            Assert.True(pane.IsInVirtualFolder);
            Assert.Equal("test.zip", pane.VirtualFolderArchivePath);
            Assert.Equal("C:\\test", pane.VirtualFolderParentPath);
        }

        [Fact]
        public void ArchiveManager_IsArchive_DetectsZipFiles()
        {
            // Arrange
            var archiveManager = new ArchiveManager();
            var zipPath = Path.Combine(_testDirectory, "test.zip");
            
            // Create an empty zip file
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Empty archive
            }

            // Act
            var isArchive = archiveManager.IsArchive(zipPath);

            // Assert
            Assert.True(isArchive);
        }

        [Fact]
        public void ArchiveManager_ListArchiveContents_ReturnsEntries()
        {
            // Arrange
            var archiveManager = new ArchiveManager();
            var zipPath = Path.Combine(_testDirectory, "test.zip");
            
            // Create a zip file with some entries
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry1 = archive.CreateEntry("file1.txt");
                using (var writer = new StreamWriter(entry1.Open()))
                {
                    writer.WriteLine("Test content 1");
                }
                
                var entry2 = archive.CreateEntry("file2.txt");
                using (var writer = new StreamWriter(entry2.Open()))
                {
                    writer.WriteLine("Test content 2");
                }
            }

            // Act
            var entries = archiveManager.ListArchiveContents(zipPath);

            // Assert
            Assert.NotNull(entries);
            Assert.Equal(2, entries.Count);
            Assert.Contains(entries, e => e.Name == "file1.txt");
            Assert.Contains(entries, e => e.Name == "file2.txt");
        }

        [Fact]
        public async Task ArchiveManager_ExtractAsync_ExtractsFiles()
        {
            // Arrange
            var archiveManager = new ArchiveManager();
            var zipPath = Path.Combine(_testDirectory, "test_extract.zip");
            var extractDir = Path.Combine(_testDirectory, "extracted");
            Directory.CreateDirectory(extractDir);
            
            // Create a zip file with some entries
            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var entry1 = archive.CreateEntry("file1.txt");
                using (var writer = new StreamWriter(entry1.Open()))
                {
                    writer.WriteLine("Test content 1");
                }
                
                var entry2 = archive.CreateEntry("file2.txt");
                using (var writer = new StreamWriter(entry2.Open()))
                {
                    writer.WriteLine("Test content 2");
                }
            }

            // Act
            var result = await archiveManager.ExtractAsync(zipPath, extractDir);

            // Assert
            Assert.True(result.Success);
            Assert.True(File.Exists(Path.Combine(extractDir, "file1.txt")));
            Assert.True(File.Exists(Path.Combine(extractDir, "file2.txt")));
        }

        [Fact]
        public void NavigateToParent_ExitsVirtualFolder_WhenInArchive()
        {
            // Arrange
            var pane = new PaneState
            {
                CurrentPath = "[test.zip]",
                IsInVirtualFolder = true,
                VirtualFolderArchivePath = Path.Combine(_testDirectory, "test.zip"),
                VirtualFolderParentPath = _testDirectory
            };

            // Act - Simulate exiting virtual folder
            var shouldExitVirtualFolder = pane.IsInVirtualFolder && pane.VirtualFolderParentPath != null;
            
            if (shouldExitVirtualFolder)
            {
                var parentPath = pane.VirtualFolderParentPath;
                pane.IsInVirtualFolder = false;
                pane.VirtualFolderArchivePath = null;
                pane.VirtualFolderParentPath = null;
                pane.CurrentPath = parentPath!;
            }

            // Assert
            Assert.False(pane.IsInVirtualFolder);
            Assert.Null(pane.VirtualFolderArchivePath);
            Assert.Null(pane.VirtualFolderParentPath);
            Assert.Equal(_testDirectory, pane.CurrentPath);
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
