using System.Text;
using Microsoft.Extensions.Logging;
using TWF.Models;
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
        /// <summary>
        /// Reads initial logging settings from the config file without initializing the full configuration system
        /// </summary>
        private static (string LogLevel, int MaxLogFiles) ReadInitialConfig()
        {
            string logLevel = "Information";
            int maxLogFiles = 5;

            try
            {
                var configDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TWF");

                var configPath = Path.Combine(configDirectory, "config.json");

                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    
                    if (doc.RootElement.TryGetProperty("LogLevel", out var logLevelElement))
                    {
                        logLevel = logLevelElement.GetString() ?? "Information";
                    }

                    if (doc.RootElement.TryGetProperty("Display", out var displayElement) &&
                        displayElement.TryGetProperty("MaxLogFiles", out var maxFilesElement))
                    {
                        maxLogFiles = maxFilesElement.GetInt32();
                    }
                }
            }
            catch { }

            return (logLevel, maxLogFiles);
        }

        static void Main(string[] args)
        {
            // Parse arguments
            string? changeDirectoryOutputFile = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-cwd" && i + 1 < args.Length)
                {
                    changeDirectoryOutputFile = args[i + 1];
                    i++;
                }
                else if (args[i].StartsWith("--cwd-file="))
                {
                    changeDirectoryOutputFile = args[i].Substring("--cwd-file=".Length);
                }
            }

            // Register encoding provider for Japanese and other code pages
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Read initial config from file without initializing full logging system
            var initialConfig = ReadInitialConfig();

            // Set up logging infrastructure with settings from config
            LoggingConfiguration.Initialize(initialConfig.LogLevel, initialConfig.MaxLogFiles);
            var logger = LoggingConfiguration.GetLogger<Program>();

            // Now load the full configuration
            var configProvider = new ConfigurationProvider();
            var config = configProvider.LoadConfiguration();

            try
            {
                logger.LogInformation("Starting TWF - Two-pane Window Filer");

                // Create all dependencies (using parameterless constructors)
                var fileSystemProvider = new FileSystemProvider();
                var listProvider = new ListProvider(configProvider, LoggingConfiguration.GetLogger<ListProvider>());
                
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
                
                // Register default ZIP provider
                archiveManager.RegisterProvider(new ZipArchiveProvider());

                // Initialize 7-Zip if possible
                try
                {
                    logger.LogInformation("Attempting to initialize 7-Zip support...");
                    string? sevenZipPath = FindSevenZipLibrary(config, logger);
                    
                    if (!string.IsNullOrEmpty(sevenZipPath))
                    {
                        try
                        {
                            logger.LogDebug("Setting 7-Zip library path to: {Path}", sevenZipPath);
                            SevenZip.SevenZipBase.SetLibraryPath(sevenZipPath);
                            
                            // Try to register provider
                            archiveManager.RegisterProvider(new SevenZipArchiveProvider());
                            logger.LogInformation("7-Zip support enabled using {Path}", sevenZipPath);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "7-Zip library found at {Path} but could not be loaded. 7-Zip support disabled. Error: {Message}", sevenZipPath, ex.Message);
                        }
                    }
                    else
                    {
                        logger.LogWarning("7-Zip library (7z.dll/7za.dll/lib7z.so) not found in any search path. 7-Zip support disabled.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to initialize 7-Zip support");
                }

                var fileOps = new FileOperations();
                var viewerManager = new ViewerManager(searchEngine);
                var historyManager = new HistoryManager(config);
                
                var keyBindings = new KeyBindingManager();
                
                var macroExpander = new MacroExpander();
                var customFunctionManager = new CustomFunctionManager(macroExpander, configProvider, LoggingConfiguration.GetLogger<CustomFunctionManager>());
                
                // Create JobManager with concurrency and throttle settings from config (min 100ms throttle)
                int updateInterval = Math.Max(100, config.Display.TaskPanelUpdateIntervalMs);
                var jobManager = new JobManager(
                    LoggingConfiguration.GetLogger<JobManager>(),
                    config.Display.MaxSimultaneousJobs,
                    updateInterval
                );
                
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
                    jobManager,
                    LoggingConfiguration.GetLogger<MainController>()
                );

                if (!string.IsNullOrEmpty(changeDirectoryOutputFile))
                {
                    controller.ChangeDirectoryOutputFile = changeDirectoryOutputFile;
                }

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

        private static string? FindSevenZipLibrary(Configuration config, ILogger logger)
        {
            logger.LogDebug("Starting search for 7-Zip library...");

            // 1. Check configured paths
            if (config.Archive.ArchiveDllPaths != null)
            {
                logger.LogDebug("Checking configured paths from config.json...");
                foreach (var path in config.Archive.ArchiveDllPaths)
                {
                    logger.LogDebug("Checking configured path: {Path}", path);
                    if (File.Exists(path)) 
                    {
                        logger.LogDebug("Found at configured path: {Path}", path);
                        return path;
                    }
                }
            }

            // 2. Check application directory (where NuGet packages might copy the native lib)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            logger.LogDebug("Checking application base directory: {BaseDir}", baseDir);
            
            string[] localPaths = { 
                Path.Combine(baseDir, "7-zip.dll"), 
                Path.Combine(baseDir, "7z.dll"), 
                Path.Combine(baseDir, "7za.dll") 
            };
            
            foreach (var p in localPaths) 
            {
                logger.LogDebug("Checking local path: {Path}", p);
                if (File.Exists(p)) 
                {
                    logger.LogDebug("Found at local path: {Path}", p);
                    return p;
                }
            }

            // 3. Common system paths
            logger.LogDebug("Checking common system paths...");
            if (OperatingSystem.IsWindows())
            {
                string[] winPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "7-Zip", "7z.dll"),
                    "7z.dll",
                    "7za.dll",
                    "7-zip.dll"
                };
                foreach (var p in winPaths) 
                {
                    logger.LogDebug("Checking system path: {Path}", p);
                    if (File.Exists(p)) 
                    {
                        logger.LogDebug("Found at system path: {Path}", p);
                        return p;
                    }
                }
            }
            else
            {
                string[] unixPaths = { "/usr/lib/p7zip/7z.so", "/usr/local/lib/p7zip/7z.so", "/usr/lib/7z.so", "/usr/lib/x86_64-linux-gnu/7z.so" };
                foreach (var p in unixPaths) 
                {
                    logger.LogDebug("Checking system path: {Path}", p);
                    if (File.Exists(p)) 
                    {
                        logger.LogDebug("Found at system path: {Path}", p);
                        return p;
                    }
                }
            }

            logger.LogDebug("7-Zip library not found.");
            return null;
        }
    }
}
