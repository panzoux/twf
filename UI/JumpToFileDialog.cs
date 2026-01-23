using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using TWF.Controllers;
using TWF.Models;
using TWF.Utilities;

namespace TWF.UI
{
    public class JumpToFileDialog : Dialog
    {
        private readonly MainController _controller;
        private readonly string _rootPath;
        private TextField _pathInput;
        private ListView _suggestionList;
        private List<string> _currentPaths = new List<string>();
        private CancellationTokenSource? _searchCts;

        public string SelectedPath { get; private set; } = string.Empty;
        public bool IsOk { get; private set; }

        public JumpToFileDialog(MainController controller, string rootPath) : base("Jump to File", 60, 15)
        {
            _controller = controller;
            _rootPath = rootPath;

            // Search/Input Field
            _pathInput = new TextField("")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(_pathInput);

            // Separator
            Add(new LineView(Terminal.Gui.Graphs.Orientation.Horizontal)
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            });

            // Suggestions List
            _suggestionList = new ListView()
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1),
                Height = Dim.Fill(1)
            };
            
            ApplyColors();
            
            // Custom rendering to show selection even when list is not focused
            _suggestionList.RowRender += (e) => 
            {
                var display = _controller.Config.Display;
                if (e.Row == _suggestionList.SelectedItem)
                {
                    var highlightFg = ColorHelper.ParseConfigColor(display.HighlightForegroundColor, Color.Black);
                    var highlightBg = ColorHelper.ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);
                    Application.Driver.SetAttribute(Application.Driver.MakeAttribute(highlightFg, highlightBg));
                }
                else
                {
                    var foreground = ColorHelper.ParseConfigColor(display.ForegroundColor, Color.White);
                    var background = ColorHelper.ParseConfigColor(display.BackgroundColor, Color.Black);
                    Application.Driver.SetAttribute(Application.Driver.MakeAttribute(foreground, background));
                }
            };

            Add(_suggestionList);

            // Events
            _pathInput.TextChanged += (t) => TriggerSearch(_pathInput.Text?.ToString() ?? "");
            
            // Handle navigation keys
            _pathInput.KeyPress += (e) =>
            {
                if (e.KeyEvent.Key == Key.CursorDown)
                {
                    _suggestionList.MoveDown();
                    _suggestionList.SetNeedsDisplay();
                    e.Handled = true;
                }
                else if (e.KeyEvent.Key == Key.CursorUp)
                {
                    _suggestionList.MoveUp();
                    _suggestionList.SetNeedsDisplay();
                    e.Handled = true;
                }
                else if (e.KeyEvent.Key == Key.Enter)
                {
                    SelectAndClose();
                    e.Handled = true;
                }
            };

            _suggestionList.OpenSelectedItem += (e) => SelectAndClose();

            // Initial search
            TriggerSearch("");
        }

        private void ApplyColors()
        {
            var display = _controller.Config.Display;
            var foreground = ColorHelper.ParseConfigColor(display.ForegroundColor, Color.White);
            var background = ColorHelper.ParseConfigColor(display.BackgroundColor, Color.Black);
            var highlightFg = ColorHelper.ParseConfigColor(display.HighlightForegroundColor, Color.Black);
            var highlightBg = ColorHelper.ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);
            
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);

            // Apply to Dialog Frame/Body
            var dialogScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };
            this.ColorScheme = dialogScheme;

            var listScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(foreground, background),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                HotNormal = Application.Driver.MakeAttribute(foreground, background),
                HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
            };
            
            _suggestionList.ColorScheme = listScheme;

            // Apply Input Colors to TextField
            var inputFg = ColorHelper.ParseConfigColor(display.InputForegroundColor, Color.White);
            var inputBg = ColorHelper.ParseConfigColor(display.InputBackgroundColor, Color.Black);
            _pathInput.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(inputFg, inputBg),
                Focus = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotNormal = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotFocus = Application.Driver.MakeAttribute(inputFg, inputBg)
            };
        }

        private void SelectAndClose()
        {
            if (_suggestionList.SelectedItem >= 0 && _suggestionList.SelectedItem < _currentPaths.Count)
            {
                SelectedPath = _currentPaths[_suggestionList.SelectedItem];
                IsOk = true;
                Application.RequestStop();
            }
            else if (!string.IsNullOrWhiteSpace(_pathInput.Text?.ToString()))
            {
                string input = SanitizeInput(_pathInput.Text.ToString()!);
                try
                {
                    string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(input);
                    // Resolve relative path against root if needed
                    if (!Path.IsPathRooted(expanded))
                    {
                        expanded = Path.Combine(_rootPath, expanded);
                    }

                    if (File.Exists(expanded))
                    {
                        SelectedPath = expanded;
                        IsOk = true;
                        Application.RequestStop();
                    }
                }
                catch { }
            }
        }

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var sanitized = new string(input.Where(c => !char.IsControl(c)).ToArray());
            return sanitized.Length > 255 ? sanitized.Substring(0, 255) : sanitized;
        }

        private void TriggerSearch(string query)
        {
            _searchCts?.Cancel();
            string cleanQuery = SanitizeInput(query);
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    // Debounce
                    await Task.Delay(100, token);
                    if (token.IsCancellationRequested) return;

                    var results = GetSuggestions(cleanQuery, token);

                    Application.MainLoop.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return; 
                        
                        _currentPaths = results;
                        _suggestionList.SetSource(_currentPaths);
                        
                        int newHeight = Math.Max(1, Math.Min(_currentPaths.Count, 10));
                        _suggestionList.Height = newHeight;

                        if (_currentPaths.Count > 0)
                        {
                            _suggestionList.SelectedItem = 0;
                        }
                    });
                }
                catch (OperationCanceledException) { }
                catch (Exception) { }
            }, token);
        }

        private List<string> GetSuggestions(string query, CancellationToken token)
        {
            var results = new List<string>();
            try
            {
                string searchPath = _rootPath;
                string pattern = "*";
                bool recursive = true;

                int maxDepth = _controller.Config.Navigation.JumpToFileSearchDepth;
                int maxResults = _controller.Config.Navigation.JumpToFileMaxResults;

                string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(query);

                // If input looks like a path, switch to that path
                bool isPath = expanded.Contains(Path.DirectorySeparatorChar) || expanded.Contains(Path.AltDirectorySeparatorChar) || (expanded.Length >= 2 && expanded[1] == ':');
                
                if (isPath)
                {
                    string? dir = null;
                    if (Directory.Exists(expanded))
                    {
                        dir = expanded;
                        pattern = "*";
                    }
                    else
                    {
                        dir = Path.GetDirectoryName(expanded);
                        pattern = Path.GetFileName(expanded) + "*";
                    }

                    // Handle root
                    if (string.IsNullOrEmpty(dir) && Path.IsPathRooted(expanded))
                    {
                        dir = expanded;
                        pattern = "*";
                    }

                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        searchPath = dir;
                        recursive = false; // Don't recurse if user is specifying a path
                    }
                    else
                    {
                        // Path part invalid, treat as simple search in root?
                        // Or just return empty?
                        // Let's assume user is typing a path that doesn't exist yet or is mistyped
                        // Try to find if the text is a filename pattern in the current root
                        if (!Path.IsPathRooted(expanded))
                        {
                            pattern = "*" + expanded + "*";
                        }
                        else
                        {
                            return results;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(query))
                {
                    pattern = "*" + query + "*";
                }

                // Perform search
                var opts = new EnumerationOptions 
                { 
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = recursive,
                    MaxRecursionDepth = recursive ? maxDepth : 0 
                };

                // Use simple enumeration. For advanced filtering (Migemo), we might need to fetch more and filter in memory.
                // But Directory.EnumerateFiles uses OS matching.
                // If query is text, we used *query* which is simple wildcard.
                // Migemo won't work well with OS globbing.
                // If we want Migemo, we should list * all files (up to limit/depth) and filter in memory.
                
                if (!isPath && !string.IsNullOrEmpty(query))
                {
                    // In-memory filter for Migemo/Fuzzy
                    var allFiles = Directory.EnumerateFiles(searchPath, "*", opts);
                    int count = 0;
                    foreach (var f in allFiles)
                    {
                        token.ThrowIfCancellationRequested();
                        string name = Path.GetFileName(f);
                        if (_controller.SearchEngine.IsMatch(name, expanded))
                        {
                            results.Add(f);
                            count++;
                            if (count >= maxResults) break;
                        }
                    }
                }
                else
                {
                    // Standard path search or empty query
                    var files = Directory.EnumerateFiles(searchPath, pattern, opts).Take(maxResults);
                    foreach (var f in files)
                    {
                        token.ThrowIfCancellationRequested();
                        results.Add(f);
                    }
                }
            }
            catch (Exception) { }

            return results;
        }
    }
}
