using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;
using TWF.Services;

namespace TWF.UI
{
    public class VirtualFileView : View
    {
        private readonly LargeFileEngine _engine;
        private FileViewMode _mode = FileViewMode.Text;
        private long _scrollOffset = 0; // Line index (Text) or Row index (Hex)
        private int _contentHeight = 0;
        
        public VirtualFileView(LargeFileEngine engine)
        {
            _engine = engine;
            CanFocus = true;
        }

        public FileViewMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                _scrollOffset = 0;
                SetNeedsDisplay();
            }
        }

        public long ScrollOffset
        {
            get => _scrollOffset;
            set
            {
                long max = GetMaxScrollOffset();
                _scrollOffset = Math.Clamp(value, 0, max);
                SetNeedsDisplay();
            }
        }

        public string? HighlightPattern { get; set; }
        public bool IsRegex { get; set; }

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
                if (lineContent.Length > maxContentWidth)
                {
                    lineContent = lineContent.Substring(0, maxContentWidth);
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
                        var matches = regex.Matches(lineContent);
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            Driver.SetAttribute(normal);
                            Driver.AddStr(lineContent.Substring(lastPos, match.Index - lastPos));
                            Driver.SetAttribute(inverted);
                            Driver.AddStr(match.Value);
                            lastPos = match.Index + match.Length;
                        }
                    }
                    else
                    {
                        int idx = lineContent.IndexOf(HighlightPattern, StringComparison.OrdinalIgnoreCase);
                        while (idx >= 0)
                        {
                            Driver.SetAttribute(normal);
                            Driver.AddStr(lineContent.Substring(lastPos, idx - lastPos));
                            Driver.SetAttribute(inverted);
                            Driver.AddStr(lineContent.Substring(idx, HighlightPattern.Length));
                            lastPos = idx + HighlightPattern.Length;
                            idx = lineContent.IndexOf(HighlightPattern, lastPos, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    Driver.SetAttribute(normal);
                    Driver.AddStr(lineContent.Substring(lastPos));
                }
                else
                {
                    Driver.SetAttribute(GetNormalColor());
                    Driver.AddStr(lineContent);
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

            for (int i = 0; i < linesToShow; i++)
            {
                int y = i;
                long currentRow = startRow + i;
                long currentOffset = currentRow * 16;

                if (currentOffset >= _engine.FileSize) break;

                // Draw Offset
                Move(0, y);
                Driver.SetAttribute(ColorScheme.HotNormal);
                Driver.AddStr(currentOffset.ToString("X8"));
                Driver.AddStr("  ");

                // Draw Hex
                Driver.SetAttribute(GetNormalColor());
                
                int bytesInRow = 16;
                if (currentOffset + 16 > _engine.FileSize)
                    bytesInRow = (int)(_engine.FileSize - currentOffset);

                StringBuilder hexPart = new StringBuilder();
                StringBuilder asciiPart = new StringBuilder();

                for (int b = 0; b < 16; b++)
                {
                    if (b < bytesInRow)
                    {
                        int byteIdx = (i * 16) + b;
                        if (byteIdx < data.Length)
                        {
                            byte val = data[byteIdx];
                            hexPart.Append(val.ToString("X2"));
                            asciiPart.Append((val >= 32 && val <= 126) ? (char)val : '.');
                        }
                        else 
                        {
                            // Should not happen if logic is correct
                            hexPart.Append("  ");
                            asciiPart.Append(" ");
                        }
                    }
                    else
                    {
                        hexPart.Append("  ");
                        asciiPart.Append(" ");
                    }

                    hexPart.Append(" ");
                    if (b == 7) hexPart.Append(" ");
                }

                Driver.AddStr(hexPart.ToString());
                Driver.AddStr(" |");
                Driver.AddStr(asciiPart.ToString());
                Driver.AddStr("|");
            }
        }
    }
}
