using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using TWF.Controllers;
using TWF.Models;
using TWF.Services;
using TWF.Utilities;

namespace TWF.UI
{
    public class JumpToPathDialog : BaseJumpDialog
    {
        public JumpToPathDialog(MainController controller) 
            : base(controller, "Jump to Directory")
        {
            // Initial search to populate history/bookmarks
            TriggerSearch("");
        }

        protected override string GetFallbackPath(string input)
        {
            try
            {
                string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(input);
                if (Directory.Exists(expanded))
                {
                    return expanded;
                }
            }
            catch { }
            return string.Empty;
        }

        protected override List<string> GetSuggestions(string query, CancellationToken token)
        {
            var results = new List<string>();
            try
            {
                var uniqueSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Data Sources
                var bookmarks = _controller.Config.RegisteredFolders
                    .Select(b => EnvironmentVariableExpander.ExpandEnvironmentVariables(b.Path))
                    .Where(p => !string.IsNullOrWhiteSpace(p));

                var history = _controller.HistoryManager.LeftHistory
                    .Concat(_controller.HistoryManager.RightHistory)
                    .Where(p => !string.IsNullOrWhiteSpace(p));

                // 2. Empty Query -> Show History + Bookmarks
                if (string.IsNullOrWhiteSpace(query))
                {
                    foreach (var path in bookmarks)
                    {
                        if (uniqueSet.Add(path)) results.Add(path);
                    }
                    foreach (var path in history)
                    {
                        if (uniqueSet.Add(path)) results.Add(path);
                    }
                    return results;
                }

                string expandedQuery = EnvironmentVariableExpander.ExpandEnvironmentVariables(query);

                // 3. File System Search (if it looks like a path)
                if (expandedQuery.Contains(Path.DirectorySeparatorChar) || expandedQuery.Contains(Path.AltDirectorySeparatorChar) || (expandedQuery.Length >= 2 && expandedQuery[1] == ':'))
                {
                    try
                    {
                        string? dir = null;
                        string filePattern = "";

                        if (Directory.Exists(expandedQuery))
                        {
                            dir = expandedQuery;
                            filePattern = "";
                        }
                        else
                        {
                            dir = Path.GetDirectoryName(expandedQuery);
                            filePattern = Path.GetFileName(expandedQuery);
                        }
                        
                        // Handle root paths like "C:\" or "/"
                        if (string.IsNullOrEmpty(dir) && Path.IsPathRooted(expandedQuery))
                        {
                            dir = expandedQuery;
                            filePattern = "";
                        }

                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            var opts = new EnumerationOptions { IgnoreInaccessible = true };
                            var dirs = Directory.GetDirectories(dir, filePattern + "*", opts).Take(50);
                            
                            foreach (var d in dirs)
                            {
                                if (uniqueSet.Add(d)) results.Add(d);
                            }
                        }
                    }
                    catch { }
                }

                // 4. Fuzzy Search on History/Bookmarks
                var staticPaths = bookmarks.Concat(history).Distinct();
                foreach (var path in staticPaths)
                {
                    token.ThrowIfCancellationRequested();
                    if (uniqueSet.Contains(path)) continue;

                    try
                    {
                        // Use SearchEngine for smart matching (supports Migemo)
                        if (_controller.SearchEngine.IsMatch(path, expandedQuery))
                        {
                            if (uniqueSet.Add(path)) results.Add(path);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception)
            {
                // Failsafe return empty list
            }

            return results.Take(100).ToList();
        }
    }
}
