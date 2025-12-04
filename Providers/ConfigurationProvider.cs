using System.Text.Json;
using System.Text.Json.Serialization;
using TWF.Models;

namespace TWF.Providers
{
    /// <summary>
    /// Provides configuration loading, saving, and session state persistence
    /// </summary>
    public class ConfigurationProvider
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly string _sessionStateFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationProvider(string? configDirectory = null)
        {
            _configDirectory = configDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TWF");

            _configFilePath = Path.Combine(_configDirectory, "config.json");
            _sessionStateFilePath = Path.Combine(_configDirectory, "session.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            EnsureConfigDirectoryExists();
        }

        /// <summary>
        /// Loads configuration from file, returns default configuration if file doesn't exist or is invalid
        /// </summary>
        public Configuration LoadConfiguration(string? configPath = null)
        {
            var path = configPath ?? _configFilePath;

            try
            {
                if (!File.Exists(path))
                {
                    var defaultConfig = CreateDefaultConfiguration();
                    SaveConfiguration(defaultConfig, path);
                    return defaultConfig;
                }

                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<Configuration>(json, _jsonOptions);

                if (config == null)
                {
                    return CreateDefaultConfiguration();
                }

                // Validate and apply defaults for any missing properties
                ValidateConfiguration(config);
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Saves configuration to file
        /// </summary>
        public void SaveConfiguration(Configuration config, string? configPath = null)
        {
            var path = configPath ?? _configFilePath;

            try
            {
                ValidateConfiguration(config);
                var json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads session state from file, returns default state if file doesn't exist
        /// </summary>
        public SessionState LoadSessionState()
        {
            try
            {
                if (!File.Exists(_sessionStateFilePath))
                {
                    return CreateDefaultSessionState();
                }

                var json = File.ReadAllText(_sessionStateFilePath);
                var state = JsonSerializer.Deserialize<SessionState>(json, _jsonOptions);

                return state ?? CreateDefaultSessionState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session state: {ex.Message}");
                return CreateDefaultSessionState();
            }
        }

        /// <summary>
        /// Saves session state to file
        /// </summary>
        public void SaveSessionState(SessionState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, _jsonOptions);
                File.WriteAllText(_sessionStateFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving session state: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a default configuration with sensible defaults
        /// </summary>
        private Configuration CreateDefaultConfiguration()
        {
            return new Configuration
            {
                Display = new DisplaySettings
                {
                    FontName = "Consolas",
                    FontSize = 12,
                    ForegroundColor = "White",
                    BackgroundColor = "Black",
                    HighlightColor = "Yellow",
                    MarkedFileColor = "Cyan",
                    DirectoryColor = "BrightCyan",
                    DirectoryBackgroundColor = "Black",
                    DefaultDisplayMode = DisplayMode.Details,
                    ShowHiddenFiles = true,
                    ShowSystemFiles = false
                },
                KeyBindings = new KeyBindings
                {
                    KeyBindingFile = "keybindings.json"
                },
                RegisteredFolders = new List<RegisteredFolder>(),
                ExtensionAssociations = new Dictionary<string, string>
                {
                    { ".txt", "notepad.exe" },
                    { ".log", "notepad.exe" }
                },
                Archive = new ArchiveSettings
                {
                    DefaultArchiveFormat = "ZIP",
                    CompressionLevel = 5,
                    ShowArchiveContentsAsVirtualFolder = true,
                    ArchiveDllPaths = new List<string>()
                },
                Viewer = new ViewerSettings
                {
                    DefaultTextEncoding = "UTF-8",
                    TextEditorPath = "notepad.exe",
                    ShowLineNumbers = true,
                    TextViewerForegroundColor = "White",
                    TextViewerBackgroundColor = "Black",
                    TextViewerStatusForegroundColor = "White",
                    TextViewerStatusBackgroundColor = "Gray",
                    TextViewerEncodingForegroundColor = "White",
                    TextViewerEncodingBackgroundColor = "Blue",
                    DefaultImageViewMode = ViewMode.FitToScreen,
                    SupportedImageExtensions = new List<string>
                    {
                        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico"
                    }
                },
                SaveSessionState = true
            };
        }

        /// <summary>
        /// Creates a default session state
        /// </summary>
        private SessionState CreateDefaultSessionState()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            return new SessionState
            {
                LeftPath = userProfile,
                RightPath = userProfile,
                LeftMask = "*",
                RightMask = "*",
                LeftSort = SortMode.NameAscending,
                RightSort = SortMode.NameAscending,
                LeftDisplayMode = DisplayMode.Details,
                RightDisplayMode = DisplayMode.Details,
                LeftPaneActive = true
            };
        }

        /// <summary>
        /// Validates configuration and applies defaults for missing values
        /// </summary>
        private void ValidateConfiguration(Configuration config)
        {
            // Ensure Display settings exist
            config.Display ??= new DisplaySettings();
            
            // Validate font size
            if (config.Display.FontSize < 8 || config.Display.FontSize > 72)
            {
                config.Display.FontSize = 12;
            }

            // Ensure KeyBindings exist
            config.KeyBindings ??= new KeyBindings();

            // Ensure collections are initialized
            config.RegisteredFolders ??= new List<RegisteredFolder>();
            config.ExtensionAssociations ??= new Dictionary<string, string>();

            // Ensure Archive settings exist
            config.Archive ??= new ArchiveSettings();
            config.Archive.ArchiveDllPaths ??= new List<string>();
            
            // Validate compression level
            if (config.Archive.CompressionLevel < 0 || config.Archive.CompressionLevel > 9)
            {
                config.Archive.CompressionLevel = 5;
            }

            // Ensure Viewer settings exist
            config.Viewer ??= new ViewerSettings();
            config.Viewer.SupportedImageExtensions ??= new List<string>
            {
                ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico"
            };
        }

        /// <summary>
        /// Ensures the configuration directory exists
        /// </summary>
        private void EnsureConfigDirectoryExists()
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        /// <summary>
        /// Gets the configuration directory path
        /// </summary>
        public string GetConfigDirectory() => _configDirectory;

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        public string GetConfigFilePath() => _configFilePath;

        /// <summary>
        /// Gets the session state file path
        /// </summary>
        public string GetSessionStateFilePath() => _sessionStateFilePath;
    }
}
