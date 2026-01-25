using Terminal.Gui;
using TWF.Models;
using TWF.Utilities;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for simple file renaming
    /// </summary>
    public class SimpleRenameDialog : Dialog
    {
        private TextField _nameField;
        public string NewName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        private Button _okButton;
        private Button _cancelButton;

        public SimpleRenameDialog(string currentName, DisplaySettings? displaySettings = null) : base("Rename", 60, 8)
        {
            var label = new Label("New name:")
            {
                X = 1,
                Y = 1
            };
            Add(label);

            _nameField = new TextField(currentName)
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            Add(_nameField);

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

            _nameField.SetFocus();
            _nameField.CursorPosition = _nameField.Text.Length;
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
            _nameField.ColorScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus, // Use button focus for consistency when navigating
                HotNormal = textNormal,
                HotFocus = btnFocus
            };

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
