using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    /// <summary>
    /// Custom view for the tab bar that supports scrolling and colors.
    /// </summary>
    public class TabBarView : View
    {
        private List<string> _tabNames = new List<string>();
        private int _activeTabIndex = 0;
        private Configuration _config;
        private int _scrollOffset = 0;

        public TabBarView(Configuration config)
        {
            _config = config;
            Height = 1;
            Width = Dim.Fill();
        }

        public void UpdateTabs(List<string> names, int activeIndex)
        {
            _tabNames = names;
            _activeTabIndex = activeIndex;
            EnsureActiveTabVisible();
            SetNeedsDisplay();
        }

        private void EnsureActiveTabVisible()
        {
            if (_tabNames.Count == 0) return;

            if (_activeTabIndex < _scrollOffset)
            {
                _scrollOffset = _activeTabIndex;
            }
            else
            {
                int width = Bounds.Width;
                if (width <= 0) return;

                // Pre-calculate positions
                var positions = new List<int>();
                int currentPos = 0;
                for (int i = 0; i < _tabNames.Count; i++)
                {
                    string displayText = i == _activeTabIndex ? $"[{_tabNames[i]}]" : $" {_tabNames[i]} ";
                    positions.Add(currentPos);
                    currentPos += CharacterWidthHelper.GetStringWidth(displayText);
                }

                // Calculate relative position based on current scroll
                // Space reserved: 1 for right '>' indicator, 2 for left '< ' if scrolled
                int GetRelativeEnd(int offset)
                {
                    int startX = offset > 0 ? 2 : 0;
                    int activeWidth = CharacterWidthHelper.GetStringWidth($"[{_tabNames[_activeTabIndex]}]");
                    return startX + (positions[_activeTabIndex] - positions[offset]) + activeWidth;
                }

                int limit = width - 1; // Always reserve 1 for potential '>'

                while (GetRelativeEnd(_scrollOffset) > limit && _scrollOffset < _activeTabIndex)
                {
                    _scrollOffset++;
                }
            }
        }

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            var display = _config.Display;
            var activeAttr = Driver.MakeAttribute(
                ColorHelper.ParseConfigColor(display.ActiveTabForegroundColor, Color.White),
                ColorHelper.ParseConfigColor(display.ActiveTabBackgroundColor, Color.Blue)
            );
            var inactiveAttr = Driver.MakeAttribute(
                ColorHelper.ParseConfigColor(display.InactiveTabForegroundColor, Color.Gray),
                ColorHelper.ParseConfigColor(display.InactiveTabBackgroundColor, Color.Black)
            );
            var barAttr = Driver.MakeAttribute(
                ColorHelper.ParseConfigColor(display.ForegroundColor, Color.White),
                ColorHelper.ParseConfigColor(display.TabbarBackgroundColor, Color.Black)
            );

            Driver.SetAttribute(barAttr);
            Move(0, 0);
            Driver.AddStr(new string(' ', bounds.Width));

            if (_tabNames.Count == 0) return;

            int x = 0;
            int limit = bounds.Width - 1; // Reserve for '>'

            if (_scrollOffset > 0)
            {
                Driver.SetAttribute(barAttr);
                Move(0, 0);
                Driver.AddRune('<');
                x = 2;
            }

            for (int i = _scrollOffset; i < _tabNames.Count; i++)
            {
                string name = _tabNames[i];
                string displayText = i == _activeTabIndex ? $"[{name}]" : $" {name} ";
                int tabWidth = CharacterWidthHelper.GetStringWidth(displayText);

                if (x + tabWidth > limit)
                {
                    Driver.SetAttribute(barAttr);
                    Move(bounds.Width - 1, 0);
                    Driver.AddRune('>');
                    break;
                }

                Driver.SetAttribute(i == _activeTabIndex ? activeAttr : inactiveAttr);
                Move(x, 0);
                Driver.AddStr(displayText);
                x += tabWidth;
            }
        }

        /// <summary>
        /// Updates the configuration used by this TabBarView
        /// </summary>
        public void UpdateConfiguration(Configuration config)
        {
            _config = config;
            SetNeedsDisplay(); // Trigger a redraw with new colors
        }
    }
}
