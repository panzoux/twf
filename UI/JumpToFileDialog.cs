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

            try
            {
                // 1. Startpoint: Search current pane items first
                if (!string.IsNullOrEmpty(query))
                {
                    foreach (var entry in _paneItems)
                    {
                        if (token.IsCancellationRequested) break;
                        if (entry.Name == "..") continue;

                        if (_controller.SearchEngine.IsMatch(entry.Name, query))
                        {
                            if (uniqueSet.Add(entry.FullPath)) results.Add(entry.FullPath);
                        }
                    }
                }

                // 2. Disk Search
                string searchPath = _rootPath;
                string pattern = "*";
                bool recursive = true;

                int maxDepth = _controller.Config.Navigation.JumpToFileSearchDepth;
                int maxResults = _controller.Config.Navigation.JumpToFileMaxResults;

                string expanded = EnvironmentVariableExpander.ExpandEnvironmentVariables(query);
                bool isPath = expanded.Contains(Path.DirectorySeparatorChar) || expanded.Contains(Path.AltDirectorySeparatorChar) || (expanded.Length >= 2 && expanded[1] == ':');
                
                if (isPath)
                {
                    string? dir = null;
                    if (Directory.Exists(expanded))
                    {
                        dir = expanded;
                        pattern = "*";
                    }
                    else
                    {
                        dir = Path.GetDirectoryName(expanded);
                        pattern = Path.GetFileName(expanded) + "*";
                    }

                    if (string.IsNullOrEmpty(dir) && Path.IsPathRooted(expanded)) dir = expanded;

                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        searchPath = dir;
                        recursive = false;
                    }
                    else if (!Path.IsPathRooted(expanded))
                    {
                        pattern = "*" + expanded + "*";
                    }
                    else return results;
                }
                else if (!string.IsNullOrEmpty(query))
                {
                    pattern = "*" + query + "*";
                }

                if (results.Count >= maxResults) return results;

                var opts = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = recursive, MaxRecursionDepth = recursive ? maxDepth : 0 };

                if (!isPath && !string.IsNullOrEmpty(query))
                {
                    foreach (var f in Directory.EnumerateFileSystemEntries(searchPath, "*", opts))
                    {
                        token.ThrowIfCancellationRequested();
                        if (uniqueSet.Add(f) && _controller.SearchEngine.IsMatch(Path.GetFileName(f), query))
                        {
                            results.Add(f);
                            if (results.Count >= maxResults) break;
                        }
                    }
                }
                else
                {
                    foreach (var f in Directory.EnumerateFileSystemEntries(searchPath, pattern, opts).Take(maxResults - results.Count))
                    {
                        token.ThrowIfCancellationRequested();
                        if (uniqueSet.Add(f)) results.Add(f);
                    }
                }
            }
            catch (Exception) { }

            return results;
        }
    }
}