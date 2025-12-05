using System.Text;
using TWF.Models;

namespace TWF.Services
{
    /// <summary>
    /// Manages text and image viewers
    /// </summary>
    public class ViewerManager
    {
        private TextViewer? _currentTextViewer;
        private ImageViewer? _currentImageViewer;

        /// <summary>
        /// Opens a text file in the text viewer
        /// </summary>
        /// <param name="filePath">Path to the text file</param>
        /// <param name="encoding">Initial encoding to use</param>
        public void OpenTextViewer(string filePath, Encoding? encoding = null)
        {
            CloseCurrentViewer();
            _currentTextViewer = new TextViewer();
            _currentTextViewer.LoadFile(filePath, encoding ?? Encoding.UTF8);
        }

        /// <summary>
        /// Opens an image file in the image viewer
        /// </summary>
        /// <param name="filePath">Path to the image file</param>
        public void OpenImageViewer(string filePath)
        {
            CloseCurrentViewer();
            _currentImageViewer = new ImageViewer();
            _currentImageViewer.LoadImage(filePath);
        }

        /// <summary>
        /// Closes the currently open viewer
        /// </summary>
        public void CloseCurrentViewer()
        {
            _currentTextViewer = null;
            _currentImageViewer = null;
        }

        /// <summary>
        /// Gets the current text viewer if one is open
        /// </summary>
        public TextViewer? CurrentTextViewer => _currentTextViewer;

        /// <summary>
        /// Gets the current image viewer if one is open
        /// </summary>
        public ImageViewer? CurrentImageViewer => _currentImageViewer;
    }

    /// <summary>
    /// Text file viewer with encoding support
    /// </summary>
    public class TextViewer
    {
        private string _filePath = string.Empty;
        private List<string> _lines = new();
        private Encoding _currentEncoding = Encoding.UTF8;
        private int _currentLine = 0;
        private string _searchPattern = string.Empty;

        // Supported encodings for cycling
        private static readonly Encoding[] SupportedEncodings = GetSupportedEncodings();

        private static Encoding[] GetSupportedEncodings()
        {
            var encodings = new List<Encoding>
            {
                Encoding.UTF8,
                Encoding.Unicode,      // UTF-16 LE
                Encoding.ASCII,
                Encoding.Latin1
            };

            // Try to add Japanese encodings if available
            try
            {
                encodings.Add(Encoding.GetEncoding("shift_jis"));  // Shift-JIS (Japanese)
            }
            catch
            {
                // Encoding not available on this system
            }

            try
            {
                encodings.Add(Encoding.GetEncoding("euc-jp"));     // EUC-JP (Japanese)
            }
            catch
            {
                // Encoding not available on this system
            }

            return encodings.ToArray();
        }

        private int _currentEncodingIndex = 0;

        /// <summary>
        /// Gets the current file path
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Gets the current encoding
        /// </summary>
        public Encoding CurrentEncoding => _currentEncoding;

        /// <summary>
        /// Gets the current line number
        /// </summary>
        public int CurrentLine => _currentLine;

        /// <summary>
        /// Gets all lines of the file
        /// </summary>
        public IReadOnlyList<string> Lines => _lines.AsReadOnly();

        /// <summary>
        /// Loads a file with the specified encoding
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <param name="encoding">Encoding to use</param>
        public void LoadFile(string path, Encoding encoding)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }

            _filePath = path;
            _currentEncoding = encoding;

            // Find the encoding index
            _currentEncodingIndex = Array.FindIndex(SupportedEncodings, e => e.CodePage == encoding.CodePage);
            if (_currentEncodingIndex < 0)
            {
                _currentEncodingIndex = 0;
            }

            // Read the file with the specified encoding
            try
            {
                _lines = File.ReadAllLines(path, encoding).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load file: {ex.Message}", ex);
            }

