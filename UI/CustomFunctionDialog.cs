using Terminal.Gui;
using TWF.Models;
using TWF.Utilities;
using System;
using System.Collections.Generic;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for selecting and executing custom functions
    /// </summary>
    public class CustomFunctionDialog : Dialog
    {
        private readonly List<CustomFunction> _functions;
        private ListView _functionList = null!;
        private Label _descriptionLabel = null!;
        private CustomFunction? _selectedFunction;

        /// <summary>
        /// Gets the selected custom function, or null if cancelled
        /// </summary>
        public CustomFunction? SelectedFunction => _selectedFunction;

        public CustomFunctionDialog(List<CustomFunction> functions, DisplaySettings? displaySettings = null) : base("Custom Functions", 70, 20)
        {
            _functions = functions ?? throw new ArgumentNullException(nameof(functions));
            
            if (displaySettings != null) ApplyColors(displaySettings);
            
            InitializeComponents(displaySettings);
        }

        private void InitializeComponents(DisplaySettings? displaySettings)
        {
            // Title label
            var titleLabel = new Label("> Select a custom function to execute:")
            {
                X = 1,
                Y = 0
            };
            Add(titleLabel);

            // Function list
            _functionList = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(5),
                AllowsMarking = false
            };

            if (displaySettings != null)
            {
                var foreground = ColorHelper.ParseConfigColor(displaySettings.ForegroundColor, Color.White);
                var background = ColorHelper.ParseConfigColor(displaySettings.BackgroundColor, Color.Black);
                var highlightFg = ColorHelper.ParseConfigColor(displaySettings.HighlightForegroundColor, Color.Black);
                var highlightBg = ColorHelper.ParseConfigColor(displaySettings.HighlightBackgroundColor, Color.Cyan);

                _functionList.ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(foreground, background),
                    Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                    HotNormal = Application.Driver.MakeAttribute(foreground, background),
                    HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
                };
            }

            var functionNames = new List<string>(_functions.Count);
            foreach (var f in _functions) functionNames.Add(f.Name);
            _functionList.SetSource(functionNames);

            _functionList.SelectedItemChanged += (args) =>
            {
                UpdateDescription();
            };

            Add(_functionList);

            _descriptionLabel = new Label("")
            {
                X = 1,
                Y = Pos.AnchorEnd(3),
                Width = Dim.Fill(1),
                Height = 1
            };
            Add(_descriptionLabel);

            // Buttons
            var executeButton = new Button("Execute", is_default: true);
            executeButton.Clicked += () =>
            {
                if (_functionList.SelectedItem >= 0 && _functionList.SelectedItem < _functions.Count)
                {
                    _selectedFunction = _functions[_functionList.SelectedItem];
                    Application.RequestStop();
                }
            };

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () =>
            {
                _selectedFunction = null;
                Application.RequestStop();
            };

            AddButton(executeButton);
            AddButton(cancelButton);

            // Show initial description
            UpdateDescription();
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

        private void UpdateDescription()
        {
            if (_functionList.SelectedItem >= 0 && _functionList.SelectedItem < _functions.Count)
            {
                var function = _functions[_functionList.SelectedItem];
                _descriptionLabel.Text = $"Description: {function.Description}\nCommand: {function.Command}";
            }
            else
            {
                _descriptionLabel.Text = "";
            }
        }
    }
}