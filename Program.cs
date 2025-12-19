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

            // Load configuration first to get log level
            var configProvider = new ConfigurationProvider();
            var config = configProvider.LoadConfiguration();

            // Set up logging infrastructure with configured log level
            LoggingConfiguration.Initialize(config.LogLevel);
            var logger = LoggingConfiguration.GetLogger<Program>();

            try
            {
                logger.LogInformation("Starting TWF - Two-pane Window Filer");

                // Create all dependencies (using parameterless constructors)
                var fileSystemProvider = new FileSystemProvider();
                var listProvider = new ListProvider(configProvider);
                
                var sortEngine = new SortEngine();
                var markingEngine = new MarkingEngine();
                
                // Initialize Migemo if enabled and available
                IMigemoProvider? migemoProvider = null;
                if (config.Migemo.Enabled)
                {
                    migemoProvider = new MigemoProvider(
                        config.Migemo.DictPath
                    );
                    if (migemoProvider.IsAvailable)
                    {
                        logger.LogInformation("Migemo search enabled");
                    }
                    else
                    {
                        logger.LogInformation("Migemo not available (library or dictionaries not found)");
                    }
                }
                
                var searchEngine = new SearchEngine(migemoProvider);
                
                var archiveManager = new ArchiveManager();
                var fileOps = new FileOperations();
                var viewerManager = new ViewerManager();
                var historyManager = new HistoryManager(config);
                
                var keyBindings = new KeyBindingManager();
                
                var macroExpander = new MacroExpander();
                var customFunctionManager = new CustomFunctionManager(macroExpander);
                
                // Create MenuManager with config directory path
                var menuManager = new MenuManager(
                    configProvider.GetConfigDirectory(),
                    LoggingConfiguration.GetLogger<MenuManager>()
                );

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
                    customFunctionManager,
                    menuManager,
                    historyManager,
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
