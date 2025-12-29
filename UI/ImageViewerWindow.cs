using System;
using System.IO;
using System.Linq;
using Terminal.Gui;
using TWF.Services;
using TWF.Models;
using System.Drawing;
using System.Drawing.Imaging;

// Alias Terminal.Gui types to avoid conflicts
using GuiColor = Terminal.Gui.Color;
using GuiAttribute = Terminal.Gui.Attribute;
using DrawingSize = System.Drawing.Size;

namespace TWF.UI
{
    /// <summary>
    /// Image viewer window for displaying images with zoom, rotation, and flip support
    /// </summary>
    public class ImageViewerWindow : Window
    {
        private readonly ImageViewer _imageViewer;
        private ImageCanvas _imageView = null!;
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

            // Create view for displaying image
            _imageView = new ImageCanvas(_imageViewer)
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
                    Normal = Application.Driver.MakeAttribute(GuiColor.Black, GuiColor.Gray)
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
                Text = "Home: Original | End: Fit Window | Q/K: Rotate | G/U: Flip | C: Console View | Arrows: Scroll | Esc: Close"
            };
            
            // Set color scheme if Application.Driver is available
            if (Application.Driver != null)
            {
                _controlsLabel.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(GuiColor.White, GuiColor.Blue)
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
                bool needsRedraw = false;
                
                switch (e.KeyEvent.Key)
                {
                    // Home: Switch to original size mode
                    case Key.Home:
                        _imageViewer.SetViewMode(ViewMode.OriginalSize);
                        UpdateStatus("View mode: Original Size");
                        needsRedraw = true;
                        break;
                    
                    // End: Switch to fit-to-window mode
                    case Key.End:
                        _imageViewer.SetViewMode(ViewMode.FitToWindow);
                        UpdateStatus("View mode: Fit to Window");
                        needsRedraw = true;
                        break;
                    
                    // Q or K: Rotate 90 degrees clockwise
                    case Key.Q:
                    case Key.K:
                        _imageViewer.Rotate(90);
                        UpdateStatus($"Rotated to {_imageViewer.Rotation}°");
                        needsRedraw = true;
                        break;
                    
                    // G: Flip horizontally
                    case Key.G:
                        _imageViewer.Flip(FlipDirection.Horizontal);
                        UpdateStatus($"Flipped horizontally (H: {_imageViewer.FlipHorizontal})");
                        needsRedraw = true;
                        break;
                    
                    // U: Flip vertically
                    case Key.U:
                        _imageViewer.Flip(FlipDirection.Vertical);
                        UpdateStatus($"Flipped vertically (V: {_imageViewer.FlipVertical})");
                        needsRedraw = true;
                        break;
                    
                    // C: Render to console directly
                    case Key.C:
                    case (Key)'c':
                        RenderToConsole();
                        break;

                    // Arrow keys: Scroll the image
                    case Key.CursorUp:
                        _scrollY = Math.Max(0, _scrollY - 1);
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        needsRedraw = true;
                        break;
                    
                    case Key.CursorDown:
                        _scrollY++;
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        needsRedraw = true;
                        break;
                    
                    case Key.CursorLeft:
                        _scrollX = Math.Max(0, _scrollX - 1);
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        needsRedraw = true;
                        break;
                    
                    case Key.CursorRight:
                        _scrollX++;
                        UpdateStatus($"Scroll position: ({_scrollX}, {_scrollY})");
                        needsRedraw = true;
                        break;
                    
                    // Escape: Close the viewer
                    case Key.Esc:
                        CloseViewer();
                        break;
                    
                    default:
                        handled = false;
                        break;
                }
                
                if (needsRedraw)
                {
                    _imageView.ScrollX = _scrollX;
                    _imageView.ScrollY = _scrollY;
                    _imageView.SetNeedsDisplay();
                }

                e.Handled = handled;
            };
        }

        /// <summary>
        /// Renders the image directly to the console using ANSI escape codes (24-bit color)
        /// Suspends the TUI temporarily.
        /// </summary>
        private void RenderToConsole()
        {
            if (Application.Driver == null) return;

            // Suspend the TUI driver
            Application.Driver.Suspend();

            try
            {
                Console.Clear();
                Console.WriteLine("Rendering image to console (24-bit color)...");

                // Load and process image
                using var original = new Bitmap(_imageViewer.FilePath);
                
                // Apply transformations
                RotateFlipType rotateFlip = RotateFlipType.RotateNoneFlipNone;
                switch (_imageViewer.Rotation)
                {
                    case 90: rotateFlip = RotateFlipType.Rotate90FlipNone; break;
                    case 180: rotateFlip = RotateFlipType.Rotate180FlipNone; break;
                    case 270: rotateFlip = RotateFlipType.Rotate270FlipNone; break;
                }
                original.RotateFlip(rotateFlip);
                if (_imageViewer.FlipHorizontal) original.RotateFlip(RotateFlipType.RotateNoneFlipX);
                if (_imageViewer.FlipVertical) original.RotateFlip(RotateFlipType.RotateNoneFlipY);

                // Calculate dimensions for console
                int consoleWidth = Console.WindowWidth;
                int consoleHeight = Console.WindowHeight - 2; // Leave room for prompts

                // Font aspect ratio correction (consoles usually have ~1:2 character aspect ratio)
                // We want to scale width by 2 to make "square" pixels using 2 half-blocks or spaces
                
                float aspectRatio = (float)original.Width / original.Height;
                int newWidth = (int)(consoleWidth * aspectRatio * 2);
                int newHeight = (int)(newWidth / aspectRatio);

                if (newHeight > consoleHeight)
                {
                    newHeight = consoleHeight;
                    newWidth = (int)(newHeight * aspectRatio * 2);
                }
                
                // Ensure dimensions are at least 1x1
                newWidth = Math.Max(1, newWidth);
                newHeight = Math.Max(1, newHeight);

                using var resized = new Bitmap(original, new DrawingSize(newWidth, newHeight));

                Console.SetCursorPosition(0, 0);

                // Render loop
                for (int y = 0; y < resized.Height; y++)
                {
                    for (int x = 0; x < resized.Width; x += 2)
                    {
                        // Pixel 1
                        System.Drawing.Color c1 = resized.GetPixel(x, y);
                        Console.Write($"\x1b[48;2;{c1.R};{c1.G};{c1.B}m "); // First half-block (space with bg)

                        // Pixel 2 (if available)
                        if (x + 1 < resized.Width)
                        {
                            System.Drawing.Color c2 = resized.GetPixel(x + 1, y);
                            Console.Write($"\x1b[48;2;{c2.R};{c2.G};{c2.B}m "); // Second half-block
                        }
                    }
                    Console.WriteLine("\x1b[0m"); // Reset color at EOL
                }

                Console.WriteLine("\x1b[0m"); // Ensure reset
                Console.WriteLine($"Image: {Path.GetFileName(_imageViewer.FilePath)} ({original.Width}x{original.Height})");
                Console.WriteLine("Press any key to return to TUI...");
                Console.ReadKey(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError rendering to console: {ex.Message}");
                Console.WriteLine("Press any key to return...");
                Console.ReadKey(true);
            }
            finally
            {
                // Resume TUI
                Application.Refresh();
            }
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

    /// <summary>
    /// Custom view to render the image using System.Drawing
    /// </summary>
    public class ImageCanvas : View
    {
        private readonly ImageViewer _imageViewer;
        private Bitmap? _cachedBitmap;
        
        // Cache invalidation keys
        private string _lastFilePath = string.Empty;
        private int _lastRotation = -1;
        private bool _lastFlipH = false;
        private bool _lastFlipV = false;
        private ViewMode _lastViewMode = ViewMode.FitToScreen;
        private double _lastZoom = -1;
        private Rect _lastBounds = Rect.Empty;

        public int ScrollX { get; set; }
        public int ScrollY { get; set; }

        public ImageCanvas(ImageViewer viewer)
        {
            _imageViewer = viewer;
        }

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            if (string.IsNullOrEmpty(_imageViewer.FilePath) || !File.Exists(_imageViewer.FilePath))
            {
                return;
            }

            try
            {
                // Refresh cache if needed
                UpdateBitmapCache(bounds);

                if (_cachedBitmap != null)
                {
                    DrawBitmap(bounds);
                }
            }
            catch (Exception)
            {
                // In case of rendering error, maybe print a message
                Driver.AddStr("Error rendering image.");
            }
        }

        private void UpdateBitmapCache(Rect bounds)
        {
            // Check if anything changed that requires re-rendering/resizing
            bool needsUpdate = _cachedBitmap == null ||
                               _imageViewer.FilePath != _lastFilePath ||
                               _imageViewer.Rotation != _lastRotation ||
                               _imageViewer.FlipHorizontal != _lastFlipH ||
                               _imageViewer.FlipVertical != _lastFlipV ||
                               _imageViewer.ViewMode != _lastViewMode ||
                               Math.Abs(_imageViewer.ZoomFactor - _lastZoom) > 0.001 ||
                               (_imageViewer.ViewMode == ViewMode.FitToWindow && bounds != _lastBounds);

            if (!needsUpdate) return;

            // Dispose old cache
            _cachedBitmap?.Dispose();
            _cachedBitmap = null;

            // Load original
            using var original = new Bitmap(_imageViewer.FilePath);

            // Apply transformations
            RotateFlipType rotateFlip = RotateFlipType.RotateNoneFlipNone;

            // Handle rotation
            switch (_imageViewer.Rotation)
            {
                case 90: rotateFlip = RotateFlipType.Rotate90FlipNone; break;
                case 180: rotateFlip = RotateFlipType.Rotate180FlipNone; break;
                case 270: rotateFlip = RotateFlipType.Rotate270FlipNone; break;
            }

            // Handle flips (XOR logic with rotation if needed, but simple application works usually)
            // Note: System.Drawing applies Rotate then Flip. 
            // We need to carefully combine them or apply sequentially.
            // Let's apply basic rotation first.
            original.RotateFlip(rotateFlip);

            // Then specific flips
            if (_imageViewer.FlipHorizontal) original.RotateFlip(RotateFlipType.RotateNoneFlipX);
            if (_imageViewer.FlipVertical) original.RotateFlip(RotateFlipType.RotateNoneFlipY);

            // Calculate new size
            int targetWidth, targetHeight;
            float aspectRatio = (float)original.Width / original.Height;

            if (_imageViewer.ViewMode == ViewMode.FitToWindow || _imageViewer.ViewMode == ViewMode.FitToScreen)
            {
                // Fit to bounds
                // Terminal characters are approx 1:2 (width:height). 
                // To preserve aspect ratio visually, we assume 1 char width = 0.5 char height.
                // So effective width = chars * 1, effective height = chars * 2.
                // We want effective w/h = aspectRatio.
                // (w) / (h * 2) = aspect -> w = h * 2 * aspect.
                
                int maxW = bounds.Width;
                int maxH = bounds.Height;
                
                // Try fitting to height
                targetHeight = maxH;
                targetWidth = (int)(targetHeight * aspectRatio * 2.0f); // 2.0 to account for font ratio

                // If too wide, fit to width
                if (targetWidth > maxW)
                {
                    targetWidth = maxW;
                    targetHeight = (int)(targetWidth / (aspectRatio * 2.0f));
                }
                
                // Ensure at least 1x1
                targetWidth = Math.Max(1, targetWidth);
                targetHeight = Math.Max(1, targetHeight);
            }
            else if (_imageViewer.ViewMode == ViewMode.FixedZoom)
            {
                // Zoom factor applies to original size
                // We still apply the 2.0 width factor for terminal aspect ratio
                targetWidth = (int)(original.Width * _imageViewer.ZoomFactor * 2.0f); // Arbitrary scaling for terminal
                targetHeight = (int)(original.Height * _imageViewer.ZoomFactor);
                
                // If zoom is 1.0, we might want to map 1 pixel to 1 char? 
                // Usually 1 pixel -> 1 char is too big. 
                // Let's assume user wants to see "pixels".
                // If we want 1-1 mapping: 
                // targetWidth = original.Width
                // targetHeight = original.Height
                // But it will look squashed. 
            }
            else // Original Size
            {
                // Attempt to map 1 pixel to 1 char (with aspect correction)
                targetWidth = (int)(original.Width * 2.0f); // Stretch width to fix aspect
                targetHeight = original.Height;
                
                // But wait, the user's code had: 
                // newWidth = (int)(consoleWidth * aspectRatio * 2);
                // That was for fitting.
                
                // Let's stick to the logic: 1 pixel vertical = 1 line.
                // 1 pixel horizontal = 2 chars (to maintain square pixels).
                // So width = orig.Width * 2. Height = orig.Height.
                targetWidth = original.Width * 2;
                targetHeight = original.Height;
            }
            
            _cachedBitmap = new Bitmap(original, new DrawingSize(targetWidth, targetHeight));

            // Update state keys
            _lastFilePath = _imageViewer.FilePath;
            _lastRotation = _imageViewer.Rotation;
            _lastFlipH = _imageViewer.FlipHorizontal;
            _lastFlipV = _imageViewer.FlipVertical;
            _lastViewMode = _imageViewer.ViewMode;
            _lastZoom = _imageViewer.ZoomFactor;
            _lastBounds = bounds;
        }

        private void DrawBitmap(Rect bounds)
        {
            if (_cachedBitmap == null) return;

            // Determine loop range based on scrolling
            // We draw relative to (0,0) of the View
            
            for (int y = 0; y < bounds.Height; y++)
            {
                int bmpY = y + ScrollY;
                if (bmpY < 0 || bmpY >= _cachedBitmap.Height) continue;

                Driver.Move(0, y); // Move to start of line relative to view? 
                // View.Redraw provides bounds absolute? No, Redraw is usually called with bounds.
                // Driver.Move uses absolute coordinates.
                // We need to map View local (col, row) to Driver absolute.
                // View.Driver.SetAttribute...
                
                // Actually, inside Redraw, we should use Move(col + bounds.X, row + bounds.Y)
                
                for (int x = 0; x < bounds.Width; x++)
                {
                    int bmpX = x + ScrollX;
                    
                    if (bmpX >= 0 && bmpX < _cachedBitmap.Width)
                    {
                        System.Drawing.Color pixelColor = _cachedBitmap.GetPixel(bmpX, bmpY);
                        GuiColor guiColor = GetClosestGuiColor(pixelColor);
                        
                        var attr = Driver.MakeAttribute(guiColor, guiColor); // Block color (fg=bg)
                        Driver.SetAttribute(attr);
                        
                        Driver.Move(x + bounds.X, y + bounds.Y);
                        Driver.AddRune(' '); // Draw block
                    }
                }
            }
        }

        private GuiColor GetClosestGuiColor(System.Drawing.Color color)
        {
            // Map 24-bit RGB to closest 4-bit Console Color (0-15)
            // Simple Euclidean distance in RGB space against the standard palette
            
            GuiColor bestColor = GuiColor.Black;
            double minDistance = double.MaxValue;

            foreach (GuiColor guiColor in Enum.GetValues(typeof(GuiColor)))
            {
                System.Drawing.Color target = MapToDrawingColor(guiColor);
                
                double rDiff = color.R - target.R;
                double gDiff = color.G - target.G;
                double bDiff = color.B - target.B;
                
                double distance = rDiff * rDiff + gDiff * gDiff + bDiff * bDiff;
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestColor = guiColor;
                }
            }
            
            return bestColor;
        }

        private System.Drawing.Color MapToDrawingColor(GuiColor color)
        {
            // Standard CGA/VGA palette approximation
            return color switch
            {
                GuiColor.Black => System.Drawing.Color.Black,
                GuiColor.Blue => System.Drawing.Color.FromArgb(0, 0, 170),
                GuiColor.Green => System.Drawing.Color.FromArgb(0, 170, 0),
                GuiColor.Cyan => System.Drawing.Color.FromArgb(0, 170, 170),
                GuiColor.Red => System.Drawing.Color.FromArgb(170, 0, 0),
                GuiColor.Magenta => System.Drawing.Color.FromArgb(170, 0, 170),
                GuiColor.Brown => System.Drawing.Color.FromArgb(170, 85, 0),
                GuiColor.Gray => System.Drawing.Color.FromArgb(170, 170, 170),
                GuiColor.DarkGray => System.Drawing.Color.FromArgb(85, 85, 85),
                GuiColor.BrightBlue => System.Drawing.Color.FromArgb(85, 85, 255),
                GuiColor.BrightGreen => System.Drawing.Color.FromArgb(85, 255, 85),
                GuiColor.BrightCyan => System.Drawing.Color.FromArgb(85, 255, 255),
                GuiColor.BrightRed => System.Drawing.Color.FromArgb(255, 85, 85),
                GuiColor.BrightMagenta => System.Drawing.Color.FromArgb(255, 85, 255),
                GuiColor.BrightYellow => System.Drawing.Color.FromArgb(255, 255, 85),
                GuiColor.White => System.Drawing.Color.White,
                _ => System.Drawing.Color.Black
            };
        }
    }
}
