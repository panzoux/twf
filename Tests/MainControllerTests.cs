using Xunit;
using TWF.Controllers;
using TWF.Services;
using TWF.Providers;
using TWF.Infrastructure;
using Microsoft.Extensions.Logging;

namespace TWF.Tests
{
    /// <summary>
    /// Basic tests for MainController initialization
    /// </summary>
    public class MainControllerTests
    {
        [Fact]
        public void MainController_Constructor_InitializesAllDependencies()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            
            var fileSystemProvider = new FileSystemProvider();
            var configProvider = new ConfigurationProvider();
            var listProvider = new ListProvider(configProvider);
            var sortEngine = new SortEngine();
            var markingEngine = new MarkingEngine();
            var searchEngine = new SearchEngine();
            var archiveManager = new ArchiveManager();
            var fileOps = new FileOperations();
            var viewerManager = new ViewerManager(new SearchEngine());
            var keyBindings = new KeyBindingManager();
            var macroExpander = new MacroExpander();
            var customFunctionManager = new CustomFunctionManager(macroExpander);
            var menuManager = new MenuManager(configProvider.GetConfigDirectory());
            var historyManager = new HistoryManager(configProvider.LoadConfiguration());
            var logger = LoggingConfiguration.GetLogger<MainController>();
            var jobManager = new JobManager(LoggingConfiguration.GetLogger<JobManager>());
            
            // Act
            var controller = new MainController(
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
                jobManager,
                logger
            );

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void MainController_GetActivePane_ReturnsLeftPaneInitially()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();

            // Act
            var activePane = controller.GetActivePane();

            // Assert
            Assert.NotNull(activePane);
            Assert.NotNull(activePane.CurrentPath);
        }

        [Fact]
        public void MainController_SwitchPane_ChangesActivePane()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var initialPane = controller.GetActivePane();

            // Act
            controller.SwitchPane();
            var newPane = controller.GetActivePane();

