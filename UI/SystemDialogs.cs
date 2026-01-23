using Terminal.Gui;
using TWF.Models;
using TWF.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
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

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
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

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
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

        public ConfirmationDialog(string title, string message, string helpText, Color helpFg, Color helpBg, DisplaySettings? displaySettings = null) : base(title, 60, 10)
        {
            if (displaySettings != null) ApplyColors(displaySettings);

            Add(new Label(message) { X = 1, Y = 1, Width = Dim.Fill(1), Height = 2 });

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
        }

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };
            this.ColorScheme = scheme;
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

            Add(new Label(message) { X = 1, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(3) });
            var okButton = new Button("OK") { X = Pos.Center(), Y = Pos.AnchorEnd(2), IsDefault = true };
            okButton.Clicked += () => Application.RequestStop();
            AddButton(okButton);
        }

        private void ApplyColors(DisplaySettings display)
        {
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);
            var scheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(dialogFg, dialogBg)
            };
            this.ColorScheme = scheme;
        }
    }
}