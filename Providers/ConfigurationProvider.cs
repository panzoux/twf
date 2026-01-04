using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using TWF.Infrastructure;
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
        private readonly string _registeredFoldersFilePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<ConfigurationProvider> _logger;
        private static bool _startupLogged = false; // Track if startup logs have been shown

        public ConfigurationProvider(string? configDirectory = null)
        {
            _logger = LoggingConfiguration.GetLogger<ConfigurationProvider>();
            _configDirectory = configDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TWF");

            _configFilePath = Path.Combine(_configDirectory, "config.json");
            _sessionStateFilePath = Path.Combine(_configDirectory, "session.json");
            _registeredFoldersFilePath = Path.Combine(_configDirectory, "registered_directory.json");

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

                // Only log startup information once
                if (!_startupLogged)
                {
                    _logger.LogInformation("ConfigurationProvider: Loading configuration from {Path}", path);
                    _logger.LogDebug("ConfigurationProvider: Raw JSON content contains LogLevel: {HasLogLevel}", json.Contains("LogLevel"));
                    _startupLogged = true; // Mark that startup logs have been shown
                }

                var config = JsonSerializer.Deserialize<Configuration>(json, _jsonOptions);

                if (config == null)
                {
                    _logger.LogWarning("ConfigurationProvider: Failed to deserialize configuration, using default");
                    config = CreateDefaultConfiguration();
                }
                else
                {
                    // Only log successful deserialization once at startup
                    if (!_startupLogged)
                    {
                        _logger.LogDebug("ConfigurationProvider: Successfully deserialized configuration, LogLevel: {LogLevel}", config.LogLevel);
                    }
                }

                // Load registered folders from separate file (with migration)
                config.RegisteredFolders = LoadRegisteredFolders(path);

                // Validate and apply defaults for any missing properties
                ValidateConfiguration(config);

                // Only log LogLevel information once at startup
                if (!_startupLogged)
                {
                    _logger.LogInformation("ConfigurationProvider: LogLevel set to: {LogLevel}", config.LogLevel);
                }

                return config;
            }
            catch (JsonException jsonEx)
            {
                var errorMsg = $"Error parsing configuration file {path}:\n{jsonEx.Message}";
                _logger.LogError(jsonEx, errorMsg);

                if (Application.Top != null)
                {
                    Application.MainLoop.Invoke(() => {
                        MessageBox.ErrorQuery("Configuration Error", errorMsg, "OK");
                    });
                }

                return CreateDefaultConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration: {Message}", ex.Message);
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
                _logger.LogError(ex, "Error saving configuration: {Message}", ex.Message);
                throw;
            }

            // Save registered folders separately
            SaveRegisteredFolders(config.RegisteredFolders);
        }

        /// <summary>
        /// Loads registered folders from separate file, with migration from config.json
        /// </summary>
        private List<RegisteredFolder> LoadRegisteredFolders(string configPath)
        {
            try
            {
                // 1. Try to load from the new separate file
                if (File.Exists(_registeredFoldersFilePath))
                {
                    var json = File.ReadAllText(_registeredFoldersFilePath);
                    return JsonSerializer.Deserialize<List<RegisteredFolder>>(json, _jsonOptions) ?? new List<RegisteredFolder>();
                }

                // 2. Migration: If new file doesn't exist, try to extract from config.json
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("RegisteredFolders", out JsonElement foldersElement))
                        {
                            var folders = JsonSerializer.Deserialize<List<RegisteredFolder>>(foldersElement.GetRawText(), _jsonOptions);
                            if (folders != null && folders.Count > 0)
                            {
                                // Migration successful, save to new file
                                SaveRegisteredFolders(folders);
                                return folders;
                            }
                        }
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                var errorMsg = $"Error parsing registered folders file:\n{jsonEx.Message}";
                _logger.LogError(jsonEx, errorMsg);

                if (Application.Top != null)
                {
                    Application.MainLoop.Invoke(() => {
                        MessageBox.ErrorQuery("Registered Folders Error", errorMsg, "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading registered folders: {Message}", ex.Message);
            }

            return new List<RegisteredFolder>();
        }

        /// <summary>
        /// Saves registered folders to separate file
        /// </summary>
        private void SaveRegisteredFolders(List<RegisteredFolder> folders)
        {
            try
            {
                var json = JsonSerializer.Serialize(folders, _jsonOptions);
                File.WriteAllText(_registeredFoldersFilePath, json);
                //Console.WriteLine($"Saved {folders.Count} registered folders to {_registeredFoldersFilePath}");
                _logger.LogDebug($"Saved {folders.Count} registered folders to {_registeredFoldersFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving registered folders: {Message}", ex.Message);
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
            catch (JsonException jsonEx)
            {
                var errorMsg = $"Error parsing session state file:\n{jsonEx.Message}";
                _logger.LogError(jsonEx, errorMsg);

                if (Application.Top != null)
                {
                    Application.MainLoop.Invoke(() => {
                        MessageBox.ErrorQuery("Session State Error", errorMsg, "OK");
                    });
                }
                return CreateDefaultSessionState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading session state: {Message}", ex.Message);
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
                _logger.LogError(ex, "Error saving session state: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a default configuration with sensible defaults
        /// </summary>
        private Configuration CreateDefaultConfiguration()
        {
            var config = new Configuration
            {
                Display = new DisplaySettings
                {
                    FontName = "Consolas",
                    FontSize = 12,
                    ForegroundColor = "White",
                    BackgroundColor = "Black",
                    HighlightForegroundColor = "Black",
                    HighlightBackgroundColor = "Cyan",
                    MarkedFileColor = "Cyan",
                    DirectoryColor = "BrightCyan",
                    DirectoryBackgroundColor = "Black",
                    FilenameLabelForegroundColor = "White",
                    FilenameLabelBackgroundColor = "Blue",
                    TopSeparatorForegroundColor = "White",
                    TopSeparatorBackgroundColor = "Black",
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
                ConfigurationProgramPath = OperatingSystem.IsWindows() ? "notepad.exe" : "vim",
                TextEditorPath = OperatingSystem.IsWindows() ? "notepad.exe" : "vim",
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
                    ShowLineNumbers = true,
                    TextViewerForegroundColor = "White",
                    TextViewerBackgroundColor = "Black",
                    TextViewerStatusForegroundColor = "Black",
                    TextViewerStatusBackgroundColor = "Gray",
                    TextViewerEncodingForegroundColor = "White",
                    TextViewerEncodingBackgroundColor = "Blue",
                    DefaultImageViewMode = ViewMode.FitToScreen,
                    SupportedImageExtensions = new List<string>
                    {
                        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico"
                    },
                    SupportedTextExtensions = new List<string>
                    {
                        ".txt", ".md", ".json", ".xml", ".cs", ".js", ".ts", ".html", ".css", ".ini", ".conf", ".log", ".bat", ".sh", ".ps1", ".cmd", ".cpp", ".h", ".c", ".py", ".rb", ".java", ".go", ".rs", ".php", ".yaml", ".yml", ".toml", ".gitignore", ".gitattributes", ".editorconfig", ".sln", ".csproj", ".fsproj", ".vbproj", ".props", ".targets", ".xaml", ".razor", ".svg", ".sql"
                    }
                },
                SaveSessionState = true,
                MaxHistoryItems = 50
            };

            // Only log default configuration creation once at startup
            if (!_startupLogged)
            {
                _logger.LogInformation("ConfigurationProvider: Creating default configuration with LogLevel: {DefaultLogLevel}", config.LogLevel);
            }
            return config;
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

            config.Viewer.SupportedTextExtensions ??= new List<string>
            {
                ".txt", ".md", ".json", ".xml", ".cs", ".js", ".ts", ".html", ".css", ".ini", ".conf", ".log", ".bat", ".sh", ".ps1", ".cmd", ".cpp", ".h", ".c", ".py", ".rb", ".java", ".go", ".rs", ".php", ".yaml", ".yml", ".toml", ".gitignore", ".gitattributes", ".editorconfig", ".sln", ".csproj", ".fsproj", ".vbproj", ".props", ".targets", ".xaml", ".razor", ".svg", ".sql"
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
