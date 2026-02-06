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

            if (displaySettings != null)
            {
                ColorHelper.ApplyStandardDialogColors(this, displaySettings, new View[] { _okButton, _cancelButton }, new View[] { _nameField });
            }

            _nameField.SetFocus();
            _nameField.CursorPosition = _nameField.Text.Length;
        }

        /// <summary>
        /// Shows the simple rename dialog and returns the new name if confirmed.
        /// Returns null if cancelled.
        /// </summary>
        public static string? Show(string currentName, DisplaySettings? displaySettings = null)
        {
            var dialog = new SimpleRenameDialog(currentName, displaySettings);
            Application.Run(dialog);
            return dialog.IsOk ? dialog.NewName : null;
        }

        private void ApplyColors(DisplaySettings display)
        {
            // Deprecated
        }
    }
}
