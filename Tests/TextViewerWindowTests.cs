using Xunit;
using System.Text;
using TWF.Services;
using TWF.UI;
using TWF.Models;

namespace TWF.Tests
{
    /// <summary>
    /// Tests for TextViewerWindow UI component
    /// </summary>
    public class TextViewerWindowTests
    {
        private KeyBindingManager CreateTestKeyBindingManager()
        {
            var keyBindings = new KeyBindingManager();
            // Load default bindings for testing
            keyBindings.SetEnabled(true);
            return keyBindings;
        }
        
        private KeyBindingManager CreateCustomKeyBindingManager()
        {
            string testFile = Path.Combine(Path.GetTempPath(), $"test_viewer_bindings_{Guid.NewGuid()}.json");
            string content = @"{
  ""version"": ""1.0"",
  ""description"": ""Test Custom TextViewer Bindings"",
  ""bindings"": {
    ""F"": ""EnterSearchMode""
  },
  ""textViewerBindings"": {
    ""Home"": ""TextViewer.GoToTop"",
    ""End"": ""TextViewer.GoToBottom"",
    ""PageUp"": ""TextViewer.PageUp"",
    ""PageDown"": ""TextViewer.PageDown"",
    ""Escape"": ""TextViewer.Close"",
    ""F4"": ""TextViewer.Search"",
    ""F3"": ""TextViewer.FindNext"",
    ""Shift+F3"": ""TextViewer.FindPrevious"",
    ""Shift+E"": ""TextViewer.CycleEncoding""
  }
}";
            File.WriteAllText(testFile, content);
            
            var keyBindings = new KeyBindingManager();
            keyBindings.LoadBindings(testFile);
            
            // Clean up the temp file
            File.Delete(testFile);
            
            return keyBindings;
        }

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
                var keyBindings = CreateTestKeyBindingManager();

                // Act: Create the window
                var window = new TextViewerWindow(textViewer, keyBindings);

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
            // Arrange
            var keyBindings = CreateTestKeyBindingManager();

