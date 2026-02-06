using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using TWF.Controllers;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    public abstract class BaseJumpDialog : Dialog
    {
        protected readonly MainController _controller;
        protected TextField _pathInput;
        protected ListView _suggestionList;
        protected Label _statusLabel;
        protected TextView _fullPathView;
        protected List<string> _currentPaths = new List<string>();
        protected CancellationTokenSource? _searchCts;
        protected HashSet<string> _ignoreFolders;
        
        protected int _spinnerIndex = 0;
        protected bool _isSearching = false;
        protected string _lastSearchQuery = "INIT_VALUE";
        protected int _lastSelectedIndex = -1;

        public string SelectedPath { get; private set; } = string.Empty;
        public bool IsOk { get; private set; }

        protected BaseJumpDialog(MainController controller, string title) : base(title, 66, 20)
        {
            _controller = controller;

            // Initialize Ignore List from Config
            var ignoreList = _controller.Config.Navigation.JumpIgnoreList ?? new List<string> { ".git" };
            _ignoreFolders = new HashSet<string>(ignoreList, StringComparer.OrdinalIgnoreCase);

            // Line 1: Search Status (Right-aligned)
            _statusLabel = new Label("")
            {
                X = Pos.AnchorEnd(12),
                Y = 1,
                Width = 10,
                TextAlignment = TextAlignment.Right
            };
            Add(_statusLabel);

            // Search/Input Field
            _pathInput = new TextField("")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(13)
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
                Height = Dim.Fill(6)
            };
            
            // Full Path Preview (Bottom)
            var pathSeparator = new LineView(Terminal.Gui.Graphs.Orientation.Horizontal)
            {
                X = 1,
                Y = Pos.Bottom(_suggestionList),
                Width = Dim.Fill(1)
            };
            Add(pathSeparator);

            _fullPathView = new TextView()
            {
                X = 1,
                Y = Pos.Bottom(pathSeparator),
                Width = Dim.Fill(1),
                Height = 4,
                ReadOnly = true,
                CanFocus = false,
                WordWrap = true
            };
            Add(_fullPathView);

            ApplyColors();
            
            // Custom rendering to show selection bar even when unfocused
            _suggestionList.RowRender += (e) => 
            {
                if (Application.Driver == null) return;

                if (e.Row == _suggestionList.SelectedItem)
                {
                    Application.Driver.SetAttribute(_suggestionList.ColorScheme.Focus);
                }
                else
                {
                    Application.Driver.SetAttribute(_suggestionList.ColorScheme.Normal);
                }
            };

            Add(_suggestionList);

            // Events
            _pathInput.TextChanged += (t) => TriggerSearch(_pathInput.Text?.ToString() ?? "");
            
            _suggestionList.SelectedItemChanged += (e) => 
            {
                if (_suggestionList.SelectedItem == _lastSelectedIndex) return;
                _lastSelectedIndex = _suggestionList.SelectedItem;

                if (_suggestionList.SelectedItem >= 0 && _suggestionList.SelectedItem < _currentPaths.Count)
                {
                    _fullPathView.Text = _currentPaths[_suggestionList.SelectedItem];
                }
                else
                {
                    _fullPathView.Text = "";
                }
            };

            // Handle navigation keys
            _pathInput.KeyPress += (e) =>
            {
                if (e.KeyEvent.Key == Key.CursorDown)
                {
                    _suggestionList.MoveDown();
                    e.Handled = true;
                }
                else if (e.KeyEvent.Key == Key.CursorUp)
                {
                    _suggestionList.MoveUp();
                    e.Handled = true;
                }
                else if (e.KeyEvent.Key == Key.PageDown)
                {
                    int pageSize = _suggestionList.Bounds.Height;
                    for (int i = 0; i < pageSize; i++) _suggestionList.MoveDown();
                    e.Handled = true;
                }
                else if (e.KeyEvent.Key == Key.PageUp)
                {
                    int pageSize = _suggestionList.Bounds.Height;
                    for (int i = 0; i < pageSize; i++) _suggestionList.MoveUp();
                    e.Handled = true;
                }
                else if (e.KeyEvent.Key == Key.Enter)
                {
                    SelectAndClose();
                    e.Handled = true;
                }
            };

            _suggestionList.OpenSelectedItem += (e) => SelectAndClose();
        }

        protected List<string> ParseTokens(string query)
        {
            var tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(query)) return tokens;

            var currentToken = new System.Text.StringBuilder();
            bool escaped = false;

            for (int i = 0; i < query.Length; i++)
            {
                char c = query[i];

                if (escaped)
                {
                    currentToken.Append(c);
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
                else
                {
                    currentToken.Append(c);
                }
            }

            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }

        protected virtual void ApplyColors()
        {
            if (Application.Driver == null) return;
            var display = _controller.Config.Display;
            var foreground = ColorHelper.ParseConfigColor(display.ForegroundColor, Color.White);
            var background = ColorHelper.ParseConfigColor(display.BackgroundColor, Color.Black);
            var highlightFg = ColorHelper.ParseConfigColor(display.HighlightForegroundColor, Color.Black);
            var highlightBg = ColorHelper.ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);
            
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);

            var dialogScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
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

            var inputFg = ColorHelper.ParseConfigColor(display.InputForegroundColor, Color.White);
            var inputBg = ColorHelper.ParseConfigColor(display.InputBackgroundColor, Color.Black);
            _pathInput.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(inputFg, inputBg),
                Focus = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotNormal = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotFocus = Application.Driver.MakeAttribute(inputFg, inputBg)
            };

            _statusLabel.ColorScheme = dialogScheme;

            var pathViewFg = ColorHelper.ParseConfigColor(display.FilenameLabelForegroundColor, Color.White);
            var pathViewBg = ColorHelper.ParseConfigColor(display.FilenameLabelBackgroundColor, Color.Black);
            _fullPathView.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(pathViewFg, pathViewBg),
                Focus = Application.Driver.MakeAttribute(pathViewFg, pathViewBg),
                HotNormal = Application.Driver.MakeAttribute(pathViewFg, pathViewBg),
                HotFocus = Application.Driver.MakeAttribute(pathViewFg, pathViewBg)
            };

        }

        protected virtual void SelectAndClose()
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
                string fallback = GetFallbackPath(input);
                if (!string.IsNullOrEmpty(fallback))
                {
                    SelectedPath = fallback;
                    IsOk = true;
                    Application.RequestStop();
                }
            }
        }

        protected string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            
            var sb = new System.Text.StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (!char.IsControl(c)) sb.Append(c);
            }
            var sanitized = sb.ToString();
            int maxLength = _controller.Config.Navigation.MaxPathInputLength;
            return sanitized.Length > maxLength ? sanitized.Substring(0, maxLength) : sanitized;
        }

        protected abstract List<string> GetSuggestions(string query, CancellationToken token);
        protected abstract string GetFallbackPath(string input);

        protected void TriggerSearch(string query)
        {
            string cleanQuery = SanitizeInput(query);
            
            if (cleanQuery == _lastSearchQuery) return;
            _lastSearchQuery = cleanQuery;

            _searchCts?.Cancel();
            _isSearching = false; 

            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            _isSearching = true;
            Task.Run(async () => {
                while (_isSearching && !token.IsCancellationRequested) {
                    Application.MainLoop.Invoke(() => {
                        _statusLabel.Text = CharacterWidthHelper.SpinnerFrames[_spinnerIndex];
                        _spinnerIndex = (_spinnerIndex + 1) % CharacterWidthHelper.SpinnerFrames.Length;
                    });
                    await Task.Delay(100);
                }
            });

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(100, token);
                    if (token.IsCancellationRequested) return;

                    var results = GetSuggestions(cleanQuery, token);
                    
                    var displayItems = new List<string>(results.Count);
                    foreach (var p in results) displayItems.Add(CharacterWidthHelper.SmartTruncate(p, 60));

                    Application.MainLoop.Invoke(() =>
                    {
                        if (token.IsCancellationRequested) return; 
                        _isSearching = false; 
                        
                        _currentPaths = results;
                        _suggestionList.SetSource(displayItems);
                        
                        int newHeight = Math.Max(1, Math.Min(_currentPaths.Count, 10));
                        _suggestionList.Height = newHeight;

                        if (_currentPaths.Count > 0)
                        {
                            _suggestionList.SelectedItem = 0;
                            _lastSelectedIndex = 0; 
                            _statusLabel.Text = $"{_currentPaths.Count}";
                            _fullPathView.Text = _currentPaths[0];
                        }
                        else
                        {
                            _statusLabel.Text = "No match";
                            _fullPathView.Text = "";
                        }
                    });
                }
                catch (OperationCanceledException) { _isSearching = false; }
                catch (Exception) { _isSearching = false; }
            }, token);
        }
    }
}