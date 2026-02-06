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
        private readonly string _rootPath;
        private readonly List<FileEntry> _paneItems;

        public JumpToPathDialog(MainController controller, string rootPath, List<FileEntry> paneItems) 
            : base(controller, "Jump to Directory")
        {
            _rootPath = rootPath;
            _paneItems = paneItems ?? new List<FileEntry>();
            // Initial search to populate history/bookmarks
            TriggerSearch("");
        }

        protected override string GetFallbackPath(string input)
        {
            try
            {
                string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(input);
                
                // If relative, resolve against root path
                if (!Path.IsPathRooted(expanded))
                {
                    expanded = Path.GetFullPath(Path.Combine(_rootPath, expanded));
                }
                else
                {
                    expanded = Path.GetFullPath(expanded);
                }

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
            var uniqueSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var preparedSearch = _controller.SearchEngine.Prepare(tokens, _controller.Config.Migemo.Enabled);
                
                // 1. Current Pane Items (Highest Priority)
                foreach (var item in _paneItems)
                {
                    if (item.IsDirectory && item.Name != ".." && uniqueSet.Add(item.FullPath))
                        results.Add(item.FullPath);
                }

                // 2. Data Sources (Bookmarks + History)
                AddStaticPaths(uniqueSet, results);

                // 3. Local Disk Search (Current Directory)
                if (tokens.Count > 0)
                    AddRecursiveDiskPaths(_rootPath, preparedSearch, uniqueSet, results, token);

                if (tokens.Count == 1)
                    AddRootedPathSuggestions(tokens[0], preparedSearch, uniqueSet, results, token);

                return FilterMatches(results, tokens, preparedSearch);
            }
            catch (Exception) { }
            return results;
        }

        private List<string> FilterMatches(List<string> results, List<string> tokens, SearchEngine.PreparedQuery preparedSearch)
        {
            var finalResults = new List<string>();
            foreach (var path in results)
            {
                // In Jump to Directory, we match against the FULL PATH to allow "dir1 dir2" matching
                if (tokens.Count == 0 || preparedSearch.IsMatch(path))
                    finalResults.Add(path);
            }
            return finalResults;
        }

        private void AddRootedPathSuggestions(string queryToken, SearchEngine.PreparedQuery preparedSearch, HashSet<string> uniqueSet, List<string> results, CancellationToken token)
        {
            string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(queryToken);
            if (Path.IsPathRooted(expanded))
            {
                string? dir = Directory.Exists(expanded) ? expanded : Path.GetDirectoryName(expanded);
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    AddRecursiveDiskPaths(dir, preparedSearch, uniqueSet, results, token);
                }
            }
        }

        private void AddStaticPaths(HashSet<string> uniqueSet, List<string> results)
        {
            var bookmarks = _controller.Config.RegisteredFolders;
            var historyLeft = _controller.HistoryManager.LeftHistory;
            var historyRight = _controller.HistoryManager.RightHistory;

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
        }

        private void AddRecursiveDiskPaths(string queryRoot, SearchEngine.PreparedQuery preparedSearch, HashSet<string> uniqueSet, List<string> results, CancellationToken token)
        {
            var config = _controller.Config.Navigation;
            int maxDepth = config.JumpToPathSearchDepth;
            int maxResults = config.JumpToPathMaxResults;

            var stack = new Stack<(string Path, int Depth)>();
            stack.Push((queryRoot, 0));

            while (stack.Count > 0 && results.Count < maxResults)
            {
                token.ThrowIfCancellationRequested();
                var (currentPath, depth) = stack.Pop();
                EnumerateAndAddDirectories(currentPath, depth, maxDepth, maxResults, preparedSearch, uniqueSet, results, stack, token);
            }
        }

        private void EnumerateAndAddDirectories(string path, int depth, int maxDepth, int maxResults, SearchEngine.PreparedQuery preparedSearch, HashSet<string> uniqueSet, List<string> results, Stack<(string Path, int Depth)> stack, CancellationToken token)
        {
            try
            {
                var opts = new EnumerationOptions { IgnoreInaccessible = true };
                foreach (var dir in Directory.EnumerateDirectories(path, "*", opts))
                {
                    token.ThrowIfCancellationRequested();
                    string name = Path.GetFileName(dir);
                    if (_ignoreFolders.Contains(name)) continue;

                    // Match against FULL PATH to support multi-token matching across directories
                    if (preparedSearch.IsMatch(dir) && uniqueSet.Add(dir))
                    {
                        results.Add(dir);
                        if (results.Count >= maxResults) return;
                    }

                    if (depth < maxDepth) stack.Push((dir, depth + 1));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
    }
}