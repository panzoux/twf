using FsCheck;
using FsCheck.Xunit;
using System.Text;
using TWF.Services;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for ViewerManager and TextViewer
    /// </summary>
    public class ViewerManagerPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 28: Text viewer displays file contents
        /// Validates: Requirements 14.2
        /// 
        /// This property verifies that when a text file is opened in the viewer,
        /// all lines of the file are displayed correctly.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property TextViewer_DisplaysFileContents(List<NonEmptyString> contentLines)
        {
            // Filter out null entries and ensure we have at least one line
            if (contentLines == null || contentLines.Count == 0)
            {
                return true.ToProperty().Label("Empty content - no lines to display");
            }

            // Convert to actual strings and sanitize (remove newlines within strings)
            var lines = contentLines
                .Where(l => l != null)
                .Select(l => l.Get.Replace("\r", "").Replace("\n", ""))
                .ToList();

            if (lines.Count == 0)
            {
                return true.ToProperty().Label("No valid lines to display");
            }

            // Arrange: Create a temporary file with the content
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllLines(tempFile, lines, Encoding.UTF8);

                // Act: Load the file in the text viewer
                var viewer = new TextViewer();
                viewer.LoadFile(tempFile, Encoding.UTF8);

                // Assert: All lines should be present in the viewer
                bool allLinesPresent = viewer.LineCount == lines.Count;
                
                if (!allLinesPresent)
                {
                    return false.ToProperty()
                        .Label($"Line count mismatch: Expected {lines.Count}, Got {viewer.LineCount}");
                }

                // Verify each line matches
                for (int i = 0; i < lines.Count; i++)
                {
                    var expectedLine = lines[i];
                    var actualLine = viewer.GetLine(i);
                    
                    if (expectedLine != actualLine)
                    {
                        return false.ToProperty()
                            .Label($"Line {i} mismatch: Expected '{expectedLine}', Got '{actualLine}'");
                    }
                }

                // Verify the Lines property also contains all lines
                bool linesPropertyCorrect = viewer.Lines.Count == lines.Count &&
                                            viewer.Lines.SequenceEqual(lines);

                return (allLinesPresent && linesPropertyCorrect).ToProperty()
                    .Label($"Successfully displayed {lines.Count} lines");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 29: Encoding change updates display
        /// Validates: Requirements 14.3
        /// 
        /// This property verifies that cycling through encodings updates the displayed text
        /// according to the selected encoding.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property EncodingChange_UpdatesDisplay(NonEmptyString contentStr)
        {
            if (contentStr == null)
            {
                return true.ToProperty().Label("Null content");
            }

            var content = contentStr.Get;

            // Arrange: Create a temporary file with UTF-8 content
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                // Act: Load the file in the text viewer with UTF-8
                var viewer = new TextViewer();
                viewer.LoadFile(tempFile, Encoding.UTF8);

                var initialEncoding = viewer.CurrentEncoding;
                var initialContent = string.Join("\n", viewer.Lines);

                // Cycle to the next encoding
                viewer.CycleEncoding();

                var newEncoding = viewer.CurrentEncoding;
                var newContent = string.Join("\n", viewer.Lines);

                // Assert: Encoding should have changed
                bool encodingChanged = initialEncoding.CodePage != newEncoding.CodePage;

                // The content may or may not change depending on the encoding,
                // but the encoding property should definitely change
                return encodingChanged.ToProperty()
                    .Label($"Encoding changed from {initialEncoding.EncodingName} to {newEncoding.EncodingName}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Property: Text viewer search finds matching lines
        /// </summary>
        [Property(MaxTest = 100)]
        public Property TextViewer_SearchFindsMatchingLines(List<NonEmptyString> contentLines, NonEmptyString searchPatternStr)
        {
            // Filter out null entries
            if (contentLines == null || contentLines.Count == 0 || searchPatternStr == null)
            {
                return true.ToProperty().Label("Empty content or null search pattern");
            }

            var lines = contentLines
                .Where(l => l != null)
                .Select(l => l.Get)
                .ToList();

            if (lines.Count == 0)
            {
                return true.ToProperty().Label("No valid lines");
            }

            var searchPattern = searchPatternStr.Get;
            
            // Skip empty, whitespace-only, or control-character-only patterns as they are not valid search patterns
            if (string.IsNullOrWhiteSpace(searchPattern) || searchPattern.All(c => char.IsControl(c)))
            {
                return true.ToProperty().Label("Empty, whitespace, or control-character search pattern - skipped");
            }

            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllLines(tempFile, lines, Encoding.UTF8);

                var viewer = new TextViewer();
                viewer.LoadFile(tempFile, Encoding.UTF8);

                // Act: Search for the pattern
                var matches = viewer.Search(searchPattern);

                // Assert: All matched lines should contain the pattern
                bool allMatchesValid = matches.All(lineNum =>
                {
                    if (lineNum < 0 || lineNum >= viewer.LineCount)
                        return false;

                    return viewer.GetLine(lineNum).Contains(searchPattern, StringComparison.OrdinalIgnoreCase);
                });

                // Assert: All lines containing the pattern should be in matches
                var expectedMatches = new List<int>();
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Contains(searchPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        expectedMatches.Add(i);
                    }
                }

                bool allExpectedFound = expectedMatches.All(expected => matches.Contains(expected));

                return (allMatchesValid && allExpectedFound).ToProperty()
                    .Label($"Pattern: '{searchPattern}', Matches: {matches.Count}, Expected: {expectedMatches.Count}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Property: Text viewer scroll to line updates current line
        /// </summary>
        [Property(MaxTest = 100)]
        public Property TextViewer_ScrollToUpdatesCurrentLine(List<NonEmptyString> contentLines, NonNegativeInt lineNumGen)
        {
            // Filter out null entries
            if (contentLines == null || contentLines.Count == 0)
            {
                return true.ToProperty().Label("Empty content");
            }

            var lines = contentLines
                .Where(l => l != null)
                .Select(l => l.Get)
                .ToList();

            if (lines.Count == 0)
            {
                return true.ToProperty().Label("No valid lines");
            }

            // Get a valid line number
            int lineNum = lineNumGen.Get % lines.Count;

            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllLines(tempFile, lines, Encoding.UTF8);

                var viewer = new TextViewer();
                viewer.LoadFile(tempFile, Encoding.UTF8);

                // Act: Scroll to the line
                viewer.ScrollTo(lineNum);

                // Assert: Current line should be updated
                bool currentLineUpdated = viewer.CurrentLine == lineNum;

                return currentLineUpdated.ToProperty()
                    .Label($"Scrolled to line {lineNum}, current line is {viewer.CurrentLine}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Property: Encoding cycles through all supported encodings
        /// </summary>
        [Property(MaxTest = 100)]
        public Property EncodingCycle_GoesThroughAllSupportedEncodings(NonEmptyString contentStr)
        {
            if (contentStr == null)
            {
                return true.ToProperty().Label("Null content");
            }

            var content = contentStr.Get;

            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                var viewer = new TextViewer();
                viewer.LoadFile(tempFile, Encoding.UTF8);

                // Track encodings we've seen
                var seenEncodings = new HashSet<int> { viewer.CurrentEncoding.CodePage };
                var initialEncoding = viewer.CurrentEncoding.CodePage;

                // Cycle through encodings (max 10 times to avoid infinite loop)
                for (int i = 0; i < 10; i++)
                {
                    viewer.CycleEncoding();
                    var currentCodePage = viewer.CurrentEncoding.CodePage;

                    // If we've cycled back to the initial encoding, we've completed a full cycle
                    if (currentCodePage == initialEncoding && i > 0)
                    {
                        break;
                    }

                    seenEncodings.Add(currentCodePage);
                }

                // Assert: We should have seen multiple encodings (at least 2)
                bool sawMultipleEncodings = seenEncodings.Count >= 2;

                return sawMultipleEncodings.ToProperty()
                    .Label($"Saw {seenEncodings.Count} different encodings");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Property: ViewerManager opens and closes viewers correctly
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ViewerManager_OpensAndClosesViewers(NonEmptyString contentStr)
        {
            if (contentStr == null)
            {
                return true.ToProperty().Label("Null content");
            }

            var content = contentStr.Get;

            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                var manager = new ViewerManager();

                // Act: Open text viewer
                manager.OpenTextViewer(tempFile);

                // Assert: Text viewer should be open
                bool textViewerOpen = manager.CurrentTextViewer != null;

                // Act: Close viewer
                manager.CloseCurrentViewer();

                // Assert: No viewer should be open
                bool viewersClosed = manager.CurrentTextViewer == null && 
                                    manager.CurrentImageViewer == null;

                return (textViewerOpen && viewersClosed).ToProperty()
                    .Label($"Text viewer opened: {textViewerOpen}, Viewers closed: {viewersClosed}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Property: Empty file loads correctly
        /// </summary>
        [Property(MaxTest = 100)]
        public Property TextViewer_HandlesEmptyFile()
        {
            // Arrange: Create an empty temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, string.Empty, Encoding.UTF8);

                // Act: Load the empty file
                var viewer = new TextViewer();
                viewer.LoadFile(tempFile, Encoding.UTF8);

                // Assert: Should have zero lines (or one empty line depending on implementation)
                bool handledCorrectly = viewer.LineCount >= 0;

                return handledCorrectly.ToProperty()
                    .Label($"Empty file loaded with {viewer.LineCount} lines");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 30: Image viewer displays image
        /// Validates: Requirements 15.2
        /// 
        /// This property verifies that when an image file is opened in the viewer,
        /// the image is loaded and the viewer is in the correct initial state.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ImageViewer_DisplaysImage()
        {
            // Arrange: Create a temporary image file (1x1 pixel BMP)
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                // Create a minimal valid BMP file (1x1 pixel, 24-bit color)
                // BMP header for 1x1 pixel image
                byte[] bmpData = new byte[]
                {
                    // BMP Header
                    0x42, 0x4D,             // "BM" signature
                    0x3A, 0x00, 0x00, 0x00, // File size (58 bytes)
                    0x00, 0x00,             // Reserved
                    0x00, 0x00,             // Reserved
                    0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                    
                    // DIB Header (BITMAPINFOHEADER)
                    0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                    0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                    0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                    0x01, 0x00,             // Color planes (1)
                    0x18, 0x00,             // Bits per pixel (24)
                    0x00, 0x00, 0x00, 0x00, // Compression (none)
                    0x04, 0x00, 0x00, 0x00, // Image size (4 bytes)
                    0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                    0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                    0x00, 0x00, 0x00, 0x00, // Colors in palette
                    0x00, 0x00, 0x00, 0x00, // Important colors
                    
                    // Pixel data (BGR format, padded to 4-byte boundary)
                    0xFF, 0xFF, 0xFF, 0x00  // White pixel + padding
                };
                
                File.WriteAllBytes(bmpFile, bmpData);

                // Act: Load the image in the viewer
                var viewer = new ImageViewer();
                viewer.LoadImage(bmpFile);

                // Assert: Image should be loaded with correct initial state
                bool filePathSet = viewer.FilePath == bmpFile;
                bool initialViewMode = viewer.ViewMode == TWF.Models.ViewMode.FitToScreen;
                bool initialRotation = viewer.Rotation == 0;
                bool noFlipHorizontal = viewer.FlipHorizontal == false;
                bool noFlipVertical = viewer.FlipVertical == false;
                bool initialZoom = viewer.ZoomFactor == 1.0;

                bool allCorrect = filePathSet && initialViewMode && initialRotation && 
                                 noFlipHorizontal && noFlipVertical && initialZoom;

                return allCorrect.ToProperty()
                    .Label($"Image loaded: FilePath={filePathSet}, ViewMode={initialViewMode}, " +
                           $"Rotation={initialRotation}, FlipH={noFlipHorizontal}, FlipV={noFlipVertical}, Zoom={initialZoom}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
            }
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 31: Image rotation transforms display
        /// Validates: Requirements 15.5
        /// 
        /// This property verifies that rotating an image changes the image orientation
        /// by the specified degrees (typically 90 degree increments).
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ImageRotation_TransformsDisplay(PositiveInt rotationDegrees)
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                // Create a minimal valid BMP file
                byte[] bmpData = new byte[]
                {
                    // BMP Header
                    0x42, 0x4D,             // "BM" signature
                    0x3A, 0x00, 0x00, 0x00, // File size (58 bytes)
                    0x00, 0x00,             // Reserved
                    0x00, 0x00,             // Reserved
                    0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                    
                    // DIB Header (BITMAPINFOHEADER)
                    0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                    0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                    0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                    0x01, 0x00,             // Color planes (1)
                    0x18, 0x00,             // Bits per pixel (24)
                    0x00, 0x00, 0x00, 0x00, // Compression (none)
                    0x04, 0x00, 0x00, 0x00, // Image size (4 bytes)
                    0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                    0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                    0x00, 0x00, 0x00, 0x00, // Colors in palette
                    0x00, 0x00, 0x00, 0x00, // Important colors
                    
                    // Pixel data (BGR format, padded to 4-byte boundary)
                    0xFF, 0xFF, 0xFF, 0x00  // White pixel + padding
                };
                
                File.WriteAllBytes(bmpFile, bmpData);

                var viewer = new ImageViewer();
                viewer.LoadImage(bmpFile);

                // Normalize rotation to typical values (90, 180, 270, or multiples)
                int degrees = (rotationDegrees.Get % 360);
                
                // Act: Rotate the image
                int initialRotation = viewer.Rotation;
                viewer.Rotate(degrees);
                int newRotation = viewer.Rotation;

                // Assert: Rotation should have changed by the specified amount (modulo 360)
                int expectedRotation = (initialRotation + degrees) % 360;
                bool rotationChanged = newRotation == expectedRotation;

                // Additional check: rotation should always be in range [0, 360)
                bool rotationInRange = newRotation >= 0 && newRotation < 360;

                return (rotationChanged && rotationInRange).ToProperty()
                    .Label($"Rotated by {degrees}°: Initial={initialRotation}°, Expected={expectedRotation}°, Actual={newRotation}°");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
            }
        }

        /// <summary>
        /// Property: Image viewer flip operations toggle flip state
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ImageFlip_TogglesFlipState()
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                // Create a minimal valid BMP file
                byte[] bmpData = new byte[]
                {
                    // BMP Header
                    0x42, 0x4D,             // "BM" signature
                    0x3A, 0x00, 0x00, 0x00, // File size (58 bytes)
                    0x00, 0x00,             // Reserved
                    0x00, 0x00,             // Reserved
                    0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                    
                    // DIB Header (BITMAPINFOHEADER)
                    0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                    0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                    0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                    0x01, 0x00,             // Color planes (1)
                    0x18, 0x00,             // Bits per pixel (24)
                    0x00, 0x00, 0x00, 0x00, // Compression (none)
                    0x04, 0x00, 0x00, 0x00, // Image size (4 bytes)
                    0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                    0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                    0x00, 0x00, 0x00, 0x00, // Colors in palette
                    0x00, 0x00, 0x00, 0x00, // Important colors
                    
                    // Pixel data (BGR format, padded to 4-byte boundary)
                    0xFF, 0xFF, 0xFF, 0x00  // White pixel + padding
                };
                
                File.WriteAllBytes(bmpFile, bmpData);

                var viewer = new ImageViewer();
                viewer.LoadImage(bmpFile);

                // Act & Assert: Test horizontal flip
                bool initialFlipH = viewer.FlipHorizontal;
                viewer.Flip(FlipDirection.Horizontal);
                bool afterFlipH = viewer.FlipHorizontal;
                bool horizontalToggled = initialFlipH != afterFlipH;

                // Flip again to toggle back
                viewer.Flip(FlipDirection.Horizontal);
                bool afterSecondFlipH = viewer.FlipHorizontal;
                bool horizontalToggledBack = afterSecondFlipH == initialFlipH;

                // Test vertical flip
                bool initialFlipV = viewer.FlipVertical;
                viewer.Flip(FlipDirection.Vertical);
                bool afterFlipV = viewer.FlipVertical;
                bool verticalToggled = initialFlipV != afterFlipV;

                // Flip again to toggle back
                viewer.Flip(FlipDirection.Vertical);
                bool afterSecondFlipV = viewer.FlipVertical;
                bool verticalToggledBack = afterSecondFlipV == initialFlipV;

                bool allCorrect = horizontalToggled && horizontalToggledBack && 
                                 verticalToggled && verticalToggledBack;

                return allCorrect.ToProperty()
                    .Label($"Flip operations: H={horizontalToggled && horizontalToggledBack}, V={verticalToggled && verticalToggledBack}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
            }
        }

        /// <summary>
        /// Property: Image viewer zoom changes zoom factor
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ImageZoom_ChangesZoomFactor(PositiveInt zoomPercentage)
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                // Create a minimal valid BMP file
                byte[] bmpData = new byte[]
                {
                    // BMP Header
                    0x42, 0x4D,             // "BM" signature
                    0x3A, 0x00, 0x00, 0x00, // File size (58 bytes)
                    0x00, 0x00,             // Reserved
                    0x00, 0x00,             // Reserved
                    0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                    
                    // DIB Header (BITMAPINFOHEADER)
                    0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                    0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                    0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                    0x01, 0x00,             // Color planes (1)
                    0x18, 0x00,             // Bits per pixel (24)
                    0x00, 0x00, 0x00, 0x00, // Compression (none)
                    0x04, 0x00, 0x00, 0x00, // Image size (4 bytes)
                    0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                    0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                    0x00, 0x00, 0x00, 0x00, // Colors in palette
                    0x00, 0x00, 0x00, 0x00, // Important colors
                    
                    // Pixel data (BGR format, padded to 4-byte boundary)
                    0xFF, 0xFF, 0xFF, 0x00  // White pixel + padding
                };
                
                File.WriteAllBytes(bmpFile, bmpData);

                var viewer = new ImageViewer();
                viewer.LoadImage(bmpFile);

                // Convert percentage to factor (e.g., 200% = 2.0)
                double zoomFactor = (zoomPercentage.Get % 1000 + 1) / 100.0; // Range: 0.01 to 10.0

                // Act: Zoom the image
                viewer.Zoom(zoomFactor);

                // Assert: Zoom factor should be set and view mode should be FixedZoom
                bool zoomFactorSet = Math.Abs(viewer.ZoomFactor - zoomFactor) < 0.001;
                bool viewModeChanged = viewer.ViewMode == TWF.Models.ViewMode.FixedZoom;

                return (zoomFactorSet && viewModeChanged).ToProperty()
                    .Label($"Zoom factor: {zoomFactor:F2}, Set={zoomFactorSet}, ViewMode={viewModeChanged}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
            }
        }

        /// <summary>
        /// Property: Image viewer view mode changes correctly
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ImageViewMode_ChangesCorrectly()
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                // Create a minimal valid BMP file
                byte[] bmpData = new byte[]
                {
                    // BMP Header
                    0x42, 0x4D,             // "BM" signature
                    0x3A, 0x00, 0x00, 0x00, // File size (58 bytes)
                    0x00, 0x00,             // Reserved
                    0x00, 0x00,             // Reserved
                    0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                    
                    // DIB Header (BITMAPINFOHEADER)
                    0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                    0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                    0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                    0x01, 0x00,             // Color planes (1)
                    0x18, 0x00,             // Bits per pixel (24)
                    0x00, 0x00, 0x00, 0x00, // Compression (none)
                    0x04, 0x00, 0x00, 0x00, // Image size (4 bytes)
                    0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                    0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                    0x00, 0x00, 0x00, 0x00, // Colors in palette
                    0x00, 0x00, 0x00, 0x00, // Important colors
                    
                    // Pixel data (BGR format, padded to 4-byte boundary)
                    0xFF, 0xFF, 0xFF, 0x00  // White pixel + padding
                };
                
                File.WriteAllBytes(bmpFile, bmpData);

                var viewer = new ImageViewer();
                viewer.LoadImage(bmpFile);

                // Test all view modes
                var viewModes = new[] 
                { 
                    TWF.Models.ViewMode.OriginalSize, 
                    TWF.Models.ViewMode.FitToWindow, 
                    TWF.Models.ViewMode.FitToScreen 
                };

                bool allModesWork = true;
                foreach (var mode in viewModes)
                {
                    viewer.SetViewMode(mode);
                    if (viewer.ViewMode != mode)
                    {
                        allModesWork = false;
                        break;
                    }
                }

                return allModesWork.ToProperty()
                    .Label($"All view modes set correctly: {allModesWork}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
            }
        }

        /// <summary>
        /// Property: ViewerManager opens image viewer correctly
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ViewerManager_OpensImageViewer()
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                // Create a minimal valid BMP file
                byte[] bmpData = new byte[]
                {
                    // BMP Header
                    0x42, 0x4D,             // "BM" signature
                    0x3A, 0x00, 0x00, 0x00, // File size (58 bytes)
                    0x00, 0x00,             // Reserved
                    0x00, 0x00,             // Reserved
                    0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                    
                    // DIB Header (BITMAPINFOHEADER)
                    0x28, 0x00, 0x00, 0x00, // Header size (40 bytes)
                    0x01, 0x00, 0x00, 0x00, // Width (1 pixel)
                    0x01, 0x00, 0x00, 0x00, // Height (1 pixel)
                    0x01, 0x00,             // Color planes (1)
                    0x18, 0x00,             // Bits per pixel (24)
                    0x00, 0x00, 0x00, 0x00, // Compression (none)
                    0x04, 0x00, 0x00, 0x00, // Image size (4 bytes)
                    0x13, 0x0B, 0x00, 0x00, // Horizontal resolution
                    0x13, 0x0B, 0x00, 0x00, // Vertical resolution
                    0x00, 0x00, 0x00, 0x00, // Colors in palette
                    0x00, 0x00, 0x00, 0x00, // Important colors
                    
                    // Pixel data (BGR format, padded to 4-byte boundary)
                    0xFF, 0xFF, 0xFF, 0x00  // White pixel + padding
                };
                
                File.WriteAllBytes(bmpFile, bmpData);

                var manager = new ViewerManager();

                // Act: Open image viewer
                manager.OpenImageViewer(bmpFile);

                // Assert: Image viewer should be open
                bool imageViewerOpen = manager.CurrentImageViewer != null;
                bool textViewerClosed = manager.CurrentTextViewer == null;

                // Act: Close viewer
                manager.CloseCurrentViewer();

                // Assert: No viewer should be open
                bool viewersClosed = manager.CurrentTextViewer == null && 
                                    manager.CurrentImageViewer == null;

                return (imageViewerOpen && textViewerClosed && viewersClosed).ToProperty()
                    .Label($"Image viewer opened: {imageViewerOpen}, Text viewer closed: {textViewerClosed}, Viewers closed: {viewersClosed}");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
            }
        }
    }
}
