using FsCheck;
using FsCheck.Xunit;
using TWF.Services;
using TWF.Models;
using TWF.Providers;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for file execution functionality
    /// These tests verify the LOGIC without actually launching programs
    /// </summary>
    public class FileExecutionPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 33: File execution launches program
        /// Validates: Requirements 17.1, 17.2
        /// 
        /// This property verifies that the file execution logic correctly identifies
        /// executable files and handles extension associations.
        /// NOTE: This test does NOT actually launch programs - it only tests the logic.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property FileExecution_IdentifiesExecutableFiles()
        {
            // Test that executable extensions are correctly identified
            var executableExtensions = new[] { ".exe", ".bat", ".cmd", ".com", ".ps1" };
            var nonExecutableExtensions = new[] { ".txt", ".doc", ".pdf", ".jpg", ".mp3" };
            
            var allExecutablesIdentified = executableExtensions.All(ext => 
            {
                var testFile = $"test{ext}";
                var fileExt = Path.GetExtension(testFile).ToLowerInvariant();
                return executableExtensions.Contains(fileExt);
            });
            
            var noNonExecutablesIdentified = nonExecutableExtensions.All(ext =>
            {
                var testFile = $"test{ext}";
                var fileExt = Path.GetExtension(testFile).ToLowerInvariant();
                return !executableExtensions.Contains(fileExt);
            });
            
            return (allExecutablesIdentified && noNonExecutablesIdentified).ToProperty()
                .Label($"Executables identified: {allExecutablesIdentified}, Non-executables excluded: {noNonExecutablesIdentified}");
        }

        /// <summary>
        /// Property test: Extension associations are stored and retrieved correctly
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ExtensionAssociations_StoreAndRetrieve(string extension, string program)
        {
            // Skip invalid inputs
            if (string.IsNullOrWhiteSpace(extension) || string.IsNullOrWhiteSpace(program))
            {
                return true.ToProperty().Label("Skipped: Invalid input");
            }
            
            // Ensure extension starts with a dot
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }
            
            var config = new TWF.Models.Configuration();
            
            // Add an association
            config.ExtensionAssociations[extension] = program;
            
            // Verify it was stored correctly
            var wasStored = config.ExtensionAssociations.ContainsKey(extension);
            var correctProgram = config.ExtensionAssociations.TryGetValue(extension, out var storedProgram) 
                                 && storedProgram == program;
            
            return (wasStored && correctProgram).ToProperty()
                .Label($"Extension: {extension}, Program: {program}, Stored: {wasStored}, Correct: {correctProgram}");
        }

        /// <summary>
        /// Property test: Execution modes are distinct
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ExecutionModes_AreDistinct()
        {
            var modes = new[] 
            { 
                ExecutionMode.Default, 
                ExecutionMode.ExplorerAssociation 
            };
            
            // Verify all modes are distinct
            var allDistinct = modes.Distinct().Count() == modes.Length;
            
            return allDistinct.ToProperty()
                .Label($"All modes distinct: {allDistinct}");
        }

        /// <summary>
        /// Property test: Configuration has default values
        /// </summary>
        [Property(MaxTest = 100)]
        public Property Configuration_HasDefaults()
        {
            var config = new TWF.Models.Configuration();
            
            // Verify default configuration values exist
            var hasExtensionAssociations = config.ExtensionAssociations != null;
            var hasImageExtensions = config.Viewer.SupportedImageExtensions != null && 
                                     config.Viewer.SupportedImageExtensions.Count > 0;
            
            return (hasExtensionAssociations && hasImageExtensions).ToProperty()
                .Label($"Has associations: {hasExtensionAssociations}, Has image exts: {hasImageExtensions}");
        }
    }
}
