using FsCheck;
using FsCheck.Xunit;
using TWF.Controllers;
using TWF.Services;
using TWF.Providers;
using TWF.Infrastructure;
using TWF.Models;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for display mode switching functionality
    /// </summary>
    public class DisplayModePropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 8: Number keys switch display modes
        /// Validates: Requirements 2.1
        /// 
        /// This property verifies that pressing number keys 1-8 switches to the
        /// corresponding display mode.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property NumberKeys_SwitchDisplayModes()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a test directory with files
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_display_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create some test files
                for (int i = 0; i < 5; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"file{i}.txt"), $"content {i}");
                }
                
                controller.NavigateToDirectory(tempDir);
                
                // Test each number key (1-8)
                var results = new List<bool>();
                
                for (int number = 1; number <= 8; number++)
                {
                    // Act
                    controller.HandleNumberKey(number);
                    
                    // Assert
                    var activePane = controller.GetActivePane();
                    var expectedMode = number switch
                    {
                        1 => DisplayMode.OneColumn,
                        2 => DisplayMode.TwoColumns,
                        3 => DisplayMode.ThreeColumns,
                        4 => DisplayMode.FourColumns,
                        5 => DisplayMode.FiveColumns,
                        6 => DisplayMode.SixColumns,
                        7 => DisplayMode.SevenColumns,
                        8 => DisplayMode.EightColumns,
                        _ => DisplayMode.Details
                    };
                    
                    results.Add(activePane.DisplayMode == expectedMode);
                }
                
                var allCorrect = results.All(r => r);
                
                return allCorrect.ToProperty()
                    .Label($"All display modes switched correctly: {allCorrect}");
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

        /// <summary>
        /// Feature: twf-file-manager, Property 11: Display mode change preserves cursor position
        /// Validates: Requirements 2.6
        /// 
        /// This property verifies that changing display mode preserves the cursor position
        /// and scroll offset.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property DisplayModeChange_PreservesCursorPosition()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a test directory with multiple files
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_cursor_preserve_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create multiple test files
                for (int i = 0; i < 20; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"file{i:D2}.txt"), $"content {i}");
                }
                
                controller.NavigateToDirectory(tempDir);
                var activePane = controller.GetActivePane();
                
                // Ensure we have entries
                if (activePane.Entries.Count < 5)
                {
                    return true.ToProperty().Label("Not enough entries to test cursor preservation");
                }
                
                // Move cursor to a specific position (middle of the list)
                var targetPosition = activePane.Entries.Count / 2;
                activePane.CursorPosition = targetPosition;
                activePane.ScrollOffset = Math.Max(0, targetPosition - 5);
                
                var initialCursorPosition = activePane.CursorPosition;
                var initialScrollOffset = activePane.ScrollOffset;
                
                // Act: Change display mode multiple times
                var results = new List<bool>();
                
                for (int number = 1; number <= 8; number++)
                {
                    controller.HandleNumberKey(number);
                    
                    // Assert cursor position and scroll offset are preserved
                    var cursorPreserved = activePane.CursorPosition == initialCursorPosition;
                    var scrollPreserved = activePane.ScrollOffset == initialScrollOffset;
                    
                    results.Add(cursorPreserved && scrollPreserved);
                }
                
                var allPreserved = results.All(r => r);
                
                return allPreserved.ToProperty()
                    .Label($"Cursor and scroll preserved across all mode changes: {allPreserved}");
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
            var viewerManager = new ViewerManager();
            var keyBindings = new KeyBindingManager();
            var logger = LoggingConfiguration.GetLogger<MainController>();

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
                logger
            );
        }
    }
}
