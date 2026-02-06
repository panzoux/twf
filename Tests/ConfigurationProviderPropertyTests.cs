using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Providers;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for ConfigurationProvider
    /// </summary>
    public class ConfigurationProviderPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 45: Session state restoration preserves paths
        /// Validates: Requirements 25.1
        /// </summary>
        [Property(MaxTest = 100)]
        public Property SessionRestore_PreservesPaths(NonEmptyString leftPath, NonEmptyString rightPath)
        {
            // Arrange: Create a temporary directory for this test
            var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDir);

            try
            {
                var provider = new ConfigurationProvider(testDir);
                
                // Sanitize paths to be valid
                var sanitizedLeftPath = SanitizePath(leftPath.Get);
                var sanitizedRightPath = SanitizePath(rightPath.Get);

                var state = new SessionState
                {
                    LeftPath = sanitizedLeftPath,
                    RightPath = sanitizedRightPath
                };

                // Act: Save and restore session state
                provider.SaveSessionState(state);
                var restored = provider.LoadSessionState();

                // Assert: Paths should be preserved
                var pathsMatch = restored.LeftPath == sanitizedLeftPath &&
                                restored.RightPath == sanitizedRightPath;

                return pathsMatch.ToProperty()
                    .Label($"Expected paths to be preserved. Original: ({sanitizedLeftPath}, {sanitizedRightPath}), " +
                           $"Restored: ({restored.LeftPath}, {restored.RightPath})");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 46: Session state restoration preserves settings
        /// Validates: Requirements 25.2, 25.3
        /// </summary>
        [Property(MaxTest = 100)]
        public Property SessionRestore_PreservesSettings(
            NonEmptyString leftMask, 
            NonEmptyString rightMask, 
            SortMode leftSort, 
            SortMode rightSort,
            DisplayMode leftDisplay,
            DisplayMode rightDisplay,
            bool leftPaneActive)
        {
            // Arrange: Create a temporary directory for this test
            var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDir);

            try
            {
                var provider = new ConfigurationProvider(testDir);
                
                // Sanitize masks to be valid file patterns
                var sanitizedLeftMask = SanitizeMask(leftMask.Get);
                var sanitizedRightMask = SanitizeMask(rightMask.Get);

                var state = new SessionState
                {
                    LeftPath = "C:\\",
                    RightPath = "C:\\",
                    LeftMask = sanitizedLeftMask,
                    RightMask = sanitizedRightMask,
                    LeftSort = leftSort,
                    RightSort = rightSort,
                    LeftDisplayMode = leftDisplay,
                    RightDisplayMode = rightDisplay,
                    LeftPaneActive = leftPaneActive
                };

                // Act: Save and restore session state
                provider.SaveSessionState(state);
                var restored = provider.LoadSessionState();

                // Assert: All settings should be preserved
                var settingsMatch = restored.LeftMask == sanitizedLeftMask &&
                                   restored.RightMask == sanitizedRightMask &&
                                   restored.LeftSort == leftSort &&
                                   restored.RightSort == rightSort &&
                                   restored.LeftDisplayMode == leftDisplay &&
                                   restored.RightDisplayMode == rightDisplay &&
                                   restored.LeftPaneActive == leftPaneActive;

                return settingsMatch.ToProperty()
                    .Label($"Expected all settings to be preserved. " +
                           $"Masks: ({sanitizedLeftMask}, {sanitizedRightMask}) -> ({restored.LeftMask}, {restored.RightMask}), " +
                           $"Sort: ({leftSort}, {rightSort}) -> ({restored.LeftSort}, {restored.RightSort}), " +
                           $"Display: ({leftDisplay}, {rightDisplay}) -> ({restored.LeftDisplayMode}, {restored.RightDisplayMode}), " +
                           $"Active: {leftPaneActive} -> {restored.LeftPaneActive}");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        /// <summary>
        /// Feature: twf-file-manager, Property 41: Configuration changes apply
        /// Validates: Requirements 22.2
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ConfigurationChanges_Apply(
            NonEmptyString foregroundColor,
            NonEmptyString backgroundColor,
            DisplayMode defaultDisplayMode,
            bool showHiddenFiles,
            bool saveSessionState,
            PositiveInt compressionLevel)
        {
            // Arrange: Create a temporary directory for this test
            var testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDir);

            try
            {
                var provider = new ConfigurationProvider(testDir);
                
                // Sanitize and constrain values to valid ranges
                var sanitizedForegroundColor = SanitizeString(foregroundColor.Get);
                var sanitizedBackgroundColor = SanitizeString(backgroundColor.Get);
                var validCompressionLevel = Math.Max(0, Math.Min(9, compressionLevel.Get));

                // Create configuration with specific settings
                var config = new TWF.Models.Configuration
                {
                    Display = new DisplaySettings
                    {
                        ForegroundColor = sanitizedForegroundColor,
                        BackgroundColor = sanitizedBackgroundColor,
                        DefaultDisplayMode = defaultDisplayMode,
                        ShowHiddenFiles = showHiddenFiles
                    },
                    SaveSessionState = saveSessionState,
                    Archive = new ArchiveSettings
                    {
                        CompressionLevel = validCompressionLevel
                    }
                };

                // Act: Save and reload configuration
                provider.SaveConfiguration(config);
                var reloaded = provider.LoadConfiguration();

                // Assert: All configuration changes should be applied (preserved after save/load)
                var changesApplied = 
                    reloaded.Display.ForegroundColor == sanitizedForegroundColor &&
                    reloaded.Display.BackgroundColor == sanitizedBackgroundColor &&
                    reloaded.Display.DefaultDisplayMode == defaultDisplayMode &&
                    reloaded.Display.ShowHiddenFiles == showHiddenFiles &&
                    reloaded.SaveSessionState == saveSessionState &&
                    reloaded.Archive.CompressionLevel == validCompressionLevel;

                return changesApplied.ToProperty()
                    .Label($"Expected configuration changes to be applied. " +
                           $"Colors: {sanitizedForegroundColor}/{sanitizedBackgroundColor} -> {reloaded.Display.ForegroundColor}/{reloaded.Display.BackgroundColor}, " +
                           $"DisplayMode: {defaultDisplayMode} -> {reloaded.Display.DefaultDisplayMode}, " +
                           $"ShowHidden: {showHiddenFiles} -> {reloaded.Display.ShowHiddenFiles}, " +
                           $"SaveSession: {saveSessionState} -> {reloaded.SaveSessionState}, " +
                           $"Compression: {validCompressionLevel} -> {reloaded.Archive.CompressionLevel}");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        /// <summary>
        /// Helper method to sanitize path strings for testing
        /// </summary>
        private string SanitizePath(string path)
        {
            // Remove invalid path characters
            var invalidChars = new HashSet<char>(Path.GetInvalidPathChars());
            var sb = new System.Text.StringBuilder();
            foreach (char c in path) if (!invalidChars.Contains(c)) sb.Append(c);
            var sanitized = sb.ToString();
            
            // Ensure it's not empty
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "C:\\";
            }
            
            // Limit length to avoid path too long errors
            if (sanitized.Length > 200)
            {
                sanitized = sanitized.Substring(0, 200);
            }

            return sanitized;
        }

        /// <summary>
        /// Helper method to sanitize mask strings for testing
        /// </summary>
        private string SanitizeMask(string mask)
        {
            // Remove invalid filename characters
            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            var sb = new System.Text.StringBuilder();
            foreach (char c in mask) if (!invalidChars.Contains(c)) sb.Append(c);
            var sanitized = sb.ToString();
            
            // Ensure it's not empty and has at least a wildcard
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "*";
            }
            
            // Limit length
            if (sanitized.Length > 50)
            {
                sanitized = sanitized.Substring(0, 50);
            }

            return sanitized;
        }

        /// <summary>
        /// Helper method to sanitize general strings for testing
        /// </summary>
        private string SanitizeString(string input)
        {
            // Remove control characters and trim
            var sb = new System.Text.StringBuilder();
            foreach (char c in input) if (!char.IsControl(c)) sb.Append(c);
            var sanitized = sb.ToString().Trim();
            
            // Ensure it's not empty
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "Default";
            }
            
            // Limit length
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100);
            }

            return sanitized;
        }
    }
}
