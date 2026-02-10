using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Providers;
using TWF.Utilities;

namespace TWF.Controllers
{
    public class PaneController
    {
        private readonly ILogger _logger;
        private readonly Configuration _config;
        private readonly FileSystemProvider _fileSystemProvider;
        private readonly ArchiveManager _archiveManager;
        private readonly DirectoryCache _directoryCache;
        private readonly object _stateLock = new();
        
        // Callbacks for UI interaction
        private readonly Action _refreshUI;
        private readonly Action _updateStatusBar;
        private readonly Action<PaneState> _updatePaneStats;

        // State
        private readonly List<TabSession> _tabs = new();
        private int _activeTabIndex = 0;
        
        // Navigation Cache
        private readonly Dictionary<string, (int cursor, int scroll)> _navigationStateCache = new();
        private readonly Dictionary<PaneState, string> _lastLoadedPaths = new();
        private readonly Dictionary<PaneState, DateTime> _lastDirectoryWriteTimes = new();
        
        // Async Loading Control
        private readonly Dictionary<PaneState, CancellationTokenSource> _loadingCts = new();

        public IReadOnlyList<TabSession> Tabs => _tabs;
        public int ActiveTabIndex => _activeTabIndex;
        public TabSession ActiveTab => _tabs[_activeTabIndex];

        public PaneState ActivePane => ActiveTab.IsLeftPaneActive ? ActiveTab.LeftState : ActiveTab.RightState;
        public PaneState InactivePane => ActiveTab.IsLeftPaneActive ? ActiveTab.RightState : ActiveTab.LeftState;
        public PaneState LeftPane => ActiveTab.LeftState;
        public PaneState RightPane => ActiveTab.RightState;
        public bool IsLeftPaneActive => ActiveTab.IsLeftPaneActive;
        public HistoryManager History => ActiveTab.History;

        public PaneController(
            Configuration config,
            FileSystemProvider fileSystemProvider,
            ArchiveManager archiveManager,
            DirectoryCache directoryCache,
            Action refreshUI,
            Action updateStatusBar,
            Action<PaneState> updatePaneStats,
            ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fileSystemProvider = fileSystemProvider ?? throw new ArgumentNullException(nameof(fileSystemProvider));
            _archiveManager = archiveManager ?? throw new ArgumentNullException(nameof(archiveManager));
            _directoryCache = directoryCache ?? throw new ArgumentNullException(nameof(directoryCache));
            _refreshUI = refreshUI ?? throw new ArgumentNullException(nameof(refreshUI));
            _updateStatusBar = updateStatusBar ?? throw new ArgumentNullException(nameof(updateStatusBar));
            _updatePaneStats = updatePaneStats ?? throw new ArgumentNullException(nameof(updatePaneStats));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeFirstTab();
        }

        private void InitializeFirstTab()
        {
            var tab = new TabSession(_config);
            tab.LeftState.CurrentPath = _config.Navigation.StartDirectory;
            tab.RightState.CurrentPath = _config.Navigation.StartDirectory;
            tab.IsLeftPaneActive = true;
            
            _tabs.Add(tab);
            _activeTabIndex = 0;
        }

        public void NewTab(string? leftPath = null, string? rightPath = null)
        {
            var tab = new TabSession(_config);
            tab.LeftState.CurrentPath = leftPath ?? _config.Navigation.StartDirectory;
            tab.RightState.CurrentPath = rightPath ?? _config.Navigation.StartDirectory;
            tab.IsLeftPaneActive = true;
            
            _tabs.Add(tab);
            _activeTabIndex = _tabs.Count - 1;
            
            LoadDirectory(tab.LeftState);
            LoadDirectory(tab.RightState);
            _refreshUI();
        }

        public void CloseTab(int index)
        {
            if (_tabs.Count <= 1) return;
            if (index < 0 || index >= _tabs.Count) return;

            var tab = _tabs[index];
            lock (_stateLock)
            {
                if (_loadingCts.TryGetValue(tab.LeftState, out var lCts)) lCts.Cancel();
                if (_loadingCts.TryGetValue(tab.RightState, out var rCts)) rCts.Cancel();
            }

            _tabs.RemoveAt(index);
            if (_activeTabIndex >= _tabs.Count) _activeTabIndex = _tabs.Count - 1;
            
            _refreshUI();
        }

