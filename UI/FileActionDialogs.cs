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
        public string FolderName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public RegisterFolderDialog(string defaultName, string targetPath, DisplaySettings? displaySettings = null) : base("Register Folder", 60, 10)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

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

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(okButton);
            AddButton(cancelButton);

            _nameField.SetFocus();
        }

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };
            this.ColorScheme = scheme;

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

    /// <summary>
    /// Dialog for entering a new directory name
    /// </summary>
    public class CreateDirectoryDialog : Dialog
    {
        private TextField _nameField;
        public string DirectoryName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public CreateDirectoryDialog(DisplaySettings? displaySettings = null) : base("Create Directory", 60, 8)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

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

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(okButton);
            AddButton(cancelButton);

            // Handle Escape key
            this.KeyPress += (e) => {
                if (e.KeyEvent.Key == (Key)27) { IsOk = false; Application.RequestStop(); e.Handled = true; }
            };

            _nameField.SetFocus();
        }

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };
            this.ColorScheme = scheme;

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

    /// <summary>
    /// Dialog for entering a new file name
    /// </summary>
    public class CreateNewFileDialog : Dialog
    {
        private TextField _nameField;
        public string FileName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public CreateNewFileDialog(DisplaySettings? displaySettings = null) : base("Create New File", 60, 8)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

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

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(okButton);
            AddButton(cancelButton);

            // Handle Escape key
            this.KeyPress += (e) => {
                if (e.KeyEvent.Key == (Key)27) { IsOk = false; Application.RequestStop(); e.Handled = true; }
            };

            _nameField.SetFocus();
        }

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };
            this.ColorScheme = scheme;

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
