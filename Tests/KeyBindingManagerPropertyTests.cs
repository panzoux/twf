using FsCheck;
using FsCheck.Xunit;
using TWF.Models;
using TWF.Services;

namespace TWF.Tests
{
    /// <summary>
    /// Property-based tests for KeyBindingManager
    /// </summary>
    public class KeyBindingManagerPropertyTests
    {
        /// <summary>
        /// Feature: twf-file-manager, Property 38: Custom key binding overrides default
        /// Validates: Requirements 21.2
        /// 
        /// This property verifies that when a custom key binding is defined,
        /// it overrides any default binding for that key.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property CustomKeyBinding_OverridesDefault(
            NonNegativeInt virtualKeyGen,
            bool shift,
            bool ctrl,
            bool alt,
            NonEmptyString commandStr)
        {
            // Generate a valid virtual key code (0-255)
            int virtualKey = virtualKeyGen.Get % 256;
            
            // Encode the key with modifiers
            int keyCode = KeyBindingManager.EncodeKeyCode(virtualKey, shift, ctrl, alt);
            
            // Sanitize the command string
            string command = SanitizeCommand(commandStr.Get);
            
            // Arrange: Create a KeyBindingManager
            var manager = new KeyBindingManager();
            
            // Enable custom bindings
            manager.SetEnabled(true);
            
            // Set a custom binding for the key
            var customBinding = new ActionBinding
            {
                Type = ActionType.Command,
                Target = command
            };
            
            manager.SetBinding(keyCode, customBinding, UiMode.Normal);
            
            // Act: Get the binding for the key
            var retrievedBinding = manager.GetBinding(keyCode, UiMode.Normal);
            
            // Assert: The retrieved binding should match the custom binding
            bool bindingExists = retrievedBinding != null;
            bool typeMatches = retrievedBinding?.Type == ActionType.Command;
            bool targetMatches = retrievedBinding?.Target == command;
            
            return (bindingExists && typeMatches && targetMatches).ToProperty()
                .Label($"KeyCode: {keyCode} (VK:{virtualKey}, Shift:{shift}, Ctrl:{ctrl}, Alt:{alt}), Command: '{command}', Retrieved: {retrievedBinding?.Target ?? "null"}");
        }
        
