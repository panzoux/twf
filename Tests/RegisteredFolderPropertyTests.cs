using FsCheck;
using FsCheck.Xunit;
using TWF.Controllers;
using TWF.Services;
using TWF.Providers;
using TWF.Infrastructure;
using TWF.Models;
using Microsoft.Extensions.Logging;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for registered folder functionality
    /// </summary>
    public class RegisteredFolderPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 32: Registered folder navigation changes path
        /// Validates: Requirements 16.2
        /// 
        /// This property verifies that selecting a registered folder changes the active pane
        /// to that directory path.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property RegisteredFolderNavigation_ChangesPath()
        {
            // Arrange: Create a temporary directory for this test
            var tempDir = Path.Combine(Path.GetTempPath(), "twf_test_registered_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            // Create a sub-directory for config to avoid mixing with data
            var configDir = Path.Combine(tempDir, "config");
            Directory.CreateDirectory(configDir);

            LoggingConfiguration.Initialize();
            var controller = CreateTestController(configDir).Result;
            
            try
            {
                // Create a registered folder
                var registeredFolder = new RegisteredFolder
                {
                    Name = "Test Folder",
                    Path = tempDir,
                    SortOrder = 1
                };
                
                // Add the registered folder to configuration
                var configProvider = new ConfigurationProvider(configDir);
                var config = configProvider.LoadConfiguration();
                config.RegisteredFolders.Add(registeredFolder);
                configProvider.SaveConfiguration(config);
                
                var initialPath = controller.GetActivePane().CurrentPath;
                
                // Act
                controller.NavigateToRegisteredFolder(registeredFolder);
                
                // Assert
                var activePane = controller.GetActivePane();
                var pathChanged = activePane.CurrentPath == tempDir;
                var pathIsDifferent = activePane.CurrentPath != initialPath;
                
                return (pathChanged && pathIsDifferent).ToProperty()
                    .Label($"Path changed: {pathChanged}, Path is different: {pathIsDifferent}, Expected: {tempDir}, Actual: {activePane.CurrentPath}");
            }
            finally
            {
                // Cleanup: Remove the test folder from config
                try
                {
                    var configProvider = new ConfigurationProvider(configDir);
                    var config = configProvider.LoadConfiguration();
                    
                    for (int i = config.RegisteredFolders.Count - 1; i >= 0; i--)
                    {
                        if (config.RegisteredFolders[i].Path == tempDir)
                        {
                            config.RegisteredFolders.RemoveAt(i);
                        }
                    }
                    
                    configProvider.SaveConfiguration(config);
                }
                catch
                {
                    // Ignore cleanup errors
                }
                
                // Cleanup: Delete the temporary directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private async Task<MainController> CreateTestController(string configDir)
        {
            var fileSystemProvider = new FileSystemProvider();
            var configProvider = new ConfigurationProvider(configDir);
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
            await controller.Initialize(false);
            return controller;
        }
    }
}
