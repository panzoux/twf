using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;
using TWF.Services;
using TWF.Models;

namespace TWF.UI
{
    public class VirtualFileView : View
    {
        private readonly LargeFileEngine _engine;
        private FileViewMode _mode = FileViewMode.Text;
        private long _scrollOffset = 0; // Line index (Text) or Row index (Hex)
        private int _horizontalOffset = 0;
        private int _contentHeight = 0;
        
        public Configuration? Configuration { get; set; }

        public VirtualFileView(LargeFileEngine engine)
        {
            _engine = engine;
            CanFocus = true;
        }

        public event Action? OffsetChanged;

        public FileViewMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                _scrollOffset = 0;
                _horizontalOffset = 0;
                SetNeedsDisplay();
                OffsetChanged?.Invoke();
            }
        }

        public long ScrollOffset
        {
            get => _scrollOffset;
            set
            {
                long max = GetMaxScrollOffset();
                long newValue = Math.Clamp(value, 0, max);
                if (_scrollOffset != newValue)
                {
                    _scrollOffset = newValue;
                    SetNeedsDisplay();
                    OffsetChanged?.Invoke();
                }
            }
        }

        public int HorizontalOffset
        {
            get => _horizontalOffset;
            set
            {
                // Approximation: max width is roughly max bytes * multiplier (for CJK/Tabs)
                double multiplier = Configuration?.Viewer?.HorizontalScrollMultiplier ?? 2.0;
                int maxScroll = Math.Max(0, (int)(_engine.MaxLineByteCount * multiplier) + 10 - Bounds.Width);
                int newValue = Math.Max(0, Math.Min(value, maxScroll));
                
                if (_horizontalOffset != newValue)
                {
                    _horizontalOffset = newValue;
                    SetNeedsDisplay();
                    OffsetChanged?.Invoke();
                }
            }
        }

        public string? HighlightPattern { get; set; }
        public HexSearchResult CurrentMatch { get; set; } = HexSearchResult.NotFound;
        public bool IsRegex { get; set; }

        private new Terminal.Gui.Attribute GetNormalColor()
        {
            return ColorScheme.Normal;
        }

        private long GetMaxScrollOffset()
        {
            // Use Frame.Height if _contentHeight hasn't been set by Redraw yet
            int height = _contentHeight > 0 ? _contentHeight : Frame.Height;

            if (_mode == FileViewMode.Text)
            {
                 // Clamping so the last line stays at the bottom of the screen if possible
                 return Math.Max(0, _engine.LineCount - height);
            }
            else
            {
                 // Hex mode: 16 bytes per row
                 long totalRows = (_engine.FileSize + 15) / 16;
                 return Math.Max(0, totalRows - height);
            }
        }

        // Navigation Methods
        public void ScrollUp(int lines) => ScrollOffset -= lines;
        public void ScrollDown(int lines) => ScrollOffset += lines;
        public void ScrollToTop() => ScrollOffset = 0;
        public void ScrollToBottom() 
        {
            if (_mode == FileViewMode.Text)
                ScrollOffset = Math.Max(0, _engine.LineCount - _contentHeight);
            else
                ScrollOffset = Math.Max(0, (_engine.FileSize / 16) - _contentHeight);
        }

        public void PageUp() => ScrollUp(_contentHeight);
        public void PageDown() => ScrollDown(_contentHeight);

        public override void Redraw(Rect bounds)
        {
            base.Redraw(bounds);

            _contentHeight = bounds.Height;
            int width = bounds.Width;

            // Clear view
            Driver.SetAttribute(GetNormalColor());
            for (int y = 0; y < _contentHeight; y++)
            {
                Move(0, y);
                Driver.AddStr(new string(' ', width));
            }

            if (_mode == FileViewMode.Text)
            {
                DrawTextMode(bounds);
            }
            else
            {
                DrawHexMode(bounds);
            }
        }

        private void DrawTextMode(Rect bounds)
        {
            // Calculate how many lines we can show
            int linesToShow = bounds.Height;
            
            // Fetch lines from engine
            // scrollOffset is line index
            int startLine = (int)_scrollOffset; // Note: LargeFileEngine uses int for line count, but long is safer for future
            
            var lines = _engine.GetTextLines(startLine, linesToShow);

            int lineNumberWidth = _engine.LineCount.ToString().Length + 1;
            
            for (int i = 0; i < lines.Count; i++)
            {
                int y = i;
                if (y >= bounds.Height) break;

                string lineContent = lines[i];
                // Replace tabs with spaces
                lineContent = lineContent.Replace("\t", "    ");

                // Draw Line Number
                Move(0, y);
                Driver.SetAttribute(ColorScheme.HotNormal); // Highlight color for line numbers
                string lineNumStr = (startLine + i + 1).ToString().PadLeft(lineNumberWidth - 1);
                Driver.AddStr(lineNumStr);
                Driver.AddStr(" | ");

                // Draw Content
                int maxContentWidth = Math.Max(0, bounds.Width - (lineNumberWidth + 3));
                
                // Horizontal scrolling slicing
                string visiblePart = "";
                if (_horizontalOffset < lineContent.Length)
                {
                    int length = Math.Min(maxContentWidth, lineContent.Length - _horizontalOffset);
                    visiblePart = lineContent.Substring(_horizontalOffset, length);
                }

                if (!string.IsNullOrEmpty(HighlightPattern))
                {
                    System.Text.RegularExpressions.Regex? regex = null;
                    if (IsRegex)
                    {
                        try { regex = new System.Text.RegularExpressions.Regex(HighlightPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase); } catch { }
                    }

                    int lastPos = 0;
                    var normal = GetNormalColor();
                    var inverted = Driver.MakeAttribute(normal.Background, normal.Foreground);

                    if (regex != null)
                    {
                        var matches = regex.Matches(visiblePart);
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            Driver.SetAttribute(normal);
                            Driver.AddStr(visiblePart.Substring(lastPos, match.Index - lastPos));
                            Driver.SetAttribute(inverted);
                            Driver.AddStr(match.Value);
                            lastPos = match.Index + match.Length;
                        }
                    }
                    else
                    {
                        int idx = visiblePart.IndexOf(HighlightPattern, StringComparison.OrdinalIgnoreCase);
                        while (idx >= 0)
                        {
                            Driver.SetAttribute(normal);
                            Driver.AddStr(visiblePart.Substring(lastPos, idx - lastPos));
                            Driver.SetAttribute(inverted);
                            Driver.AddStr(visiblePart.Substring(idx, HighlightPattern.Length));
                            lastPos = idx + HighlightPattern.Length;
                            idx = visiblePart.IndexOf(HighlightPattern, lastPos, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    Driver.SetAttribute(normal);
                    Driver.AddStr(visiblePart.Substring(lastPos));
                }
                else
                {
                    Driver.SetAttribute(GetNormalColor());
                    Driver.AddStr(visiblePart);
                }
            }
        }

        private void DrawHexMode(Rect bounds)
        {
            int linesToShow = bounds.Height;
            long startRow = _scrollOffset;
            long startByte = startRow * 16;
            int bytesNeeded = linesToShow * 16;

            byte[] data = _engine.GetBytes(startByte, bytesNeeded);

            // Prepare highlighting info
            byte[]? highlightBytes = null;
            string? highlightPattern = HighlightPattern;
            if (!string.IsNullOrEmpty(highlightPattern))
            {
                if (!IsValidHexQuery(highlightPattern, out highlightBytes))
                {
                    highlightBytes = Encoding.ASCII.GetBytes(highlightPattern);
                }
            }

            // Find all match start positions in the visible data buffer
            List<int> visibleMatches = new List<int>();
            if (highlightBytes != null && highlightBytes.Length > 0)
            {
                for (int i = 0; i <= data.Length - highlightBytes.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < highlightBytes.Length; j++)
                    {
                        if (data[i + j] != highlightBytes[j]) { match = false; break; }
                    }
                    if (match) visibleMatches.Add(i);
                }
            }

            for (int i = 0; i < linesToShow; i++)
            {
                int y = i;
                long currentRow = startRow + i;
                long currentOffset = currentRow * 16;

                if (currentOffset >= _engine.FileSize) break;

                // Draw Offset
                Move(0, y);
                Driver.SetAttribute(ColorScheme.HotNormal);
                string offsetStr = currentOffset.ToString("X8");
                
                if (highlightPattern != null && CurrentMatch.MatchType == HexMatchType.Address && CurrentMatch.Offset == currentOffset)
                {
                    DrawHighlightedString(offsetStr, highlightPattern, ColorScheme.HotNormal, GetInvertedAttribute(ColorScheme.HotNormal));
                }
                else
                {
                    Driver.AddStr(offsetStr);
                }
                Driver.AddStr("  ");

                // Draw Hex and ASCII columns
                int bytesInRow = 16;
                if (currentOffset + 16 > _engine.FileSize)
                    bytesInRow = (int)(_engine.FileSize - currentOffset);

                // --- Hex Column ---
                Driver.SetAttribute(GetNormalColor());
                for (int b = 0; b < 16; b++)
                {
                    if (b < bytesInRow)
                    {
                        int byteIdx = (i * 16) + b;
                        byte val = data[byteIdx];
                        
                        bool isMatch = IsByteInMatch(byteIdx, currentOffset + b, visibleMatches, highlightBytes);

                        if (isMatch) Driver.SetAttribute(GetInvertedAttribute(GetNormalColor()));
                        else Driver.SetAttribute(GetNormalColor());

                        Driver.AddStr(val.ToString("X2"));
                        
                        Driver.SetAttribute(GetNormalColor());
                        Driver.AddStr(" ");
                        if (b == 7) Driver.AddStr(" ");
                    }
                    else
                    {
                        Driver.AddStr("   ");
                        if (b == 7) Driver.AddStr(" ");
                    }
                }

                // --- ASCII Column ---
                Driver.AddStr("|");
                for (int b = 0; b < 16; b++)
                {
                    if (b < bytesInRow)
                    {
                        int byteIdx = (i * 16) + b;
                        byte val = data[byteIdx];
                        
                        bool isMatch = IsByteInMatch(byteIdx, currentOffset + b, visibleMatches, highlightBytes);

                        if (isMatch) Driver.SetAttribute(GetInvertedAttribute(GetNormalColor()));
                        else Driver.SetAttribute(GetNormalColor());

                        Driver.AddRune((val >= 32 && val <= 126) ? (System.Rune)(int)val : (System.Rune)'.');
                    }
                    else
                    {
                        Driver.AddStr(" ");
                    }
                }
                Driver.SetAttribute(GetNormalColor());
                Driver.AddStr("|");
            }
        }

        private bool IsByteInMatch(int bufferIdx, long fileOffset, List<int> visibleMatches, byte[]? pattern)
        {
            if (pattern == null || pattern.Length == 0) return false;

            // 1. Check if part of the CurrentMatch (Jump target)
            if (CurrentMatch.MatchType == HexMatchType.Data)
            {
                if (fileOffset >= CurrentMatch.Offset && fileOffset < CurrentMatch.Offset + pattern.Length)
                    return true;
            }

            // 2. Check if part of any passive match visible on screen
            foreach (int matchStart in visibleMatches)
            {
                if (bufferIdx >= matchStart && bufferIdx < matchStart + pattern.Length)
                    return true;
            }

            return false;
        }

        private void DrawHighlightedString(string text, string pattern, Terminal.Gui.Attribute normal, Terminal.Gui.Attribute highlighted)
        {
            int lastPos = 0;
            int idx = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                Driver.SetAttribute(normal);
                Driver.AddStr(text.Substring(lastPos, idx - lastPos));
                Driver.SetAttribute(highlighted);
                Driver.AddStr(text.Substring(idx, pattern.Length));
                lastPos = idx + pattern.Length;
                idx = text.IndexOf(pattern, lastPos, StringComparison.OrdinalIgnoreCase);
            }
            Driver.SetAttribute(normal);
            Driver.AddStr(text.Substring(lastPos));
        }

        private Terminal.Gui.Attribute GetInvertedAttribute(Terminal.Gui.Attribute attr)
        {
            return Driver.MakeAttribute(attr.Background, attr.Foreground);
        }

        private bool IsValidHexQuery(string query, out byte[] bytes)
        {
            string clean = query.Replace(" ", "").Replace("-", "");
            if (clean.Length % 2 != 0 || clean.Length == 0)
            {
                bytes = Array.Empty<byte>();
                return false;
            }

            try
            {
                bytes = new byte[clean.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
                }
                return true;
            }
            catch
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }
    }
}
