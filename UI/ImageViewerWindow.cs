using Terminal.Gui;
using TWF.Services;
using TWF.Models;

namespace TWF.UI
{
    /// <summary>
    /// Image viewer window for displaying images with zoom, rotation, and flip support
    /// </summary>
    public class ImageViewerWindow : Window
    {
        private readonly ImageViewer _imageViewer;
        private View _imageView = null!;
        private Label _statusLabel = null!;
        private Label _controlsLabel = null!;
        private int _scrollX = 0;
        private int _scrollY = 0;

        /// <summary>
        /// Initializes a new instance of ImageViewerWindow
        /// </summary>
        /// <param name="imageViewer">The image viewer instance containing the image data</param>
        public ImageViewerWindow(ImageViewer imageViewer) : base("Image Viewer")
        {
            _imageViewer = imageViewer ?? throw new ArgumentNullException(nameof(imageViewer));
            
            InitializeComponents();
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

            // Create view for displaying image (placeholder for terminal-based rendering)
            _imageView = new View()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2), // Leave room for status and controls labels
                CanFocus = true
            };
            Add(_imageView);

            // Create status label (second to last line)
            _statusLabel = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(),
                Height = 1,
                Text = GetStatusText()
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

            // Create controls label (last line)
            _controlsLabel = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(0),
                Width = Dim.Fill(),
                Height = 1,
                Text = "Home: Original | End: Fit Window | Q/K: Rotate | G/U: Flip | Arrows: Scroll | Esc: Close"
            };
            
            // Set color scheme if Application.Driver is available
            if (Application.Driver != null)
            {
                _controlsLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue)
                };
            }
            Add(_controlsLabel);
        }

        /// <summary>
        /// Gets the status text showing current image state
        /// </summary>
        private string GetStatusText()
        {
            string fileName = Path.GetFileName(_imageViewer.FilePath);
            string viewMode = _imageViewer.ViewMode.ToString();
            string rotation = $"{_imageViewer.Rotation}°";
            string flip = "";
            
            if (_imageViewer.FlipHorizontal && _imageViewer.FlipVertical)
                flip = " | Flipped: H+V";
            else if (_imageViewer.FlipHorizontal)
                flip = " | Flipped: H";
            else if (_imageViewer.FlipVertical)
                flip = " | Flipped: V";
            
            string zoom = _imageViewer.ViewMode == ViewMode.FixedZoom 
                ? $" | Zoom: {_imageViewer.ZoomFactor:P0}" 
                : "";
            
            return $"File: {fileName} | Mode: {viewMode} | Rotation: {rotation}{flip}{zoom}";
        }

        /// <summary>
        /// Sets up key event handlers
        /// </summary>
        private void SetupKeyHandlers()
        {
            KeyPress += (e) =>
            {
                bool handled = true;
                
                switch (e.KeyEvent.Key)
                {
                    // Home: Switch to original size mode
                    case Key.Home:
                        _imageViewer.SetViewMode(ViewMode.OriginalSize);
                        UpdateStatus("View mode: Original Size");
                        break;
                    
                    // End: Switch to fit-to-window mode
                    case Key.End:
                        _imageViewer.SetViewMode(ViewMode.FitToWindow);
                        UpdateStatus("View mode: Fit to Window");
                        break;
                    
                    // Q or K: Rotate 90 degrees clockwise
                    case Key.Q:
                    case Key.K:
                        _imageViewer.Rotate(90);
                        UpdateStatus($"Rotated to {_imageViewer.Rotation}°");
                        break;
                    
                    // G: Flip horizontally
                    case Key.G:
                        _imageViewer.Flip(FlipDirection.Horizontal);
                        UpdateStatus($"Flipped horizontally (H: {_imageViewer.FlipHorizontal})");
                        break;
                    
                    // U: Flip vertically
                    case Key.U:
                        _imageViewer.Flip(FlipDirection.Vertical);
                        UpdateStatus($"Flipped vertically (V: {_imageViewer.FlipVertical})");
                        break;
                    
                    // Arrow keys: Scroll the image
                    case Key.CursorUp:
                        _scrollY = Math.Max(0, _scrollY - 1);
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        break;
                    
                    case Key.CursorDown:
                        _scrollY++;
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        break;
                    
                    case Key.CursorLeft:
                        _scrollX = Math.Max(0, _scrollX - 1);
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        break;
                    
                    case Key.CursorRight:
                        _scrollX++;
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        break;
                    
                    // Escape: Close the viewer
                    case Key.Esc:
                        CloseViewer();
                        break;
                    
                    default:
                        handled = false;
                        break;
                }
                
                e.Handled = handled;
            };
        }

        /// <summary>
        /// Updates the status label with current image state
        /// </summary>
        private void UpdateStatus(string? message = null)
        {
            _statusLabel.Text = GetStatusText();
            
            if (!string.IsNullOrEmpty(message))
            {
                _statusLabel.Text += $" | {message}";
            }
        }

        /// <summary>
        /// Closes the viewer window
        /// </summary>
        private void CloseViewer()
        {
            Application.RequestStop();
        }

        /// <summary>
        /// Gets the current scroll position
        /// </summary>
        public (int X, int Y) GetScrollPosition() => (_scrollX, _scrollY);
    }
}
