using System;
using System.Diagnostics;
using System.IO;
using Terminal.Gui;
using Microsoft.Extensions.Logging;
using TWF.Infrastructure;

namespace TWF.Services
{
    /// <summary>
    /// Handles launching external applications like text editors and image viewers.
    /// </summary>
    public class ExternalAppLauncher
    {
        private readonly ILogger<ExternalAppLauncher> _logger;

        public ExternalAppLauncher(ILogger<ExternalAppLauncher>? logger = null)
        {
            _logger = logger ?? LoggingConfiguration.GetLogger<ExternalAppLauncher>();
        }

        /// <summary>
        /// Gets the default editor command from environment variables or OS defaults.
        /// </summary>
        public string GetEditorCommand()
        {
            var editor = Environment.GetEnvironmentVariable("VISUAL")
                         ?? Environment.GetEnvironmentVariable("EDITOR");
            if (!string.IsNullOrWhiteSpace(editor)) return editor;
            return OperatingSystem.IsWindows() ? "notepad.exe" : "vim";
        }

        /// <summary>
        /// Launches an application and optionally waits for it to exit.
        /// Suspends Terminal.Gui if wait is true (typical for CLI editors).
        /// </summary>
        /// <param name="appPath">Path to the executable or command.</param>
        /// <param name="filePath">Path to the file to open.</param>
        /// <param name="wait">If true, suspends the UI and waits for the process to exit.</param>
        /// <returns>Exit code if wait is true, otherwise 0 if started successfully.</returns>
        public int LaunchApp(string appPath, string filePath, bool wait = false)
        {
            if (string.IsNullOrWhiteSpace(appPath))
            {
                // Fallback to default shell association if no app is specified
                return LaunchWithShellExecute(filePath);
            }

            // Normalize app path: handle legacy "notepad.exe" defaults on non-Windows
            if (!OperatingSystem.IsWindows() && 
                (appPath.EndsWith("notepad.exe", StringComparison.OrdinalIgnoreCase) || 
                 appPath.Equals("notepad", StringComparison.OrdinalIgnoreCase)))
            {
                appPath = wait ? "vim" : "xdg-open"; // Rough fallbacks
            }

            string prog;
            string args;
            
            // Handle cases where appPath might contain arguments
            var parts = appPath.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            prog = parts[0];
            args = parts.Length > 1 ? parts[1] : "";
            
            // Append file path
            args = string.IsNullOrEmpty(args) ? Quote(filePath) : args + " " + Quote(filePath);

            if (wait)
            {
                return LaunchAndWait(prog, args);
            }
            else
            {
                return LaunchAsynchronous(prog, args);
            }
        }

        private int LaunchAndWait(string prog, string args)
        {
            int exitCode = -1;
            
            // Suspend Terminal.Gui to restore terminal for the external app
            if (Application.Driver != null)
            {
                Application.Driver.Suspend();
                Console.WriteLine($"Launching {prog}...");
                Console.WriteLine("Waiting for application to close...");
            }

            try {
                _logger.LogInformation("Launching external app (blocking): {Prog} {Args}", prog, args);
                using var p = new Process();
                p.StartInfo.FileName = prog;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false; 
                
                p.Start();
                p.WaitForExit();
                exitCode = p.ExitCode;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to start application '{Prog}'", prog);
                Console.WriteLine($"Failed to start application '{prog}': {ex.Message}");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            } finally {
                if (Application.Driver != null)
                {
                    Application.Refresh();
                }
            }
            
            return exitCode;
        }

        private int LaunchAsynchronous(string prog, string args)
        {
            try {
                _logger.LogInformation("Launching external app (async): {Prog} {Args}", prog, args);
                var startInfo = new ProcessStartInfo
                {
                    FileName = prog,
                    Arguments = args,
                    UseShellExecute = true // Usually better for GUI apps
                };
                
                Process.Start(startInfo);
                return 0;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to start application (async) '{Prog}'", prog);
                return -1;
            }
        }

        private int LaunchWithShellExecute(string filePath)
        {
            try {
                _logger.LogInformation("Launching file with shell association: {FilePath}", filePath);
                var startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(startInfo);
                return 0;
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to open file with shell execute: {FilePath}", filePath);
                return -1;
            }
        }

        private static string Quote(string s) => s.Contains(' ') ? $"\"{s}\"" : s;

        /// <summary>
        /// Launches an application in the background and calls onExit when it finishes.
        /// Does not suspend Terminal.Gui.
        /// </summary>
        public void LaunchBackground(string appPath, string filePath, Action onExit)
        {
            string prog;
            string args;

            // Handle legacy notepad defaults
            if (!OperatingSystem.IsWindows() && (appPath == "notepad.exe" || appPath == "notepad"))
            {
                appPath = "xdg-open";
            }

            var parts = appPath.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            prog = parts[0];
            args = parts.Length > 1 ? parts[1] : "";
            args = string.IsNullOrEmpty(args) ? Quote(filePath) : args + " " + Quote(filePath);

            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Launching external app (background): {Prog} {Args}", prog, args);
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = prog,
                        Arguments = args,
                        UseShellExecute = true
                    };

                    using var p = Process.Start(startInfo);
                    if (p != null)
                    {
                        p.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start background application '{Prog}'", prog);
                }
                finally
                {
                    onExit?.Invoke();
                }
            });
        }
    }
}
