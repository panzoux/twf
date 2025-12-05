using Terminal.Gui;
using TWF.Services;
using TWF.Models;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TWF.UI
{
    /// <summary>
    /// Text viewer window for displaying file contents with encoding support
    /// </summary>
    public class TextViewerWindow : Window
    {
        private readonly TextViewer _textViewer;
        private readonly KeyBindingManager _keyBindings;
        private readonly Configuration? _configuration;
        private readonly ILogger<TextViewerWindow>? _logger;
        private TextView _textView = null!;
        private Label _statusLabel = null!;
        private Label _encodingLabel = null!;
        private string _searchPattern = string.Empty;
        private List<int> _searchMatches = new List<int>();
        private int _currentMatchIndex = -1;
        private bool _isHexMode = false;

        /// <summary>
        /// Initializes a new instance of TextViewerWindow
        /// </summary>
        /// <param name="textViewer">The text viewer instance containing the file data</param>
        /// <param name="keyBindings">The key binding manager for handling keyboard shortcuts</param>
        /// <param name="configuration">Optional configuration for colors</param>
        /// <param name="logger">Optional logger for diagnostic information</param>
        /// <param name="startInHexMode">If true, starts in hex mode instead of text mode</param>
        public TextViewerWindow(TextViewer textViewer, KeyBindingManager keyBindings, Configuration? configuration = null, ILogger<TextViewerWindow>? logger = null, bool startInHexMode = false) : base(startInHexMode ? "Binary Viewer" : "Text Viewer")
        {
            _textViewer = textViewer ?? throw new ArgumentNullException(nameof(textViewer));
            _keyBindings = keyBindings ?? throw new ArgumentNullException(nameof(keyBindings));
            _configuration = configuration;
            _logger = logger;
            _isHexMode = startInHexMode;
            
            InitializeComponents();
            LoadContent();
            SetupKeyHandlers();
        }

        /// <summary>
        /// Initializes UI components
        /// </summary>
        private void InitializeComponents()
        {
            // Set window properties
            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();
            Modal = true;

            // Get colors from configuration
            var textFg = ParseColor(_configuration?.Viewer.TextViewerForegroundColor, Color.White);
            var textBg = ParseColor(_configuration?.Viewer.TextViewerBackgroundColor, Color.Black);
            var statusFg = ParseColor(_configuration?.Viewer.TextViewerStatusForegroundColor, Color.White);
            var statusBg = ParseColor(_configuration?.Viewer.TextViewerStatusBackgroundColor, Color.Gray);
            var encodingFg = ParseColor(_configuration?.Viewer.TextViewerEncodingForegroundColor, Color.White);
            var encodingBg = ParseColor(_configuration?.Viewer.TextViewerEncodingBackgroundColor, Color.Blue);

            // Create text view for displaying file contents
            _textView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2), // Leave room for status and encoding labels
                ReadOnly = true,
                WordWrap = false
            };
            
            // Set text view colors
            if (Application.Driver != null)
            {
                _textView.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(textFg, textBg),
                    Focus = Application.Driver.MakeAttribute(textFg, textBg)
                };
            }
            Add(_textView);

            // Create status label (second to last line)
            _statusLabel = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(),
                Height = 1,
                Text = $"File: {Path.GetFileName(_textViewer.FilePath)} | Lines: {_textViewer.LineCount} | Encoding: {_textViewer.CurrentEncoding.EncodingName}"
            };
            
            // Set status label colors
            if (Application.Driver != null)
            {
                _statusLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(statusFg, statusBg)
                };
            }
            Add(_statusLabel);

            // Create encoding label (last line)
            _encodingLabel = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(0),
                Width = Dim.Fill(),
                Height = 1,
                Text = $"Encoding: {_textViewer.CurrentEncoding.EncodingName} | Shift+E: Change Encoding | F4: Search | Esc/Enter: Close"
            };
            
            // Set encoding label colors
            if (Application.Driver != null)
            {
                _encodingLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(encodingFg, encodingBg)
                };
            }
            Add(_encodingLabel);
        }

        /// <summary>
        /// Loads file content into the text view with line numbers
        /// </summary>
        private void LoadContent()
        {
            if (_isHexMode)
            {
                LoadContentAsHex();
            }
            else
            {
                LoadContentAsText();
            }
        }

        /// <summary>
        /// Loads file content as text with line numbers
        /// </summary>
        private void LoadContentAsText()
        {
            var contentBuilder = new StringBuilder();
            
            // Calculate the width needed for line numbers
            int lineNumberWidth = _textViewer.LineCount.ToString().Length;
            
            // Build content with line numbers
            for (int i = 0; i < _textViewer.LineCount; i++)
            {
                string lineNumber = (i + 1).ToString().PadLeft(lineNumberWidth);
                string line = _textViewer.GetLine(i);
                contentBuilder.AppendLine($"{lineNumber} | {line}");
            }
            
            _textView.Text = contentBuilder.ToString();
        }

        /// <summary>
        /// Loads file content as hexadecimal dump
        /// </summary>
        private void LoadContentAsHex()
        {
            var contentBuilder = new StringBuilder();
            
            try
            {
                // Get all bytes from the file
                byte[] allBytes = _textViewer.GetAllBytes();
                
                if (allBytes.Length == 0)
                {
                    contentBuilder.AppendLine("(Empty file)");
                }
                else
                {
                    // Process bytes in chunks of 16
                    int offset = 0;
                    while (offset < allBytes.Length)
                    {
                        // Get up to 16 bytes for this line
                        int bytesToRead = Math.Min(16, allBytes.Length - offset);
                        byte[] lineBytes = new byte[bytesToRead];
                        Array.Copy(allBytes, offset, lineBytes, 0, bytesToRead);
                        
                        // Format and add the hex line
                        contentBuilder.AppendLine(FormatHexLine(offset, lineBytes));
                        
                        offset += bytesToRead;
                    }
                }
                
                _textView.Text = contentBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading file in hex mode: {Message}", ex.Message);
                contentBuilder.AppendLine($"Error loading hex view: {ex.Message}");
                _textView.Text = contentBuilder.ToString();
            }
        }

        /// <summary>
        /// Sets up key event handlers using KeyBindingManager
        /// </summary>
        private void SetupKeyHandlers()
        {
            // Note: We override ProcessKey instead of using KeyPress event
            // because TextView consumes keys before they bubble up to KeyPress
        }
        
        /// <summary>
        /// Processes keyboard input before child controls
        /// </summary>
        public override bool ProcessKey(KeyEvent keyEvent)
        {
            try
            {
                // Convert key to string representation
                string keyString = ConvertKeyToString(keyEvent.Key);
                _logger?.LogDebug("TextViewer key pressed: {Key} (Raw: {RawKey}, KeyValue: {KeyValue}, Char: '{Char}')", 
                    keyString, keyEvent.Key, (int)keyEvent.Key, (char)(keyEvent.Key & ~(Key.ShiftMask | Key.CtrlMask | Key.AltMask)));
                
                // Get action from KeyBindingManager for TextViewer mode
                string? action = _keyBindings.GetActionForKey(keyString, UiMode.TextViewer);
                
                if (action != null)
                {
                    _logger?.LogDebug("Found custom binding for key '{Key}': {Action}", keyString, action);
                    if (ExecuteTextViewerAction(action))
                    {
                        return true; // Key handled, don't pass to child controls
                    }
                }
                else
                {
                    // Fall back to default hardcoded bindings if no custom binding exists
                    _logger?.LogDebug("No custom binding found for key '{Key}', falling back to default bindings", keyString);
                    if (ExecuteDefaultBinding(keyEvent.Key))
                    {
                        return true; // Key handled, don't pass to child controls
                    }
                }
                
                _logger?.LogDebug("No binding found for key: {Key}, passing to base", keyString);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling key press in TextViewer: {Message}", ex.Message);
                UpdateStatusLabel($"Key handling error: {ex.Message}");
            }
            
            // Let base class and child controls handle the key
            return base.ProcessKey(keyEvent);
        }
        
        /// <summary>
        /// Executes default hardcoded bindings as fallback
        /// </summary>
        private bool ExecuteDefaultBinding(Key key)
        {
            // Handle F5 - go to top of file
            if (key == Key.F5)
            {
                GoToFileTop();
                return true;
            }
            // Handle F6 - go to bottom of file
            else if (key == Key.F6)
            {
                GoToFileBottom();
                return true;
            }
            // Handle Shift+E for encoding cycling
            // Terminal.Gui sends uppercase 'E' (KeyValue == 'E') when Shift+E is pressed
            else if (key == Key.E || key == (Key)'E')
            {
                CycleEncoding();
                return true;
            }
            // Handle F7 for Cycle Encoding
            else if (key == Key.F7)
            {
                CycleEncoding();
                return true;
            }
            // Handle F4 for search
            else if (key == Key.F4)
            {
                ShowSearchDialog();
                return true;
            }
            // Handle Escape or Enter to close
            else if (key == Key.Esc || key == Key.Enter)
            {
                CloseViewer();
                return true;
            }
            // Handle F3 or Ctrl+G for find next
            else if (key == Key.F3 || key == (Key.G | Key.CtrlMask))
            {
                FindNext();
                return true;
            }
            // Handle Shift+F3 for find previous
            else if (key == (Key.F3 | Key.ShiftMask))
            {
                FindPrevious();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Converts a Terminal.Gui Key to a string representation for key binding lookup
        /// </summary>
        private string ConvertKeyToString(Key key)
        {
            var parts = new List<string>();
            bool hasShift = false;
            
            // Check for modifiers
            if ((key & Key.ShiftMask) == Key.ShiftMask)
            {
                hasShift = true;
                parts.Add("Shift");
            }
            if ((key & Key.CtrlMask) == Key.CtrlMask)
                parts.Add("Ctrl");
            if ((key & Key.AltMask) == Key.AltMask)
                parts.Add("Alt");
            
            // Get the base key (remove modifiers)
            Key baseKey = key & ~(Key.ShiftMask | Key.CtrlMask | Key.AltMask);
            
            // Check if this is a lowercase letter (will be converted to uppercase for binding lookup)
            bool isLowercaseLetter = baseKey >= (Key)'a' && baseKey <= (Key)'z';
            
            // Convert base key to string
            string keyName = baseKey switch
            {
                Key.Enter => "Enter",
                Key.Backspace => "Backspace",
                Key.Tab => "Tab",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.CursorUp => "Up",
                Key.CursorDown => "Down",
                Key.CursorLeft => "Left",
                Key.CursorRight => "Right",
                Key.Space => "Space",
                (Key)27 => "Escape",
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                Key.D0 => "0",
                _ => baseKey >= Key.A && baseKey <= Key.Z ? ((char)baseKey).ToString() :
                     baseKey >= (Key)'a' && baseKey <= (Key)'z' ? ((char)baseKey).ToString().ToUpper() :
                     ((char)baseKey).ToString()
            };
            
            // Handle uppercase letters as Shift+Letter
            // Terminal.Gui sends uppercase letters WITHOUT ShiftMask when Shift is pressed
            // But don't add Shift if this was originally a lowercase letter
            if (baseKey >= Key.A && baseKey <= Key.Z && !hasShift && parts.Count == 0 && !isLowercaseLetter)
            {
                // This is an uppercase letter without explicit Shift modifier
                // Add Shift to the parts
                parts.Insert(0, "Shift");
            }
            
            parts.Add(keyName);
            
            return string.Join("+", parts);
        }
        
        /// <summary>
        /// Executes a TextViewer action by name
        /// </summary>
        private bool ExecuteTextViewerAction(string actionName)
        {
            try
            {
                _logger?.LogDebug("Executing TextViewer action: {Action}", actionName);
                
                switch (actionName)
                {
                    case "TextViewer.GoToTop":
                    case "TextViewer.GoToFileTop":
                        GoToTop();
                        return true;
                        
                    case "TextViewer.GoToBottom":
                    case "TextViewer.GoToFileBottom":
                        GoToBottom();
                        return true;
                        
                    case "TextViewer.GoToLineStart":
                        // Let TextView handle this naturally
                        return false;
                        
                    case "TextViewer.GoToLineEnd":
                        // Let TextView handle this naturally
                        return false;
                        
                    case "TextViewer.PageUp":
                        PageUp();
                        return true;
                        
                    case "TextViewer.PageDown":
                        PageDown();
                        return true;
                        
                    case "TextViewer.Close":
                        Close();
                        return true;
                        
                    case "TextViewer.Search":
                        Search();
                        return true;
                        
                    case "TextViewer.FindNext":
                        FindNext();
                        return true;
                        
                    case "TextViewer.FindPrevious":
                        FindPrevious();
                        return true;
                        
                    case "TextViewer.CycleEncoding":
                        CycleEncoding();
                        return true;
                        
                    case "TextViewer.ToggleHexMode":
                        ToggleHexMode();
                        return true;
                        
                    default:
                        _logger?.LogWarning("Unknown TextViewer action '{Action}' - action ignored", actionName);
                        UpdateStatusLabel($"Unknown action: {actionName}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing TextViewer action '{Action}': {Message}", actionName, ex.Message);
                UpdateStatusLabel($"Error executing {actionName}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Scrolls to the top of the file (F5)
        /// </summary>
        private void GoToFileTop()
        {
            try
            {
                _textView.CursorPosition = new Point(0, 0);
                _textViewer.ScrollTo(0);
                _logger?.LogDebug("Navigated to top of file");
                UpdateStatusLabel("Top of file");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to top of file: {Message}", ex.Message);
                UpdateStatusLabel($"Navigation error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Scrolls to the bottom of the file (F6)
        /// </summary>
        private void GoToFileBottom()
        {
            try
            {
                int lastLine = Math.Max(0, _textViewer.LineCount - 1);
                _textView.CursorPosition = new Point(0, lastLine);
                _textViewer.ScrollTo(lastLine);
                _logger?.LogDebug("Navigated to bottom of file (line {Line})", lastLine + 1);
                UpdateStatusLabel("Bottom of file");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to bottom of file: {Message}", ex.Message);
                UpdateStatusLabel($"Navigation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Action method: Scrolls to the top of the file
        /// </summary>
        private void GoToTop()
        {
            GoToFileTop();
        }

        /// <summary>
        /// Action method: Scrolls to the bottom of the file
        /// </summary>
        private void GoToBottom()
        {
            GoToFileBottom();
        }

        /// <summary>
        /// Action method: Scrolls up one page
        /// </summary>
        private void PageUp()
        {
            try
            {
                // Get the current cursor position
                var currentPos = _textView.CursorPosition;
                
                // Calculate page size (height of text view minus 1 for overlap)
                int pageSize = Math.Max(1, _textView.Frame.Height - 1);
                
                // Calculate new line position
                int newLine = Math.Max(0, currentPos.Y - pageSize);
                
                // Update cursor position
                _textView.CursorPosition = new Point(currentPos.X, newLine);
                _textViewer.ScrollTo(newLine);
                
                _logger?.LogDebug("Page up to line {Line}", newLine + 1);
                UpdateStatusLabel($"Page up - Line {newLine + 1}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during page up: {Message}", ex.Message);
                UpdateStatusLabel($"Page up error: {ex.Message}");
            }
        }

        /// <summary>
        /// Action method: Scrolls down one page
        /// </summary>
        private void PageDown()
        {
            try
            {
                // Get the current cursor position
                var currentPos = _textView.CursorPosition;
                
                // Calculate page size (height of text view minus 1 for overlap)
                int pageSize = Math.Max(1, _textView.Frame.Height - 1);
                
                // Calculate new line position
                int maxLine = Math.Max(0, _textViewer.LineCount - 1);
                int newLine = Math.Min(maxLine, currentPos.Y + pageSize);
                
                // Update cursor position
                _textView.CursorPosition = new Point(currentPos.X, newLine);
                _textViewer.ScrollTo(newLine);
                
                _logger?.LogDebug("Page down to line {Line}", newLine + 1);
                UpdateStatusLabel($"Page down - Line {newLine + 1}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during page down: {Message}", ex.Message);
                UpdateStatusLabel($"Page down error: {ex.Message}");
            }
        }

        /// <summary>
        /// Action method: Closes the viewer window
        /// </summary>
        private void Close()
        {
            CloseViewer();
        }

        /// <summary>
        /// Action method: Shows the search dialog
        /// </summary>
        private void Search()
        {
            ShowSearchDialog();
        }

        /// <summary>
        /// Action method: Toggles between text and hex display modes
        /// </summary>
        private void ToggleHexMode()
        {
            try
            {
                // Store current scroll position
                var currentPos = _textView.CursorPosition;
                
                // Toggle mode
                _isHexMode = !_isHexMode;
                
                // Update window title
                Title = _isHexMode ? "Binary Viewer" : "Text Viewer";
                
                // Reload content in new mode
                LoadContent();
                
                // Try to restore approximate scroll position
                // In hex mode, each line represents 16 bytes
                // In text mode, line count varies
                try
                {
                    _textView.CursorPosition = new Point(0, Math.Min(currentPos.Y, _textView.Text.ToString().Split('\n').Length - 1));
                }
                catch
                {
                    // If position restore fails, just go to top
                    _textView.CursorPosition = new Point(0, 0);
                }
                
                // Update status bar
                string mode = _isHexMode ? "HEX" : "TEXT";
                _logger?.LogInformation("Switched to {Mode} mode", mode);
                UpdateStatusLabel($"Mode: {mode}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling hex mode: {Message}", ex.Message);
                UpdateStatusLabel($"Error toggling mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Cycles through available encodings and reloads the file
        /// </summary>
        private void CycleEncoding()
        {
            try
            {
                var previousEncoding = _textViewer.CurrentEncoding.EncodingName;
                _textViewer.CycleEncoding();
                LoadContent();
                UpdateEncodingLabel();
                var newEncoding = _textViewer.CurrentEncoding.EncodingName;
                _logger?.LogInformation("Encoding changed from {PreviousEncoding} to {NewEncoding}", previousEncoding, newEncoding);
                UpdateStatusLabel($"Encoding changed to: {newEncoding}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error changing encoding: {Message}", ex.Message);
                UpdateStatusLabel($"Error changing encoding: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a search dialog and performs the search
        /// </summary>
        private void ShowSearchDialog()
        {
            try
            {
                var searchDialog = new Dialog("Search", 60, 8);
                
                var searchLabel = new Label("Enter search text:")
                {
                    X = 1,
                    Y = 1
                };
                searchDialog.Add(searchLabel);
                
                var searchField = new TextField(_searchPattern)
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                searchDialog.Add(searchField);
                
                var okButton = new Button("OK", is_default: true);
                okButton.Clicked += () =>
                {
                    _searchPattern = searchField.Text.ToString() ?? string.Empty;
                    PerformSearch();
                    Application.RequestStop();
                };
                
                var cancelButton = new Button("Cancel");
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                searchDialog.AddButton(okButton);
                searchDialog.AddButton(cancelButton);
                
                Application.Run(searchDialog);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing search dialog: {Message}", ex.Message);
                UpdateStatusLabel($"Error showing search dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Performs a search for the current search pattern
        /// </summary>
        private void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(_searchPattern))
            {
                _logger?.LogDebug("Search attempted with empty pattern");
                UpdateStatusLabel("Search pattern is empty");
                return;
            }
            
            try
            {
                _logger?.LogInformation("Searching for pattern: {Pattern}", _searchPattern);
                _searchMatches = _textViewer.Search(_searchPattern);
                _currentMatchIndex = -1;
                
                if (_searchMatches.Count > 0)
                {
                    _logger?.LogInformation("Found {Count} matches for pattern: {Pattern}", _searchMatches.Count, _searchPattern);
                    UpdateStatusLabel($"Found {_searchMatches.Count} match(es)");
                    FindNext();
                }
                else
                {
                    _logger?.LogInformation("No matches found for pattern: {Pattern}", _searchPattern);
                    UpdateStatusLabel($"No matches found for: {_searchPattern}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Search error for pattern '{Pattern}': {Message}", _searchPattern, ex.Message);
                UpdateStatusLabel($"Search error: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds and navigates to the next search match
        /// </summary>
        private void FindNext()
        {
            if (_searchMatches.Count == 0)
            {
                UpdateStatusLabel("No search results. Press F4 to search.");
                return;
            }
            
            _currentMatchIndex = (_currentMatchIndex + 1) % _searchMatches.Count;
            NavigateToMatch();
        }

        /// <summary>
        /// Finds and navigates to the previous search match
        /// </summary>
        private void FindPrevious()
        {
            if (_searchMatches.Count == 0)
            {
                UpdateStatusLabel("No search results. Press F4 to search.");
                return;
            }
            
            _currentMatchIndex--;
            if (_currentMatchIndex < 0)
            {
                _currentMatchIndex = _searchMatches.Count - 1;
            }
            NavigateToMatch();
        }

        /// <summary>
        /// Navigates to the current search match
        /// </summary>
        private void NavigateToMatch()
        {
            if (_currentMatchIndex < 0 || _currentMatchIndex >= _searchMatches.Count)
            {
                return;
            }
            
            int lineNumber = _searchMatches[_currentMatchIndex];
            _textViewer.ScrollTo(lineNumber);
            
            // Calculate the position in the text view (accounting for line numbers)
            // Each line in the display includes the line number prefix
            _textView.CursorPosition = new Point(0, lineNumber);
            
            UpdateStatusLabel($"Match {_currentMatchIndex + 1} of {_searchMatches.Count} at line {lineNumber + 1}");
        }

        /// <summary>
        /// Updates the status label text
        /// </summary>
        private void UpdateStatusLabel(string message)
        {
            _statusLabel.Text = $"File: {Path.GetFileName(_textViewer.FilePath)} | Lines: {_textViewer.LineCount} | Encoding: {_textViewer.CurrentEncoding.EncodingName} | {message}";
        }

        /// <summary>
        /// Updates the encoding label text
        /// </summary>
        private void UpdateEncodingLabel()
        {
            _encodingLabel.Text = $"Encoding: {_textViewer.CurrentEncoding.EncodingName} | Shift+E: Change Encoding | F4: Search | Esc/Enter: Close";
        }

        /// <summary>
        /// Closes the viewer window
        /// </summary>
        private void CloseViewer()
        {
            // Hide cursor before closing
            try
            {
                Console.CursorVisible = false;
            }
            catch
            {
                // Ignore errors hiding cursor
            }
            
            Application.RequestStop();
        }
        
        /// <summary>
        /// Formats a single byte as two-digit hexadecimal
        /// </summary>
        private string FormatHexByte(byte b)
        {
            return b.ToString("X2");
        }

        /// <summary>
        /// Formats a byte as ASCII character or dot for non-printable
        /// </summary>
        private char FormatAsciiChar(byte b)
        {
            // Printable ASCII range is 0x20 (space) to 0x7E (~)
            return (b >= 0x20 && b <= 0x7E) ? (char)b : '.';
        }

        /// <summary>
        /// Formats a line of bytes in hex dump format
        /// </summary>
        /// <param name="offset">Byte offset for this line</param>
        /// <param name="bytes">Bytes to format (up to 16)</param>
        /// <returns>Formatted hex line</returns>
        private string FormatHexLine(int offset, byte[] bytes)
        {
            var sb = new StringBuilder();

            // Add offset (8 hex digits)
            sb.Append(offset.ToString("X8"));
            sb.Append("  ");

            // Add hex bytes (16 bytes per line, with extra space after 8th byte)
            for (int i = 0; i < 16; i++)
            {
                if (i < bytes.Length)
                {
                    sb.Append(FormatHexByte(bytes[i]));
                }
                else
                {
                    sb.Append("  "); // Two spaces for missing bytes
                }

                // Add space after each byte
                sb.Append(" ");

                // Add extra space after 8th byte for visual grouping
                if (i == 7)
                {
                    sb.Append(" ");
                }
            }

            // Add ASCII representation
            sb.Append(" |");
            for (int i = 0; i < Math.Min(16, bytes.Length); i++)
            {
                sb.Append(FormatAsciiChar(bytes[i]));
            }
            // Pad ASCII column if less than 16 bytes
            for (int i = bytes.Length; i < 16; i++)
            {
                sb.Append(' ');
            }
            sb.Append("|");

            return sb.ToString();
        }

        /// <summary>
        /// Parses a color string to Terminal.Gui Color enum
        /// </summary>
        private Color ParseColor(string? colorName, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(colorName))
                return defaultColor;
            
            return colorName.ToLower() switch
            {
                "black" => Color.Black,
                "blue" => Color.Blue,
                "green" => Color.Green,
                "cyan" => Color.Cyan,
                "red" => Color.Red,
                "magenta" => Color.Magenta,
                "brown" => Color.Brown,
                "gray" => Color.Gray,
                "darkgray" => Color.DarkGray,
                "brightblue" => Color.BrightBlue,
                "brightgreen" => Color.BrightGreen,
                "brightcyan" => Color.BrightCyan,
                "brightred" => Color.BrightRed,
                "brightmagenta" => Color.BrightMagenta,
                "yellow" => Color.Brown, // Terminal.Gui uses Brown for yellow
                "white" => Color.White,
                _ => defaultColor
            };
        }
    }
}
