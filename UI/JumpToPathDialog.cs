using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using TWF.Controllers;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    public class JumpToPathDialog : Dialog
    {
        private readonly MainController _controller;
        private TextField _pathInput;
        private ListView _suggestionList;
        private List<string> _currentPaths = new List<string>();
        private CancellationTokenSource? _searchCts;

        public string SelectedPath { get; private set; } = string.Empty;
        public bool IsOk { get; private set; }

        public JumpToPathDialog(MainController controller) : base("Jump to Directory", 60, 15)
        {
            _controller = controller;

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
            
            // Handle navigation keys in the text field to control the list
            _pathInput.KeyPress += (e) =>
            {
                if (e.KeyEvent.Key == Key.CursorDown)
                {
                    _suggestionList.MoveDown();
                    // Force redraw to update highlighting
                    _suggestionList.SetNeedsDisplay();
                    e.Handled = true;
                }
                else if (e.KeyEvent.Key == Key.CursorUp)
                {
                    _suggestionList.MoveUp();
                    // Force redraw to update highlighting
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

            // Initial search to populate history/bookmarks
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
                // Fallback: If user typed a path that isn't in the list but hit enter
                string input = SanitizeInput(_pathInput.Text.ToString()!);
                try
                {
                    string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(input);
                    if (Directory.Exists(expanded))
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
            // Remove control characters and ensure length limit to prevent regex/path overload
            var sanitized = new string(input.Where(c => !char.IsControl(c)).ToArray());
            return sanitized.Length > 255 ? sanitized.Substring(0, 255) : sanitized;
        }

        private void TriggerSearch(string query)
        {
            _searchCts?.Cancel();
            
            // Sanitize input to prevent crashes from pasted text containing newlines or being too long
            string cleanQuery = SanitizeInput(query);
            
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    // Debounce: Wait 100ms. If cancelled during this time, we abort.
                    // This prevents rapid, partial queries (especially from pasting) from overwhelming the search/Migemo
                    await Task.Delay(100, token);
                    if (token.IsCancellationRequested) return;

                    var results = GetSuggestions(cleanQuery, token);

                    Application.MainLoop.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return; 
                        
                        _currentPaths = results;
                        _suggestionList.SetSource(_currentPaths);
                        
                        // Resize list to match content height (max 10) to prevent color leak in empty space
                        // This ensures the ListView doesn't draw "blank" lines with the "Selected" color attribute
                        int newHeight = Math.Max(1, Math.Min(_currentPaths.Count, 10));
                        _suggestionList.Height = newHeight;

                        if (_currentPaths.Count > 0)
                        {
                            _suggestionList.SelectedItem = 0;
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                }
                catch (Exception)
                {
                    // Log error if possible, but don't crash UI
                }
            }, token);
        }

        private List<string> GetSuggestions(string query, CancellationToken token)
        {
            var results = new List<string>();
            try
            {
                var uniqueSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Data Sources
                var bookmarks = _controller.Config.RegisteredFolders
                    .Select(b => EnvironmentVariableExpander.ExpandEnvironmentVariables(b.Path))
                    .Where(p => !string.IsNullOrWhiteSpace(p));

                var history = _controller.HistoryManager.LeftHistory
                    .Concat(_controller.HistoryManager.RightHistory)
                    .Where(p => !string.IsNullOrWhiteSpace(p));

                // 2. Empty Query -> Show History + Bookmarks
                if (string.IsNullOrWhiteSpace(query))
                {
                    foreach (var path in bookmarks)
                    {
                        if (uniqueSet.Add(path)) results.Add(path);
                    }
                    foreach (var path in history)
                    {
                        if (uniqueSet.Add(path)) results.Add(path);
                    }
                    return results;
                }

                string expandedQuery = EnvironmentVariableExpander.ExpandEnvironmentVariables(query);

                // 3. File System Search (if it looks like a path)
                if (expandedQuery.Contains(Path.DirectorySeparatorChar) || expandedQuery.Contains(Path.AltDirectorySeparatorChar) || (expandedQuery.Length >= 2 && expandedQuery[1] == ':'))
                {
                    try
                    {
                        string? dir = null;
                        string filePattern = "";

                        if (Directory.Exists(expandedQuery))
                        {
                            dir = expandedQuery;
                            filePattern = "";
                        }
                        else
                        {
                            dir = Path.GetDirectoryName(expandedQuery);
                            filePattern = Path.GetFileName(expandedQuery);
                        }
                        
                        // Handle root paths like "C:\" or "/"
                        if (string.IsNullOrEmpty(dir) && Path.IsPathRooted(expandedQuery))
                        {
                            dir = expandedQuery;
                            filePattern = "";
                        }

                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            var opts = new EnumerationOptions { IgnoreInaccessible = true };
                            var dirs = Directory.GetDirectories(dir, filePattern + "*", opts).Take(50);
                            
                            foreach (var d in dirs)
                            {
                                if (uniqueSet.Add(d)) results.Add(d);
                            }
                        }
                    }
                    catch { }
                }

                // 4. Fuzzy Search on History/Bookmarks
                var staticPaths = bookmarks.Concat(history).Distinct();
                foreach (var path in staticPaths)
                {
                    token.ThrowIfCancellationRequested();
                    if (uniqueSet.Contains(path)) continue;

                    try
                    {
                        // Use SearchEngine for smart matching (supports Migemo)
                        if (_controller.SearchEngine.IsMatch(path, expandedQuery))
                        {
                            if (uniqueSet.Add(path)) results.Add(path);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception)
            {
                // Failsafe return empty list
            }

            return results.Take(100).ToList();
        }
    }
}