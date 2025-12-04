using System.Text;
using Microsoft.Extensions.Logging;
using TWF.Controllers;
using TWF.Services;
using TWF.Providers;
using TWF.Infrastructure;

namespace TWF
{
    /// <summary>
    /// Entry point for TWF (Two-pane Window Filer) application
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Register encoding provider for Japanese and other code pages
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Set up logging infrastructure
            LoggingConfiguration.Initialize();
            var logger = LoggingConfiguration.GetLogger<Program>();

            try
            {
                logger.LogInformation("Starting TWF - Two-pane Window Filer");

                // Create all dependencies (using parameterless constructors)
                var fileSystemProvider = new FileSystemProvider();
                var configProvider = new ConfigurationProvider();
                var listProvider = new ListProvider(configProvider);
                
                var sortEngine = new SortEngine();
                var markingEngine = new MarkingEngine();
                var searchEngine = new SearchEngine(); // No Migemo support initially
                
                var archiveManager = new ArchiveManager();
                var fileOps = new FileOperations();
                var viewerManager = new ViewerManager();
                
                var keyBindings = new KeyBindingManager();

                // Create and initialize MainController
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
                    LoggingConfiguration.GetLogger<MainController>()
                );

                controller.Initialize();
                controller.Run();

                logger.LogInformation("TWF application exited normally");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Fatal error in TWF application");
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
    }
}
