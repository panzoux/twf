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
    /// Property-based tests for navigation functionality in MainController
    /// </summary>
    public class NavigationPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 1: Directory navigation updates pane contents
        /// Validates: Requirements 1.1
        /// 
        /// This property verifies that navigating to a directory updates the pane to display
        /// the contents of that directory.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property DirectoryNavigation_UpdatesPaneContents()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Get a valid directory path to navigate to
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_nav_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create some test files in the directory
                var testFile1 = Path.Combine(tempDir, "test1.txt");
                var testFile2 = Path.Combine(tempDir, "test2.txt");
                File.WriteAllText(testFile1, "test content 1");
                File.WriteAllText(testFile2, "test content 2");
                
                var initialPath = controller.GetActivePane().CurrentPath;
                
                // Act
                controller.NavigateToDirectory(tempDir);
                
                // Assert
                var activePane = controller.GetActivePane();
                var pathChanged = activePane.CurrentPath == tempDir;
                var hasEntries = activePane.Entries.Count > 0;
                var containsTestFiles = activePane.Entries.Any(e => e.Name == "test1.txt") &&
                                       activePane.Entries.Any(e => e.Name == "test2.txt");
                
                return (pathChanged && hasEntries && containsTestFiles).ToProperty()
                    .Label($"Path changed: {pathChanged}, Has entries: {hasEntries}, Contains test files: {containsTestFiles}");
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
        /// Feature: twf-file-manager, Property 2: Backspace navigates to parent
        /// Validates: Requirements 1.2
        /// 
        /// This property verifies that navigating to the parent directory moves up one level
        /// in the directory hierarchy.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ParentNavigation_MovesUpOneLevel()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a nested directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_parent_" + Guid.NewGuid().ToString());
            var subDir = Path.Combine(tempDir, "subdir");
            Directory.CreateDirectory(subDir);
            
            try
            {
                // Navigate to the subdirectory
                controller.NavigateToDirectory(subDir);
                var initialPath = controller.GetActivePane().CurrentPath;
                
                // Act
                controller.NavigateToParent();
                
                // Assert
                var activePane = controller.GetActivePane();
                var movedToParent = activePane.CurrentPath == tempDir;
                var pathIsParent = Directory.GetParent(initialPath)?.FullName == activePane.CurrentPath;
                
                return (movedToParent && pathIsParent).ToProperty()
                    .Label($"Moved to parent: {movedToParent}, Path is parent: {pathIsParent}");
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
        /// Feature: twf-file-manager, Property 3: Tab toggles pane focus
        /// Validates: Requirements 1.3
        /// 
        /// This property verifies that switching panes changes which pane is active.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property PaneSwitching_TogglesActivePane()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            var initialPane = controller.GetActivePane();
            
            // Act
            controller.SwitchPane();
            var newPane = controller.GetActivePane();
            
            // Switch back
            controller.SwitchPane();
            var finalPane = controller.GetActivePane();
            
            // Assert
            var panesAreDifferent = !ReferenceEquals(initialPane, newPane);
            var switchedBack = ReferenceEquals(initialPane, finalPane);
            
            return (panesAreDifferent && switchedBack).ToProperty()
                .Label($"Panes are different: {panesAreDifferent}, Switched back: {switchedBack}");
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 4: Arrow keys move cursor
        /// Validates: Requirements 1.4
        /// 
        /// This property verifies that arrow keys move the cursor up and down in the file list.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property CursorMovement_ArrowKeysMoveCursor()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a directory with multiple files
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_cursor_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create multiple test files
                for (int i = 0; i < 5; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"file{i}.txt"), $"content {i}");
                }
                
                controller.NavigateToDirectory(tempDir);
                var activePane = controller.GetActivePane();
                
                // Ensure we have entries
                if (activePane.Entries.Count < 2)
                {
                    return true.ToProperty().Label("Not enough entries to test cursor movement");
                }
                
                var initialPosition = activePane.CursorPosition;
                
                // Act: Move down
                controller.MoveCursorDown();
                var positionAfterDown = activePane.CursorPosition;
                
                // Move up
                controller.MoveCursorUp();
                var positionAfterUp = activePane.CursorPosition;
                
                // Assert
                var movedDown = positionAfterDown == initialPosition + 1;
                var movedBackUp = positionAfterUp == initialPosition;
                
                return (movedDown && movedBackUp).ToProperty()
                    .Label($"Moved down: {movedDown}, Moved back up: {movedBackUp}");
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
        /// Feature: twf-file-manager, Property 5: Ctrl+PageUp moves to first entry
        /// Feature: twf-file-manager, Property 6: Ctrl+PageDown moves to last entry
        /// Validates: Requirements 1.5, 1.6
        /// 
        /// This property verifies that Ctrl+PageUp moves to the first entry and
        /// Ctrl+PageDown moves to the last entry.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property JumpToFirstLast_MovesToBoundaries()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a directory with multiple files
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_jump_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create multiple test files
                for (int i = 0; i < 10; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"file{i:D2}.txt"), $"content {i}");
                }
                
                controller.NavigateToDirectory(tempDir);
                var activePane = controller.GetActivePane();
                
                // Ensure we have entries
                if (activePane.Entries.Count == 0)
                {
                    return true.ToProperty().Label("No entries to test jump");
                }
                
                // Act: Move to last
                controller.MoveCursorToLast();
                var lastPosition = activePane.CursorPosition;
                
                // Move to first
                controller.MoveCursorToFirst();
                var firstPosition = activePane.CursorPosition;
                
                // Assert
                var movedToLast = lastPosition == activePane.Entries.Count - 1;
                var movedToFirst = firstPosition == 0;
                
                return (movedToLast && movedToFirst).ToProperty()
                    .Label($"Moved to last: {movedToLast}, Moved to first: {movedToFirst}");
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
        /// Feature: twf-file-manager, Property 7: Home navigates to drive root
        /// Validates: Requirements 1.7
        /// 
        /// This property verifies that navigating to root moves to the drive root directory.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property RootNavigation_MovesToDriveRoot()
        {
            // Arrange
            LoggingConfiguration.Initialize();
            var controller = CreateTestController();
            
            // Create a nested directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_root_" + Guid.NewGuid().ToString());
            var subDir = Path.Combine(tempDir, "subdir", "nested");
            Directory.CreateDirectory(subDir);
            
            try
            {
                // Navigate to the nested subdirectory
                controller.NavigateToDirectory(subDir);
                var initialPath = controller.GetActivePane().CurrentPath;
                var expectedRoot = Path.GetPathRoot(initialPath);
                
                // Act
                controller.NavigateToRoot();
                
                // Assert
                var activePane = controller.GetActivePane();
                var movedToRoot = activePane.CurrentPath == expectedRoot;
                
                return movedToRoot.ToProperty()
                    .Label($"Moved to root: {movedToRoot}, Expected: {expectedRoot}, Actual: {activePane.CurrentPath}");
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
            var macroExpander = new MacroExpander();
            var customFunctionManager = new CustomFunctionManager(macroExpander);
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
                customFunctionManager,
                logger
            );
        }
    }
}
