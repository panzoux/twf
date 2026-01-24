using Terminal.Gui;
using TWF.Models;
using TWF.Utilities;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for pattern-based batch renaming
    /// </summary>
    public class PatternRenameDialog : Dialog
    {
        private TextField _patternField;
        private TextField _replacementField;
        public string Pattern => _patternField.Text.ToString() ?? string.Empty;
        public string Replacement => _replacementField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        private Button _okButton;
        private Button _cancelButton;

        public PatternRenameDialog(DisplaySettings? displaySettings = null) : base("Pattern Rename", 60, 10)
        {
            var label1 = new Label("Search pattern (e.g. s/old/new/ or .txt):")
            {
                X = 1,
                Y = 1
            };
            Add(label1);

            _patternField = new TextField("")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            Add(_patternField);

            var label2 = new Label("Replacement (leave empty if using s/ or tr/):")
            {
                X = 1,
                Y = 3
            };
            Add(label2);

            _replacementField = new TextField("")
            {
                X = 1,
                Y = 4,
                Width = Dim.Fill(1)
            };
            Add(_replacementField);

            _okButton = new Button("OK", is_default: true);
            _okButton.Clicked += () =>
            {
                IsOk = true;
                Application.RequestStop();
            };

            _cancelButton = new Button("Cancel");
            _cancelButton.Clicked += () =>
            {
                IsOk = false;
                Application.RequestStop();
            };

            AddButton(_okButton);
            AddButton(_cancelButton);

            if (displaySettings != null) ApplyColors(displaySettings);

            _patternField.SetFocus();
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
            var inputScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus,
                HotNormal = textNormal,
                HotFocus = btnFocus
            };
            _patternField.ColorScheme = inputScheme;
            _replacementField.ColorScheme = inputScheme;

            // Explicitly set button colors
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