        /// <summary>
        /// Feature: twf-file-manager, Property 40: Mode-specific bindings apply correctly
        /// Validates: Requirements 21.6
        /// 
        /// This property verifies that for any UI mode (Normal, Image Viewer, Text Viewer),
        /// the system applies the key bindings defined for that specific mode.
        /// Bindings set for one mode should not affect other modes.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property ModeSpecificBindings_ApplyCorrectly(
            NonNegativeInt virtualKeyGen,
            NonEmptyString command1,
            NonEmptyString command2,
            NonEmptyString command3)
        {
            // Generate a valid virtual key code
            int virtualKey = virtualKeyGen.Get % 256;
            int keyCode = KeyBindingManager.EncodeKeyCode(virtualKey);
            
            // Sanitize commands - ensure they're all different
            string normalCommand = SanitizeCommand(command1.Get);
            string imageViewerCommand = SanitizeCommand(command2.Get);
            string textViewerCommand = SanitizeCommand(command3.Get);
            
            // Ensure commands are distinct by appending mode suffix if needed
            if (normalCommand == imageViewerCommand)
                imageViewerCommand = imageViewerCommand + "_img";
            if (normalCommand == textViewerCommand)
                textViewerCommand = textViewerCommand + "_txt";
            if (imageViewerCommand == textViewerCommand)
                textViewerCommand = textViewerCommand + "_2";
            
            // Arrange: Create a KeyBindingManager
            var manager = new KeyBindingManager();
            manager.SetEnabled(true);
            
            // Set different bindings for each mode
            var normalBinding = new ActionBinding
            {
                Type = ActionType.Command,
                Target = normalCommand
            };
            
            var imageViewerBinding = new ActionBinding
            {
                Type = ActionType.Command,
                Target = imageViewerCommand
            };
            
            var textViewerBinding = new ActionBinding
            {
                Type = ActionType.Command,
                Target = textViewerCommand
            };
            
            manager.SetBinding(keyCode, normalBinding, UiMode.Normal);
            manager.SetBinding(keyCode, imageViewerBinding, UiMode.ImageViewer);
            manager.SetBinding(keyCode, textViewerBinding, UiMode.TextViewer);
            
            // Act: Retrieve bindings for each mode
            var normalRetrieved = manager.GetBinding(keyCode, UiMode.Normal);
            var imageViewerRetrieved = manager.GetBinding(keyCode, UiMode.ImageViewer);
            var textViewerRetrieved = manager.GetBinding(keyCode, UiMode.TextViewer);
            
            // Assert: Each mode should have its own binding
            bool normalCorrect = normalRetrieved?.Target == normalCommand;
            bool imageViewerCorrect = imageViewerRetrieved?.Target == imageViewerCommand;
            bool textViewerCorrect = textViewerRetrieved?.Target == textViewerCommand;
            
            // Verify that bindings are mode-specific (don't leak between modes)
            bool bindingsAreDistinct = 
                normalRetrieved?.Target != imageViewerRetrieved?.Target &&
                normalRetrieved?.Target != textViewerRetrieved?.Target &&
                imageViewerRetrieved?.Target != textViewerRetrieved?.Target;
            
            bool allBindingsExist = normalRetrieved != null && 
                                   imageViewerRetrieved != null && 
                                   textViewerRetrieved != null;
            
            return (normalCorrect && imageViewerCorrect && textViewerCorrect && 
                    bindingsAreDistinct && allBindingsExist).ToProperty()
                .Label($"KeyCode: {keyCode}, " +
                       $"Normal: '{normalRetrieved?.Target ?? "null"}', " +
                       $"ImageViewer: '{imageViewerRetrieved?.Target ?? "null"}', " +
                       $"TextViewer: '{textViewerRetrieved?.Target ?? "null"}', " +
                       $"Distinct: {bindingsAreDistinct}");
        }
        
        /// <summary>
        /// Property: Key redirects should resolve to target binding
        /// 
        /// This verifies that when a key is redirected to another key,
        /// the system returns the target key's binding.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property KeyRedirect_ResolvesToTargetBinding(
            NonNegativeInt sourceKeyGen,
            NonNegativeInt targetKeyGen,
            NonEmptyString commandStr)
        {
            // Generate valid key codes
            int sourceKey = KeyBindingManager.EncodeKeyCode(sourceKeyGen.Get % 256);
            int targetKey = KeyBindingManager.EncodeKeyCode(targetKeyGen.Get % 256);
            
            // Ensure source and target are different
            if (sourceKey == targetKey)
            {
                targetKey = KeyBindingManager.EncodeKeyCode((targetKeyGen.Get + 1) % 256);
            }
            
            string command = SanitizeCommand(commandStr.Get);
            
            // Arrange
            var manager = new KeyBindingManager();
            manager.SetEnabled(true);
            
            // Set a command binding for the target key
            var targetBinding = new ActionBinding
            {
                Type = ActionType.Command,
                Target = command
            };
            manager.SetBinding(targetKey, targetBinding, UiMode.Normal);
            
            // Set a redirect from source to target
            var redirectBinding = new ActionBinding
            {
                Type = ActionType.KeyRedirect,
                Target = targetKey.ToString()
            };
            manager.SetBinding(sourceKey, redirectBinding, UiMode.Normal);
            
            // Act: Get binding for source key
            var resolvedBinding = manager.GetBinding(sourceKey, UiMode.Normal);
            
            // Assert: Should resolve to the target's command
            bool bindingExists = resolvedBinding != null;
            bool isCommand = resolvedBinding?.Type == ActionType.Command;
            bool commandMatches = resolvedBinding?.Target == command;
            
            return (bindingExists && isCommand && commandMatches).ToProperty()
                .Label($"Source: {sourceKey}, Target: {targetKey}, Command: '{command}', Resolved: '{resolvedBinding?.Target}'");
        }
        
        /// <summary>
        /// Property: Disabled bindings should return null
        /// 
        /// This verifies that when custom bindings are disabled,
        /// GetBinding returns null even if bindings are configured.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property DisabledBindings_ReturnNull(
            NonNegativeInt virtualKeyGen,
            NonEmptyString commandStr)
        {
            // Generate a valid key code
            int keyCode = KeyBindingManager.EncodeKeyCode(virtualKeyGen.Get % 256);
            string command = SanitizeCommand(commandStr.Get);
            
            // Arrange
            var manager = new KeyBindingManager();
            
            // Set a binding
            var binding = new ActionBinding
            {
                Type = ActionType.Command,
                Target = command
            };
            manager.SetBinding(keyCode, binding, UiMode.Normal);
            
            // Ensure bindings are disabled
            manager.SetEnabled(false);
            
            // Act
            var retrievedBinding = manager.GetBinding(keyCode, UiMode.Normal);
            
            // Assert: Should return null when disabled
            bool isNull = retrievedBinding == null;
            
            return isNull.ToProperty()
                .Label($"KeyCode: {keyCode}, Enabled: false, Retrieved: {(retrievedBinding == null ? "null" : "not null")}");
        }
        
        /// <summary>
        /// Property: Encoding and decoding key codes should be reversible
        /// 
        /// This verifies that encoding a key with modifiers and then decoding
        /// returns the original values.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property KeyCodeEncoding_IsReversible(
            NonNegativeInt virtualKeyGen,
            bool shift,
            bool ctrl,
            bool alt)
        {
            // Generate a valid virtual key code (0-255)
            int originalVirtualKey = virtualKeyGen.Get % 256;
            
            // Act: Encode and then decode
            int encoded = KeyBindingManager.EncodeKeyCode(originalVirtualKey, shift, ctrl, alt);
            var (decodedVirtualKey, decodedShift, decodedCtrl, decodedAlt) = KeyBindingManager.DecodeKeyCode(encoded);
            
            // Assert: Decoded values should match original
            bool virtualKeyMatches = decodedVirtualKey == originalVirtualKey;
            bool shiftMatches = decodedShift == shift;
            bool ctrlMatches = decodedCtrl == ctrl;
            bool altMatches = decodedAlt == alt;
            
            return (virtualKeyMatches && shiftMatches && ctrlMatches && altMatches).ToProperty()
                .Label($"Original: VK={originalVirtualKey}, S={shift}, C={ctrl}, A={alt} | " +
                       $"Encoded: {encoded} | " +
                       $"Decoded: VK={decodedVirtualKey}, S={decodedShift}, C={decodedCtrl}, A={decodedAlt}");
        }
        
        /// <summary>
        /// Property: Loading bindings from file should populate bindings
        /// 
        /// This verifies that when a valid key binding file is loaded,
        /// the bindings are correctly parsed and stored.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property LoadBindings_PopulatesBindings(
            NonNegativeInt virtualKeyGen,
            NonEmptyString commandStr)
        {
            // Generate a valid virtual key code
            int virtualKey = virtualKeyGen.Get % 256;
            string command = SanitizeCommand(commandStr.Get);
            
            // Create a temporary key binding file
            string tempFile = Path.GetTempFileName();
            
            try
            {
                // Write a simple key binding file
                string keyCode = virtualKey.ToString("D4");
                string content = $@"[KEYCUST]
ON=1

[NORMAL]
K0000=""{keyCode}{command}""
";
                File.WriteAllText(tempFile, content);
                
                // Arrange
                var manager = new KeyBindingManager();
                
                // Act: Load bindings from file
                manager.LoadBindings(tempFile);
                
                // Get the binding
                int encodedKey = KeyBindingManager.EncodeKeyCode(virtualKey);
                var binding = manager.GetBinding(encodedKey, UiMode.Normal);
                
                // Assert: Binding should exist and match
                bool bindingExists = binding != null;
                bool isCommand = binding?.Type == ActionType.Command;
                bool commandMatches = binding?.Target == command;
                bool isEnabled = manager.IsEnabled;
                
                return (bindingExists && isCommand && commandMatches && isEnabled).ToProperty()
                    .Label($"VirtualKey: {virtualKey}, Command: '{command}', Retrieved: '{binding?.Target}', Enabled: {isEnabled}");
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
        /// Feature: twf-file-manager, Property 39: Key binding with modifiers recognized
        /// Validates: Requirements 21.5
        /// 
        /// This property verifies that key bindings with SHIFT, CTRL, or ALT modifiers
        /// are properly recognized and can be retrieved correctly.
        /// For any key binding with modifiers, the system should recognize and execute
        /// the modified key combination.
        /// </summary>
        [Property(MaxTest = 100)]
        public Property KeyBindingWithModifiers_IsRecognized(
            NonNegativeInt virtualKeyGen,
            bool shift,
            bool ctrl,
            bool alt,
            NonEmptyString commandStr)
        {
            // Ensure at least one modifier is set (otherwise it's not testing modifiers)
            if (!shift && !ctrl && !alt)
            {
                shift = true; // Force at least one modifier
            }
            
            // Generate a valid virtual key code (0-255)
            int virtualKey = virtualKeyGen.Get % 256;
            string command = SanitizeCommand(commandStr.Get);
            
            // Arrange: Create a KeyBindingManager
            var manager = new KeyBindingManager();
            manager.SetEnabled(true);
            
            // Encode the key with modifiers
            int keyCodeWithModifiers = KeyBindingManager.EncodeKeyCode(virtualKey, shift, ctrl, alt);
            
            // Set a binding for the key with modifiers
            var binding = new ActionBinding
            {
                Type = ActionType.Command,
                Target = command
            };
            manager.SetBinding(keyCodeWithModifiers, binding, UiMode.Normal);
            
            // Act: Retrieve the binding using the same key code with modifiers
            var retrievedBinding = manager.GetBinding(keyCodeWithModifiers, UiMode.Normal);
            
            // Assert: The binding should be recognized and retrieved correctly
            bool bindingExists = retrievedBinding != null;
            bool typeMatches = retrievedBinding?.Type == ActionType.Command;
            bool targetMatches = retrievedBinding?.Target == command;
            
            // Also verify that the key WITHOUT modifiers doesn't retrieve this binding
            int keyCodeWithoutModifiers = KeyBindingManager.EncodeKeyCode(virtualKey);
            var bindingWithoutModifiers = manager.GetBinding(keyCodeWithoutModifiers, UiMode.Normal);
            bool modifiersAreDistinct = keyCodeWithModifiers == keyCodeWithoutModifiers || 
                                       bindingWithoutModifiers == null ||
                                       bindingWithoutModifiers.Target != command;
            
            string modifierStr = $"Shift:{shift}, Ctrl:{ctrl}, Alt:{alt}";
            
            return (bindingExists && typeMatches && targetMatches && modifiersAreDistinct).ToProperty()
                .Label($"VirtualKey: {virtualKey}, Modifiers: [{modifierStr}], " +
                       $"KeyCode: {keyCodeWithModifiers}, Command: '{command}', " +
                       $"Retrieved: '{retrievedBinding?.Target ?? "null"}', " +
                       $"ModifiersDistinct: {modifiersAreDistinct}");
        }
        
        /// <summary>
        /// Helper method to sanitize command strings
        /// </summary>
        private static string SanitizeCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return "notepad.exe";
            
            // Remove quotes, newlines, and colons that could break the format
            // Colons are special in the key binding format (used for key redirects)
            var sanitized = command
                .Replace("\"", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(":", "")
                .Trim();
            
            if (string.IsNullOrWhiteSpace(sanitized))
                return "notepad.exe";
            
            // Limit length to avoid extremely long commands
            if (sanitized.Length > 100)
                sanitized = sanitized.Substring(0, 100);
            
            return sanitized;
        }
    }
}
