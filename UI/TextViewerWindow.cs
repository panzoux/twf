using Terminal.Gui;
using TWF.Services;
using TWF.Models;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TWF.UI
{
    /// <summary>
    /// Text/Binary viewer window for displaying file contents efficiently
    /// </summary>
    public class TextViewerWindow : Window
    {
        private readonly LargeFileEngine _fileEngine;
        private readonly KeyBindingManager _keyBindings;
        private readonly Configuration? _configuration;
        private readonly ILogger<TextViewerWindow>? _logger;
        
        private VirtualFileView _fileView = null!;
        private Label _statusLabel = null!;
        private Label _messageLabel = null!;
        
        private string _searchPattern = string.Empty;
        private object _statusUpdateToken;

        /// <summary>
        /// Initializes a new instance of TextViewerWindow
        /// </summary>
        public TextViewerWindow(LargeFileEngine fileEngine, KeyBindingManager keyBindings, Configuration? configuration = null, ILogger<TextViewerWindow>? logger = null, bool startInHexMode = false) : base(startInHexMode ? "Binary Viewer" : "Text Viewer")
        {
            _fileEngine = fileEngine ?? throw new ArgumentNullException(nameof(fileEngine));
            _keyBindings = keyBindings ?? throw new ArgumentNullException(nameof(keyBindings));
            _configuration = configuration;
            _logger = logger;
            
            InitializeComponents();
            
            if (startInHexMode)
            {
                _fileView.Mode = FileViewMode.Hex;
            }
            
            SetupKeyHandlers();

            // Hook into engine events
            // We do NOT hook into IndexingProgressChanged here because it fires too frequently (every 1MB)
            // and flooding MainLoop.Invoke causes high CPU usage.
            // Instead, we rely on the timer below to poll for updates.
            
            _fileEngine.IndexingCompleted += (s, e) => Application.MainLoop.Invoke(() => { 
                _logger?.LogInformation("IndexingCompleted event received");
                UpdateStatusLabel(); 
                UpdateMessageLabel(); 
                _fileView.SetNeedsDisplay(); // Final redraw to ensure all lines are accessible
            });

            // Start status update timer for indexing progress
            _statusUpdateToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), (loop) => 
            {
                if (_fileEngine.IsIndexing)
                {
                    UpdateStatusLabel();
                    UpdateMessageLabel();
                    
                    // Smart Redraw: Only redraw file view if new lines are visible
                    // This prevents high CPU usage from constant redrawing when viewing top of large files
                    long topVisibleLine = _fileView.ScrollOffset;
                    long bottomVisibleLine = topVisibleLine + _fileView.Frame.Height;
                    
                    // If the current indexed line count is within or just past the visible range, redraw
                    // This handles the initial load and "tailing" behavior if user is at the bottom
                    if (_fileEngine.LineCount >= topVisibleLine && _fileEngine.LineCount <= bottomVisibleLine + 100) 
                    {
                        _fileView.SetNeedsDisplay();
                    }
                    
                    return true;
                }
                return true;
            });

            // Cleanup timer on close
            this.Closed += (e) => {
                if (_statusUpdateToken != null)
                {
                    Application.MainLoop.RemoveTimeout(_statusUpdateToken);
                    _statusUpdateToken = null!;
                }
            };
        }

        private void InitializeComponents()
        {
            X = 0;
            Y = 0;
            Width = Dim.Fill();
            Height = Dim.Fill();
            Modal = true;

            // Colors
            var textFg = ParseColor(_configuration?.Viewer.TextViewerForegroundColor, Color.White);
            var textBg = ParseColor(_configuration?.Viewer.TextViewerBackgroundColor, Color.Black);
            var statusFg = ParseColor(_configuration?.Viewer.TextViewerStatusForegroundColor, Color.Black);
            var statusBg = ParseColor(_configuration?.Viewer.TextViewerStatusBackgroundColor, Color.Gray);
            var messageFg = ParseColor(_configuration?.Viewer.TextViewerMessageForegroundColor, Color.White);
            var messageBg = ParseColor(_configuration?.Viewer.TextViewerMessageBackgroundColor, Color.Blue);

            // Virtual File View
            _fileView = new VirtualFileView(_fileEngine)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2)
            };
            
            if (Application.Driver != null)
            {
                _fileView.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(textFg, textBg),
                    Focus = Application.Driver.MakeAttribute(textFg, textBg),
                    HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, textBg) // For line numbers/offsets
                };
            }
            Add(_fileView);

            // Status Label
            _statusLabel = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(2),
                Width = Dim.Fill(),
                Height = 1,
                Text = "Loading..."
            };
            
            if (Application.Driver != null)
            {
                _statusLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(statusFg, statusBg)
                };
            }
            Add(_statusLabel);

            // Message Label (formerly Encoding Label)
            _messageLabel = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(),
                Height = 1,
                Text = ""
            };
            
            if (Application.Driver != null)
            {
                _messageLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(messageFg, messageBg)
                };
            }
            Add(_messageLabel);

            UpdateStatusLabel();
            UpdateMessageLabel();
        }

        private void SetupKeyHandlers()
        {
            this.KeyDown += (e) =>
            {
                var keyEvent = e.KeyEvent;
                
                // Allow cancelling indexing with Esc
                if (keyEvent.Key == Key.Esc && _fileEngine.IsIndexing)
                {
                    _fileEngine.CancelIndexing();
                    UpdateStatusLabel();
                    UpdateMessageLabel();
                    e.Handled = true;
                    return;
                }

                try
                {
                    string keyString = ConvertKeyToString(keyEvent.Key);
                    
                    // Check custom bindings
                    string? action = _keyBindings.GetActionForKey(keyString, UiMode.TextViewer);
                    
                    if (action != null)
                    {
                        if (ExecuteTextViewerAction(action))
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                    else
                    {
                        // Default bindings
                        if (ExecuteDefaultBinding(keyEvent.Key))
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error handling key down");
                }
            };
        }

        private bool ExecuteDefaultBinding(Key key)
        {
            switch (key)
            {
                case Key.F5:
                    _fileView.ScrollToTop();
                    return true;
                case Key.F6:
                    _fileView.ScrollToBottom();
                    return true;
                case Key.PageUp:
                    _fileView.PageUp();
                    return true;
                case Key.PageDown:
                    _fileView.PageDown();
                    return true;
                case Key.Home:
                    _fileView.ScrollToTop();
                    return true;
                case Key.End:
                    _fileView.ScrollToBottom();
                    return true;
                case Key.CursorUp:
                    _fileView.ScrollUp(1);
                    return true;
                case Key.CursorDown:
                    _fileView.ScrollDown(1);
                    return true;
                case Key.Esc:
                case Key.Enter:
                    Application.RequestStop();
                    return true;
                case (Key.B | Key.CtrlMask):
                    ToggleHexMode();
                    return true;
                case Key.F7:
                    // Cycle Encoding (Not fully impl yet)
                    return true;
            }
            return false;
        }

        private bool ExecuteTextViewerAction(string actionName)
        {
            switch (actionName)
            {
                case "TextViewer.GoToTop":
                    _fileView.ScrollToTop();
                    return true;
                case "TextViewer.GoToBottom":
                    _fileView.ScrollToBottom();
                    return true;
                case "TextViewer.PageUp":
                    _fileView.PageUp();
                    return true;
                case "TextViewer.PageDown":
                    _fileView.PageDown();
                    return true;
                case "TextViewer.Close":
                    Application.RequestStop();
                    return true;
                case "TextViewer.ToggleHexMode":
                    ToggleHexMode();
                    return true;
            }
            return false;
        }

        private void ToggleHexMode()
        {
            if (_fileView.Mode == FileViewMode.Text)
            {
                _fileView.Mode = FileViewMode.Hex;
                Title = "Binary Viewer";
            }
            else
            {
                _fileView.Mode = FileViewMode.Text;
                Title = "Text Viewer";
            }
            UpdateStatusLabel();
            UpdateMessageLabel();
        }

        private void UpdateStatusLabel()
        {
            string status = "";
            string fileName = Path.GetFileName(_fileEngine.FilePath);
            if (_fileView.Mode == FileViewMode.Text)
            {
                status = $"Lines: {_fileEngine.LineCount:N0}";
                // Status label shows File, Lines, and Encoding
                _statusLabel.Text = $"File: {fileName} | {status} | {_fileEngine.CurrentEncoding.EncodingName}";
            }
            else
            {
                status = $"Size: {_fileEngine.FileSize:N0} bytes";
                _statusLabel.Text = $"File: {fileName} | {status}";
            }
        }

        private void UpdateMessageLabel()
        {
            if (_fileEngine.IsIndexing)
            {
                string text = $"Reading... {_fileEngine.IndexingProgress:P0} (Esc to stop)";
                // _logger?.LogDebug("Updating message label (Indexing): {Text}", text);
                _messageLabel.Text = text;
            }
            else
            {
                // _logger?.LogDebug("Updating message label (Idle)");
                _messageLabel.Text = "Ctrl+B: Hex/Text | F5/F6: Top/Bot | F4: Search | Esc: Close";
            }
            _messageLabel.SetNeedsDisplay();
        }

        // Helper for key string conversion
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
            if (baseKey >= Key.A && baseKey <= Key.Z && !hasShift && parts.Count == 0 && !isLowercaseLetter)
            {
                parts.Insert(0, "Shift");
            }
            
            parts.Add(keyName);
            
            return string.Join("+", parts);
        }
        
        private Color ParseColor(string? colorName, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(colorName)) return defaultColor;
            if (Enum.TryParse<Color>(colorName, true, out var color)) return color;
            return defaultColor;
        }
    }
}
