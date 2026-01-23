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

        public SimpleRenameDialog(string currentName, DisplaySettings? displaySettings = null) : base("Rename", 60, 8)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

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

            var okButton = new Button("OK", is_default: true);
            okButton.Clicked += () =>
            {
                IsOk = true;
                Application.RequestStop();
            };

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () =>
            {
                IsOk = false;
                Application.RequestStop();
            };

            AddButton(okButton);
            AddButton(cancelButton);

            _nameField.SetFocus();
            _nameField.CursorPosition = _nameField.Text.Length;
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
            _nameField.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(inputFg, inputBg),
                Focus = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotNormal = Application.Driver.MakeAttribute(inputFg, inputBg),
                HotFocus = Application.Driver.MakeAttribute(inputFg, inputBg)
            };
        }
    }
}
