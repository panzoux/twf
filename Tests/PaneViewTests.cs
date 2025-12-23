using Xunit;
using TWF.UI;
using TWF.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace TWF.Tests
{
    /// <summary>
    /// Unit tests for PaneView UI component
    /// </summary>
    public class PaneViewTests
    {
        [Fact]
        public void PaneView_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var paneView = new PaneView();
            
            // Assert
            Assert.NotNull(paneView);
            Assert.True(paneView.CanFocus);
            Assert.Null(paneView.State);
            Assert.False(paneView.IsActive);
        }
        
        [Fact]
        public void PaneView_SetState_UpdatesState()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false, Size = 100 },
                    new FileEntry { Name = "file2.txt", IsDirectory = false, Size = 200 }
                }
            };
            
            // Act
            paneView.State = state;
            
            // Assert
            Assert.Equal(state, paneView.State);
        }
        
        [Fact]
        public void PaneView_SetIsActive_UpdatesActiveState()
        {
            // Arrange
            var paneView = new PaneView();
            
            // Act
            paneView.IsActive = true;
            
            // Assert
            Assert.True(paneView.IsActive);
        }
        
        [Fact]
        public void PaneView_MoveCursorDown_IncreasesCursorPosition()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 0,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false },
                    new FileEntry { Name = "file3.txt", IsDirectory = false }
                }
            };
            paneView.State = state;
            
            // Act
            paneView.MoveCursorDown();
            
            // Assert
            Assert.Equal(1, state.CursorPosition);
        }
        
        [Fact]
        public void PaneView_MoveCursorUp_DecreasesCursorPosition()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 2,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false },
                    new FileEntry { Name = "file3.txt", IsDirectory = false }
                }
            };
            paneView.State = state;
            
            // Act
            paneView.MoveCursorUp();
            
            // Assert
            Assert.Equal(1, state.CursorPosition);
        }
        
        [Fact]
        public void PaneView_MoveCursorToFirst_SetsCursorToZero()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 5,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false },
                    new FileEntry { Name = "file3.txt", IsDirectory = false }
                }
            };
            paneView.State = state;
            
            // Act
            paneView.MoveCursorToFirst();
            
            // Assert
            Assert.Equal(0, state.CursorPosition);
        }
        
        [Fact]
        public void PaneView_MoveCursorToLast_SetsCursorToLastEntry()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 0,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false },
                    new FileEntry { Name = "file3.txt", IsDirectory = false }
                }
            };
            paneView.State = state;
            
            // Act
            paneView.MoveCursorToLast();
            
            // Assert
            Assert.Equal(2, state.CursorPosition);
        }
        
        [Fact]
        public void PaneView_ToggleMark_AddsMarkToUnmarkedEntry()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 0,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false }
                }
            };
            paneView.State = state;
            
            // Act
            paneView.ToggleMark();
            
            // Assert
            Assert.True(state.Entries[0].IsMarked);
        }
        
        [Fact]
        public void PaneView_ToggleMark_RemovesMarkFromMarkedEntry()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 0,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false }
                }
            };
            state.Entries[0].IsMarked = true;
            paneView.State = state;
            
            // Act
            paneView.ToggleMark();
            
            // Assert
            Assert.False(state.Entries[0].IsMarked);
        }
        
        [Fact]
        public void PaneView_GetCurrentEntry_ReturnsEntryAtCursor()
        {
            // Arrange
            var paneView = new PaneView();
            var entry1 = new FileEntry { Name = "file1.txt", IsDirectory = false };
            var entry2 = new FileEntry { Name = "file2.txt", IsDirectory = false };
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 1,
                Entries = new List<FileEntry> { entry1, entry2 }
            };
            paneView.State = state;
            
            // Act
            var currentEntry = paneView.GetCurrentEntry();
            
            // Assert
            Assert.Equal(entry2, currentEntry);
        }
        
        [Fact]
        public void PaneView_GetMarkedEntries_ReturnsMarkedFiles()
        {
            // Arrange
            var paneView = new PaneView();
            var entry1 = new FileEntry { Name = "file1.txt", IsDirectory = false };
            var entry2 = new FileEntry { Name = "file2.txt", IsDirectory = false };
            var entry3 = new FileEntry { Name = "file3.txt", IsDirectory = false };
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                Entries = new List<FileEntry> { entry1, entry2, entry3 }
            };
            state.Entries[0].IsMarked = true;
            state.Entries[2].IsMarked = true;
            paneView.State = state;
            
            // Act
            var markedEntries = paneView.GetMarkedEntries();
            
            // Assert
            Assert.Equal(2, markedEntries.Count);
            Assert.Contains(entry1, markedEntries);
            Assert.Contains(entry3, markedEntries);
        }
        
        [Fact]
        public void PaneView_MoveCursorDown_DoesNotExceedBounds()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 2,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false },
                    new FileEntry { Name = "file3.txt", IsDirectory = false }
                }
            };
            paneView.State = state;
            
            // Act
            paneView.MoveCursorDown();
            
            // Assert - cursor should stay at last position
            Assert.Equal(2, state.CursorPosition);
        }
        
        [Fact]
        public void PaneView_MoveCursorUp_DoesNotGoBelowZero()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                CursorPosition = 0,
                Entries = new List<FileEntry>
                {
                    new FileEntry { Name = "file1.txt", IsDirectory = false },
                    new FileEntry { Name = "file2.txt", IsDirectory = false }
                }
            };
            paneView.State = state;
            
            // Act
            paneView.MoveCursorUp();
            
            // Assert - cursor should stay at 0
            Assert.Equal(0, state.CursorPosition);
        }
        
        [Fact]
        public void PaneView_WithEmptyEntries_HandlesGracefully()
        {
            // Arrange
            var paneView = new PaneView();
            var state = new PaneState
            {
                CurrentPath = @"C:\Test",
                Entries = new List<FileEntry>()
            };
            paneView.State = state;
            
            // Act & Assert - should not throw
            paneView.MoveCursorDown();
            paneView.MoveCursorUp();
            paneView.ToggleMark();
            var current = paneView.GetCurrentEntry();
            var marked = paneView.GetMarkedEntries();
            
            Assert.Null(current);
            Assert.Empty(marked);
        }
    }
}
