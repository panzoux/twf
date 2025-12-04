using Terminal.Gui;
using TWF.Services;
using System.Text;

namespace TWF.UI
{
    /// <summary>
    /// Text viewer window for displaying file contents with encoding support
    /// </summary>
    public class TextViewerWindow : Window
    {
        private readonly TextViewer _textViewer;
        private TextView _textView = null!;
        private Label _statusLabel = null!;
        private Label _encodingLabel = null!;
        private string _searchPattern = string.Empty;
        private List<int> _searchMatches = new List<int>();
        private int _currentMatchIndex = -1;

        /// <summary>
        /// Initializes a new instance of TextViewerWindow
        /// </summary>
        /// <param name="textViewer">The text viewer instance containing the file data</param>
        public TextViewerWindow(TextViewer textViewer) : base("Text Viewer")
        {
            _textViewer = textViewer ?? throw new ArgumentNullException(nameof(textViewer));
            
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
            
            // Set color scheme if Application.Driver is available
            if (Application.Driver != null)
            {
                _statusLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray)
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
            
            // Set color scheme if Application.Driver is available
            if (Application.Driver != null)
            {
                _encodingLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue)
                };
            }
            Add(_encodingLabel);
        }

        /// <summary>
        /// Loads file content into the text view with line numbers
        /// </summary>
        private void LoadContent()
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
        /// Sets up key event handlers
        /// </summary>
        private void SetupKeyHandlers()
        {
            KeyPress += (e) =>
            {
                // Handle F5 - go to top of file
                if (e.KeyEvent.Key == Key.F5)
                {
                    GoToFileTop();
                    e.Handled = true;
                }
                // Handle F6 - go to bottom of file
                else if (e.KeyEvent.Key == Key.F6)
                {
                    GoToFileBottom();
                    e.Handled = true;
                }
                // Handle Home - go to start of current line (default TextView behavior)
                // Note: Home key naturally moves cursor to line start - we keep this behavior
                // Handle End - go to end of current line (default TextView behavior)
                // Note: End key naturally moves cursor to line end - we keep this behavior
                
                // Handle Shift+E for encoding cycling
                // Terminal.Gui sends uppercase 'E' (KeyValue == 'E') when Shift+E is pressed
                // We check if KeyValue is exactly uppercase 'E' (ASCII 69)
                else if (e.KeyEvent.KeyValue == 69) // 'E' = ASCII 69
                {
                    CycleEncoding();
                    e.Handled = true;
                }
                 // Handle F7 for Cycle Encoding
                else if (e.KeyEvent.Key == Key.F7)
                {
                    CycleEncoding();
                    e.Handled = true;
                }
                // Handle F4 for search
                else if (e.KeyEvent.Key == Key.F4)
                {
                    ShowSearchDialog();
                    e.Handled = true;
                }
                // Handle Escape or Enter to close
                else if (e.KeyEvent.Key == Key.Esc || e.KeyEvent.Key == Key.Enter)
                {
                    CloseViewer();
                    e.Handled = true;
                }
                // Handle F3 or Ctrl+G for find next (common text viewer shortcuts)
                else if (e.KeyEvent.Key == Key.F3 || e.KeyEvent.Key == (Key.G | Key.CtrlMask))
                {
                    FindNext();
                    e.Handled = true;
                }
                // Handle Shift+F3 for find previous
                else if (e.KeyEvent.Key == (Key.F3 | Key.ShiftMask))
                {
                    FindPrevious();
                    e.Handled = true;
                }
            };
        }
        
        /// <summary>
        /// Scrolls to the top of the file (F5)
        /// </summary>
        private void GoToFileTop()
        {
            _textView.CursorPosition = new Point(0, 0);
            _textViewer.ScrollTo(0);
            UpdateStatusLabel("Top of file");
        }
        
        /// <summary>
        /// Scrolls to the bottom of the file (F6)
        /// </summary>
        private void GoToFileBottom()
        {
            int lastLine = Math.Max(0, _textViewer.LineCount - 1);
            _textView.CursorPosition = new Point(0, lastLine);
            _textViewer.ScrollTo(lastLine);
            UpdateStatusLabel("Bottom of file");
        }

        /// <summary>
        /// Cycles through available encodings and reloads the file
        /// </summary>
        private void CycleEncoding()
        {
            try
            {
                _textViewer.CycleEncoding();
                LoadContent();
                UpdateEncodingLabel();
                UpdateStatusLabel($"Encoding changed to: {_textViewer.CurrentEncoding.EncodingName}");
            }
            catch (Exception ex)
            {
                UpdateStatusLabel($"Error changing encoding: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a search dialog and performs the search
        /// </summary>
        private void ShowSearchDialog()
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

        /// <summary>
        /// Performs a search for the current search pattern
        /// </summary>
        private void PerformSearch()
        {
            if (string.IsNullOrWhiteSpace(_searchPattern))
            {
                UpdateStatusLabel("Search pattern is empty");
                return;
            }
            
            try
            {
                _searchMatches = _textViewer.Search(_searchPattern);
                _currentMatchIndex = -1;
                
                if (_searchMatches.Count > 0)
                {
                    UpdateStatusLabel($"Found {_searchMatches.Count} match(es)");
                    FindNext();
                }
                else
                {
                    UpdateStatusLabel($"No matches found for: {_searchPattern}");
                }
            }
            catch (Exception ex)
            {
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
            Application.RequestStop();
        }
    }
}
