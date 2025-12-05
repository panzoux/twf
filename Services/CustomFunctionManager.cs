using System.Text.Json;
using TWF.Models;
using Microsoft.Extensions.Logging;

namespace TWF.Services
{
    /// <summary>
    /// Manages custom user-defined functions with macro expansion
    /// </summary>
    public class CustomFunctionManager
    {
        private readonly MacroExpander _macroExpander;
        private readonly ILogger<CustomFunctionManager>? _logger;
        private CustomFunctionsConfig? _config;

        public CustomFunctionManager(MacroExpander macroExpander, ILogger<CustomFunctionManager>? logger = null)
        {
            _macroExpander = macroExpander ?? throw new ArgumentNullException(nameof(macroExpander));
            _logger = logger;
        }

        /// <summary>
        /// Loads custom functions from a JSON file
        /// </summary>
        /// <param name="configPath">Path to custom_functions.json</param>
        public void LoadFunctions(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    _logger?.LogInformation("Custom functions file not found: {Path}, creating default", configPath);
                    CreateDefaultFunctionsFile(configPath);
                    LoadFunctions(configPath);
                    return;
                }

                var json = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<CustomFunctionsConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger?.LogInformation("Loaded {Count} custom functions from {Path}", _config?.Functions.Count ?? 0, configPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load custom functions from {Path}", configPath);
                _config = new CustomFunctionsConfig();
            }
        }

        /// <summary>
        /// Gets all available custom functions
        /// </summary>
        public List<CustomFunction> GetFunctions()
        {
            return _config?.Functions ?? new List<CustomFunction>();
        }

        /// <summary>
        /// Executes a custom function with macro expansion
        /// </summary>
        /// <param name="function">The function to execute</param>
        /// <param name="activePane">Active pane state</param>
        /// <param name="inactivePane">Inactive pane state</param>
        /// <param name="leftPane">Left pane state</param>
        /// <param name="rightPane">Right pane state</param>
        /// <returns>True if executed successfully, false if cancelled or failed</returns>
        public bool ExecuteFunction(CustomFunction function, PaneState activePane, PaneState inactivePane, PaneState leftPane, PaneState rightPane)
        {
            try
            {
                _logger?.LogInformation("Executing custom function: {Name}", function.Name);

                // Expand macros
                var expandedCommand = _macroExpander.ExpandMacros(function.Command, activePane, inactivePane, leftPane, rightPane);
                
                if (expandedCommand == null)
                {
                    _logger?.LogInformation("Custom function cancelled by user");
                    return false;
                }

                _logger?.LogDebug("Expanded command: {Command}", expandedCommand);

                // Execute the command
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {expandedCommand}",
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        _logger?.LogInformation("Custom function completed with exit code: {ExitCode}", process.ExitCode);
                        return process.ExitCode == 0;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing custom function: {Name}", function.Name);
                return false;
            }
        }

        /// <summary>
        /// Creates a default custom_functions.json file with examples
        /// </summary>
        private void CreateDefaultFunctionsFile(string configPath)
        {
            var defaultConfig = new CustomFunctionsConfig
            {
                Version = "1.0",
                Functions = new List<CustomFunction>
                {
                    new CustomFunction
                    {
                        Name = "Open in Notepad",
                        Command = "notepad \"$P\\$F\"",
                        Description = "Open current file in Notepad"
                    },
                    new CustomFunction
                    {
                        Name = "Copy to Other Pane",
                        Command = "cmd /c copy \"$P\\$F\" \"$O\\\"",
                        Description = "Copy current file to other pane"
                    },
                    new CustomFunction
                    {
                        Name = "Open Command Prompt Here",
                        Command = "cmd /k cd /d \"$P\"",
                        Description = "Open command prompt in current directory"
                    },
                    new CustomFunction
                    {
                        Name = "Custom Copy",
                        Command = "cmd /c copy \"$P\\$F\" \"$I\"Destination path\"\"",
                        Description = "Copy file to custom location (prompts for path)"
                    },
                    new CustomFunction
                    {
                        Name = "Batch Rename",
                        Command = "cmd /c ren \"$P\\$F\" \"$I\"New filename\"\"",
                        Description = "Rename current file (prompts for new name)"
                    }
                }
            };

            try
            {
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(configPath, json);
                _logger?.LogInformation("Created default custom functions file at {Path}", configPath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create default custom functions file");
            }
        }
    }
}
