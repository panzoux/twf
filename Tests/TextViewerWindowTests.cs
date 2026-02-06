using Xunit;
using System.Text;
using TWF.Services;
using TWF.UI;
using TWF.Models;
using Terminal.Gui;

namespace TWF.Tests
{
    public class TextViewerWindowTests
    {
        private KeyBindingManager CreateTestKeyBindingManager()
        {
            var keyBindings = new KeyBindingManager();
            keyBindings.SetEnabled(true);
            return keyBindings;
        }

        [Fact]
        public void TextViewerWindow_CanBeCreated()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "test", Encoding.UTF8);
                using (var engine = new LargeFileEngine(tempFile))
                {
                    engine.Initialize(new ViewerSettings());

                    var window = new TextViewerWindow(engine, CreateTestKeyBindingManager(), new SearchEngine());
                    Assert.NotNull(window);
                    Assert.Equal("Text Viewer", window.Title);
                }
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void TextViewerWindow_RequiresEngine()
        {
            Assert.Throws<ArgumentNullException>(() => new TextViewerWindow(null!, CreateTestKeyBindingManager(), new SearchEngine()));
        }

        [Fact]
        public void TextViewerWindow_ToggleHexMode()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "test", Encoding.UTF8);
                using (var engine = new LargeFileEngine(tempFile))
                {
                    engine.Initialize(new ViewerSettings());

                    var window = new TextViewerWindow(engine, CreateTestKeyBindingManager(), new SearchEngine());
                    
                    // Simulate Ctrl+B (default binding) is tricky in unit test without UI loop
                    // But we can check if window initializes
                    Assert.Equal("Text Viewer", window.Title);
                    
                    // Manually trigger toggle if possible, or just verify startInHexMode constructor
                    var hexWindow = new TextViewerWindow(engine, CreateTestKeyBindingManager(), new SearchEngine(), startInHexMode: true);
                    Assert.Equal("Binary Viewer", hexWindow.Title);
                }
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
