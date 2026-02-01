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
        private Button _okButton;
        private Button _cancelButton;
        public string Pattern => _patternField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public WildcardMarkingDialog(Configuration config) : base("Wildcard Mark", 60, 8)
        {
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

            _okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            _okButton.Clicked += () =>
            {
                IsOk = true;
                Application.RequestStop();
            };

            _cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            _cancelButton.Clicked += () =>
            {
                IsOk = false;
                Application.RequestStop();
            };

            AddButton(_okButton);
            AddButton(_cancelButton);

            ApplyColors(config.Display);

            _patternField.SetFocus();
        }

        /// <summary>
        /// Shows the wildcard marking dialog and returns the entered pattern if confirmed.
        /// Returns null if cancelled.
        /// </summary>
        public static string? Show(Configuration config)
        {
            var dialog = new WildcardMarkingDialog(config);
            Application.Run(dialog);
            return dialog.IsOk ? dialog.Pattern : null;
        }

        private void ApplyColors(DisplaySettings display)
        {
            if (Application.Driver == null) return;

            // Define attributes based on user requirements
            var btnNormal = Application.Driver.MakeAttribute(Color.Black, Color.Gray);
            var btnFocus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);
            var hotNormal = Application.Driver.MakeAttribute(Color.Cyan, Color.Gray);
            var hotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray);
            var textNormal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);

            this.ColorScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };

            // Apply Input Colors
            _patternField.ColorScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus,
                HotNormal = textNormal,
                HotFocus = btnFocus
            };

            // Explicitly set button colors to ensure they show focus
            var buttonScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _okButton.ColorScheme = buttonScheme;
            _cancelButton.ColorScheme = buttonScheme;
        }
    }
}
