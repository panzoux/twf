using Xunit;
using TWF.Models;
using TWF.Providers;

namespace TWF.Tests
{
    /// <summary>
    /// Unit tests for ListProvider class
    /// </summary>
    public class ListProviderTests
    {
        private readonly ConfigurationProvider _configProvider;
        private readonly ListProvider _listProvider;

        public ListProviderTests()
        {
            _configProvider = new ConfigurationProvider();
            _listProvider = new ListProvider(_configProvider);
        }

        [Fact]
        public void GetDriveList_ReturnsNonEmptyList()
        {
            // Act
            var drives = _listProvider.GetDriveList();

            // Assert
            Assert.NotNull(drives);
            Assert.NotEmpty(drives);
            Assert.All(drives, drive =>
            {
                Assert.NotNull(drive.DriveLetter);
                Assert.NotEmpty(drive.DriveLetter);
            });
        }

        [Fact]
        public void GetJumpList_ReturnsRegisteredFolders()
        {
            // Act
            var jumpList = _listProvider.GetJumpList();

            // Assert
            Assert.NotNull(jumpList);
            // List may be empty if no registered folders are configured
        }

        [Fact]
        public void AddToHistory_AddsItemToDirectoryHistory()
        {
            // Arrange
            var path = @"C:\Test\Path";

            // Act
            _listProvider.AddToHistory(HistoryType.DirectoryHistory, path);
            var history = _listProvider.GetHistoryList(HistoryType.DirectoryHistory);

            // Assert
            Assert.Contains(path, history);
            Assert.Equal(path, history[0]); // Should be at the beginning
        }

        [Fact]
        public void AddToHistory_RemovesDuplicates()
        {
            // Arrange
            var path = @"C:\Test\Path";

            // Act
            _listProvider.AddToHistory(HistoryType.DirectoryHistory, path);
            _listProvider.AddToHistory(HistoryType.DirectoryHistory, path);
            var history = _listProvider.GetHistoryList(HistoryType.DirectoryHistory);

            // Assert
            int count = 0;
            foreach (var h in history)
            {
                if (h == path) count++;
            }
            Assert.Equal(1, count);
        }

        [Fact]
        public void AddToHistory_MaintainsMaxSize()
        {
            // Arrange
            var maxItems = 50;

            // Act
            for (int i = 0; i < maxItems + 10; i++)
            {
                _listProvider.AddToHistory(HistoryType.SearchHistory, $"search{i}");
            }
            var history = _listProvider.GetHistoryList(HistoryType.SearchHistory);

            // Assert
            Assert.Equal(maxItems, history.Count);
        }

        [Fact]
        public void ClearHistory_RemovesAllItems()
        {
            // Arrange
            _listProvider.AddToHistory(HistoryType.CommandHistory, "command1");
            _listProvider.AddToHistory(HistoryType.CommandHistory, "command2");

            // Act
            _listProvider.ClearHistory(HistoryType.CommandHistory);
            var history = _listProvider.GetHistoryList(HistoryType.CommandHistory);

            // Assert
            Assert.Empty(history);
        }

        [Fact]
        public void GetContextMenu_WithNullEntry_ReturnsBasicMenu()
        {
            // Act
            var menu = _listProvider.GetContextMenu(null, false);

            // Assert
            Assert.NotNull(menu);
            Assert.NotEmpty(menu);
            Assert.Contains(menu, m => m.Action == "Refresh");
            Assert.Contains(menu, m => m.Action == "CreateDirectory");
        }

        [Fact]
        public void GetContextMenu_WithDirectory_ReturnsDirectoryMenu()
        {
            // Arrange
            var entry = new FileEntry
            {
                Name = "TestDir",
                IsDirectory = true,
                IsArchive = false
            };

            // Act
            var menu = _listProvider.GetContextMenu(entry, false);

            // Assert
            Assert.NotNull(menu);
            Assert.NotEmpty(menu);
            Assert.Contains(menu, m => m.Action == "Navigate");
        }

        [Fact]
        public void GetContextMenu_WithFile_ReturnsFileMenu()
        {
            // Arrange
            var entry = new FileEntry
            {
                Name = "test.txt",
                Extension = ".txt",
                IsDirectory = false,
                IsArchive = false
            };

            // Act
            var menu = _listProvider.GetContextMenu(entry, false);

            // Assert
            Assert.NotNull(menu);
            Assert.NotEmpty(menu);
            Assert.Contains(menu, m => m.Action == "Execute");
            Assert.Contains(menu, m => m.Action == "Copy");
            Assert.Contains(menu, m => m.Action == "Move");
            Assert.Contains(menu, m => m.Action == "Delete");
        }

        [Fact]
        public void GetContextMenu_WithMarkedFiles_ReturnsMarkedFileMenu()
        {
            // Arrange
            var entry = new FileEntry
            {
                Name = "test.txt",
                Extension = ".txt",
                IsDirectory = false,
                IsArchive = false
            };

            // Act
            var menu = _listProvider.GetContextMenu(entry, hasMarkedFiles: true);

            // Assert
            Assert.NotNull(menu);
            Assert.Contains(menu, m => m.Label.Contains("Marked"));
        }

        [Fact]
        public void GetContextMenu_WithArchive_ReturnsArchiveMenu()
        {
            // Arrange
            var entry = new FileEntry
            {
                Name = "test.zip",
                Extension = ".zip",
                IsDirectory = false,
                IsArchive = true
            };

            // Act
            var menu = _listProvider.GetContextMenu(entry, false);

            // Assert
            Assert.NotNull(menu);
            Assert.Contains(menu, m => m.Action == "BrowseArchive");
            Assert.Contains(menu, m => m.Action == "ExtractArchive");
        }

        [Fact]
        public void GetContextMenu_WithTextFile_ReturnsViewTextOption()
        {
            // Arrange
            var entry = new FileEntry
            {
                Name = "test.txt",
                Extension = ".txt",
                IsDirectory = false,
                IsArchive = false
            };

            // Act
            var menu = _listProvider.GetContextMenu(entry, false);

            // Assert
            Assert.Contains(menu, m => m.Action == "ViewText");
        }

        [Fact]
        public void GetContextMenu_WithImageFile_ReturnsViewImageOption()
        {
            // Arrange
            var entry = new FileEntry
            {
                Name = "test.png",
                Extension = ".png",
                IsDirectory = false,
                IsArchive = false
            };

            // Act
            var menu = _listProvider.GetContextMenu(entry, false);

            // Assert
            Assert.Contains(menu, m => m.Action == "ViewImage");
        }

        [Fact]
        public void GetHistoryList_ReturnsIndependentCopy()
        {
            // Arrange
            _listProvider.AddToHistory(HistoryType.DirectoryHistory, "path1");

            // Act
            var history1 = _listProvider.GetHistoryList(HistoryType.DirectoryHistory);
            history1.Add("path2"); // Modify the returned list
            var history2 = _listProvider.GetHistoryList(HistoryType.DirectoryHistory);

            // Assert
            Assert.Single(history2); // Original should not be modified
        }
    }
}
