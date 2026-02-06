using Terminal.Gui;
using TWF.Models;
using TWF.Utilities;
using System;
using System.Collections.Generic;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for showing application help
    /// </summary>
    public class HelpDialog : Dialog
    {
        public HelpDialog(string helpText, int width, int height, DisplaySettings? displaySettings = null) : base("Help", width, height)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

            var textView = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                ReadOnly = true,
                Text = helpText
            };
            Add(textView);

            var closeButton = new Button("Close")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(0)
            };
            closeButton.Clicked += () => Application.RequestStop();
            AddButton(closeButton);
        }

        /// <summary>
        /// Shows the help dialog.
        /// </summary>
        public static void Show(string helpText, int width, int height, DisplaySettings? displaySettings = null)
        {
            var dialog = new HelpDialog(helpText, width, height, displaySettings);
            Application.Run(dialog);
        }

        private void ApplyColors(DisplaySettings display)
        {
            if (Application.Driver == null) return;
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var highlightFg = ColorHelper.ParseConfigColor(display.HighlightForegroundColor, Color.Black);
            var highlightBg = ColorHelper.ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);

            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
            };
            this.ColorScheme = scheme;
        }
    }

    /// <summary>
    /// Dialog for showing file properties
    /// </summary>
    public class FilePropertiesDialog : Dialog
    {
        public FilePropertiesDialog(string infoText, DisplaySettings? displaySettings = null) : base("File Properties", 70, 18)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

            var label = new Label(infoText)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(2)
            };
            Add(label);

            var okButton = new Button("OK")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1),
                IsDefault = true
            };
            okButton.Clicked += () => Application.RequestStop();
            AddButton(okButton);
        }

        /// <summary>
        /// Shows the file properties dialog.
        /// </summary>
        public static void Show(string infoText, DisplaySettings? displaySettings = null)
        {
            var dialog = new FilePropertiesDialog(infoText, displaySettings);
            Application.Run(dialog);
        }

        private void ApplyColors(DisplaySettings display)
        {
            if (Application.Driver == null) return;
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var highlightFg = ColorHelper.ParseConfigColor(display.HighlightForegroundColor, Color.Black);
            var highlightBg = ColorHelper.ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);

            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
            };
            this.ColorScheme = scheme;
        }
    }

    /// <summary>
    /// Dialog for showing a context menu of operations
    /// </summary>
    public class ContextMenuDialog : Dialog
    {
        public TWF.Models.MenuItem? SelectedItem { get; private set; }

        public ContextMenuDialog(List<TWF.Models.MenuItem> menuItems, DisplaySettings? displaySettings = null) : base("Context Menu", 60, Math.Min(menuItems.Count + 6, 25))
        {
            if (displaySettings != null) ApplyColors(displaySettings);

            Add(new Label("Select an operation:") { X = 1, Y = 1 });

            var displayItems = new List<string>();
            foreach (var item in menuItems)
            {
                if (item.IsSeparator) displayItems.Add("─────────────────────────────────");
                else
                {
                    var text = item.Label;
                    if (!string.IsNullOrEmpty(item.Shortcut)) text += $" ({item.Shortcut})";
                    displayItems.Add(text);
                }
            }

            var listView = new ListView(displayItems)
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1),
                Height = Dim.Fill(3),
                AllowsMarking = false
            };
            Add(listView);

            var okButton = new Button("OK") { X = Pos.Center() - 10, Y = Pos.AnchorEnd(1), IsDefault = true };
            okButton.Clicked += () => { if (listView.SelectedItem >= 0 && !menuItems[listView.SelectedItem].IsSeparator) SelectedItem = menuItems[listView.SelectedItem]; Application.RequestStop(); };

            var cancelButton = new Button("Cancel") { X = Pos.Center() + 2, Y = Pos.AnchorEnd(1) };
            cancelButton.Clicked += () => { Application.RequestStop(); };

            AddButton(okButton); AddButton(cancelButton);
            listView.OpenSelectedItem += (e) => { if (!menuItems[listView.SelectedItem].IsSeparator) SelectedItem = menuItems[listView.SelectedItem]; Application.RequestStop(); };
        }

        /// <summary>
        /// Shows the context menu dialog and returns the selected item.
        /// </summary>
        public static TWF.Models.MenuItem? Show(List<TWF.Models.MenuItem> menuItems, DisplaySettings? displaySettings = null)
        {
            var dialog = new ContextMenuDialog(menuItems, displaySettings);
            Application.Run(dialog);
            return dialog.SelectedItem;
        }

        private void ApplyColors(DisplaySettings display)
        {
            if (Application.Driver == null) return;
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var highlightFg = ColorHelper.ParseConfigColor(display.HighlightForegroundColor, Color.Black);
            var highlightBg = ColorHelper.ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);

            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
            };
            this.ColorScheme = scheme;
        }
    }

    /// <summary>
    /// A generic confirmation dialog
    /// </summary>
    public class ConfirmationDialog : Dialog
    {
        public bool Confirmed { get; private set; }

        public ConfirmationDialog(string title, string message, string helpText, Color helpFg, Color helpBg, DisplaySettings? displaySettings = null) : base(title, 60, 12)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

            var messageView = new TextView() 
            { 
                X = 1, 
                Y = 1, 
                Width = Dim.Fill(1), 
                Height = Dim.Fill(3),
                Text = message,
                ReadOnly = true,
                WordWrap = true
            };
            Add(messageView);

            var helpBar = new Label(helpText)
            {
                X = 0,
                Y = Pos.AnchorEnd(2),
                Width = Dim.Fill(),
                Height = 1,
                ColorScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(helpFg, helpBg) }
            };
            Add(helpBar);

            var yesButton = new Button("Yes") { X = Pos.Center() - 10, Y = Pos.AnchorEnd(1), IsDefault = true };
            yesButton.Clicked += () => { Confirmed = true; Application.RequestStop(); };

            var noButton = new Button("No") { X = Pos.Center() + 2, Y = Pos.AnchorEnd(1) };
            noButton.Clicked += () => { Confirmed = false; Application.RequestStop(); };

            AddButton(yesButton); AddButton(noButton);

            if (displaySettings != null)
            {
                ColorHelper.ApplyStandardDialogColors(this, displaySettings, new[] { yesButton, noButton }, new[] { messageView });
            }
        }

        /// <summary>
        /// Shows a confirmation dialog and returns true if confirmed.
        /// </summary>
        public static bool Show(string title, string message, string helpText, Color helpFg, Color helpBg, DisplaySettings? displaySettings = null)
        {
            var dialog = new ConfirmationDialog(title, message, helpText, helpFg, helpBg, displaySettings);
            Application.Run(dialog);
            return dialog.Confirmed;
        }

        private void ApplyColors(DisplaySettings display)
        {
            // Method is now deprecated but kept for compatibility or will be removed in cleanup
        }
    }

    /// <summary>
    /// A simple message display dialog
    /// </summary>
    public class MessageDialog : Dialog
    {
        public MessageDialog(string title, string message, DisplaySettings? displaySettings = null) : base(title, 60, 15)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

            var messageView = new TextView() 
            { 
                X = 1, 
                Y = 1, 
                Width = Dim.Fill(1), 
                Height = Dim.Fill(3),
                Text = message,
                ReadOnly = true,
                WordWrap = true
            };
            Add(messageView);
            var okButton = new Button("OK") { X = Pos.Center(), Y = Pos.AnchorEnd(2), IsDefault = true };
            okButton.Clicked += () => Application.RequestStop();
            AddButton(okButton);

            if (displaySettings != null)
            {
                ColorHelper.ApplyStandardDialogColors(this, displaySettings, new[] { okButton }, new[] { messageView });
            }
        }

        /// <summary>
        /// Shows a message dialog.
        /// </summary>
        public static void Show(string title, string message, DisplaySettings? displaySettings = null)
        {
            var dialog = new MessageDialog(title, message, displaySettings);
            Application.Run(dialog);
        }

        private void ApplyColors(DisplaySettings display)
        {
            // Deprecated
        }
    }

    /// <summary>
    /// A simple input dialog
    /// </summary>
    public class InputDialog : Dialog
    {
        private TextField _inputField;
        public string? Result { get; private set; }
        public bool IsOk { get; private set; }

        public InputDialog(string title, string prompt, string initialValue = "", int width = 60, DisplaySettings? displaySettings = null) : base(title, width, 10)
        {
            var label = new Label(prompt)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(label);

            _inputField = new TextField(initialValue)
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            Add(_inputField);

            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 5,
                IsDefault = true
            };
            okButton.Clicked += () => { Result = _inputField.Text.ToString(); IsOk = true; Application.RequestStop(); };

            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 5
            };
            cancelButton.Clicked += () => { Result = null; IsOk = false; Application.RequestStop(); };

            AddButton(okButton);
            AddButton(cancelButton);

            if (displaySettings != null)
            {
                ColorHelper.ApplyStandardDialogColors(this, displaySettings, new View[] { okButton, cancelButton }, new View[] { _inputField });
            }

            _inputField.SetFocus();
        }

        public static string? Show(string title, string prompt, string initialValue = "", int width = 60, DisplaySettings? displaySettings = null)
        {
            var dialog = new InputDialog(title, prompt, initialValue, width, displaySettings);
            Application.Run(dialog);
            return dialog.IsOk ? dialog.Result : null;
        }
    }
}