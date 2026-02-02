using Terminal.Gui;
using TWF.Models;

namespace TWF.Utilities
{
    public static class ColorHelper
    {
        public static Color ParseConfigColor(string? name, Color defaultColor)
        {
            if (string.IsNullOrEmpty(name)) return defaultColor;
            return name.ToLower() switch
            {
                "black" => Color.Black,
                "blue" => Color.Blue,
                "green" => Color.Green,
                "cyan" => Color.Cyan,
                "red" => Color.Red,
                "magenta" => Color.Magenta,
                "brown" => Color.Brown,
                "gray" => Color.Gray,
                "darkgray" => Color.DarkGray,
                "brightblue" => Color.BrightBlue,
                "brightgreen" => Color.BrightGreen,
                "brightcyan" => Color.BrightCyan,
                "brightred" => Color.BrightRed,
                "brightmagenta" => Color.BrightMagenta,
                "yellow" => Color.Brown,
                "white" => Color.White,
                _ => Enum.TryParse<Color>(name, true, out var color) ? color : defaultColor
            };
        }

        /// <summary>
        /// Applies a standard color scheme to a dialog and its components.
        /// </summary>
        public static void ApplyStandardDialogColors(Dialog dialog, DisplaySettings display, IEnumerable<View>? buttons = null, IEnumerable<View>? textFields = null, ColorScheme? inputOverrideScheme = null)
        {
            if (Application.Driver == null) return;

            var dialogFg = ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            
            var btnFg = ParseConfigColor(display.ButtonForegroundColor, Color.Black);
            var btnBg = ParseConfigColor(display.ButtonBackgroundColor, Color.Gray);
            var btnFocusFg = ParseConfigColor(display.ButtonFocusForegroundColor, Color.White);
            var btnFocusBg = ParseConfigColor(display.ButtonFocusBackgroundColor, Color.DarkGray);

            var inputFg = ParseConfigColor(display.InputForegroundColor, Color.White);
            var inputBg = ParseConfigColor(display.InputBackgroundColor, Color.DarkGray);
            
            var highlightFg = ParseConfigColor(display.HighlightForegroundColor, Color.Black);
            var highlightBg = ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);

            var dialogScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
            };
            dialog.ColorScheme = dialogScheme;

            if (buttons != null)
            {
                var buttonScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(btnFg, btnBg),
                    Focus = Application.Driver.MakeAttribute(btnFocusFg, btnFocusBg),
                    HotNormal = Application.Driver.MakeAttribute(Color.Cyan, btnBg),
                    HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, btnFocusBg)
                };
                foreach (var btn in buttons) btn.ColorScheme = buttonScheme;
            }

            if (textFields != null)
            {
                var inputScheme = inputOverrideScheme ?? new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(inputFg, inputBg),
                    Focus = Application.Driver.MakeAttribute(btnFocusFg, btnFocusBg),
                    HotNormal = Application.Driver.MakeAttribute(inputFg, inputBg),
                    HotFocus = Application.Driver.MakeAttribute(btnFocusFg, btnFocusBg)
                };
                foreach (var field in textFields) field.ColorScheme = inputScheme;
            }
        }
    }
}
