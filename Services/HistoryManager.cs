using System;
using System.Collections.Generic;
using TWF.Models;

namespace TWF.Services
{
    public class HistoryManager
    {
        private readonly List<string> _leftHistory = new List<string>();
        private readonly List<string> _rightHistory = new List<string>();
        private readonly Configuration _configuration;
        private int _leftIndex = 0;
        private int _rightIndex = 0;

        public HistoryManager(Configuration configuration)
        {
            _configuration = configuration;
        }

        public IReadOnlyList<string> LeftHistory => _leftHistory;
        public IReadOnlyList<string> RightHistory => _rightHistory;

        public void SetHistory(bool isLeft, List<string> history)
        {
            var target = isLeft ? _leftHistory : _rightHistory;
            target.Clear();
            if (history != null)
            {
                foreach (var p in history)
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        target.Add(p);
                    }
                }
            }
            if (isLeft) _leftIndex = 0; else _rightIndex = 0;
            TrimHistory(isLeft);
        }

        public void Add(bool isLeft, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var target = isLeft ? _leftHistory : _rightHistory;
            
            // If the path is already at the current index, don't do anything
            int currentIndex = isLeft ? _leftIndex : _rightIndex;
            if (target.Count > currentIndex && string.Equals(target[currentIndex], path, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Move to top if exists, otherwise just insert at top
            target.Remove(path);
            target.Insert(0, path);

            // Reset navigation pointer to head
            if (isLeft) _leftIndex = 0; else _rightIndex = 0;

            TrimHistory(isLeft);
        }

        /// <summary>
        /// Moves the history pointer back by one and returns the path.
        /// </summary>
        public string? GoBack(bool isLeft)
        {
            var target = isLeft ? _leftHistory : _rightHistory;
            int currentIndex = isLeft ? _leftIndex : _rightIndex;

            if (currentIndex + 1 < target.Count)
            {
                currentIndex++;
                if (isLeft) _leftIndex = currentIndex; else _rightIndex = currentIndex;
                return target[currentIndex];
            }
            return null;
        }

        /// <summary>
        /// Moves the history pointer forward by one and returns the path.
        /// </summary>
        public string? GoForward(bool isLeft)
        {
            var target = isLeft ? _leftHistory : _rightHistory;
            int currentIndex = isLeft ? _leftIndex : _rightIndex;

            if (currentIndex > 0 && target.Count > 0)
            {
                currentIndex--;
                if (isLeft) _leftIndex = currentIndex; else _rightIndex = currentIndex;
                return target[currentIndex];
            }
            return null;
        }

        private void TrimHistory(bool isLeft)
        {
            var target = isLeft ? _leftHistory : _rightHistory;
            int maxItems = _configuration.MaxHistoryItems;
            if (maxItems > 0 && target.Count > maxItems)
            {
                target.RemoveRange(maxItems, target.Count - maxItems);
            }
        }

        public void Clear(bool isLeft)
        {
            var target = isLeft ? _leftHistory : _rightHistory;
            target.Clear();
        }
    }
}
