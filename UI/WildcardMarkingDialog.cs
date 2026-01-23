using Terminal.Gui;
using TWF.Models;
using TWF.Utilities;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for entering wildcard or regex patterns to mark files
    /// </summary>
    public class WildcardMarkingDialog : Dialog
    {
        private TextField _patternField;
        public string Pattern => _patternField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public WildcardMarkingDialog(Configuration config) : base("Wildcard Mark", 60, 8)
        {
            ApplyColors(config.Display);

            var label = new Label("Enter pattern (* = any chars, ? = single char):")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(label);

            _patternField = new TextField("")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            Add(_patternField);

            var helpFg = ColorHelper.ParseConfigColor(config.Display.DialogHelpForegroundColor, Color.BrightYellow);
            var helpBg = ColorHelper.ParseConfigColor(config.Display.DialogHelpBackgroundColor, Color.Blue);

            var helpLabel = new Label("Prefix with : to exclude. Use m/ for regex.")
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(helpFg, helpBg)
                }
            };
            Add(helpLabel);

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            okButton.Clicked += () =>
            {
                IsOk = true;
                Application.RequestStop();
            };

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            cancelButton.Clicked += () =>
            {
                IsOk = false;
                Application.RequestStop();
            };

            AddButton(okButton);
            AddButton(cancelButton);

            _patternField.SetFocus();
        }

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            this.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };

            // Apply Input Colors
            var inputFg = ColorHelper.ParseConfigColor(display.InputForegroundColor, Color.White);
            var inputBg = ColorHelper.ParseConfigColor(display.InputBackgroundColor, Color.Black);
            _patternField.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(inputFg, inputBg),
                Focus = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotNormal = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotFocus = Application.Driver.MakeAttribute(inputFg, inputBg)
            };
        }
    }
}
