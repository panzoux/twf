using System;
using System.Collections.Generic;
using System.IO;
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
            var tokens = ParseTokens(query);

            try
            {
                var uniqueSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. Data Sources
                var bookmarks = _controller.Config.RegisteredFolders;
                var historyLeft = _controller.HistoryManager.LeftHistory;
                var historyRight = _controller.HistoryManager.RightHistory;

                // 2. Empty Query -> Show History + Bookmarks
                if (tokens.Count == 0)
                {
                    foreach (var b in bookmarks)
                    {
                        string p = EnvironmentVariableExpander.ExpandEnvironmentVariables(b.Path);
                        if (!string.IsNullOrWhiteSpace(p) && uniqueSet.Add(p)) results.Add(p);
                    }
                    foreach (var p in historyLeft)
                    {
                        if (!string.IsNullOrWhiteSpace(p) && uniqueSet.Add(p)) results.Add(p);
                    }
                    foreach (var p in historyRight)
                    {
                        if (!string.IsNullOrWhiteSpace(p) && uniqueSet.Add(p)) results.Add(p);
                    }
                    return results;
                }

                // 3. File System Search (if it looks like a path)
                if (tokens.Count == 1)
                {
                    string expandedQuery = EnvironmentVariableExpander.ExpandEnvironmentVariables(tokens[0]);
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
                                filePattern = Path.GetFileName(expandedQuery) ?? "";
                            }
                            
                            if (string.IsNullOrEmpty(dir) && Path.IsPathRooted(expandedQuery))
                            {
                                dir = expandedQuery;
                                filePattern = "";
                            }

                            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                            {
                                var opts = new EnumerationOptions { IgnoreInaccessible = true };
                                int count = 0;
                                foreach (var d in Directory.EnumerateDirectories(dir, filePattern + "*", opts))
                                {
                                    if (uniqueSet.Add(d))
                                    {
                                        results.Add(d);
                                        if (++count >= 50) break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                // 4. Fuzzy Search on History/Bookmarks
                // We combine bookmarks and history into a single list to iterate
                var staticPaths = new List<string>();
                foreach (var b in bookmarks) staticPaths.Add(EnvironmentVariableExpander.ExpandEnvironmentVariables(b.Path));
                staticPaths.AddRange(historyLeft);
                staticPaths.AddRange(historyRight);

                foreach (var path in staticPaths)
                {
                    token.ThrowIfCancellationRequested();
                    if (string.IsNullOrWhiteSpace(path) || uniqueSet.Contains(path)) continue;

                    bool allMatch = true;
                    foreach (var t in tokens)
                    {
                        if (!_controller.SearchEngine.IsMatch(path, t))
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    if (allMatch)
                    {
                        if (uniqueSet.Add(path)) results.Add(path);
                    }
                    
                    if (results.Count >= 100) break;
                }
            }
            catch (Exception) { }

            return results;
        }
    }
}