            // Assert
            Assert.NotSame(initialPane, newPane);
        }

        [Fact]
        public void MainController_GetCurrentMode_ReturnsNormalInitially()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();

            // Act
            var mode = controller.GetCurrentMode();

            // Assert
            Assert.Equal(TWF.Models.UiMode.Normal, mode);
        }

        [Fact]
        public void MainController_ApplyWildcardPattern_MarksMatchingFiles()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Get the active pane and add some test entries
            var activePane = controller.GetActivePane();
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test2.txt", FullPath = "test2.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "readme.md", FullPath = "readme.md", IsDirectory = false }
            };

            // Use reflection to call the private ApplyWildcardPattern method
            var method = typeof(MainController).GetMethod("ApplyWildcardPattern", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act - Apply wildcard pattern "*.txt"
            method?.Invoke(controller, new object[] { "*.txt" });

            // Assert - Should mark only .txt files
            Assert.Equal(2, activePane.GetMarkedEntries().Count);
            var markedFiles = activePane.GetMarkedEntries();
            Assert.All(markedFiles, f => Assert.EndsWith(".txt", f.Name));
        }

        [Fact]
        public void MainController_ApplyWildcardPattern_WithExclusion_MarksCorrectFiles()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            var activePane = controller.GetActivePane();
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test2.txt", FullPath = "test2.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file1.txt", FullPath = "file1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "readme.md", FullPath = "readme.md", IsDirectory = false }
            };

            var method = typeof(MainController).GetMethod("ApplyWildcardPattern", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act - Apply pattern "*.txt :test*" (all .txt files except those starting with "test")
            method?.Invoke(controller, new object[] { "*.txt :test*" });

            // Assert - Should mark only file1.txt (not test1.txt or test2.txt)
            Assert.Single(activePane.GetMarkedEntries());
            var markedFiles = activePane.GetMarkedEntries();
            Assert.Single(markedFiles);
            Assert.Equal("file1.txt", markedFiles[0].Name);
        }

        [Fact]
        public void MainController_ApplyWildcardPattern_WithRegex_MarksMatchingFiles()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            var activePane = controller.GetActivePane();
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test2.txt", FullPath = "test2.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test3.md", FullPath = "test3.md", IsDirectory = false }
            };

            var method = typeof(MainController).GetMethod("ApplyWildcardPattern", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act - Apply regex pattern "m/test\d+" (files starting with "test" followed by digits)
            method?.Invoke(controller, new object[] { "m/test\\d+" });

            // Assert - Should mark test1.txt, test2.txt, and test3.md
            Assert.Equal(3, activePane.GetMarkedEntries().Count);
            var markedFiles = activePane.GetMarkedEntries();
            Assert.All(markedFiles, f => Assert.StartsWith("test", f.Name));
        }

        [Fact]
        public void MainController_ApplyWildcardPattern_ClearsPreviousMarks()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            var activePane = controller.GetActivePane();
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test2.txt", FullPath = "test2.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false }
            };

            // Mark all files initially
            activePane.Entries[0].IsMarked = true;
            activePane.Entries[1].IsMarked = true;
            activePane.Entries[2].IsMarked = true;
            Assert.Equal(3, activePane.GetMarkedEntries().Count);

            var method = typeof(MainController).GetMethod("ApplyWildcardPattern", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act - Apply pattern that matches only .txt files
            method?.Invoke(controller, new object[] { "*.txt" });

            // Assert - Should clear previous marks and only mark .txt files
            Assert.Equal(2, activePane.GetMarkedEntries().Count);
            var markedFiles = activePane.GetMarkedEntries();
            Assert.All(markedFiles, f => Assert.EndsWith(".txt", f.Name));
        }

        [Fact]
        public void MainController_ApplyWildcardPattern_WithMultiplePatterns_MarksAllMatches()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            var activePane = controller.GetActivePane();
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "readme.md", FullPath = "readme.md", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "data.csv", FullPath = "data.csv", IsDirectory = false }
            };

            var method = typeof(MainController).GetMethod("ApplyWildcardPattern", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act - Apply multiple patterns "*.txt *.md"
            method?.Invoke(controller, new object[] { "*.txt *.md" });

            // Assert - Should mark both .txt and .md files
            Assert.Equal(2, activePane.GetMarkedEntries().Count);
            var markedFiles = activePane.GetMarkedEntries();
            Assert.Contains(markedFiles, f => f.Name == "test1.txt");
            Assert.Contains(markedFiles, f => f.Name == "readme.md");
        }

        [Fact]
        public void MainController_CreateDirectory_CreatesDirectoryAndPositionsCursor()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a temporary test directory
            var testDir = Path.Combine(Path.GetTempPath(), $"twf_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(testDir);
            
            try
            {
                // Navigate to the test directory
                controller.NavigateToDirectory(testDir);
                var activePane = controller.GetActivePane();
                
                // Get initial entry count
                int initialCount = activePane.Entries.Count;
                
                // Use reflection to call the private CreateDirectory method
                var method = typeof(MainController).GetMethod("CreateDirectory", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                // Act - Create a new directory
                string newDirName = "TestNewDirectory";
                method?.Invoke(controller, new object[] { newDirName });
                
                // Assert - Directory should be created
                var newDirPath = Path.Combine(testDir, newDirName);
                Assert.True(Directory.Exists(newDirPath), "Directory should be created");
                
                // Assert - Pane should be reloaded with new directory
                Assert.Equal(initialCount + 1, activePane.Entries.Count);
                
                // Assert - Cursor should be positioned on the new directory
                var currentEntry = activePane.GetCurrentEntry();
                Assert.NotNull(currentEntry);
                Assert.Equal(newDirName, currentEntry.Name);
                Assert.True(currentEntry.IsDirectory);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        [Fact]
        public void MainController_CreateDirectory_HandlesInvalidDirectoryName()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a temporary test directory
            var testDir = Path.Combine(Path.GetTempPath(), $"twf_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(testDir);
            
            try
            {
                // Navigate to the test directory
                controller.NavigateToDirectory(testDir);
                var activePane = controller.GetActivePane();
                
                // Get initial entry count
                int initialCount = activePane.Entries.Count;
                
                // Use reflection to call the private CreateDirectory method
                var method = typeof(MainController).GetMethod("CreateDirectory", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                // Act - Try to create a directory with invalid characters
                string invalidDirName = "Test<>|Directory";
                method?.Invoke(controller, new object[] { invalidDirName });
                
                // Assert - Directory should not be created
                var invalidDirPath = Path.Combine(testDir, invalidDirName);
                Assert.False(Directory.Exists(invalidDirPath), "Invalid directory should not be created");
                
                // Assert - Entry count should remain the same
                Assert.Equal(initialCount, activePane.Entries.Count);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        [Fact]
        public void MainController_CreateDirectory_HandlesDuplicateDirectoryName()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a temporary test directory
            var testDir = Path.Combine(Path.GetTempPath(), $"twf_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(testDir);
            
            try
            {
                // Navigate to the test directory
                controller.NavigateToDirectory(testDir);
                
                // Create a directory first
                string dirName = "ExistingDirectory";
                var existingDirPath = Path.Combine(testDir, dirName);
                Directory.CreateDirectory(existingDirPath);
                
                // Reload the pane to show the existing directory
                controller.NavigateToDirectory(testDir);
                var activePane = controller.GetActivePane();
                int countAfterFirst = activePane.Entries.Count;
                
                // Use reflection to call the private CreateDirectory method
                var method = typeof(MainController).GetMethod("CreateDirectory", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                // Act - Try to create a directory with the same name
                method?.Invoke(controller, new object[] { dirName });
                
                // Assert - Entry count should remain the same (no duplicate created)
                Assert.Equal(countAfterFirst, activePane.Entries.Count);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        [Fact]
        public void MainController_CycleSortMode_CyclesThroughAllSortModes()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            // Add some test entries
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "zebra.txt", FullPath = "zebra.txt", IsDirectory = false, Size = 100, LastModified = DateTime.Now.AddDays(-1) },
                new TWF.Models.FileEntry { Name = "apple.txt", FullPath = "apple.txt", IsDirectory = false, Size = 200, LastModified = DateTime.Now },
                new TWF.Models.FileEntry { Name = "banana.doc", FullPath = "banana.doc", IsDirectory = false, Size = 150, LastModified = DateTime.Now.AddDays(-2) }
            };
            
            // Set initial sort mode
            activePane.SortMode = TWF.Models.SortMode.NameAscending;
            
            // Act & Assert - Cycle through all sort modes
            var expectedModes = new[]
            {
                TWF.Models.SortMode.NameDescending,
                TWF.Models.SortMode.ExtensionAscending,
                TWF.Models.SortMode.ExtensionDescending,
                TWF.Models.SortMode.SizeAscending,
                TWF.Models.SortMode.SizeDescending,
                TWF.Models.SortMode.DateAscending,
                TWF.Models.SortMode.DateDescending,
                TWF.Models.SortMode.Unsorted,
                TWF.Models.SortMode.NameAscending // Should wrap back to beginning
            };
            
            foreach (var expectedMode in expectedModes)
            {
                controller.CycleSortMode();
                Assert.Equal(expectedMode, activePane.SortMode);
            }
        }
        
        [Fact]
        public void MainController_CycleSortMode_UpdatesFileOrder()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            // Add test entries in unsorted order
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "zebra.txt", FullPath = "zebra.txt", IsDirectory = false, Size = 100 },
                new TWF.Models.FileEntry { Name = "apple.txt", FullPath = "apple.txt", IsDirectory = false, Size = 200 },
                new TWF.Models.FileEntry { Name = "banana.txt", FullPath = "banana.txt", IsDirectory = false, Size = 150 }
            };
            
            // Set to unsorted initially
            activePane.SortMode = TWF.Models.SortMode.Unsorted;
            
            // Act - Cycle to NameAscending
            controller.CycleSortMode();
            
            // Assert - Files should be sorted alphabetically
            Assert.Equal(TWF.Models.SortMode.NameAscending, activePane.SortMode);
            Assert.Equal("apple.txt", activePane.Entries[0].Name);
            Assert.Equal("banana.txt", activePane.Entries[1].Name);
            Assert.Equal("zebra.txt", activePane.Entries[2].Name);
        }
        
        [Fact]
        public void MainController_CycleSortMode_FromNameAscendingToNameDescending()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "apple.txt", FullPath = "apple.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "banana.txt", FullPath = "banana.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "zebra.txt", FullPath = "zebra.txt", IsDirectory = false }
            };
            
            activePane.SortMode = TWF.Models.SortMode.NameAscending;
            
            // Act
            controller.CycleSortMode();
            
            // Assert
            Assert.Equal(TWF.Models.SortMode.NameDescending, activePane.SortMode);
            Assert.Equal("zebra.txt", activePane.Entries[0].Name);
            Assert.Equal("banana.txt", activePane.Entries[1].Name);
            Assert.Equal("apple.txt", activePane.Entries[2].Name);
        }
        
        [Fact]
        public void MainController_CycleSortMode_FromSizeAscendingToSizeDescending()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "small.txt", FullPath = "small.txt", IsDirectory = false, Size = 100 },
                new TWF.Models.FileEntry { Name = "medium.txt", FullPath = "medium.txt", IsDirectory = false, Size = 200 },
                new TWF.Models.FileEntry { Name = "large.txt", FullPath = "large.txt", IsDirectory = false, Size = 300 }
            };
            
            activePane.SortMode = TWF.Models.SortMode.SizeAscending;
            
            // Act
            controller.CycleSortMode();
            
            // Assert
            Assert.Equal(TWF.Models.SortMode.SizeDescending, activePane.SortMode);
            Assert.Equal("large.txt", activePane.Entries[0].Name);
            Assert.Equal("medium.txt", activePane.Entries[1].Name);
            Assert.Equal("small.txt", activePane.Entries[2].Name);
        }
        
        [Fact]
        public void MainController_CycleSortMode_PreservesMarksOnCorrectFiles()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "zebra.txt", FullPath = "zebra.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "apple.txt", FullPath = "apple.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "banana.txt", FullPath = "banana.txt", IsDirectory = false }
            };
            
            // Files are initially unsorted list.
            // Mark "apple.txt" (index 1) and "zebra.txt" (index 0)
            activePane.Entries[1].IsMarked = true; // apple.txt
            activePane.Entries[0].IsMarked = true; // zebra.txt
            
            activePane.SortMode = TWF.Models.SortMode.Unsorted;
            
            // Act - Cycle to NameAscending (Apple, Banana, Zebra)
            controller.CycleSortMode();
            
            // Assert
            Assert.Equal(TWF.Models.SortMode.NameAscending, activePane.SortMode);
            
            // Verify order
            Assert.Equal("apple.txt", activePane.Entries[0].Name);
            Assert.Equal("banana.txt", activePane.Entries[1].Name);
            Assert.Equal("zebra.txt", activePane.Entries[2].Name);
            
            // Verify marks followed the files
            Assert.True(activePane.Entries[0].IsMarked, "apple.txt should be marked"); // apple.txt
            Assert.False(activePane.Entries[1].IsMarked, "banana.txt should NOT be marked"); // banana.txt
            Assert.True(activePane.Entries[2].IsMarked, "zebra.txt should be marked"); // zebra.txt
            
            // Verify count
            Assert.Equal(2, activePane.GetMarkedEntries().Count);
        }

        [Fact]
        public void MainController_ShowFileMaskDialog_MethodExists()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();

            // Act & Assert - Just verify the method exists and can be called
            // We can't fully test the dialog without Terminal.Gui initialization
            Assert.NotNull(controller);
            
            // Verify the method exists by checking it's callable
            var method = controller.GetType().GetMethod("ShowFileMaskDialog");
            Assert.NotNull(method);
        }

        [Fact]
        public void MainController_EnterSearchMode_ChangesUiModeToSearch()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            // Add some test entries
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false }
            };
            
            // Verify initial mode is Normal
            Assert.Equal(TWF.Models.UiMode.Normal, controller.GetCurrentMode());
            
            // Act
            controller.EnterSearchMode();
            
            // Assert
            Assert.Equal(TWF.Models.UiMode.Search, controller.GetCurrentMode());
            Assert.True(controller.IsInSearchMode());
        }

        [Fact]
        public void MainController_ExitSearchMode_ChangesUiModeToNormal()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false }
            };
            
            // Enter search mode first
            controller.EnterSearchMode();
            Assert.Equal(TWF.Models.UiMode.Search, controller.GetCurrentMode());
            
            // Act
            controller.ExitSearchMode();
            
            // Assert
            Assert.Equal(TWF.Models.UiMode.Normal, controller.GetCurrentMode());
            Assert.False(controller.IsInSearchMode());
        }

        [Fact]
        public void MainController_HandleSearchInput_MovesToMatchingFile()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "apple.txt", FullPath = "apple.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "banana.txt", FullPath = "banana.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "cherry.txt", FullPath = "cherry.txt", IsDirectory = false }
            };
            
            activePane.CursorPosition = 0;
            controller.EnterSearchMode();
            
            // Act - Search for "b"
            controller.HandleSearchInput('b');
            
            // Assert - Cursor should move to "banana.txt"
            Assert.Equal(1, activePane.CursorPosition);
            Assert.Equal("banana.txt", activePane.GetCurrentEntry()?.Name);
        }

        [Fact]
        public void MainController_HandleSearchInput_MultipleCharacters_FindsMatch()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "apple.txt", FullPath = "apple.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "banana.txt", FullPath = "banana.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "cherry.txt", FullPath = "cherry.txt", IsDirectory = false }
            };
            
            activePane.CursorPosition = 0;
            controller.EnterSearchMode();
            
            // Act - Search for "ch"
            controller.HandleSearchInput('c');
            controller.HandleSearchInput('h');
            
            // Assert - Cursor should move to "cherry.txt"
            Assert.Equal(2, activePane.CursorPosition);
            Assert.Equal("cherry.txt", activePane.GetCurrentEntry()?.Name);
        }

        [Fact]
        public void MainController_HandleSearchBackspace_RemovesLastCharacter()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "apple.txt", FullPath = "apple.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "banana.txt", FullPath = "banana.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "cherry.txt", FullPath = "cherry.txt", IsDirectory = false }
            };
            
            activePane.CursorPosition = 0;
            controller.EnterSearchMode();
            
            // Type "ch" to move to cherry
            controller.HandleSearchInput('c');
            controller.HandleSearchInput('h');
            Assert.Equal(2, activePane.CursorPosition);
            
            // Act - Backspace to remove 'h'
            controller.HandleSearchBackspace();
            
            // Assert - Should now match "cherry.txt" with just "c"
            Assert.Equal(2, activePane.CursorPosition);
            Assert.Equal("cherry.txt", activePane.GetCurrentEntry()?.Name);
        }

        [Fact]
        public void MainController_HandleSearchMarkAndNext_MarksCurrentAndMovesToNext()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test2.txt", FullPath = "test2.txt", IsDirectory = false }
            };
            
            activePane.CursorPosition = 0;
            controller.EnterSearchMode();
            
            // Search for "test"
            controller.HandleSearchInput('t');
            controller.HandleSearchInput('e');
            controller.HandleSearchInput('s');
            controller.HandleSearchInput('t');
            
            Assert.Equal(0, activePane.CursorPosition); // Should be at test1.txt
            
            // Act - Mark and find next
            controller.HandleSearchMarkAndNext();
            
            // Assert - test1.txt should be marked and cursor should move to test2.txt
            Assert.True(activePane.Entries[0].IsMarked);
            Assert.Equal(2, activePane.CursorPosition);
            Assert.Equal("test2.txt", activePane.GetCurrentEntry()?.Name);
        }

        [Fact]
        public void MainController_HandleSearchNext_MovesToNextMatch()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test2.txt", FullPath = "test2.txt", IsDirectory = false }
            };
            
            activePane.CursorPosition = 0;
            controller.EnterSearchMode();
            
            // Search for "test"
            controller.HandleSearchInput('t');
            controller.HandleSearchInput('e');
            controller.HandleSearchInput('s');
            controller.HandleSearchInput('t');
            
            Assert.Equal(0, activePane.CursorPosition); // Should be at test1.txt
            
            // Act - Find next
            controller.HandleSearchNext();
            
            // Assert - Cursor should move to test2.txt
            Assert.Equal(2, activePane.CursorPosition);
            Assert.Equal("test2.txt", activePane.GetCurrentEntry()?.Name);
        }

        [Fact]
        public void MainController_HandleSearchPrevious_MovesToPreviousMatch()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "test1.txt", FullPath = "test1.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "file.doc", FullPath = "file.doc", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "test2.txt", FullPath = "test2.txt", IsDirectory = false }
            };
            
            activePane.CursorPosition = 2; // Start at test2.txt
            controller.EnterSearchMode();
            
            // Search for "test"
            controller.HandleSearchInput('t');
            controller.HandleSearchInput('e');
            controller.HandleSearchInput('s');
            controller.HandleSearchInput('t');
            
            // Act - Find previous
            controller.HandleSearchPrevious();
            
            // Assert - Cursor should move to test1.txt
            Assert.Equal(0, activePane.CursorPosition);
            Assert.Equal("test1.txt", activePane.GetCurrentEntry()?.Name);
        }

        [Fact]
        public void MainController_HandleSearchInput_CaseInsensitive()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>
            {
                new TWF.Models.FileEntry { Name = "Apple.txt", FullPath = "Apple.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "BANANA.txt", FullPath = "BANANA.txt", IsDirectory = false },
                new TWF.Models.FileEntry { Name = "cherry.txt", FullPath = "cherry.txt", IsDirectory = false }
            };
            
            activePane.CursorPosition = 0;
            controller.EnterSearchMode();
            
            // Act - Search for lowercase "b" should match "BANANA.txt"
            controller.HandleSearchInput('b');
            
            // Assert
            Assert.Equal(1, activePane.CursorPosition);
            Assert.Equal("BANANA.txt", activePane.GetCurrentEntry()?.Name);
        }

        [Fact]
        public void MainController_EnterSearchMode_WithEmptyPane_DoesNotCrash()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            var activePane = controller.GetActivePane();
            
            activePane.Entries = new List<TWF.Models.FileEntry>(); // Empty list
            
            // Act & Assert - Should not throw
            controller.EnterSearchMode();
            
            // Mode should still be Normal since there are no files to search
            Assert.Equal(TWF.Models.UiMode.Normal, controller.GetCurrentMode());
        }

        [Fact]
        public void MainController_HandleCompressionOperation_MethodExists()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();

            // Act & Assert - Just verify the method exists and can be called
            // We can't fully test the dialog without Terminal.Gui initialization
            Assert.NotNull(controller);
            
            // Verify the method exists by checking it's callable
            var method = controller.GetType().GetMethod("HandleCompressionOperation");
            Assert.NotNull(method);
        }

        private MainController CreateTestController()
        {
            var fileSystemProvider = new FileSystemProvider();
            var configProvider = new ConfigurationProvider();
            var listProvider = new ListProvider(configProvider);
            var sortEngine = new SortEngine();
            var markingEngine = new MarkingEngine();
            var searchEngine = new SearchEngine();
            var archiveManager = new ArchiveManager();
            var fileOps = new FileOperations();
            var viewerManager = new ViewerManager(new SearchEngine());
            var keyBindings = new KeyBindingManager();
            var historyManager = new HistoryManager(configProvider.LoadConfiguration());
            var macroExpander = new MacroExpander();
            var customFunctionManager = new CustomFunctionManager(macroExpander);
            var menuManager = new MenuManager(configProvider.GetConfigDirectory());
            var logger = LoggingConfiguration.GetLogger<MainController>();
            var jobManager = new JobManager(LoggingConfiguration.GetLogger<JobManager>());

            return new MainController(
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
                jobManager,
                logger
            );
        }
    }
}
