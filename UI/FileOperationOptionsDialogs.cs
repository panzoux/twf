using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for selecting archive format and name
    /// </summary>
    public class CompressionOptionsDialog : Dialog
    {
        private ListView _formatListView;
        private TextField _nameField;
        private RadioGroup _levelRadioGroup;
        private List<ArchiveFormat> _supportedFormats;
        private Button _okButton;
        private Button _cancelButton;
        public ArchiveFormat SelectedFormat { get; private set; } = ArchiveFormat.ZIP;
        public string ArchiveName => _nameField.Text.ToString() ?? string.Empty;
        public int SelectedCompressionLevel { get; private set; } = 5;
        public bool IsOk { get; private set; }

        public CompressionOptionsDialog(int fileCount, string defaultName, List<ArchiveFormat> supportedFormats, DisplaySettings? displaySettings = null) : base("Compress Files", 75, 20)
        {
            _supportedFormats = supportedFormats;
            var infoLabel = new Label($"Compressing {fileCount} file(s)")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            Add(infoLabel);

            var formatLabel = new Label("Select archive format:")
            {
                X = 1,
                Y = 3,
                Width = 25
            };
            Add(formatLabel);

            var formatOptions = new List<string>(_supportedFormats.Count);
            foreach (var f in _supportedFormats)
            {
                formatOptions.Add(f == ArchiveFormat.SevenZip ? "7-zip" : f.ToString());
            }

            _formatListView = new ListView(formatOptions)
            {
                X = 1,
                Y = 4,
                Width = 25,
                Height = Math.Min(10, formatOptions.Count),
                AllowsMarking = false,
                CanFocus = true
            };
            _formatListView.SelectedItem = 0;
            Add(_formatListView);

            var levelLabel = new Label("Compression level:")
            {
                X = 30,
                Y = 3,
                Width = Dim.Fill(1)
            };
            Add(levelLabel);

            _levelRadioGroup = new RadioGroup(new NStack.ustring[] { "Store (0)", "Fastest (1)", "Fast (3)", "Normal (5)", "Maximum (7)", "Ultra (9)" })
            {
                X = 30,
                Y = 4,
                SelectedItem = 3 // Normal (5)
            };
            Add(_levelRadioGroup);

            var nameLabel = new Label("Archive name:")
            {
                X = 1,
                Y = 14,
                Width = Dim.Fill(1)
            };
            Add(nameLabel);

            // Ensure default name has extension of default format
            if (_supportedFormats.Count > 0)
            {
                string ext = GetExtensionForFormat(_supportedFormats[0]);
                if (!defaultName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    defaultName += ext;
                }
            }

            _nameField = new TextField(defaultName)
            {
                X = 1,
                Y = 15,
                Width = Dim.Fill(1)
            };
            Add(_nameField);

            _okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 17,
                IsDefault = true
            };
            _okButton.Clicked += () =>
            {
                if (_formatListView.SelectedItem >= 0 && _formatListView.SelectedItem < _supportedFormats.Count)
                {
                    SelectedFormat = _supportedFormats[_formatListView.SelectedItem];
                    SelectedCompressionLevel = _levelRadioGroup.SelectedItem switch {
                        0 => 0,
                        1 => 1,
                        2 => 3,
                        3 => 5,
                        4 => 7,
                        5 => 9,
                        _ => 5
                    };
                    IsOk = true;
                }
                Application.RequestStop();
            };

            _cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 17
            };
            _cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okButton);
            AddButton(_cancelButton);

            _formatListView.SelectedItemChanged += (args) =>
            {
                if (args.Item < 0 || args.Item >= _supportedFormats.Count) return;
                UpdateExtension(_supportedFormats[args.Item]);
            };

            if (displaySettings != null) ApplyColors(displaySettings);

            _nameField.SetFocus();
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

            var scheme = new ColorScheme()
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            this.ColorScheme = scheme;

            // Explicitly set colors for buttons and list to show focus
            var buttonScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _okButton.ColorScheme = buttonScheme;
            _cancelButton.ColorScheme = buttonScheme;

            var listScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _formatListView.ColorScheme = listScheme;
            _levelRadioGroup.ColorScheme = listScheme;

            _nameField.ColorScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus,
                HotNormal = textNormal,
                HotFocus = btnFocus
            };
        }

        private string GetExtensionForFormat(ArchiveFormat format)
        {
            return format switch
            {
                ArchiveFormat.ZIP => ".zip",
                ArchiveFormat.TAR => ".tar",
                ArchiveFormat.TGZ => ".tar.gz",
                ArchiveFormat.SevenZip => ".7z",
                ArchiveFormat.RAR => ".rar",
                ArchiveFormat.LZH => ".lzh",
                ArchiveFormat.CAB => ".cab",
                ArchiveFormat.BZ2 => ".bz2",
                ArchiveFormat.XZ => ".xz",
                ArchiveFormat.LZMA => ".lzma",
                _ => ".zip"
            };
        }

        private void UpdateExtension(ArchiveFormat format)
        {
            var currentName = _nameField.Text.ToString();
            if (string.IsNullOrEmpty(currentName)) return;

            string newExt = GetExtensionForFormat(format);
            string baseName = currentName;
            
            // Strip known extensions
            string[] extensions = { ".zip", ".tar.gz", ".tar", ".7z", ".rar", ".lzh", ".cab", ".bz2", ".xz", ".lzma" };
            foreach (var ext in extensions)
            {
                if (baseName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = baseName.Substring(0, baseName.Length - ext.Length);
                    break;
                }
            }
            
            if (baseName.Length > 0)
            {
                _nameField.Text = baseName + newExt;
            }
        }
    }

    /// <summary>
    /// Dialog for selecting file split parameters
    /// </summary>
    public class FileSplitOptionsDialog : Dialog
    {
        private TextField _sizeField;
        private TextField _dirField;
        private Button _okButton;
        private Button _cancelButton;
        public long PartSize { get; private set; }
        public string OutputDirectory => _dirField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public FileSplitOptionsDialog(string fileName, long fileSize, string initialOutputDir, DisplaySettings? displaySettings = null) : base("Split File", 70, 16)
        {
            var fileInfo = new Label($"File: {fileName} ({FormatSize(fileSize)})")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme() { Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black) }
            };
            Add(fileInfo);

            var sizeLabel = new Label("Part size (bytes):") { X = 1, Y = 3 };
            Add(sizeLabel);

            _sizeField = new TextField("1048576") { X = 25, Y = 3, Width = 20 };
            Add(_sizeField);

            var btnY = 5;
            var s1 = new Button("1 MB") { X = 1, Y = btnY }; s1.Clicked += () => _sizeField.Text = (1024 * 1024).ToString(); Add(s1);
            var s10 = new Button("10 MB") { X = 10, Y = btnY }; s10.Clicked += () => _sizeField.Text = (10 * 1024 * 1024).ToString(); Add(s10);
            var s100 = new Button("100 MB") { X = 20, Y = btnY }; s100.Clicked += () => _sizeField.Text = (100 * 1024 * 1024).ToString(); Add(s100);
            var s1G = new Button("1 GB") { X = 32, Y = btnY }; s1G.Clicked += () => _sizeField.Text = (1024L * 1024 * 1024).ToString(); Add(s1G);

            Add(new Label("Output directory:") { X = 1, Y = 7 });
            _dirField = new TextField(initialOutputDir) { X = 1, Y = 8, Width = Dim.Fill(1) };
            Add(_dirField);

            _okButton = new Button("OK") { X = Pos.Center() - 10, Y = 11, IsDefault = true };
            _okButton.Clicked += () =>
            {
                if (long.TryParse(_sizeField.Text.ToString(), out long sz) && sz > 0)
                {
                    PartSize = sz;
                    IsOk = true;
                    Application.RequestStop();
                }
            };

            _cancelButton = new Button("Cancel") { X = Pos.Center() + 2, Y = 11 };
            _cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okButton);
            AddButton(_cancelButton);

            if (displaySettings != null) ApplyColors(displaySettings);
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

            var scheme = new ColorScheme()
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            this.ColorScheme = scheme;

            // Explicitly set colors for buttons to show focus
            var buttonScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _okButton.ColorScheme = buttonScheme;
            _cancelButton.ColorScheme = buttonScheme;

            var inputScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus,
                HotNormal = textNormal,
                HotFocus = btnFocus
            };
            _sizeField.ColorScheme = inputScheme;
            _dirField.ColorScheme = inputScheme;
        }

        private string FormatSize(long size)
        {
            if (size < 1024) return $"{size} B";
            if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
            return $"{size / (1024.0 * 1024.0):F1} MB";
        }
    }

    /// <summary>
    /// Dialog for selecting file join parameters
    /// </summary>
    public class FileJoinOptionsDialog : Dialog
    {
        private TextField _outputField;
        private Button _okButton;
        private Button _cancelButton;
        public string OutputFile => _outputField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public FileJoinOptionsDialog(int partCount, List<string> partNames, string initialOutputFile, DisplaySettings? displaySettings = null) : base("Join Split Files", 70, 18)
        {
            Add(new Label($"Found {partCount} part file(s) to join:") 
            {
                X = 1, Y = 1, Width = Dim.Fill(1),
                ColorScheme = new ColorScheme() { Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black) }
            });

            var previewNames = new List<string>();
            int limit = Math.Min(5, partNames.Count);
            for (int i = 0; i < limit; i++) previewNames.Add(partNames[i]);
            
            Add(new Label(string.Join("\n", previewNames)) { X = 3, Y = 2, Width = Dim.Fill(1), Height = 5 });
            if (partCount > 5) Add(new Label($"... and {partCount - 5} more") { X = 3, Y = 7 });

            Add(new Label("Output file:") { X = 1, Y = 9 });
            _outputField = new TextField(initialOutputFile) { X = 1, Y = 10, Width = Dim.Fill(1) };
            Add(_outputField);

            _okButton = new Button("OK") { X = Pos.Center() - 10, Y = 13, IsDefault = true };
            _okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            _cancelButton = new Button("Cancel") { X = Pos.Center() + 2, Y = 13 };
            _cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okButton);
            AddButton(_cancelButton);

            if (displaySettings != null) ApplyColors(displaySettings);
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

            var scheme = new ColorScheme()
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            this.ColorScheme = scheme;

            // Explicitly set colors for buttons to show focus
            var buttonScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _okButton.ColorScheme = buttonScheme;
            _cancelButton.ColorScheme = buttonScheme;

            _outputField.ColorScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus,
                HotNormal = textNormal,
                HotFocus = btnFocus
            };
        }
    }

    /// <summary>
    /// Dialog for selecting file comparison criteria
    /// </summary>
    public class FileComparisonDialog : Dialog
    {
        private RadioGroup _radioGroup;
        private TextField _toleranceField;
        private Button _okButton;
        private Button _cancelButton;
        public ComparisonCriteria Criteria { get; private set; }
        public TimeSpan? TimestampTolerance { get; private set; }
        public bool IsOk { get; private set; }

        public FileComparisonDialog(DisplaySettings? displaySettings = null) : base("File Comparison", 70, 18)
        {
            Add(new Label("Select comparison criteria to mark matching files in both panes:") { X = 1, Y = 1, Width = Dim.Fill(1) });
            Add(new Label("Comparison Criteria:") { X = 1, Y = 3 });

            _radioGroup = new RadioGroup(new NStack.ustring[] { "Size", "Timestamp", "Name" }) { X = 3, Y = 4, SelectedItem = 0 };
            Add(_radioGroup);

            var tolLabel = new Label("Timestamp Tolerance (seconds):") { X = 1, Y = 8, Visible = false };
            _toleranceField = new TextField("2") { X = 35, Y = 8, Width = 10, Visible = false };
            Add(tolLabel); Add(_toleranceField);

            var descLabel = new Label("") { X = 1, Y = 11, Width = Dim.Fill(1), Height = 3, ColorScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black) } };
            Add(descLabel);

            _radioGroup.SelectedItemChanged += (args) =>
            {
                bool isTs = args.SelectedItem == 1;
                tolLabel.Visible = _toleranceField.Visible = isTs;
                descLabel.Text = args.SelectedItem switch {
                    0 => "Marks files with identical file sizes in both panes.",
                    1 => "Marks files with matching last modified timestamps\n(within the specified tolerance).",
                    2 => "Marks files with identical names in both panes\n(case-insensitive comparison).",
                    _ => ""
                };
            };

            _okButton = new Button("OK") { X = Pos.Center() - 10, Y = Pos.AnchorEnd(2), IsDefault = true };
            _okButton.Clicked += () =>
            {
                Criteria = _radioGroup.SelectedItem switch { 0 => ComparisonCriteria.Size, 1 => ComparisonCriteria.Timestamp, 2 => ComparisonCriteria.Name, _ => ComparisonCriteria.Size };
                if (Criteria == ComparisonCriteria.Timestamp && double.TryParse(_toleranceField.Text.ToString(), out double sec)) TimestampTolerance = TimeSpan.FromSeconds(sec);
                IsOk = true; Application.RequestStop();
            };

            _cancelButton = new Button("Cancel") { X = Pos.Center() + 2, Y = Pos.AnchorEnd(2) };
            _cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okButton); AddButton(_cancelButton);

            if (displaySettings != null) ApplyColors(displaySettings);

            _radioGroup.SelectedItem = 0; // Trigger update
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

            var scheme = new ColorScheme()
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            this.ColorScheme = scheme;

            // Explicitly set colors for buttons and list to show focus
            var buttonScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _okButton.ColorScheme = buttonScheme;
            _cancelButton.ColorScheme = buttonScheme;
            _radioGroup.ColorScheme = buttonScheme;

            _toleranceField.ColorScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus,
                HotNormal = textNormal,
                HotFocus = btnFocus
            };
        }
    }

    /// <summary>
    /// Dialog for handling file collisions during operations
    /// </summary>
    public class FileCollisionDialog : Dialog
    {
        private Button _overwriteBtn;
        private Button _skipBtn;
        private Button _renameBtn;
        private Button _cancelBtn;
        public FileCollisionResult Result { get; private set; } = new FileCollisionResult { Action = FileCollisionAction.Cancel };

        public FileCollisionDialog(string filename, DisplaySettings? displaySettings = null) : base("File Exists", 60, 11)
        {
            Add(new Label($"File already exists:\n{filename}") { X = 1, Y = 1, Width = Dim.Fill(1), Height = 2 });

            Add(new Label("Ctrl+Enter to 'Overwrite All' or 'Skip All'")
            {
                X = 1, Y = 4, Width = Dim.Fill(1),
                ColorScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Blue) }
            });

            _overwriteBtn = new Button("Overwrite") { X = 1, Y = Pos.AnchorEnd(1) };
            _skipBtn = new Button("Skip") { X = Pos.Right(_overwriteBtn) + 1, Y = Pos.AnchorEnd(1) };
            _renameBtn = new Button("Rename") { X = Pos.Right(_skipBtn) + 1, Y = Pos.AnchorEnd(1) };
            _cancelBtn = new Button("Cancel") { X = Pos.Right(_renameBtn) + 1, Y = Pos.AnchorEnd(1) };

            bool isShiftHeld = false;
            this.KeyPress += (e) =>
            {
                var key = e.KeyEvent.Key;
                var cleanKey = key & ~Key.ShiftMask & ~Key.AltMask & ~Key.CtrlMask;
                bool shift = (key & Key.ShiftMask) != 0;
                bool alt = (key & Key.AltMask) != 0;
                bool ctrl = (key & Key.CtrlMask) != 0;

                if (alt && shift && cleanKey == Key.O) { Result = new FileCollisionResult { Action = FileCollisionAction.OverwriteAll }; Application.RequestStop(); e.Handled = true; return; }
                if (alt && shift && cleanKey == Key.S) { Result = new FileCollisionResult { Action = FileCollisionAction.SkipAll }; Application.RequestStop(); e.Handled = true; return; }

                if (ctrl && cleanKey == Key.Enter)
                {
                    if (_overwriteBtn.HasFocus) { Result = new FileCollisionResult { Action = FileCollisionAction.OverwriteAll }; Application.RequestStop(); e.Handled = true; return; }
                    if (_skipBtn.HasFocus) { Result = new FileCollisionResult { Action = FileCollisionAction.SkipAll }; Application.RequestStop(); e.Handled = true; return; }
                }

                if (alt && cleanKey == Key.O) { Result = new FileCollisionResult { Action = FileCollisionAction.Overwrite }; Application.RequestStop(); e.Handled = true; return; }
                if (alt && cleanKey == Key.S) { Result = new FileCollisionResult { Action = FileCollisionAction.Skip }; Application.RequestStop(); e.Handled = true; return; }
                
                isShiftHeld = shift;
            };

            _overwriteBtn.Clicked += () => { Result = new FileCollisionResult { Action = isShiftHeld ? FileCollisionAction.OverwriteAll : FileCollisionAction.Overwrite }; Application.RequestStop(); };
            _skipBtn.Clicked += () => { Result = new FileCollisionResult { Action = isShiftHeld ? FileCollisionAction.SkipAll : FileCollisionAction.Skip }; Application.RequestStop(); };
            _renameBtn.Clicked += () =>
            {
                var inputDialog = new RenameConflictDialog(filename, displaySettings);
                Application.Run(inputDialog);
                if (inputDialog.IsOk) { Result = new FileCollisionResult { Action = FileCollisionAction.Rename, NewName = inputDialog.NewName }; Application.RequestStop(); }
            };
            _cancelBtn.Clicked += () => { Result = new FileCollisionResult { Action = FileCollisionAction.Cancel }; Application.RequestStop(); };

            AddButton(_overwriteBtn); AddButton(_skipBtn); AddButton(_renameBtn); AddButton(_cancelBtn);

            if (displaySettings != null) ApplyColors(displaySettings);

            _renameBtn.IsDefault = true;
        }

        private void ApplyColors(DisplaySettings display)
        {
            if (Application.Driver == null) return;

            // Define attributes based on user requirements
            var btnNormal = Application.Driver.MakeAttribute(Color.Black, Color.Gray);
            var btnFocus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray);
            var hotNormal = Application.Driver.MakeAttribute(Color.Cyan, Color.Gray);
            var hotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray);

            var scheme = new ColorScheme()
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            this.ColorScheme = scheme;

            // Explicitly set colors for buttons to show focus
            var buttonScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _overwriteBtn.ColorScheme = buttonScheme;
            _skipBtn.ColorScheme = buttonScheme;
            _renameBtn.ColorScheme = buttonScheme;
            _cancelBtn.ColorScheme = buttonScheme;
        }
    }

    /// <summary>
    /// Small sub-dialog for entering a new name during a collision
    /// </summary>
    public class RenameConflictDialog : Dialog
    {
        private TextField _nameField;
        private Button _okBtn;
        private Button _cancelBtn;
        public string NewName => _nameField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        public RenameConflictDialog(string filename, DisplaySettings? displaySettings = null) : base("Rename File", 60, 7)
        {
            Add(new Label("New name:") { X = 1, Y = 1 });
            _nameField = new TextField(filename) { X = 1, Y = 2, Width = Dim.Fill(1) };
            Add(_nameField);

            _okBtn = new Button("OK") { IsDefault = true };
            _cancelBtn = new Button("Cancel");

            _okBtn.Clicked += () => { IsOk = true; Application.RequestStop(); };
            _cancelBtn.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(_okBtn); AddButton(_cancelBtn);

            if (displaySettings != null) ApplyColors(displaySettings);
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

            var scheme = new ColorScheme()
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            this.ColorScheme = scheme;

            // Explicitly set colors for buttons to show focus
            var buttonScheme = new ColorScheme
            {
                Normal = btnNormal,
                Focus = btnFocus,
                HotNormal = hotNormal,
                HotFocus = hotFocus
            };
            _okBtn.ColorScheme = buttonScheme;
            _cancelBtn.ColorScheme = buttonScheme;

            _nameField.ColorScheme = new ColorScheme
            {
                Normal = textNormal,
                Focus = btnFocus,
                HotNormal = textNormal,
                HotFocus = btnFocus
            };
        }
    }
}