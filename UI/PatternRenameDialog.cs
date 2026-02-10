using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    /// <summary>
    /// Advanced dialog for pattern-based batch renaming with live preview
    /// </summary>
    public class PatternRenameDialog : Dialog
    {
        private readonly List<FileEntry> _sourceFiles;
        private readonly Configuration _config;
        private CancellationTokenSource? _previewCts;
        
        // UI Controls
        private TextField _findField;
        private TextField _replaceField;
        private CheckBox _regexCheck;
        private CheckBox _caseCheck;
        private NavigatingListView _previewListLeft; 
        private NavigatingListView _previewListRight;
        private Label _statusLabel;
        private Label _viewModeLabel;
        private Label _separatorMid;
        
        // View State
        private enum ViewMode { Preview, Original, SideBySide }
        private ViewMode _currentView = ViewMode.SideBySide; // Requirement: SideBySide as default
        private bool _filterMatchesOnly = false; // Requirement: Filter ALL as default
        private List<PreviewItem> _currentPreviewItems = new List<PreviewItem>();
        private int _horizontalOffset = 0; 
        
        // Result Properties
        public string Pattern => _findField.Text.ToString() ?? string.Empty;
        public string Replacement => _replaceField.Text.ToString() ?? string.Empty;
        public bool IsOk { get; private set; }

        private class PreviewItem
        {
            public string OriginalName { get; set; } = string.Empty;
            public string NewName { get; set; } = string.Empty;
            public bool IsChanged => OriginalName != NewName;
            public string? ValidationError { get; set; }
        }

        // Custom ListView to override focus navigation behavior
        private class NavigatingListView : ListView
        {
            public Action<KeyEvent>? OnNavigationKey;

            public override bool ProcessKey(KeyEvent keyEvent)
            {
                var key = keyEvent.Key;
                
                // Intercept keys that usually move focus and use them for scrolling
                // Bug 12: Intercept Left, Right, Ctrl+F, Ctrl+B, h, l
                if (key == Key.CursorLeft || key == Key.CursorRight || 
                    key == (Key.B | Key.CtrlMask) || key == (Key.F | Key.CtrlMask) ||
                    key == Key.h || key == Key.l)
                {
                    OnNavigationKey?.Invoke(keyEvent);
                    return true; // Stop Terminal.Gui from moving focus to buttons
                }

                return base.ProcessKey(keyEvent);
            }
        }

        public PatternRenameDialog(List<FileEntry> files, Configuration config) 
            : base("Pattern Rename", 
                   Application.Driver.Cols - 2, 
                   Math.Max(15, (int)(Application.Driver.Rows * 0.75)))
        {
            _sourceFiles = files;
            _config = config;

            // --- Input Section (Order: Find -> Replace -> Regex -> Case Sens) ---
            var label1 = new Label("Find:") { X = 1, Y = 1 };
            _findField = new TextField("") { X = 10, Y = 1, Width = Dim.Fill(1) };
            _findField.TextChanged += (e) => TriggerPreviewUpdate();

            var label2 = new Label("Replace:") { X = 1, Y = 2 };
            _replaceField = new TextField("") { X = 10, Y = 2, Width = Dim.Fill(1) };
            _replaceField.TextChanged += (e) => TriggerPreviewUpdate();

            _regexCheck = new CheckBox("Regex (Alt+R)", true) { X = 10, Y = 3 };
            _regexCheck.Toggled += (e) => TriggerPreviewUpdate();

            _caseCheck = new CheckBox("Case Sens (Alt+S)", false) { X = 10, Y = 4 };
            _caseCheck.Toggled += (e) => TriggerPreviewUpdate();

            Add(label1, _findField, label2, _replaceField, _regexCheck, _caseCheck);

            _findField.KeyPress += HandleInputKeys;
            _replaceField.KeyPress += HandleInputKeys;

            // --- View Controls & Headers ---
            var separator = new Label(new string('─', 500)) { X = 0, Y = 5, Width = Dim.Fill() };
            Add(separator);

            _viewModeLabel = new Label("") { X = 1, Y = 6, Width = Dim.Fill(1) };
            Add(_viewModeLabel);

            // --- Preview Lists ---
            _previewListLeft = new NavigatingListView()
            {
                X = 1, Y = 7, Width = Dim.Fill(1), Height = Dim.Fill(3),
                AllowsMarking = false, CanFocus = true 
            };
            
            _previewListRight = new NavigatingListView()
            {
                X = Pos.Percent(50) + 1, Y = 7, Width = Dim.Fill(1), Height = Dim.Fill(3),
                AllowsMarking = false, CanFocus = false, Visible = false 
            };

            _separatorMid = new Label("║") { X = Pos.Percent(50), Y = 7, Width = 1, Height = Dim.Fill(3), Visible = false };

            // Bind custom navigation keys
            _previewListLeft.OnNavigationKey = (keyEvent) => HandleListNavigation(new KeyEventEventArgs(keyEvent), _previewListLeft);

            // Sync vertical selection
            _previewListLeft.SelectedItemChanged += (args) => SyncLists(_previewListLeft, _previewListRight);

            Add(_previewListLeft, _previewListRight, _separatorMid);

            // --- Bottom Section ---
            var separator2 = new Label(new string('─', 500)) { X = 0, Y = Pos.AnchorEnd(3), Width = Dim.Fill() };
            Add(separator2);

            _statusLabel = new Label("Ready") { X = 1, Y = Pos.AnchorEnd(2), Width = Dim.Fill(1) };
            Add(_statusLabel);

            var okButton = new Button("OK", is_default: true);
            okButton.Clicked += () => { IsOk = true; Application.RequestStop(); };

            var cancelButton = new Button("Cancel");
            cancelButton.Clicked += () => { IsOk = false; Application.RequestStop(); };

            AddButton(okButton);
            AddButton(cancelButton);

            KeyPress += (e) => HandleKeys(e);

            ApplyColors();
            UpdateViewLayout(); 
            TriggerPreviewUpdate(); 
        }

        private void HandleListNavigation(KeyEventEventArgs e, ListView list)
        {
            var key = e.KeyEvent.Key;
            
            // Scroll Left: CursorLeft, h, Ctrl+B
            if (key == Key.CursorLeft || key == Key.h || key == (Key.B | Key.CtrlMask))
            {
                if (_horizontalOffset > 0)
                {
                    _horizontalOffset = Math.Max(0, _horizontalOffset - 4);
                    _previewListLeft.SetNeedsDisplay();
                    _previewListRight.SetNeedsDisplay();
                }
                e.Handled = true;
            }
            // Scroll Right: CursorRight, l, Ctrl+F
            else if (key == Key.CursorRight || key == Key.l || key == (Key.F | Key.CtrlMask))
            {
                _horizontalOffset += 4;
                _previewListLeft.SetNeedsDisplay();
                _previewListRight.SetNeedsDisplay();
                e.Handled = true;
            }
        }

        private void SyncLists(ListView source, ListView target)
        {
            if (_currentView == ViewMode.SideBySide && target.Visible)
            {
                if (target.SelectedItem != source.SelectedItem) target.SelectedItem = source.SelectedItem;
                if (target.TopItem != source.TopItem) target.TopItem = source.TopItem;
                target.SetNeedsDisplay();
            }
        }

        private void HandleInputKeys(KeyEventEventArgs e)
        {
            var key = e.KeyEvent.Key;
            int count = _previewListLeft.Source?.Count ?? 0;
            if (count == 0) return;

            if (key == Key.CursorUp)
            {
                if (_previewListLeft.TopItem > 0)
                {
                    _previewListLeft.TopItem--;
                    _previewListLeft.SelectedItem = _previewListLeft.TopItem;
                    SyncLists(_previewListLeft, _previewListRight);
                }
                e.Handled = true;
            }
            else if (key == Key.CursorDown)
            {
                int visibleHeight = _previewListLeft.Bounds.Height;
                int maxTop = Math.Max(0, count - visibleHeight);

                if (_previewListLeft.TopItem < maxTop)
                {
                    _previewListLeft.TopItem++;
                    _previewListLeft.SelectedItem = _previewListLeft.TopItem;
                    SyncLists(_previewListLeft, _previewListRight);
                }
                e.Handled = true;
            }
            else if (key == Key.PageUp || key == Key.PageDown)
            {
                if (key == Key.PageUp) _previewListLeft.MovePageUp();
                else _previewListLeft.MovePageDown();
                SyncLists(_previewListLeft, _previewListRight);
                e.Handled = true;
            }
        }

        private void HandleKeys(KeyEventEventArgs e)
        {
            var key = e.KeyEvent.Key;
            if (key == (Key.AltMask | Key.R)) { _regexCheck.Checked = !_regexCheck.Checked; TriggerPreviewUpdate(); e.Handled = true; }
            else if (key == (Key.AltMask | Key.S)) { _caseCheck.Checked = !_caseCheck.Checked; TriggerPreviewUpdate(); e.Handled = true; }
            else if (key == (Key.AltMask | Key.P)) { ToggleViewMode(); e.Handled = true; }
            else if (key == (Key.AltMask | Key.A)) { _filterMatchesOnly = !_filterMatchesOnly; UpdateViewLayout(); TriggerPreviewUpdate(); e.Handled = true; }
        }

        private void ToggleViewMode()
        {
            if (_currentView == ViewMode.Preview) _currentView = ViewMode.Original;
            else if (_currentView == ViewMode.Original) _currentView = ViewMode.SideBySide;
            else _currentView = ViewMode.Preview;
            UpdateViewLayout();
        }

        private void UpdateViewLayout()
        {
            string viewText = "";
            switch (_currentView)
            {
                case ViewMode.Preview: viewText = "  Original  [ PREVIEW ]  SideBySide "; break;
                case ViewMode.Original: viewText = "[ ORIGINAL ]  Preview    SideBySide "; break;
                case ViewMode.SideBySide: viewText = "  Original    Preview  [ SIDE-BY-SIDE ]"; break;
            }
            string filterText = _filterMatchesOnly ? "[ MATCHES ] All" : "  Matches [ ALL ]";
            _viewModeLabel.Text = $"{viewText} (Alt+P)    Filter: {filterText} (Alt+A)";

            if (_currentView == ViewMode.SideBySide)
            {
                _previewListLeft.Width = Dim.Percent(50);
                _previewListRight.Visible = true;
                _separatorMid.Visible = true;
                _previewListLeft.Source = new PreviewListWrapper(this, _currentPreviewItems, _config, false, true);
                _previewListRight.Source = new PreviewListWrapper(this, _currentPreviewItems, _config, true, false);
                SyncLists(_previewListLeft, _previewListRight);
            }
            else
            {
                _previewListLeft.Width = Dim.Fill(1);
                _previewListRight.Visible = false;
                _separatorMid.Visible = false;
                bool showOriginal = _currentView == ViewMode.Original;
                _previewListLeft.Source = new PreviewListWrapper(this, _currentPreviewItems, _config, false, showOriginal);
            }
            _previewListLeft.SetNeedsDisplay();
        }

        private void TriggerPreviewUpdate()
        {
            _previewCts?.Cancel();
            _previewCts = new CancellationTokenSource();
            var token = _previewCts.Token;

            string findText = _findField.Text.ToString() ?? "";
            string replaceText = _replaceField.Text.ToString() ?? "";
            
            string effectivePattern = ConstructPattern(findText, replaceText, _regexCheck.Checked, _caseCheck.Checked);
            
            Task.Delay(200, token).ContinueWith(t => 
            {
                if (t.IsCanceled) return;
                CalculatePreview(effectivePattern, token);
            }, token);
        }

        private string ConstructPattern(string find, string replace, bool isRegex, bool caseSensitive)
        {
            if (string.IsNullOrEmpty(find)) return "";
            if (find.StartsWith("s/") || find.StartsWith("tr/")) return find;

            if (isRegex)
            {
                string escapedFind = find.Replace("/", "\\/");
                string escapedReplace = replace.Replace("/", "\\/");
                string flags = caseSensitive ? "" : "i";
                return $"s/{escapedFind}/{escapedReplace}/{flags}";
            }
            return find;
        }

        private void CalculatePreview(string effectivePattern, CancellationToken token)
        {
            try
            {
                var results = new List<PreviewItem>();
                int maxItems = _config.Navigation.MaxRenamePreviewResults;
                int matchesFound = 0;
                int itemsScanned = 0;
                
                string errorMsg = "";
                char[] invalidChars = Path.GetInvalidFileNameChars();
                List<char> foundInvalidList = new List<char>();

                foreach (var file in _sourceFiles)
                {
                    if (token.IsCancellationRequested) return;
                    
                    // Logic: Show all files in gray when find is empty (skip filter)
                    bool skipFilter = string.IsNullOrEmpty(effectivePattern);
                    
                    if (results.Count >= maxItems && (_filterMatchesOnly && !skipFilter)) break; 
                    if (itemsScanned >= maxItems && (!_filterMatchesOnly || skipFilter)) break; 
                    
                    itemsScanned++;
                    string newName = file.Name;
                    string? itemError = null;

                    if (!string.IsNullOrEmpty(effectivePattern))
                    {
                        try 
                        {
                            newName = FileOperations.ApplyRenamePattern(file.Name, effectivePattern, _replaceField.Text.ToString() ?? "");
                            foreach (char c in newName)
                            {
                                if (Array.IndexOf(invalidChars, c) >= 0)
                                {
                                    bool found = false;
                                    foreach (var ex in foundInvalidList) if (ex == c) found = true;
                                    if (!found) foundInvalidList.Add(c);
                                    itemError = "Invalid character";
                                }
                            }
                        }
                        catch { errorMsg = "Regex Error"; itemError = "Pattern error"; }
                    }

                    bool isMatch = newName != file.Name;
                    if (isMatch) matchesFound++;

                    // Add to results if it matches, OR if we are showing all, OR if find is empty
                    if (isMatch || !_filterMatchesOnly || skipFilter)
                    {
                        results.Add(new PreviewItem { OriginalName = file.Name, NewName = newName, ValidationError = itemError });
                    }
                }

                Application.MainLoop.Invoke(() => 
                {
                    if (token.IsCancellationRequested) return;
                    _currentPreviewItems = results;
                    UpdateViewLayout();
                    
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"Matches: {matchesFound}");
                    if (results.Count >= maxItems) sb.Append($" (Top {maxItems} shown)");
                    if (!string.IsNullOrEmpty(errorMsg)) { sb.Append("  ["); sb.Append(errorMsg); sb.Append("]"); }
                    if (foundInvalidList.Count > 0)
                    {
                        sb.Append("  [Invalid Chars: ");
                        for (int i = 0; i < foundInvalidList.Count; i++) { if (i > 0) sb.Append(" "); sb.Append("'"); sb.Append(foundInvalidList[i]); sb.Append("'"); }
                        sb.Append("]");
                    }
                    _statusLabel.Text = sb.ToString();
                    _statusLabel.ColorScheme = (errorMsg != "" || foundInvalidList.Count > 0) ? Colors.Error : Colors.Base;
                });
            }
            catch (Exception ex) { Application.MainLoop.Invoke(() => { _statusLabel.Text = $"Error: {ex.Message}"; _statusLabel.ColorScheme = Colors.Error; }); }
        }

        private class PreviewListWrapper : Terminal.Gui.IListDataSource
        {
            private readonly PatternRenameDialog _parent;
            private readonly List<PreviewItem> _items;
            private readonly Configuration _config;
            private readonly bool _isRightPane;
            private readonly bool _isOriginalMode;

            public PreviewListWrapper(PatternRenameDialog parent, List<PreviewItem> items, Configuration config, bool isRightPane, bool isOriginalMode)
            {
                _parent = parent; _items = items; _config = config; _isRightPane = isRightPane; _isOriginalMode = isOriginalMode;
            }

            public int Count => _items.Count;
            public int Length => Count; 
            public bool IsMarked(int item) => false;
            public void SetMark(int item, bool value) { }

            public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start)
            {
                if (item < 0 || item >= _items.Count) return;
                container.Move(col, line);
                var data = _items[item];
                string text = _isOriginalMode ? data.OriginalName : data.NewName;
                
                var display = _config.Display;
                var normalFg = ColorHelper.ParseConfigColor(display.DialogListBoxForegroundColor, Color.Gray);
                var normalBg = ColorHelper.ParseConfigColor(display.DialogListBoxBackgroundColor, Color.Black);
                var changedFg = ColorHelper.ParseConfigColor(display.DialogListBoxSelectedForegroundColor, Color.BrightYellow);
                
                var attr = driver.MakeAttribute(normalFg, normalBg);
                if (data.IsChanged)
                {
                    if (_isOriginalMode) attr = driver.MakeAttribute(Color.White, normalBg);
                    else attr = driver.MakeAttribute(changedFg, normalBg);
                }
                else attr = driver.MakeAttribute(normalFg, normalBg);

                if (data.ValidationError != null && !_isOriginalMode) attr = driver.MakeAttribute(Color.Red, normalBg);
                
                if (selected)
                {
                    attr = driver.MakeAttribute(attr.Foreground, normalBg);
                }

                driver.SetAttribute(attr);
                int offset = _parent._horizontalOffset;
                if (offset > 0)
                {
                    if (offset < text.Length) text = text.Substring(offset);
                    else text = "";
                }
                if (text.Length < width) text = text.PadRight(width);
                if (text.Length > width) text = text.Substring(0, width); 
                driver.AddStr(text);
            }
            public System.Collections.IList ToList() => _items;
        }

        private void ApplyColors()
        {
            if (Application.Driver == null) return;
            var display = _config.Display;
            ColorHelper.ApplyStandardDialogColors(this, display, new View[] { _viewModeLabel }, new View[] { _findField, _replaceField });
            
            _previewListLeft.CanFocus = true;
            _previewListRight.CanFocus = false; 

            var listFg = ColorHelper.ParseConfigColor(display.DialogListBoxForegroundColor, Color.Gray);
            var listBg = ColorHelper.ParseConfigColor(display.DialogListBoxBackgroundColor, Color.Black);
            var selFg = ColorHelper.ParseConfigColor(display.DialogListBoxSelectedForegroundColor, Color.BrightYellow);
            var selBg = listBg; 

            var scheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(listFg, listBg), Focus = Application.Driver.MakeAttribute(selFg, selBg) };
            _previewListLeft.ColorScheme = _previewListRight.ColorScheme = scheme;
        }

        public static (string Pattern, string Replacement)? Show(List<FileEntry> files, Configuration config)
        {
            var dialog = new PatternRenameDialog(files, config);
            Application.Run(dialog);
            if (dialog.IsOk)
            {
                string findText = dialog.Pattern;
                if (dialog._regexCheck.Checked && !string.IsNullOrEmpty(findText) && !findText.StartsWith("s/") && !findText.StartsWith("tr/"))
                {
                    return (dialog.ConstructPattern(findText, dialog.Replacement, true, dialog._caseCheck.Checked), "");
                }
                return (findText, dialog.Replacement);
            }
            return null;
        }
    }
}
