using Terminal.Gui;
using TWF.Models;
using TWF.Utilities;
using System;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for registering a folder with a custom name
    /// </summary>
    public class RegisterFolderDialog : Dialog
    {
        private TextField _nameField;
        private Button _okButton;
        private Button _cancelButton;
        public string FolderName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public RegisterFolderDialog(string defaultName, string targetPath, DisplaySettings? displaySettings = null) : base("Register Folder", 60, 10)
        {
            var label = new Label("Enter a name for this folder:")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(label);

            var pathLabel = new Label($"Path: {targetPath}")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
                }
            };
            Add(pathLabel);

            _nameField = new TextField(defaultName)
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1)
            };
            Add(_nameField);

            _okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            _okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            _cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            _cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okButton);
            AddButton(_cancelButton);

            if (displaySettings != null)
            {
                ColorHelper.ApplyStandardDialogColors(this, displaySettings, new View[] { _okButton, _cancelButton }, new View[] { _nameField });
            }

            _nameField.SetFocus();
        }

        private void ApplyColors(DisplaySettings display)
        {
            // Deprecated
        }
    }


    /// <summary>
    /// Dialog for entering a new directory name
    /// </summary>
    public class CreateDirectoryDialog : Dialog
    {
        private TextField _nameField;
        private Button _okButton;
        private Button _cancelButton;
        public string DirectoryName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public CreateDirectoryDialog(DisplaySettings? displaySettings = null) : base("Create Directory", 60, 8)
        {
            var label = new Label("Enter directory name:")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(label);

            _nameField = new TextField("")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            Add(_nameField);

            _okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            _okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            _cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            _cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okButton);
            AddButton(_cancelButton);

            // Handle Escape key
            this.KeyPress += (e) => {
                if (e.KeyEvent.Key == (Key)27) { IsOk = false; Application.RequestStop(); e.Handled = true; }
            };

            if (displaySettings != null)
            {
                ColorHelper.ApplyStandardDialogColors(this, displaySettings, new View[] { _okButton, _cancelButton }, new View[] { _nameField });
            }

            _nameField.SetFocus();
        }

        /// <summary>
        /// Shows the create directory dialog and returns the entered name if confirmed.
        /// Returns null if cancelled.
        /// </summary>
        public static string? Show(DisplaySettings? displaySettings = null)
        {
            var dialog = new CreateDirectoryDialog(displaySettings);
            Application.Run(dialog);
            return dialog.IsOk ? dialog.DirectoryName : null;
        }

        private void ApplyColors(DisplaySettings display)
        {
            // Deprecated
        }
    }

    /// <summary>
    /// Dialog for entering a new file name
    /// </summary>
    public class CreateNewFileDialog : Dialog
    {
        private TextField _nameField;
        private Button _okButton;
        private Button _cancelButton;
        public string FileName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public CreateNewFileDialog(DisplaySettings? displaySettings = null) : base("Create New File", 60, 8)
        {
            var label = new Label("Enter new file name:")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(label);

            _nameField = new TextField("")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            Add(_nameField);

            _okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            _okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            _cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            _cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okButton);
            AddButton(_cancelButton);

            // Handle Escape key
            this.KeyPress += (e) => {
                if (e.KeyEvent.Key == (Key)27) { IsOk = false; Application.RequestStop(); e.Handled = true; }
            };

            if (displaySettings != null)
            {
                ColorHelper.ApplyStandardDialogColors(this, displaySettings, new View[] { _okButton, _cancelButton }, new View[] { _nameField });
            }

            _nameField.SetFocus();
        }

        /// <summary>
        /// Shows the create file dialog and returns the entered name if confirmed.
        /// Returns null if cancelled.
        /// </summary>
        public static string? Show(DisplaySettings? displaySettings = null)
        {
            var dialog = new CreateNewFileDialog(displaySettings);
            Application.Run(dialog);
            return dialog.IsOk ? dialog.FileName : null;
        }

        private void ApplyColors(DisplaySettings display)
        {
            // Deprecated
        }
    }
}
