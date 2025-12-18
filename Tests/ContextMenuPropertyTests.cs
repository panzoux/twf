using Xunit;
using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Providers;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for context menu functionality
    /// **Feature: twf-file-manager, Property 44: Context menu shows applicable operations**
    /// **Validates: Requirements 24.3**
    /// </summary>
    public class ContextMenuPropertyTests
    {
        private readonly ConfigurationProvider _configProvider;
        private readonly ListProvider _listProvider;

        public ContextMenuPropertyTests()
        {
            _configProvider = new ConfigurationProvider();
            _listProvider = new ListProvider(_configProvider);
        }

        /// <summary>
        /// Property 44: Context menu shows applicable operations
        /// For any file selection state, the context menu should display operations that are valid for the current selection
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ContextMenu_ShowsApplicableOperations_ForAnyFileSelectionState()
        {
            // Generator for file entries with various properties
            var fileEntryGen = Gen.OneOf<FileEntry?>(
                // Directory entry
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "TestDirectory",
                    IsDirectory = true,
                    IsArchive = false,
                    Extension = string.Empty
                }),
                // Regular file
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "test.txt",
                    IsDirectory = false,
                    IsArchive = false,
                    Extension = ".txt"
                }),
                // Archive file
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "test.zip",
                    IsDirectory = false,
                    IsArchive = true,
                    Extension = ".zip"
                }),
                // Image file
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "test.png",
                    IsDirectory = false,
                    IsArchive = false,
                    Extension = ".png"
                }),
                // Null entry (empty pane)
                Gen.Constant<FileEntry?>(null)
            );


            // Generator for marked files state
            var hasMarkedFilesGen = Arb.Generate<bool>();

            return Prop.ForAll(
                fileEntryGen.ToArbitrary(),
                hasMarkedFilesGen.ToArbitrary(),
                (entry, hasMarkedFiles) =>
                {
                    // Act
                    var menu = _listProvider.GetContextMenu(entry, hasMarkedFiles);

                    // Assert: Menu should not be null
                    if (menu == null)
                        return false;

                    // Assert: Menu should not be empty
                    if (menu.Count == 0)
                        return false;

                    // Assert: For null entry (empty pane), should show basic operations
                    if (entry == null)
                    {
                        return menu.Any(m => m.Action == "Refresh") &&
                               menu.Any(m => m.Action == "CreateDirectory");
                    }

                    // Assert: For directory, should show Navigate action
                    if (entry.IsDirectory)
                    {
                        return menu.Any(m => m.Action == "Navigate");
                    }

                    // Assert: For regular file, should show Execute action
                    if (!entry.IsDirectory && !entry.IsArchive)
                    {
                        if (!menu.Any(m => m.Action == "Execute"))
                            return false;
                    }

                    // Assert: For archive file, should show archive-specific actions
                    if (entry.IsArchive)
                    {
                        if (!menu.Any(m => m.Action == "BrowseArchive"))
                            return false;
                        if (!menu.Any(m => m.Action == "ExtractArchive"))
                            return false;
                    }

                    // Assert: When marked files exist, should show batch operation options
                    if (hasMarkedFiles)
                    {
                        return menu.Any(m => m.Label.Contains("Marked"));
                    }

                    // Assert: All non-directory files should have Copy, Move, Delete actions
                    if (!entry.IsDirectory)
                    {
                        return menu.Any(m => m.Action == "Copy") &&
                               menu.Any(m => m.Action == "Move") &&
                               menu.Any(m => m.Action == "Delete");
                    }

                    return true;
                });
        }

        /// <summary>
        /// Property: Context menu for text files includes view text option
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ContextMenu_ForTextFile_IncludesViewTextOption()
        {
            var textExtensions = new[] { ".txt", ".log", ".md", ".cs", ".json", ".xml", ".config", ".ini" };
            
            var textFileGen = Gen.Elements(textExtensions)
                .Select(ext => new FileEntry
                {
                    Name = $"test{ext}",
                    Extension = ext,
                    IsDirectory = false,
                    IsArchive = false
                });

            return Prop.ForAll(
                textFileGen.ToArbitrary(),
                (textFile) =>
                {
                    // Act
                    var menu = _listProvider.GetContextMenu(textFile, false);

                    // Assert: Should include ViewText action
                    return menu.Any(m => m.Action == "ViewText");
                });
        }

        /// <summary>
        /// Property: Context menu for image files includes view image option
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ContextMenu_ForImageFile_IncludesViewImageOption()
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".ico" };
            
            var imageFileGen = Gen.Elements(imageExtensions)
                .Select(ext => new FileEntry
                {
                    Name = $"test{ext}",
                    Extension = ext,
                    IsDirectory = false,
                    IsArchive = false
                });

            return Prop.ForAll(
                imageFileGen.ToArbitrary(),
                (imageFile) =>
                {
                    // Act
                    var menu = _listProvider.GetContextMenu(imageFile, false);

                    // Assert: Should include ViewImage action
                    return menu.Any(m => m.Action == "ViewImage");
                });
        }

        /// <summary>
        /// Property: Context menu always includes Properties action for non-null entries
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ContextMenu_ForNonNullEntry_IncludesPropertiesAction()
        {
            var fileEntryGen = Gen.OneOf(
                Gen.Constant(new FileEntry
                {
                    Name = "TestDirectory",
                    IsDirectory = true,
                    IsArchive = false
                }),
                Gen.Constant(new FileEntry
                {
                    Name = "test.txt",
                    IsDirectory = false,
                    IsArchive = false,
                    Extension = ".txt"
                })
            );

            return Prop.ForAll(
                fileEntryGen.ToArbitrary(),
                (entry) =>
                {
                    // Act
                    var menu = _listProvider.GetContextMenu(entry, false);

                    // Assert: Should include Properties action
                    return menu.Any(m => m.Action == "Properties");
                });
        }

        /// <summary>
        /// Property: Context menu with marked files shows clear marks option
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ContextMenu_WithMarkedFiles_ShowsClearMarksOption()
        {
            var fileEntryGen = Gen.Constant(new FileEntry
            {
                Name = "test.txt",
                IsDirectory = false,
                IsArchive = false,
                Extension = ".txt"
            });

            return Prop.ForAll(
                fileEntryGen.ToArbitrary(),
                (entry) =>
                {
                    // Act
                    var menu = _listProvider.GetContextMenu(entry, hasMarkedFiles: true);

                    // Assert: Should include ClearMarks action
                    return menu.Any(m => m.Action == "ClearMarks");
                });
        }

        /// <summary>
        /// Property: Context menu items have valid labels and actions
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ContextMenu_AllItems_HaveValidLabelsAndActions()
        {
            // Generator for file entries with various properties
            var fileEntryGen = Gen.OneOf<FileEntry?>(
                // Directory entry
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "TestDirectory",
                    IsDirectory = true,
                    IsArchive = false,
                    Extension = string.Empty
                }),
                // Regular file
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "test.txt",
                    IsDirectory = false,
                    IsArchive = false,
                    Extension = ".txt"
                }),
                // Archive file
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "test.zip",
                    IsDirectory = false,
                    IsArchive = true,
                    Extension = ".zip"
                }),
                // Image file
                Gen.Constant<FileEntry?>(new FileEntry
                {
                    Name = "test.png",
                    IsDirectory = false,
                    IsArchive = false,
                    Extension = ".png"
                }),
                // Null entry (empty pane)
                Gen.Constant<FileEntry?>(null)
            );


            var hasMarkedFilesGen = Arb.Generate<bool>();

            return Prop.ForAll(
                fileEntryGen.ToArbitrary(),
                hasMarkedFilesGen.ToArbitrary(),
                (entry, hasMarkedFiles) =>
                {
                    // Act
                    var menu = _listProvider.GetContextMenu(entry, hasMarkedFiles);

                    // Assert: All non-separator items should have non-empty labels and actions
                    return menu.All(item =>
                        item.IsSeparator ||
                        (!string.IsNullOrEmpty(item.Label) && !string.IsNullOrEmpty(item.Action))
                    );
                });
        }
    }
}
