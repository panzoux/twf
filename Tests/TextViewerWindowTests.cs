using Xunit;
using System.Text;
using TWF.Services;
using TWF.UI;

namespace TWF.Tests
{
    /// <summary>
    /// Tests for TextViewerWindow UI component
    /// </summary>
    public class TextViewerWindowTests
    {
        [Fact]
        public void TextViewerWindow_CanBeCreated()
        {
            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                var content = "Line 1\nLine 2\nLine 3";
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempFile, Encoding.UTF8);

                // Act: Create the window
                var window = new TextViewerWindow(textViewer);

                // Assert: Window should be created successfully
                Assert.NotNull(window);
                Assert.Equal("Text Viewer", window.Title);
                Assert.True(window.Modal);
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

        [Fact]
        public void TextViewerWindow_RequiresTextViewer()
        {
            // Act & Assert: Creating window with null viewer should throw
            Assert.Throws<ArgumentNullException>(() => new TextViewerWindow(null!));
        }

        [Fact]
        public void TextViewerWindow_DisplaysFileWithLineNumbers()
        {
            // Arrange: Create a temporary file with known content
            var tempFile = Path.GetTempFileName();
            try
            {
                var lines = new[] { "First line", "Second line", "Third line" };
                File.WriteAllLines(tempFile, lines, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempFile, Encoding.UTF8);

                // Act: Create the window
                var window = new TextViewerWindow(textViewer);

                // Assert: Window should be created and contain the file data
                Assert.NotNull(window);
                Assert.Equal(3, textViewer.LineCount);
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
    }
}
