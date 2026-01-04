using Xunit;
using TWF.Services;
using TWF.UI;
using TWF.Models;

namespace TWF.Tests
{
    /// <summary>
    /// Tests for ImageViewerWindow UI component
    /// </summary>
    public class ImageViewerWindowTests
    {
        /// <summary>
        /// Creates a minimal valid BMP file for testing
        /// </summary>
        private byte[] CreateTestBitmap()
        {
            // Create a minimal 1x1 pixel BMP file (54 byte header + 4 bytes pixel data)
            var bmpData = new byte[]
            {
                // BMP Header (14 bytes)
                0x42, 0x4D,             // "BM" signature
                0x3A, 0x00, 0x00, 0x00, // File size (58 bytes)
                0x00, 0x00,             // Reserved
                0x00, 0x00,             // Reserved
                0x36, 0x00, 0x00, 0x00, // Offset to pixel data (54 bytes)
                
                // DIB Header (40 bytes - BITMAPINFOHEADER)
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
                
                // Pixel data (4 bytes - 1 pixel BGR + padding)
                0xFF, 0xFF, 0xFF, 0x00  // White pixel + padding
            };
            
            return bmpData;
        }

        [Fact]
        public void ImageViewerWindow_CanBeCreated()
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                File.WriteAllBytes(bmpFile, CreateTestBitmap());

                var imageViewer = new ImageViewer();
                imageViewer.LoadImage(bmpFile);

                // Act: Create the window
                var keyBindings = new KeyBindingManager();
                var config = new Configuration();
                var window = new ImageViewerWindow(imageViewer, keyBindings, config);

                // Assert: Window should be created successfully
                Assert.NotNull(window);
                Assert.Equal("Image Viewer", window.Title);
                Assert.True(window.Modal);
            }
            finally
            {
                // Cleanup
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void ImageViewerWindow_RequiresImageViewer()
        {
            var keyBindings = new KeyBindingManager();
            var config = new Configuration();
            // Act & Assert: Creating window with null viewer should throw
            Assert.Throws<ArgumentNullException>(() => new ImageViewerWindow(null!, keyBindings, config));
        }

        [Fact]
        public void ImageViewerWindow_InitializesWithDefaultScrollPosition()
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                File.WriteAllBytes(bmpFile, CreateTestBitmap());

                var imageViewer = new ImageViewer();
                imageViewer.LoadImage(bmpFile);

                // Act: Create the window
                var keyBindings = new KeyBindingManager();
                var config = new Configuration();
                var window = new ImageViewerWindow(imageViewer, keyBindings, config);
                var scrollPosition = window.GetScrollPosition();

                // Assert: Scroll position should start at (0, 0)
                Assert.Equal(0, scrollPosition.X);
                Assert.Equal(0, scrollPosition.Y);
            }
            finally
            {
                // Cleanup
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void ImageViewerWindow_DisplaysImageWithDefaultViewMode()
        {
            // Arrange: Create a temporary image file
            var tempFile = Path.GetTempFileName();
            var bmpFile = Path.ChangeExtension(tempFile, ".bmp");
            
            try
            {
                File.WriteAllBytes(bmpFile, CreateTestBitmap());

                var imageViewer = new ImageViewer();
                imageViewer.LoadImage(bmpFile);

                // Act: Create the window
                var keyBindings = new KeyBindingManager();
                var config = new Configuration();
                var window = new ImageViewerWindow(imageViewer, keyBindings, config);

                // Assert: Window should be created and image viewer should have default view mode
                Assert.NotNull(window);
                Assert.Equal(ViewMode.FitToScreen, imageViewer.ViewMode);
            }
            finally
            {
                // Cleanup
                if (File.Exists(bmpFile))
                {
                    File.Delete(bmpFile);
                }
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
