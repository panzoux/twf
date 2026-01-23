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
    public class JumpToFileDialog : BaseJumpDialog
    {
        private readonly string _rootPath;
        private readonly List<FileEntry> _paneItems;

        public JumpToFileDialog(MainController controller, string rootPath, List<FileEntry> paneItems) 
            : base(controller, "Jump to File")
        {
            _rootPath = rootPath;
            _paneItems = paneItems ?? new List<FileEntry>();

            // Initial search
            TriggerSearch("");
        }

        protected override string GetFallbackPath(string input)
        {
            try
            {
                string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(input);
                if (!Path.IsPathRooted(expanded))
                {
                    expanded = Path.Combine(_rootPath, expanded);
                }

                if (File.Exists(expanded) || Directory.Exists(expanded))
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
            var uniqueSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tokens = ParseTokens(query);

            try
            {
                // 1. Startpoint: Search current pane items first
                if (tokens.Count > 0)
                {
                    foreach (var entry in _paneItems)
                    {
                        if (token.IsCancellationRequested) break;
                        if (entry.Name == "..") continue;

                        bool allMatch = true;
                        foreach (var t in tokens)
                        {
                            if (!_controller.SearchEngine.IsMatch(entry.Name, t))
                            {
                                allMatch = false;
                                break;
                            }
                        }

                        if (allMatch)
                        {
                            if (uniqueSet.Add(entry.FullPath)) results.Add(entry.FullPath);
                        }
                    }
                }

                // 2. Disk Search
                string searchPath = _rootPath;
                bool recursive = true;

                int maxDepth = _controller.Config.Navigation.JumpToFileSearchDepth;
                int maxResults = _controller.Config.Navigation.JumpToFileMaxResults;

                // If only one token and it looks like a path, adjust search root
                if (tokens.Count == 1)
                {
                    string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(tokens[0]);
                    bool isPath = expanded.Contains(Path.DirectorySeparatorChar) || expanded.Contains(Path.AltDirectorySeparatorChar) || (expanded.Length >= 2 && expanded[1] == ':');
                    
                    if (isPath)
                    {
                        string? dir = null;
                        if (Directory.Exists(expanded))
                        {
                            dir = expanded;
                        }
                        else
                        {
                            dir = Path.GetDirectoryName(expanded);
                        }

                        if (string.IsNullOrEmpty(dir) && Path.IsPathRooted(expanded)) dir = expanded;

                        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        {
                            searchPath = dir;
                            recursive = false;
                        }
                    }
                }

                if (results.Count >= maxResults) return results;

                var stack = new Stack<(string Path, int Depth)>();
                stack.Push((searchPath, 0));

                while (stack.Count > 0 && results.Count < maxResults)
                {
                    token.ThrowIfCancellationRequested();
                    var (currentPath, depth) = stack.Pop();

                    try
                    {
                        foreach (var entry in Directory.EnumerateFileSystemEntries(currentPath))
                        {
                            token.ThrowIfCancellationRequested();
                            string name = Path.GetFileName(entry);

                            // Check Ignore List
                            if (_ignoreFolders.Contains(name)) continue;

                            bool allMatch = true;
                            if (tokens.Count > 0)
                            {
                                foreach (var t in tokens)
                                {
                                    if (!_controller.SearchEngine.IsMatch(name, t))
                                    {
                                        allMatch = false;
                                        break;
                                    }
                                }
                            }

                            if (allMatch)
                            {
                                if (uniqueSet.Add(entry))
                                {
                                    results.Add(entry);
                                    if (results.Count >= maxResults) break;
                                }
                            }

                            if (recursive && depth < maxDepth && Directory.Exists(entry))
                            {
                                stack.Push((entry, depth + 1));
                            }
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (IOException) { }
                }
            }
            catch (Exception) { }

            return results;
        }
    }
}
