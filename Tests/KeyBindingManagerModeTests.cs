using TWF.Models;
using TWF.Services;
using Xunit;

namespace TWF.Tests
{
    /// <summary>
    /// Unit tests for mode-specific key binding functionality
    /// </summary>
    public class KeyBindingManagerModeTests
    {
        [Fact]
        public void GetActionForKey_WithTextViewerMode_ReturnsTextViewerAction()
        {
            // Arrange
            string testFile = CreateTestBindingsFile();
            
            try
            {
                var manager = new KeyBindingManager();
                manager.LoadBindings(testFile);
                
                // Debug: Check if bindings were loaded
                Assert.True(manager.IsEnabled, "Manager should be enabled after loading bindings");
                
                // Debug: Check normal mode binding first
                var normalAction = manager.GetActionForKey("F");
                Assert.Equal("EnterSearchMode", normalAction);
                
                // Act
                var action = manager.GetActionForKey("Home", UiMode.TextViewer);
                
                // Assert
                Assert.Equal("TextViewer.GoToTop", action);
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        
        private static string CreateTestBindingsFile()
        {
            string testFile = Path.Combine(Path.GetTempPath(), $"test_bindings_{Guid.NewGuid()}.json");
            string content = @"{
  ""version"": ""1.0"",
  ""description"": ""Test TextViewer Bindings"",
  ""bindings"": {
    ""F"": ""EnterSearchMode"",
    ""V"": ""ViewFileAsText""
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
            return testFile;
        }
        
        [Fact]
        public void GetActionForKey_WithTextViewerMode_ReturnsSearchAction()
        {
            // Arrange
            string testFile = CreateTestBindingsFile();
            
            try
            {
                var manager = new KeyBindingManager();
                manager.LoadBindings(testFile);
                
                // Act
                var action = manager.GetActionForKey("F4", UiMode.TextViewer);
                
                // Assert
                Assert.Equal("TextViewer.Search", action);
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        
        [Fact]
        public void GetActionForKey_WithTextViewerModeAndModifier_ReturnsCorrectAction()
        {
            // Arrange
            string testFile = CreateTestBindingsFile();
            
            try
            {
                var manager = new KeyBindingManager();
                manager.LoadBindings(testFile);
                
                // Act
                var action = manager.GetActionForKey("Shift+F3", UiMode.TextViewer);
                
                // Assert
                Assert.Equal("TextViewer.FindPrevious", action);
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        
        [Fact]
        public void GetActionForKey_WithNormalMode_DoesNotReturnTextViewerAction()
        {
            // Arrange
            string testFile = CreateTestBindingsFile();
            
            try
            {
                var manager = new KeyBindingManager();
                manager.LoadBindings(testFile);
                
                // Act
                var action = manager.GetActionForKey("Home", UiMode.Normal);
                
                // Assert
                // Should return null because "Home" is not defined in normal bindings in the test file
                // The TextViewer binding should NOT leak into Normal mode
                Assert.Null(action);
                
                // Verify that a key that IS defined in normal bindings works
                var definedAction = manager.GetActionForKey("F", UiMode.Normal);
                Assert.Equal("EnterSearchMode", definedAction);
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        
        [Fact]
        public void GetActionForKey_WithoutTextViewerBindingsSection_ReturnsNull()
        {
            // Arrange
            var manager = new KeyBindingManager();
            manager.LoadBindings("keybindings.json"); // This file doesn't have textViewerBindings
            
            // Act
            var action = manager.GetActionForKey("Home", UiMode.TextViewer);
            
            // Assert
            // Should return null when no TextViewer binding is defined (fallback behavior)
            Assert.Null(action);
        }
        
        [Fact]
        public void LoadBindings_WithInvalidTextViewerAction_LogsWarningAndSkips()
        {
            // Arrange
            string testFile = Path.Combine(Path.GetTempPath(), $"test_invalid_{Guid.NewGuid()}.json");
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
            
            try
            {
                var manager = new KeyBindingManager();
                
                // Act
                manager.LoadBindings(testFile);
                
                // Assert
                // Invalid action should be skipped
                var invalidAction = manager.GetActionForKey("Home", UiMode.TextViewer);
                Assert.Null(invalidAction);
                
                // Valid action should still work
                var validAction = manager.GetActionForKey("End", UiMode.TextViewer);
                Assert.Equal("TextViewer.GoToBottom", validAction);
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        
        [Fact]
        public void LoadBindings_WithUnknownTextViewerAction_LogsWarningAndSkips()
        {
            // Arrange
            string testFile = Path.Combine(Path.GetTempPath(), $"test_unknown_{Guid.NewGuid()}.json");
            string content = @"{
  ""version"": ""1.0"",
  ""description"": ""Test Unknown Action"",
  ""bindings"": {
    ""F"": ""EnterSearchMode""
  },
  ""textViewerBindings"": {
    ""Home"": ""TextViewer.UnknownAction"",
    ""End"": ""TextViewer.GoToBottom""
  }
}";
            File.WriteAllText(testFile, content);
            
            try
            {
                var manager = new KeyBindingManager();
                
                // Act
                manager.LoadBindings(testFile);
                
                // Assert
                // Unknown action should be skipped (even though it has TextViewer. prefix)
                var unknownAction = manager.GetActionForKey("Home", UiMode.TextViewer);
                Assert.Null(unknownAction);
                
                // Valid action should still work
                var validAction = manager.GetActionForKey("End", UiMode.TextViewer);
                Assert.Equal("TextViewer.GoToBottom", validAction);
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
        
        [Fact]
        public void GetActionForKey_WithMultipleTextViewerBindings_ReturnsCorrectActions()
        {
            // Arrange
            string testFile = CreateTestBindingsFile();
            
            try
            {
                var manager = new KeyBindingManager();
                manager.LoadBindings(testFile);
                
                // Act & Assert
                Assert.Equal("TextViewer.GoToTop", manager.GetActionForKey("Home", UiMode.TextViewer));
                Assert.Equal("TextViewer.GoToBottom", manager.GetActionForKey("End", UiMode.TextViewer));
                Assert.Equal("TextViewer.PageUp", manager.GetActionForKey("PageUp", UiMode.TextViewer));
                Assert.Equal("TextViewer.PageDown", manager.GetActionForKey("PageDown", UiMode.TextViewer));
                Assert.Equal("TextViewer.Close", manager.GetActionForKey("Escape", UiMode.TextViewer));
                Assert.Equal("TextViewer.CycleEncoding", manager.GetActionForKey("Shift+E", UiMode.TextViewer));
            }
            finally
            {
                if (File.Exists(testFile))
                {
                    File.Delete(testFile);
                }
            }
        }
    }
}
