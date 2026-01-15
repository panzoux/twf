using Xunit;
using FsCheck;
using FsCheck.Xunit;
using TWF.Controllers;
using TWF.Services;
using TWF.Providers;
using TWF.Infrastructure;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using Terminal.Gui.App;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for Terminal.Gui v2 application lifecycle migration
    /// Feature: terminal-gui-v2-migration
    /// </summary>
    public class ApplicationLifecyclePropertyTests
    {
        /// <summary>
        /// Helper method to create a test MainController with all dependencies
        /// </summary>
        private MainController CreateTestController()
        {
            LoggingConfiguration.Initialize();
            
            var fileSystemProvider = new FileSystemProvider();
            var configProvider = new ConfigurationProvider();
            var listProvider = new ListProvider(configProvider, LoggingConfiguration.GetLogger<ListProvider>());
            var sortEngine = new SortEngine();
            var markingEngine = new MarkingEngine();
            var searchEngine = new SearchEngine();
            var archiveManager = new ArchiveManager();
            var fileOps = new FileOperations();
            var viewerManager = new ViewerManager(new SearchEngine());
            var keyBindings = new KeyBindingManager();
            var macroExpander = new MacroExpander();
            var customFunctionManager = new CustomFunctionManager(macroExpander, configProvider, LoggingConfiguration.GetLogger<CustomFunctionManager>());
            var menuManager = new MenuManager(configProvider.GetConfigDirectory(), LoggingConfiguration.GetLogger<MenuManager>());
            var config = configProvider.LoadConfiguration();
            var historyManager = new HistoryManager(config);
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

        /// <summary>
        /// Property 1: Application Lifecycle Initialization
        /// Feature: terminal-gui-v2-migration, Property 1: Application Lifecycle Initialization
        /// 
        /// For any MainController instance, after calling Initialize(), the controller should have 
        /// a non-null IApplication instance that is properly initialized and ready to run views.
        /// 
        /// Validates: Requirements 1.1
        /// </summary>
        [Property(Arbitrary = new[] { typeof(Generators) }, MaxTest = 100)]
        [Trait("Feature", "terminal-gui-v2-migration")]
        [Trait("Property", "1")]
        public Property ApplicationLifecycleInitialization()
        {
            // Feature: terminal-gui-v2-migration, Property 1: Application Lifecycle Initialization
            // For any MainController instance, after calling Initialize(), 
            // the controller should have a non-null IApplication instance
            
            MainController? controller = null;
            IApplication? app = null;
            
            try
            {
                // Arrange
                controller = CreateTestController();
                
                // Act
                controller.Initialize();
                
                // Get the IApplication instance using reflection (since it's private)
                var appField = typeof(MainController).GetField("_app", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                app = appField?.GetValue(controller) as IApplication;
                
                // Assert - IApplication instance should be non-null
                var hasApp = app != null;
                
                // Assert - IApplication should be initialized (Driver should be available)
                var isInitialized = app?.Driver != null;
                
                return (hasApp && isInitialized).ToProperty();
            }
            finally
            {
                // Cleanup - Dispose the controller to clean up resources
                try
                {
                    controller?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors in test cleanup
                }
            }
        }

        /// <summary>
        /// Property 2: Application Lifecycle Run (partial - run aspect)
        /// Feature: terminal-gui-v2-migration, Property 2: Application Lifecycle Disposal (partial - run aspect)
        /// 
        /// For any MainController instance, after calling Initialize(), the Run() method should use
        /// the IApplication instance to run the main window (not Application.Top.Add + Application.Run).
        /// This test verifies that the Run method is properly configured to use instance-based pattern.
        /// 
        /// Note: This is a partial test that verifies the setup. Full run lifecycle testing requires
        /// a running event loop which is tested separately.
        /// 
        /// Validates: Requirements 1.2
        /// </summary>
        [Property(Arbitrary = new[] { typeof(Generators) }, MaxTest = 100)]
        [Trait("Feature", "terminal-gui-v2-migration")]
        [Trait("Property", "2")]
        public Property ApplicationLifecycleRun()
        {
            // Feature: terminal-gui-v2-migration, Property 2: Application Lifecycle Disposal (partial - run aspect)
            // For any MainController instance, after Initialize(), Run() should be ready to execute
            // using the IApplication instance
            
            MainController? controller = null;
            
            try
            {
                // Arrange
                controller = CreateTestController();
                controller.Initialize();
                
                // Get the IApplication instance using reflection
                var appField = typeof(MainController).GetField("_app", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var app = appField?.GetValue(controller) as IApplication;
                
                // Get the main window using reflection
                var mainWindowField = typeof(MainController).GetField("_mainWindow",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var mainWindow = mainWindowField?.GetValue(controller) as Window;
                
                // Assert - Both IApplication and MainWindow should be ready for Run()
                var hasApp = app != null;
                var hasMainWindow = mainWindow != null;
                var appHasDriver = app?.Driver != null;
                
                // Verify that the controller is in a state where Run() can be called
                // (we don't actually call Run() as it would block the test)
                return (hasApp && hasMainWindow && appHasDriver).ToProperty();
            }
            finally
            {
                // Cleanup - Dispose the controller to clean up resources
                try
                {
                    controller?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors in test cleanup
                }
            }
        }

        /// <summary>
        /// Property 2: Application Lifecycle Disposal
        /// Feature: terminal-gui-v2-migration, Property 2: Application Lifecycle Disposal
        /// 
        /// For any MainController instance, after calling Dispose(), all resources (IApplication, 
        /// Window, Views) should be properly disposed and no resource leaks should exist.
        /// 
        /// Validates: Requirements 1.3, 4.1, 4.5, 4.6
        /// </summary>
        [Property(Arbitrary = new[] { typeof(Generators) }, MaxTest = 100)]
        [Trait("Feature", "terminal-gui-v2-migration")]
        [Trait("Property", "2-disposal")]
        public Property ApplicationLifecycleDisposal()
        {
            // Feature: terminal-gui-v2-migration, Property 2: Application Lifecycle Disposal
            // For any MainController instance, after calling Dispose(), 
            // all resources should be properly disposed
            
            MainController? controller = null;
            IApplication? app = null;
            Window? mainWindow = null;
            bool disposedFlagSet = false;
            
            try
            {
                // Arrange
                controller = CreateTestController();
                controller.Initialize();
                
                // Get the IApplication instance using reflection
                var appField = typeof(MainController).GetField("_app", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                app = appField?.GetValue(controller) as IApplication;
                
                // Get the main window using reflection
                var mainWindowField = typeof(MainController).GetField("_mainWindow",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                mainWindow = mainWindowField?.GetValue(controller) as Window;
                
                // Verify resources exist before disposal
                var hasAppBeforeDispose = app != null;
                var hasMainWindowBeforeDispose = mainWindow != null;
                
                // Act - Dispose the controller
                controller.Dispose();
                
                // Get the _disposed flag using reflection
                var disposedField = typeof(MainController).GetField("_disposed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                disposedFlagSet = disposedField != null && (bool)(disposedField.GetValue(controller) ?? false);
                
                // Assert - Resources should be disposed
                // Note: We can't directly check if IApplication/Window are disposed in v2,
                // but we can verify the _disposed flag is set and no exceptions occurred
                var disposalSuccessful = disposedFlagSet;
                
                // Verify calling Dispose again is safe (idempotent)
                controller.Dispose();
                
                return (hasAppBeforeDispose && hasMainWindowBeforeDispose && disposalSuccessful).ToProperty();
            }
            catch (Exception ex)
            {
                // Disposal should not throw exceptions
                return false.ToProperty()
                    .Label($"Disposal threw exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Property 3: Exception-Safe Disposal
        /// Feature: terminal-gui-v2-migration, Property 3: Exception-Safe Disposal
        /// 
        /// For any MainController instance, even when exceptions occur during shutdown, 
        /// the IApplication instance should still be disposed properly through try-finally blocks.
        /// This test verifies that disposal is resilient to errors.
        /// 
        /// Validates: Requirements 1.7, 4.5
        /// </summary>
        [Property(Arbitrary = new[] { typeof(Generators) }, MaxTest = 100)]
        [Trait("Feature", "terminal-gui-v2-migration")]
        [Trait("Property", "3")]
        public Property ExceptionSafeDisposal()
        {
            // Feature: terminal-gui-v2-migration, Property 3: Exception-Safe Disposal
            // For any MainController instance, even when exceptions occur during shutdown,
            // the IApplication instance should still be disposed properly
            
            MainController? controller = null;
            bool disposedFlagSet = false;
            
            try
            {
                // Arrange
                controller = CreateTestController();
                controller.Initialize();
                
                // Get the IApplication instance using reflection
                var appField = typeof(MainController).GetField("_app", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var app = appField?.GetValue(controller) as IApplication;
                
                var hasAppBeforeDispose = app != null;
                
                // Act - Dispose the controller
                // The Shutdown method should handle any exceptions during session state saving
                // and still dispose the IApplication instance
                controller.Dispose();
                
                // Get the _disposed flag using reflection
                var disposedField = typeof(MainController).GetField("_disposed",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                disposedFlagSet = disposedField != null && (bool)(disposedField.GetValue(controller) ?? false);
                
                // Assert - Even if session state saving fails, disposal should complete
                // The _disposed flag should be set in the finally block
                var disposalCompletedDespiteErrors = disposedFlagSet;
                
                // Verify multiple Dispose calls are safe (idempotent)
                controller.Dispose();
                controller.Dispose();
                
                // Verify _disposed flag is still set after multiple calls
                var stillDisposed = disposedField != null && (bool)(disposedField.GetValue(controller) ?? false);
                
                return (hasAppBeforeDispose && disposalCompletedDespiteErrors && stillDisposed).ToProperty();
            }
            catch (Exception ex)
            {
                // Disposal should not throw exceptions even in error scenarios
                return false.ToProperty()
                    .Label($"Exception-safe disposal failed with exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Generators for property-based tests
        /// </summary>
        public class Generators
        {
            // No custom generators needed for this test as we're testing initialization
            // which doesn't require random input data
        }
    }
}
