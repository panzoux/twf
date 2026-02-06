using System;
using System.Collections.Generic;
using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    /// <summary>
    /// Dialog for selecting and managing tabs.
    /// Supports filtering, jumping to tabs, and closing tabs.
    /// </summary>
    public class TabSelectorDialog : Dialog
    {
        private readonly List<TabItem> _allTabs;
        private readonly SearchEngine _searchEngine;
        private readonly Configuration _configuration;
        private readonly Func<int, bool> _onCloseTab;
        
        private ListView _tabList = null!;
        private TextView _leftPathView = null!;
        private TextView _rightPathView = null!;
        private Label _helpBar = null!;
        private Label _searchLabel = null!;
        private Label _searchTextLabel = null!;

        private List<TabItem> _filteredItems = new List<TabItem>();
        private string _searchPattern = "";

        public int SelectedTabIndex { get; private set; } = -1;
        public bool IsJumped { get; private set; } = false;

        public class TabItem
        {
            public int OriginalIndex { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string LeftPath { get; set; } = string.Empty;
            public string RightPath { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        public TabSelectorDialog(
            List<TabItem> tabs,
            SearchEngine searchEngine,
            Configuration configuration,
            Func<int, bool> onCloseTab)
            : base("Select Tab", 70, 25)
        {
            _allTabs = tabs;
            _searchEngine = searchEngine;
            _configuration = configuration;
            _onCloseTab = onCloseTab;
            _filteredItems = new List<TabItem>(_allTabs);

            InitializeComponents();
            ApplyColors();
            
            // Set initial selection to active tab
            var activeIdx = _filteredItems.FindIndex(t => t.IsActive);
            if (activeIdx >= 0) _tabList.SelectedItem = activeIdx;
        }

        private void InitializeComponents()
        {
            // Tab List
            _tabList = new ListView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(13),
                AllowsMarking = false
            };

            var displayNames = new List<string>(_filteredItems.Count);
            foreach (var t in _filteredItems) displayNames.Add(t.DisplayName);
            _tabList.Source = new ListWrapper(displayNames);

            _tabList.SelectedItemChanged += (args) => UpdatePathPreview();
            
            _tabList.KeyPress += (e) =>
            {
                var keyEvent = e.KeyEvent;

                if (keyEvent.Key == Key.Esc)
                {
                    Application.RequestStop();
                    e.Handled = true;
                    return;
                }

                if (keyEvent.Key == (Key.K | Key.CtrlMask))
                {
                    _searchPattern = "";
                    FilterTabs();
                    e.Handled = true;
                    return;
                }

                if (keyEvent.Key == Key.Delete || keyEvent.Key == Key.DeleteChar)
                {
                    CloseSelectedTab();
                    e.Handled = true;
                    return;
                }

                var key = keyEvent.Key;
                var cleanKey = (Key)((uint)key & 0xFFFF);
                if (cleanKey == Key.Enter || cleanKey == (Key)10 || cleanKey == (Key)13)
                {
                    JumpToSelected();
                    e.Handled = true;
                    return;
                }

                if (keyEvent.Key == Key.Backspace)
                {
                    if (_searchPattern.Length > 0)
                    {
                        _searchPattern = _searchPattern.Substring(0, _searchPattern.Length - 1);
                        FilterTabs();
                    }
                    e.Handled = true;
                    return;
                }

                if ((keyEvent.Key & (Key.AltMask | Key.CtrlMask)) == 0)
                {
                    uint val = (uint)keyEvent.Key;
                    if ((val >= 32 && val <= 126))
                    {
                        _searchPattern += (char)val;
                        FilterTabs();
                        e.Handled = true;
                        return;
                    }
                }
            };

            Add(_tabList);

            // Path Preview Area
            var separator = new Label(new string('─', 68)) { X = 0, Y = Pos.Bottom(_tabList), Width = Dim.Fill() };
            Add(separator);

            var lLabel = new Label("L:") { X = 0, Y = Pos.Bottom(separator) };
            _leftPathView = new TextView() 
            { 
                X = 2, 
                Y = Pos.Bottom(separator), 
                Width = 64, 
                Height = 4, 
                ReadOnly = true,
                WordWrap = true
            };
            Add(lLabel, _leftPathView);

            var rLabel = new Label("R:") { X = 0, Y = Pos.Bottom(_leftPathView)+1 };
            _rightPathView = new TextView() 
            { 
                X = 2, 
                Y = Pos.Bottom(_leftPathView)+1, 
                Width = 64, 
                Height = 4, 
                ReadOnly = true,
                WordWrap = true
            };
            Add(rLabel, _rightPathView);

            var separator2 = new Label(new string('─', 68)) { X = 0, Y = Pos.Bottom(_rightPathView), Width = Dim.Fill() };
            Add(separator2);

            // Help Bar
            _helpBar = new Label("[Enter] Jump [Del] Close [Ctrl+K] Clear [Esc] Cancel")
            {
                X = 0,
                Y = Pos.Bottom(separator2),
                Width = Dim.Fill()
            };
            Add(_helpBar);

            // Search Line
            _searchLabel = new Label("/") { X = 0, Y = Pos.Bottom(_helpBar), Width = 1 };
            _searchTextLabel = new Label("") { X = 1, Y = Pos.Bottom(_helpBar), Width = Dim.Fill() };
            Add(_searchLabel, _searchTextLabel);

            UpdatePathPreview();
        }

        private void FilterTabs()
        {
            _searchTextLabel.Text = _searchPattern;
            if (string.IsNullOrEmpty(_searchPattern))
            {
                _filteredItems = new List<TabItem>(_allTabs);
            }
            else
            {
                var tempEntries = new List<FileEntry>(_allTabs.Count);
                foreach (var t in _allTabs) tempEntries.Add(new FileEntry { Name = t.DisplayName });
                
                var matches = _searchEngine.FindMatches(tempEntries, _searchPattern, _configuration.Migemo.Enabled);
                
                _filteredItems = new List<TabItem>(matches.Count);
                foreach (var idx in matches) _filteredItems.Add(_allTabs[idx]);
            }

            var displayNames = new List<string>(_filteredItems.Count);
            foreach (var t in _filteredItems) displayNames.Add(t.DisplayName);
            _tabList.Source = new ListWrapper(displayNames);
            
            if (_filteredItems.Count > 0) _tabList.SelectedItem = 0;
            UpdatePathPreview();
        }

        private void UpdatePathPreview()
        {
            if (_tabList.SelectedItem >= 0 && _tabList.SelectedItem < _filteredItems.Count)
            {
                var item = _filteredItems[_tabList.SelectedItem];
                _leftPathView.Text = item.LeftPath;
                _rightPathView.Text = item.RightPath;
            }
            else
            {
                _leftPathView.Text = "";
                _rightPathView.Text = "";
            }
        }

        private void JumpToSelected()
        {
            if (_tabList.SelectedItem >= 0 && _tabList.SelectedItem < _filteredItems.Count)
            {
                SelectedTabIndex = _filteredItems[_tabList.SelectedItem].OriginalIndex;
                IsJumped = true;
                Application.RequestStop();
            }
        }

        private void CloseSelectedTab()
        {
            if (_filteredItems.Count <= 1) return; // Don't close last tab in dialog

            if (_tabList.SelectedItem >= 0 && _tabList.SelectedItem < _filteredItems.Count)
            {
                var item = _filteredItems[_tabList.SelectedItem];
                if (_onCloseTab(item.OriginalIndex))
                {
                    _allTabs.Remove(item);
                    // Update original indices for remaining items
                    for (int i = 0; i < _allTabs.Count; i++)
                    {
                        if (_allTabs[i].OriginalIndex > item.OriginalIndex)
                            _allTabs[i].OriginalIndex--;
                    }
                    FilterTabs();
                }
            }
        }

        private void ApplyColors()
        {
            if (Application.Driver == null) return;
            var display = _configuration.Display;
            var foreground = ColorHelper.ParseConfigColor(display.ForegroundColor, Color.White);
            var background = ColorHelper.ParseConfigColor(display.BackgroundColor, Color.Black);
            var highlightFg = ColorHelper.ParseConfigColor(display.HighlightForegroundColor, Color.Black);
            var highlightBg = ColorHelper.ParseConfigColor(display.HighlightBackgroundColor, Color.Cyan);
            
            var dialogFg = ColorHelper.ParseConfigColor(display.DialogForegroundColor, Color.Black);
            var dialogBg = ColorHelper.ParseConfigColor(display.DialogBackgroundColor, Color.Gray);

            // Apply to Dialog Frame/Body
            var dialogScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg),
                HotNormal = Application.Driver.MakeAttribute(dialogFg, dialogBg),
                HotFocus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
            };
            this.ColorScheme = dialogScheme;

            _tabList.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(foreground, background),
                Focus = Application.Driver.MakeAttribute(highlightFg, highlightBg)
            };

            var helpFg = ColorHelper.ParseConfigColor(display.DialogHelpForegroundColor, Color.BrightYellow);
            var helpBg = ColorHelper.ParseConfigColor(display.DialogHelpBackgroundColor, Color.Blue);
            _helpBar.ColorScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(helpFg, helpBg) };
            
            var inputFg = ColorHelper.ParseConfigColor(display.InputForegroundColor, Color.White);
            var inputBg = ColorHelper.ParseConfigColor(display.InputBackgroundColor, Color.DarkGray);
            _leftPathView.ColorScheme = _rightPathView.ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(inputFg, inputBg),
                Focus = Application.Driver.MakeAttribute(inputFg, inputBg)
            };
            
            _searchLabel.ColorScheme = _searchTextLabel.ColorScheme = new ColorScheme { Normal = Application.Driver.MakeAttribute(dialogFg, dialogBg) };
        }
    }
}
