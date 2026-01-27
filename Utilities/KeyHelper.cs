using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace TWF.Utilities
{
    /// <summary>
    /// Helper class for handling Terminal.Gui key events and conversions
    /// </summary>
    public static class KeyHelper
    {
        /// <summary>
        /// Converts a Terminal.Gui Key to a string representation for key binding lookup
        /// </summary>
        public static string ConvertKeyToString(Key key)
        {
            var parts = new List<string>();
            
            // 1. Get Modifiers
            bool hasShift = (key & Key.ShiftMask) == Key.ShiftMask;
            if (hasShift) parts.Add("Shift");
            if ((key & Key.CtrlMask) == Key.CtrlMask) parts.Add("Ctrl");
            if ((key & Key.AltMask) == Key.AltMask) parts.Add("Alt");
            
            // 2. Get Base Key Name
            Key baseKey = key & ~(Key.ShiftMask | Key.CtrlMask | Key.AltMask);
            string keyName = GetBaseKeyName(baseKey);
            
            // 3. Handle uppercase letters as Shift+Letter (Terminal.Gui convention)
            bool isLowercaseLetter = baseKey >= (Key)'a' && baseKey <= (Key)'z';
            if (baseKey >= Key.A && baseKey <= Key.Z && !hasShift && parts.Count == 0 && !isLowercaseLetter)
            {
                parts.Insert(0, "Shift");
            }
            
            parts.Add(keyName);
            return string.Join("+", parts);
        }

        private static string GetBaseKeyName(Key baseKey)
        {
            return baseKey switch
            {
                Key.Enter => "Enter",
                Key.Backspace => "Backspace",
                Key.Tab => "Tab",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.CursorUp => "Up",
                Key.CursorDown => "Down",
                Key.CursorLeft => "Left",
                Key.CursorRight => "Right",
                Key.Space => "Space",
                (Key)27 => "Escape",
                _ => GetOtherKeyName(baseKey)
            };
        }

        private static string GetOtherKeyName(Key baseKey)
        {
            // Handle F keys and Numbers
            if (baseKey >= Key.F1 && baseKey <= Key.F10) return baseKey.ToString();
            if (baseKey >= Key.D0 && baseKey <= Key.D9) return ((char)baseKey).ToString();

            // Handle standard ASCII and symbols
            return baseKey switch
            {
                (Key)'$' => "$", (Key)'@' => "@", (Key)':' => ":", (Key)'`' => "`", (Key)'~' => "~",
                (Key)'!' => "!", (Key)'#' => "#", (Key)'%' => "%", (Key)'^' => "^", (Key)'&' => "&",
                (Key)'*' => "*", (Key)'(' => "(", (Key)')' => ")", (Key)'-' => "-", (Key)'_' => "_",
                (Key)'=' => "=", (Key)'+' => "+", (Key)'[' => "[", (Key)']' => "]", (Key)'{' => "{",
                (Key)'}' => "}", (Key)'|' => "|", (Key)';' => ";", (Key)'\'' => "'", (Key)'"' => "\"",
                (Key)',' => ",", (Key)'.' => ".", (Key)'<' => "<", (Key)'>' => ">", (Key)'/' => "/",
                (Key)'?' => "?",
                _ => baseKey >= Key.A && baseKey <= Key.Z ? ((char)baseKey).ToString() :
                     baseKey >= (Key)'a' && baseKey <= (Key)'z' ? ((char)baseKey).ToString().ToUpper() :
                     ((char)baseKey).ToString()
            };
        }
    }
}