            // Act & Assert: Creating window with null viewer should throw
            Assert.Throws<ArgumentNullException>(() => new TextViewerWindow(null!, keyBindings));
        }

        [Fact]
        public void TextViewerWindow_RequiresKeyBindingManager()
        {
            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                var content = "Line 1\nLine 2\nLine 3";
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempFile, Encoding.UTF8);

                // Act & Assert: Creating window with null keyBindings should throw
                Assert.Throws<ArgumentNullException>(() => new TextViewerWindow(textViewer, null!));
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
                var keyBindings = CreateTestKeyBindingManager();

                // Act: Create the window
                var window = new TextViewerWindow(textViewer, keyBindings);

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
        
        [Fact]
        public void TextViewerWindow_WithCustomBindings_UsesCustomKeybindings()
        {
            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                var content = "Line 1\nLine 2\nLine 3";
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempFile, Encoding.UTF8);
                var keyBindings = CreateCustomKeyBindingManager();

                // Act: Create the window with custom keybindings
                var window = new TextViewerWindow(textViewer, keyBindings);

                // Assert: Window should be created successfully with custom bindings
                Assert.NotNull(window);
                
                // Verify that custom bindings are loaded
                Assert.Equal("TextViewer.GoToTop", keyBindings.GetActionForKey("Home", UiMode.TextViewer));
                Assert.Equal("TextViewer.Search", keyBindings.GetActionForKey("F4", UiMode.TextViewer));
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
        public void TextViewerWindow_WithoutCustomBindings_FallsBackToDefaults()
        {
            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                var content = "Line 1\nLine 2\nLine 3";
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempFile, Encoding.UTF8);
                
                // Create a KeyBindingManager without textViewerBindings section
                var keyBindings = new KeyBindingManager();
                keyBindings.LoadBindings("keybindings.json"); // Default file without textViewerBindings

                // Act: Create the window
                var window = new TextViewerWindow(textViewer, keyBindings);

                // Assert: Window should be created successfully
                Assert.NotNull(window);
                
                // Verify that no custom TextViewer bindings are loaded (returns null)
                Assert.Null(keyBindings.GetActionForKey("Home", UiMode.TextViewer));
                Assert.Null(keyBindings.GetActionForKey("F4", UiMode.TextViewer));
                
                // The window should still work with hardcoded defaults
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
        public void TextViewerWindow_AllTextViewerActions_AreRecognized()
        {
            // Arrange: Create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                var content = "Line 1\nLine 2\nLine 3";
                File.WriteAllText(tempFile, content, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempFile, Encoding.UTF8);
                var keyBindings = CreateCustomKeyBindingManager();

                // Act: Create the window
                var window = new TextViewerWindow(textViewer, keyBindings);

                // Assert: Verify all expected actions are bound
                var expectedActions = new[]
                {
                    ("Home", "TextViewer.GoToTop"),
                    ("End", "TextViewer.GoToBottom"),
                    ("PageUp", "TextViewer.PageUp"),
                    ("PageDown", "TextViewer.PageDown"),
                    ("Escape", "TextViewer.Close"),
                    ("F4", "TextViewer.Search"),
                    ("F3", "TextViewer.FindNext"),
                    ("Shift+F3", "TextViewer.FindPrevious"),
                    ("Shift+E", "TextViewer.CycleEncoding")
                };

                foreach (var (key, expectedAction) in expectedActions)
                {
                    var action = keyBindings.GetActionForKey(key, UiMode.TextViewer);
                    Assert.Equal(expectedAction, action);
                }
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
        public void TextViewerWindow_WithInvalidAction_IgnoresInvalidBinding()
        {
            // Arrange: Create a config with an invalid action
            string testFile = Path.Combine(Path.GetTempPath(), $"test_invalid_action_{Guid.NewGuid()}.json");
            string content = @"{
  ""version"": ""1.0"",
  ""description"": ""Test Invalid Action"",
  ""bindings"": {
    ""F"": ""EnterSearchMode""
  },
  ""textViewerBindings"": {
    ""Home"": ""InvalidAction"",
    ""End"": ""TextViewer.GoToBottom""
  }
}";
            File.WriteAllText(testFile, content);
            
            var tempViewerFile = Path.GetTempFileName();
            try
            {
                var fileContent = "Line 1\nLine 2\nLine 3";
                File.WriteAllText(tempViewerFile, fileContent, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempViewerFile, Encoding.UTF8);
                
                var keyBindings = new KeyBindingManager();
                keyBindings.LoadBindings(testFile);

                // Act: Create the window
                var window = new TextViewerWindow(textViewer, keyBindings);

                // Assert: Invalid action should be ignored (returns null)
                Assert.Null(keyBindings.GetActionForKey("Home", UiMode.TextViewer));
                
                // Valid action should still work
                Assert.Equal("TextViewer.GoToBottom", keyBindings.GetActionForKey("End", UiMode.TextViewer));
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
                if (File.Exists(tempViewerFile))
                {
                    File.Delete(tempViewerFile);
                }
            }
        }
        
        [Fact]
        public void TextViewerWindow_BackwardCompatibility_WorksWithoutTextViewerBindings()
        {
            // Arrange: Create a config file without textViewerBindings section (old format)
            string testFile = Path.Combine(Path.GetTempPath(), $"test_old_format_{Guid.NewGuid()}.json");
            string content = @"{
  ""version"": ""1.0"",
  ""description"": ""Old Format Without TextViewer Bindings"",
  ""bindings"": {
    ""F"": ""EnterSearchMode"",
    ""Tab"": ""SwitchPane""
  }
}";
            File.WriteAllText(testFile, content);
            
            var tempViewerFile = Path.GetTempFileName();
            try
            {
                var fileContent = "Line 1\nLine 2\nLine 3";
                File.WriteAllText(tempViewerFile, fileContent, Encoding.UTF8);

                var textViewer = new TextViewer();
                textViewer.LoadFile(tempViewerFile, Encoding.UTF8);
                
                var keyBindings = new KeyBindingManager();
                keyBindings.LoadBindings(testFile);

                // Act: Create the window with old format config
                var window = new TextViewerWindow(textViewer, keyBindings);

                // Assert: Window should be created successfully
                Assert.NotNull(window);
                
                // Normal mode bindings should work
                Assert.Equal("EnterSearchMode", keyBindings.GetActionForKey("F", UiMode.Normal));
                
                // TextViewer bindings should return null (will fall back to hardcoded defaults)
                Assert.Null(keyBindings.GetActionForKey("Home", UiMode.TextViewer));
                Assert.Null(keyBindings.GetActionForKey("F4", UiMode.TextViewer));
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
                if (File.Exists(tempViewerFile))
                {
                    File.Delete(tempViewerFile);
                }
            }
        }
    }
}