        public void SwitchTab(int index)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _activeTabIndex = index;
                EnsureTabLoaded(index);
                _refreshUI();
            }
        }

        public void NextTab()
        {
            if (_tabs.Count <= 1) return;
            _activeTabIndex = (_activeTabIndex + 1) % _tabs.Count;
            EnsureTabLoaded(_activeTabIndex);
            _refreshUI();
        }

        public void PreviousTab()
        {
            if (_tabs.Count <= 1) return;
            _activeTabIndex = (_activeTabIndex - 1 + _tabs.Count) % _tabs.Count;
            EnsureTabLoaded(_activeTabIndex);
            _refreshUI();
        }

        private void EnsureTabLoaded(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;
            var tab = _tabs[index];

            bool leftNeedsLoad;
            bool rightNeedsLoad;

            lock (_stateLock)
            {
                leftNeedsLoad = !_lastLoadedPaths.ContainsKey(tab.LeftState);
                rightNeedsLoad = !_lastLoadedPaths.ContainsKey(tab.RightState);
            }

            if (leftNeedsLoad)
            {
                _ = LoadDirectory(tab.LeftState, tab.LeftFocusTarget);
                tab.LeftFocusTarget = null;
            }
            if (rightNeedsLoad)
            {
                _ = LoadDirectory(tab.RightState, tab.RightFocusTarget);
                tab.RightFocusTarget = null;
            }
        }

        public void SwitchPane()
        {
            ActiveTab.IsLeftPaneActive = !ActiveTab.IsLeftPaneActive;
            _refreshUI();
        }

        public void SetActivePane(bool isLeft)
        {
            ActiveTab.IsLeftPaneActive = isLeft;
            _refreshUI();
        }

        public Task LoadDirectory(PaneState pane, string? focusTarget = null, IEnumerable<string>? preserveMarks = null, int? initialScrollOffset = null, bool skipHistory = false)
        {
            return LoadDirectoryAsync(pane, focusTarget, preserveMarks, initialScrollOffset, skipHistory);
        }

        public CancellationTokenSource SetupLoadingCts(PaneState pane)
        {
            lock (_stateLock)
            {
                if (_loadingCts.TryGetValue(pane, out var existingCts))
                {
                    existingCts.Cancel();
                    existingCts.Dispose();
                }
                
                var cts = new CancellationTokenSource();
                _loadingCts[pane] = cts;
                if (Application.MainLoop != null) Application.MainLoop.Invoke(() => _updateStatusBar());
                else _updateStatusBar();
                return cts;
            }
        }

        public void FinalizeLoadingCts(PaneState pane, CancellationTokenSource cts)
        {
            lock (_stateLock)
            {
                if (_loadingCts.TryGetValue(pane, out var currentCts) && currentCts == cts)
                {
                    _loadingCts.Remove(pane);
                    cts.Dispose();
                }
            }
            if (Application.MainLoop != null) Application.MainLoop.Invoke(() => _updateStatusBar());
            else _updateStatusBar();
        }

        private async Task LoadDirectoryAsync(PaneState pane, string? focusTarget = null, IEnumerable<string>? preserveMarks = null, int? initialScrollOffset = null, bool skipHistory = false)
        {
            CancellationTokenSource? cts = null;
            string? lastPath = null;
            try
            {
                lock (_stateLock)
                {
                    if (_lastLoadedPaths.TryGetValue(pane, out lastPath))
                    {
                        _navigationStateCache[lastPath] = (pane.CursorPosition, pane.ScrollOffset);
                    }
                }

                if (pane.IsInVirtualFolder && !string.IsNullOrEmpty(pane.VirtualFolderArchivePath))
                {
                    pane.Entries = new List<FileEntry>();
                    if (Application.MainLoop != null) Application.MainLoop.Invoke(_refreshUI);
                    else _refreshUI();

                    var archiveEntries = await _archiveManager.ListArchiveContentsAsync(
                        pane.VirtualFolderArchivePath, 
                        pane.VirtualFolderInternalPath ?? "");
                    
                    Action updateAction = () => 
                    {
                        pane.Entries = SortEngine.Sort(archiveEntries, pane.SortMode);
                        lock (_stateLock)
                        {
                            _lastLoadedPaths[pane] = pane.CurrentPath;
                        }
                        _updatePaneStats(pane);
                        RestoreCursor(pane, focusTarget, initialScrollOffset);
                        _refreshUI();
                    };

                    if (Application.MainLoop != null) Application.MainLoop.Invoke(updateAction);
                    else updateAction();
                    return;
                }

                if (!skipHistory)
                {
                    History.Add(pane == LeftPane, pane.CurrentPath);
                }

                bool useCache = string.IsNullOrEmpty(pane.FileMask) || pane.FileMask == "*";
                if (useCache && _directoryCache.TryGet(pane.CurrentPath, out var cachedEntries) && cachedEntries != null)
                {
                    lock (_stateLock)
                    {
                        if (_loadingCts.TryGetValue(pane, out var existingCts))
                        {
                            existingCts.Cancel();
                            existingCts.Dispose();
                            _loadingCts.Remove(pane);
                        }
                        
                        _lastLoadedPaths[pane] = pane.CurrentPath;
                    }
                    
                    pane.Entries = SortEngine.Sort(cachedEntries, pane.SortMode);
                    RestoreCursor(pane, focusTarget, initialScrollOffset);
                    _updatePaneStats(pane);

                    try {
                        if (Directory.Exists(pane.CurrentPath))
                        {
                            var writeTime = Directory.GetLastWriteTime(pane.CurrentPath);
                            lock (_stateLock) _lastDirectoryWriteTimes[pane] = writeTime;
                        }
                    } catch {}
                    
                    if (Application.MainLoop != null)
                    {
                        Application.MainLoop.Invoke(() => {
                            _refreshUI();
                            _updateStatusBar();
                        });
                    }
                    else
                    {
                        _refreshUI();
                        _updateStatusBar();
                    }
                    return;
                }

                cts = SetupLoadingCts(pane);
                var token = cts.Token;
                
                try {
                    if (Directory.Exists(pane.CurrentPath))
                    {
                        var writeTime = Directory.GetLastWriteTime(pane.CurrentPath);
                        lock (_stateLock) _lastDirectoryWriteTimes[pane] = writeTime;
                    }
                } catch {}

                var newEntries = new List<FileEntry>();
                var rawEntriesForCache = new List<FileEntry>();
                var batch = new List<FileEntry>();
                var lastUpdateTime = DateTime.UtcNow;
                bool isCancelled = false;

                await Task.Run(async () => 
                {
                    try
                    {
                        await foreach (var item in _fileSystemProvider.EnumerateDirectoryAsync(pane.CurrentPath, token))
                        {
                            if (token.IsCancellationRequested) { isCancelled = true; break; }

                            var entry = item.ToFileEntry();
                            batch.Add(entry);
                            if (useCache) rawEntriesForCache.Add(entry);

                            if (batch.Count >= 100 || (DateTime.UtcNow - lastUpdateTime).TotalMilliseconds > 100)
                            {
                                var batchCopy = new List<FileEntry>(batch);
                                batch.Clear();
                                lastUpdateTime = DateTime.UtcNow;
                                if (!useCache) batchCopy = _fileSystemProvider.ApplyFileMask(batchCopy, pane.FileMask);

                                if (batchCopy.Count > 0)
                                {
                                    Action updateAction = () => 
                                    {
                                        if (token.IsCancellationRequested) return;
                                        newEntries.AddRange(batchCopy);
                                        pane.Entries = SortEngine.Sort(new List<FileEntry>(newEntries), pane.SortMode);
                                        RestoreCursor(pane, focusTarget, initialScrollOffset);
                                        _updatePaneStats(pane);
                                        _refreshUI();
                                    };

                                    if (Application.MainLoop != null) Application.MainLoop.Invoke(updateAction);
                                    else updateAction();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                         if (ex is not OperationCanceledException)
                         {
                            Action errorAction = () => ErrorHelper.Handle(ex, "Error loading directory");
                            if (Application.MainLoop != null) Application.MainLoop.Invoke(errorAction);
                            else errorAction();
                         }
                    }
                }, token).ConfigureAwait(false);

                if (isCancelled || token.IsCancellationRequested) return;

                if (batch.Count > 0)
                {
                     var batchCopy = new List<FileEntry>(batch);
                     if (!useCache) batchCopy = _fileSystemProvider.ApplyFileMask(batchCopy, pane.FileMask);
                     newEntries.AddRange(batchCopy);
                }

                Action finalUpdateAction = () => 
                {
                    pane.Entries = SortEngine.Sort(new List<FileEntry>(newEntries), pane.SortMode);
                    if (useCache) _directoryCache.Add(pane.CurrentPath, rawEntriesForCache);
                    lock (_stateLock)
                    {
                        _lastLoadedPaths[pane] = pane.CurrentPath;
                    }
                    RestoreCursor(pane, focusTarget, initialScrollOffset);
                    _updatePaneStats(pane);
                    _refreshUI();
                };

                if (Application.MainLoop != null) Application.MainLoop.Invoke(finalUpdateAction);
                else finalUpdateAction();
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException)
                {
                    Action finalErrorAction = () => ErrorHelper.Handle(ex, "Failed to access directory");
                    if (Application.MainLoop != null) Application.MainLoop.Invoke(finalErrorAction);
                    else finalErrorAction();
                }
            }
            finally
            {
                if (cts != null) FinalizeLoadingCts(pane, cts);
            }
        }

        private void RestoreCursor(PaneState pane, string? focusTarget, int? initialScrollOffset = null)
        {
            if (!string.IsNullOrEmpty(focusTarget))
            {
                int index = pane.Entries.FindIndex(e => string.Equals(e.Name, focusTarget, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    pane.CursorPosition = index;
                    if (initialScrollOffset.HasValue) pane.ScrollOffset = initialScrollOffset.Value;
                    return;
                }
            }

            if (initialScrollOffset.HasValue)
            {
                pane.ScrollOffset = initialScrollOffset.Value;
                if (pane.CursorPosition < pane.ScrollOffset)
                    pane.CursorPosition = pane.ScrollOffset;
                return;
            }

            lock (_stateLock)
            {
                if (_navigationStateCache.TryGetValue(pane.CurrentPath, out var state))
                {
                    pane.CursorPosition = Math.Max(0, Math.Min(state.cursor, pane.Entries.Count - 1));
                    pane.ScrollOffset = Math.Max(0, state.scroll);
                    return;
                }
            }

            pane.CursorPosition = 0;
            pane.ScrollOffset = 0;
        }

        public SessionState GetSessionState()
        {
            var sessionState = new SessionState
            {
                LeftPath = GetSessionPaneState(LeftPane).path,
                RightPath = GetSessionPaneState(RightPane).path,
                LeftFocusTarget = GetSessionPaneState(LeftPane).target,
                RightFocusTarget = GetSessionPaneState(RightPane).target,
                LeftMask = LeftPane.FileMask,
                RightMask = RightPane.FileMask,
                LeftSort = LeftPane.SortMode,
                RightSort = RightPane.SortMode,
                LeftDisplayMode = LeftPane.DisplayMode,
                RightDisplayMode = RightPane.DisplayMode,
                LeftPaneActive = IsLeftPaneActive,
                LeftHistory = new List<string>(History.LeftHistory),
                RightHistory = new List<string>(History.RightHistory),
                ActiveTabIndex = _activeTabIndex
            };

            foreach (var tab in _tabs)
            {
                var tLeft = GetSessionPaneState(tab.LeftState);
                var tRight = GetSessionPaneState(tab.RightState);

                sessionState.Tabs.Add(new TabSessionState
                {
                    LeftPath = tLeft.path,
                    RightPath = tRight.path,
                    LeftFocusTarget = tLeft.target,
                    RightFocusTarget = tRight.target,
                    LeftMask = tab.LeftState.FileMask,
                    RightMask = tab.RightState.FileMask,
                    LeftSort = tab.LeftState.SortMode,
                    RightSort = tab.RightState.SortMode,
                    LeftDisplayMode = tab.LeftState.DisplayMode,
                    RightDisplayMode = tab.RightState.DisplayMode,
                    LeftPaneActive = tab.IsLeftPaneActive,
                    LeftHistory = new List<string>(tab.History.LeftHistory),
                    RightHistory = new List<string>(tab.History.RightHistory)
                });
            }
            
            return sessionState;
        }

        private (string path, string? target) GetSessionPaneState(PaneState pane)
        {
            if (pane.IsInVirtualFolder && !string.IsNullOrEmpty(pane.VirtualFolderParentPath))
            {
                string? archiveName = null;
                if (!string.IsNullOrEmpty(pane.VirtualFolderArchivePath))
                {
                    archiveName = Path.GetFileName(pane.VirtualFolderArchivePath);
                }
                return (pane.VirtualFolderParentPath, archiveName);
            }
            return (pane.CurrentPath, pane.GetCurrentEntry()?.Name);
        }

        public async Task RestoreSession(SessionState sessionState)
        {
            if (sessionState == null) return;

            _tabs.Clear();

            if (sessionState.Tabs != null && sessionState.Tabs.Count > 0)
            {
                foreach (var tabState in sessionState.Tabs)
                {
                    var tabSession = new TabSession(_config);
                    
                    tabSession.LeftState.CurrentPath = tabState.LeftPath ?? _config.Navigation.StartDirectory;
                    tabSession.RightState.CurrentPath = tabState.RightPath ?? _config.Navigation.StartDirectory;
                    tabSession.LeftState.FileMask = tabState.LeftMask ?? "*";
                    tabSession.RightState.FileMask = tabState.RightMask ?? "*";
                    tabSession.LeftState.SortMode = tabState.LeftSort;
                    tabSession.RightState.SortMode = tabState.RightSort;
                    tabSession.LeftState.DisplayMode = tabState.LeftDisplayMode;
                    tabSession.RightState.DisplayMode = tabState.RightDisplayMode;
                    tabSession.LeftFocusTarget = tabState.LeftFocusTarget;
                    tabSession.RightFocusTarget = tabState.RightFocusTarget;
                    tabSession.IsLeftPaneActive = tabState.LeftPaneActive;
                    
                    tabSession.History.SetHistory(true, tabState.LeftHistory);
                    tabSession.History.SetHistory(false, tabState.RightHistory);
                    
                    _tabs.Add(tabSession);
                }
                
                if (sessionState.ActiveTabIndex >= 0 && sessionState.ActiveTabIndex < _tabs.Count)
                {
                    _activeTabIndex = sessionState.ActiveTabIndex;
                }
                else
                {
                    _activeTabIndex = 0;
                }
            }
            else
            {
                var tabSession = new TabSession(_config);
                
                tabSession.LeftState.CurrentPath = sessionState.LeftPath ?? _config.Navigation.StartDirectory;
                tabSession.RightState.CurrentPath = sessionState.RightPath ?? _config.Navigation.StartDirectory;
                tabSession.LeftState.FileMask = sessionState.LeftMask ?? "*";
                tabSession.RightState.FileMask = sessionState.RightMask ?? "*";
                tabSession.LeftState.SortMode = sessionState.LeftSort;
                tabSession.RightState.SortMode = sessionState.RightSort;
                tabSession.LeftFocusTarget = sessionState.LeftFocusTarget;
                tabSession.RightFocusTarget = sessionState.RightFocusTarget;
                
                // Restore history
                tabSession.History.SetHistory(true, sessionState.LeftHistory);
                tabSession.History.SetHistory(false, sessionState.RightHistory);
                
                _tabs.Add(tabSession);
                _activeTabIndex = 0;
            }

            // Await both directory loads
            await Task.WhenAll(
                LoadDirectory(ActiveTab.LeftState, ActiveTab.LeftFocusTarget),
                LoadDirectory(ActiveTab.RightState, ActiveTab.RightFocusTarget)
            ).ConfigureAwait(false);
            
            ActiveTab.LeftFocusTarget = null;
            ActiveTab.RightFocusTarget = null;
        }

        public bool IsLoading(PaneState pane) 
        {
            lock (_stateLock) return _loadingCts.ContainsKey(pane);
        }

        public bool IsAnyPaneLoading 
        {
            get { lock (_stateLock) return _loadingCts.Count > 0; }
        }

        public void CheckForUpdates()
        {
            foreach (var tab in _tabs)
            {
                CheckPaneUpdate(tab.LeftState);
                CheckPaneUpdate(tab.RightState);
            }
        }

        private void CheckPaneUpdate(PaneState pane)
        {
            if (pane.IsInVirtualFolder) return;

            try
            {
                if (Directory.Exists(pane.CurrentPath))
                {
                    var currentWriteTime = Directory.GetLastWriteTime(pane.CurrentPath);
                    lock (_stateLock)
                    {
                        if (_lastDirectoryWriteTimes.TryGetValue(pane, out var lastTime))
                        {
                            if (currentWriteTime > lastTime)
                            {
                                _directoryCache.Invalidate(pane.CurrentPath);
                                LoadDirectory(pane, null, null, null, true);
                            }
                        }
                        else
                        {
                            _lastDirectoryWriteTimes[pane] = currentWriteTime;
                        }
                    }
                }
            }
            catch {}
        }
    }
}