            _currentLine = 0;
        }

        /// <summary>
        /// Cycles to the next encoding and reloads the file
        /// </summary>
        public void CycleEncoding()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                return;
            }

            _currentEncodingIndex = (_currentEncodingIndex + 1) % SupportedEncodings.Length;
            _currentEncoding = SupportedEncodings[_currentEncodingIndex];

            // Reload the file with the new encoding
            try
            {
                _lines = File.ReadAllLines(_filePath, _currentEncoding).ToList();
            }
            catch
            {
                // If loading fails, keep the previous content
            }
        }

        /// <summary>
        /// Searches for a pattern in the file
        /// </summary>
        /// <param name="pattern">Pattern to search for</param>
        /// <returns>List of line numbers containing the pattern</returns>
        public List<int> Search(string pattern)
        {
            _searchPattern = pattern;
            var matches = new List<int>();

            if (string.IsNullOrWhiteSpace(pattern))
            {
                return matches;
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(i);
                }
            }

            return matches;
        }

        /// <summary>
        /// Scrolls to a specific line
        /// </summary>
        /// <param name="line">Line number to scroll to</param>
        public void ScrollTo(int line)
        {
            if (line >= 0 && line < _lines.Count)
            {
                _currentLine = line;
            }
        }

        /// <summary>
        /// Gets the content of a specific line
        /// </summary>
        /// <param name="lineNumber">Line number (0-based)</param>
        /// <returns>Content of the line, or empty string if out of range</returns>
        public string GetLine(int lineNumber)
        {
            if (lineNumber >= 0 && lineNumber < _lines.Count)
            {
                return _lines[lineNumber];
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets the total number of lines
        /// </summary>
        public int LineCount => _lines.Count;

        /// <summary>
        /// Gets raw bytes from the file starting at the specified offset
        /// </summary>
        /// <param name="startOffset">Starting byte offset</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Array of bytes read from the file</returns>
        public byte[] GetBytes(int startOffset, int length)
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                return Array.Empty<byte>();
            }

            try
            {
                using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                
                // Validate offset
                if (startOffset < 0 || startOffset >= fileStream.Length)
                {
                    return Array.Empty<byte>();
                }

                // Adjust length if it exceeds file size
                long availableBytes = fileStream.Length - startOffset;
                int bytesToRead = (int)Math.Min(length, availableBytes);

                // Seek to start position
                fileStream.Seek(startOffset, SeekOrigin.Begin);

                // Read bytes
                byte[] buffer = new byte[bytesToRead];
                int bytesRead = fileStream.Read(buffer, 0, bytesToRead);

                // Return only the bytes actually read
                if (bytesRead < bytesToRead)
                {
                    Array.Resize(ref buffer, bytesRead);
                }

                return buffer;
            }
            catch (Exception)
            {
                // Return empty array on error
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Gets all bytes from the file
        /// </summary>
        /// <returns>Array containing all file bytes</returns>
        public byte[] GetAllBytes()
        {
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                return Array.Empty<byte>();
            }

            try
            {
                return File.ReadAllBytes(_filePath);
            }
            catch (Exception)
            {
                // Return empty array on error
                return Array.Empty<byte>();
            }
        }
    }

    /// <summary>
    /// Image file viewer with zoom and rotation
    /// </summary>
    public class ImageViewer
    {
        private string _filePath = string.Empty;
        private ViewMode _viewMode = ViewMode.FitToScreen;
        private int _rotation = 0;
        private bool _flipHorizontal = false;
        private bool _flipVertical = false;
        private double _zoomFactor = 1.0;

        /// <summary>
        /// Gets the current file path
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// Gets the current view mode
        /// </summary>
        public ViewMode ViewMode => _viewMode;

        /// <summary>
        /// Gets the current rotation in degrees
        /// </summary>
        public int Rotation => _rotation;

        /// <summary>
        /// Gets whether the image is flipped horizontally
        /// </summary>
        public bool FlipHorizontal => _flipHorizontal;

        /// <summary>
        /// Gets whether the image is flipped vertically
        /// </summary>
        public bool FlipVertical => _flipVertical;

        /// <summary>
        /// Gets the current zoom factor
        /// </summary>
        public double ZoomFactor => _zoomFactor;

        /// <summary>
        /// Loads an image file
        /// </summary>
        /// <param name="path">Path to the image file</param>
        public void LoadImage(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Image file not found: {path}");
            }

            _filePath = path;
            _viewMode = ViewMode.FitToScreen;
            _rotation = 0;
            _flipHorizontal = false;
            _flipVertical = false;
            _zoomFactor = 1.0;
        }

        /// <summary>
        /// Rotates the image by the specified degrees
        /// </summary>
        /// <param name="degrees">Degrees to rotate (typically 90, 180, 270)</param>
        public void Rotate(int degrees)
        {
            _rotation = (_rotation + degrees) % 360;
            if (_rotation < 0)
            {
                _rotation += 360;
            }
        }

        /// <summary>
        /// Flips the image in the specified direction
        /// </summary>
        /// <param name="direction">Direction to flip</param>
        public void Flip(FlipDirection direction)
        {
            switch (direction)
            {
                case FlipDirection.Horizontal:
                    _flipHorizontal = !_flipHorizontal;
                    break;
                case FlipDirection.Vertical:
                    _flipVertical = !_flipVertical;
                    break;
            }
        }

        /// <summary>
        /// Zooms the image by the specified factor
        /// </summary>
        /// <param name="factor">Zoom factor (1.0 = 100%, 2.0 = 200%, etc.)</param>
        public void Zoom(double factor)
        {
            if (factor > 0)
            {
                _zoomFactor = factor;
                _viewMode = ViewMode.FixedZoom;
            }
        }

        /// <summary>
        /// Sets the view mode
        /// </summary>
        /// <param name="mode">View mode to set</param>
        public void SetViewMode(ViewMode mode)
        {
            _viewMode = mode;
            
            // Reset zoom factor for non-fixed zoom modes
            if (mode != ViewMode.FixedZoom)
            {
                _zoomFactor = 1.0;
            }
        }
    }

    /// <summary>
    /// Direction for flipping images
    /// </summary>
    public enum FlipDirection
    {
        Horizontal,
        Vertical
    }
}
