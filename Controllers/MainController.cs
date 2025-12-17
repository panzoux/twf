using Terminal.Gui;
using TWF.Models;
using TWF.Services;
using TWF.Providers;
using TWF.UI;
using Microsoft.Extensions.Logging;
using System.Text;

namespace TWF.Controllers
{
    /// <summary>
    /// Main controller that orchestrates the entire application, manages UI state, and coordinates between components
    /// </summary>
    public class MainController
    {
        // UI Components
        private Window? _mainWindow;
        private PaneView? _leftPane;
        private PaneView? _rightPane;
        private Label? _pathsLabel;
        private Label? _topSeparator;
        private Label? _filenameLabel;
        private Label? _statusBar;
        private Label? _messageArea;
        
        // State
        private PaneState _leftState;
        private PaneState _rightState;
        private UiMode _currentMode;
        private bool _leftPaneActive;
        
        // Search state
        private string _searchPattern = string.Empty;
        private int _searchStartIndex = 0;
        
        // Dependencies
        private readonly KeyBindingManager _keyBindings;
        private readonly FileOperations _fileOps;
        private readonly MarkingEngine _markingEngine;
        private readonly SortEngine _sortEngine;
        private readonly SearchEngine _searchEngine;
        private readonly ArchiveManager _archiveManager;
        private readonly ViewerManager _viewerManager;
        private readonly ConfigurationProvider _configProvider;
        private readonly FileSystemProvider _fileSystemProvider;
        private readonly ListProvider _listProvider;
        private readonly CustomFunctionManager _customFunctionManager;
        private readonly MenuManager _menuManager;
        private readonly ILogger<MainController> _logger;
        
        /// <summary>
        /// Initializes a new instance of MainController with all required dependencies
        /// </summary>
        public MainController(
            KeyBindingManager keyBindings,
            FileOperations fileOps,
            MarkingEngine markingEngine,
            SortEngine sortEngine,
            SearchEngine searchEngine,
            ArchiveManager archiveManager,
            ViewerManager viewerManager,
            ConfigurationProvider configProvider,
            FileSystemProvider fileSystemProvider,
            ListProvider listProvider,
            CustomFunctionManager customFunctionManager,
            MenuManager menuManager,
            ILogger<MainController> logger)
        {
            _keyBindings = keyBindings ?? throw new ArgumentNullException(nameof(keyBindings));
            _fileOps = fileOps ?? throw new ArgumentNullException(nameof(fileOps));
            _markingEngine = markingEngine ?? throw new ArgumentNullException(nameof(markingEngine));
            _sortEngine = sortEngine ?? throw new ArgumentNullException(nameof(sortEngine));
            _searchEngine = searchEngine ?? throw new ArgumentNullException(nameof(searchEngine));
            _archiveManager = archiveManager ?? throw new ArgumentNullException(nameof(archiveManager));
            _viewerManager = viewerManager ?? throw new ArgumentNullException(nameof(viewerManager));
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _fileSystemProvider = fileSystemProvider ?? throw new ArgumentNullException(nameof(fileSystemProvider));
            _listProvider = listProvider ?? throw new ArgumentNullException(nameof(listProvider));
            _customFunctionManager = customFunctionManager ?? throw new ArgumentNullException(nameof(customFunctionManager));
            _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _leftState = new PaneState();
            _rightState = new PaneState();
            _currentMode = UiMode.Normal;
            _leftPaneActive = true;
        }
        
        /// <summary>
        /// Initializes the Terminal.Gui application and creates all UI components
        /// </summary>
        public void Initialize()
        {
            try
            {
                _logger.LogInformation("Initializing MainController");
                
                // Initialize Terminal.Gui
                Application.Init();
                
                // Load configuration
                var config = _configProvider.LoadConfiguration();
                _logger.LogInformation("Configuration loaded");
                
                // Configure CJK character width
                TWF.Utilities.CharacterWidthHelper.CJKCharacterWidth = config.Display.CJK_CharacterWidth;
                _logger.LogInformation($"CJK character width set to: {config.Display.CJK_CharacterWidth}");
                
                // Always load key bindings (they are always configurable)
                string keyBindingPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TWF",
                    config.KeyBindings.KeyBindingFile
                );
                
                // If the file doesn't exist in AppData, check current directory
                if (!File.Exists(keyBindingPath))
                {
                    keyBindingPath = config.KeyBindings.KeyBindingFile;
                }
                
                _keyBindings.LoadBindings(keyBindingPath);
                _logger.LogInformation($"Key bindings loaded from: {keyBindingPath}");
                
                // Load custom functions
                string customFunctionsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TWF",
                    "custom_functions.json"
                );
                
                // If the file doesn't exist in AppData, check current directory
                if (!File.Exists(customFunctionsPath))
                {
                    customFunctionsPath = "custom_functions.json";
                }
                
                _customFunctionManager.LoadFunctions(customFunctionsPath);
                _logger.LogInformation($"Custom functions loaded from: {customFunctionsPath}");
                
                // Set MenuManager for custom function manager
                _customFunctionManager.SetMenuManager(_menuManager);
                _logger.LogInformation("MenuManager configured for custom functions");
                
                // Set built-in action executor for menu items
                _customFunctionManager.SetBuiltInActionExecutor(ExecuteAction);
                _logger.LogInformation("Built-in action executor configured for menu items");
                
                // Load session state if enabled
                if (config.SaveSessionState)
                {
                    var sessionState = _configProvider.LoadSessionState();
                    if (sessionState != null)
                    {
                        _leftState.CurrentPath = sessionState.LeftPath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        _rightState.CurrentPath = sessionState.RightPath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        _leftState.FileMask = sessionState.LeftMask ?? "*";
                        _rightState.FileMask = sessionState.RightMask ?? "*";
                        _leftState.SortMode = sessionState.LeftSort;
                        _rightState.SortMode = sessionState.RightSort;
                        _logger.LogInformation("Session state restored");
                    }
                    else
                    {
                        InitializeDefaultPaths();
                    }
                }
                else
                {
                    InitializeDefaultPaths();
                }
                
                // Load initial directory contents
                LoadPaneDirectory(_leftState);
                LoadPaneDirectory(_rightState);
                
                // Create main window
                CreateMainWindow();
                
                _logger.LogInformation("MainController initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MainController");
                throw;
            }
        }
        
        /// <summary>
        /// Initializes pane paths to default values
        /// </summary>
        private void InitializeDefaultPaths()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _leftState.CurrentPath = userProfile;
            _rightState.CurrentPath = userProfile;
            _logger.LogInformation($"Initialized default paths to: {userProfile}");
        }
        
        /// <summary>
        /// Creates the main window with borderless layout
        /// </summary>
        private void CreateMainWindow()
        {
            // Hide cursor in the main window
            try
            {
                Console.CursorVisible = false;
            }
            catch
            {
                // Ignore errors hiding cursor
            }
            
            // Create main window without border
            _mainWindow = new Window("")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Border = new Border()
                {
                    BorderStyle = BorderStyle.None
                }
            };
            
            // Line 0: Paths display (top line)
            _pathsLabel = new Label()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 1,
                Text = "",
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.White, Color.Black)
                }
            };
            _mainWindow.Add(_pathsLabel);
            
            // Load configuration for pane colors
            var config = _configProvider.LoadConfiguration();
            
            // Parse colors from configuration
            var backgroundColor = ParseConfigColor(config.Display.BackgroundColor, Color.Black);
            var foregroundColor = ParseConfigColor(config.Display.ForegroundColor, Color.White);
            var borderColor = ParseConfigColor(config.Display.PaneBorderColor, Color.Black);
            
            // Line 1: Top separator
            _topSeparator = new Label()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = 1,
                Text = new string('─', 80), // Will be updated on resize
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(borderColor, Color.Black)
                }
            };
            _mainWindow.Add(_topSeparator);
            
            // Line 2+: Left pane (file list)
            _leftPane = new PaneView()
            {
                X = 0,
                Y = 2,
                Width = Dim.Percent(50),
                Height = Dim.Fill(4), // Leave room for filename, separator, drive stats, message
                CanFocus = true,
                State = _leftState,
                IsActive = _leftPaneActive,
                Configuration = config,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(foregroundColor, backgroundColor),
                    Focus = Application.Driver.MakeAttribute(foregroundColor, backgroundColor),
                    HotNormal = Application.Driver.MakeAttribute(borderColor, backgroundColor)
                }
            };
            _leftPane.KeyPress += HandleKeyPress;
            _mainWindow.Add(_leftPane);
            
            // Line 2+: Right pane (file list)
            _rightPane = new PaneView()
            {
                X = Pos.Percent(50),
                Y = 2,
                Width = Dim.Percent(50),
                Height = Dim.Fill(4), // Leave room for filename, separator, drive stats, message
                CanFocus = true,
                State = _rightState,
                IsActive = !_leftPaneActive,
                Configuration = config,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(foregroundColor, backgroundColor),
                    Focus = Application.Driver.MakeAttribute(foregroundColor, backgroundColor),
                    HotNormal = Application.Driver.MakeAttribute(borderColor, backgroundColor)
                }
            };
            _rightPane.KeyPress += HandleKeyPress;
            _mainWindow.Add(_rightPane);
            
            // Line N-2: Filename display
            _filenameLabel = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(2),
                Width = Dim.Fill(),
                Height = 1,
                Text = "",
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue)
                }
            };
            _mainWindow.Add(_filenameLabel);
            
            // Line N-2: Drive usage (status bar)
            _statusBar = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(3),
                Width = Dim.Fill(),
                Height = 1,
                Text = "",
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray)
                }
            };
            _mainWindow.Add(_statusBar);
            
            // Line N: Message area (last line)
            _messageArea = new Label()
            {
                X = 0,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill(),
                Height = 1,
                Text = "TWF Ready - Press F1 for help",
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.White, Color.Black)
                }
            };
            _mainWindow.Add(_messageArea);
            
            // Set initial focus
            _leftPane.SetFocus();
            
            // Add global key handler for the main window
            _mainWindow.KeyPress += HandleKeyPress;
            
            // Add resize handler to refresh display when window size changes
            Application.Resized += (e) =>
            {
                _logger.LogDebug("Terminal resized, updating display");
                // Only update display, don't reload data
                UpdateDisplay();
            };
            
            // Initial display update
            RefreshPanes();
            
            _logger.LogInformation("Main window created with borderless layout");
        }
        
        /// <summary>
        /// Updates the paths label to show both pane paths
        /// </summary>
        private void UpdateWindowTitle()
        {
            if (_pathsLabel == null) return;
            
            int windowWidth = Math.Max(40, Application.Driver.Cols);
            int halfWidth = windowWidth / 2;
            
            // Truncate paths if needed
            string leftPath = TWF.Utilities.CharacterWidthHelper.TruncateToWidth(_leftState.CurrentPath, halfWidth - 4);
            string rightPath = TWF.Utilities.CharacterWidthHelper.TruncateToWidth(_rightState.CurrentPath, halfWidth - 4);
            
            // Add indicator for active pane
            leftPath = _leftPaneActive ? $"►{leftPath}" : $" {leftPath}";
            rightPath = !_leftPaneActive ? $"►{rightPath}" : $" {rightPath}";
            
            // Set paths label with both paths separated by │
             _pathsLabel.Text = $"{TWF.Utilities.CharacterWidthHelper.PadToWidth(leftPath,halfWidth - 1)}│{rightPath}";
            
            // Update top separator width
            if (_topSeparator != null)
            {
                _topSeparator.Text = new string('─', windowWidth);
            }
        }
        
        /// <summary>
        /// Updates the status bar with drive usage information
        /// </summary>
        private void UpdateStatusBar()
        {
            if (_statusBar == null) return;
            
            try
            {
                // Get drive info for left pane
                string leftDrive = Path.GetPathRoot(_leftState.CurrentPath) ?? "";
                var leftDriveInfo = new System.IO.DriveInfo(leftDrive);
                string leftStats = FormatDriveStats(leftDriveInfo);
                
                // Get drive info for right pane
                string rightDrive = Path.GetPathRoot(_rightState.CurrentPath) ?? "";
                var rightDriveInfo = new System.IO.DriveInfo(rightDrive);
                string rightStats = FormatDriveStats(rightDriveInfo);
                
                // Format: "LeftStats  │  RightStats"
                int halfWidth = Math.Max(20, (Application.Driver.Cols - 6) / 2);
                _statusBar.Text = $" {leftStats.PadRight(halfWidth)} │ {rightStats}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update drive stats");
                _statusBar.Text = " Drive info unavailable";
            }
        }
        
        /// <summary>
        /// Formats drive statistics
        /// </summary>
        private string FormatDriveStats(System.IO.DriveInfo drive)
        {
            if (!drive.IsReady)
                return "Drive not ready";
            
            long usedBytes = drive.TotalSize - drive.AvailableFreeSpace;
            double usedGB = usedBytes / (1024.0 * 1024.0 * 1024.0);
            double freeGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            double freePercent = (drive.AvailableFreeSpace * 100.0) / drive.TotalSize;
            
            return $"{usedGB:F1} GB used  {freeGB:F1} GB free ({freePercent:F1}%)";
        }
        
        /// <summary>
        /// Updates the filename label with current filename
        /// </summary>
        private void UpdateBottomBorder()
        {
            if (_filenameLabel == null) return;
            
            var activePane = GetActivePane();
            var currentEntry = activePane.GetCurrentEntry();
            
            if (currentEntry != null)
            {
                // Get the current filename
                string filename = currentEntry.Name;
                
                // Truncate filename if needed (accounting for CJK character widths)
                int maxWidth = Math.Max(10, Application.Driver.Cols - 2);
                filename = TWF.Utilities.CharacterWidthHelper.TruncateToWidth(filename, maxWidth);
                
                // Display filename
                _filenameLabel.Text = filename;
            }
            else
            {
                _filenameLabel.Text = "";
            }
        }
        
        /// <summary>
        /// Truncates a path to fit within specified width (accounting for CJK character widths)
        /// </summary>
        private string TruncatePath(string path, int maxWidth)
        {
            int currentWidth = TWF.Utilities.CharacterWidthHelper.GetStringWidth(path);
            if (currentWidth <= maxWidth)
                return path;
            
            // Try to show drive and end of path
            if (path.Length > 3 && path[1] == ':')
            {
                string drive = path.Substring(0, 3); // "C:\"
                int driveWidth = TWF.Utilities.CharacterWidthHelper.GetStringWidth(drive);
                int ellipsisWidth = 3; // "..."
                int remaining = maxWidth - driveWidth - ellipsisWidth;
                
                if (remaining > 0)
                {
                    // Find the substring from the end that fits in remaining width
                    string endPart = "";
                    int endWidth = 0;
                    for (int i = path.Length - 1; i >= drive.Length && endWidth < remaining; i--)
                    {
                        int charWidth = TWF.Utilities.CharacterWidthHelper.GetCharWidth(path[i]);
                        if (endWidth + charWidth <= remaining)
                        {
                            endPart = path[i] + endPart;
                            endWidth += charWidth;
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    if (endPart.Length > 0)
                    {
                        return drive + "..." + endPart;
                    }
                }
            }
            
            // Fallback to simple truncation
            return TWF.Utilities.CharacterWidthHelper.TruncateToWidth(path, maxWidth);
        }
        
        /// <summary>
        /// Parses a color string from configuration to Terminal.Gui Color enum
        /// </summary>
        private Color ParseConfigColor(string colorName, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(colorName))
                return defaultColor;
            
            return colorName.ToLower() switch
            {
                "black" => Color.Black,
                "blue" => Color.Blue,
                "green" => Color.Green,
                "cyan" => Color.Cyan,
                "red" => Color.Red,
                "magenta" => Color.Magenta,
                "brown" => Color.Brown,
                "gray" => Color.Gray,
                "darkgray" => Color.DarkGray,
                "brightblue" => Color.BrightBlue,
                "brightgreen" => Color.BrightGreen,
                "brightcyan" => Color.BrightCyan,
                "brightred" => Color.BrightRed,
                "brightmagenta" => Color.BrightMagenta,
                "yellow" => Color.Brown, // Terminal.Gui uses Brown for yellow
                "white" => Color.White,
                _ => defaultColor
            };
        }
        
        /// <summary>
        /// Handles key press events for the main window
        /// Routes keys to appropriate handlers based on current UI mode
        /// </summary>
        private void HandleKeyPress(View.KeyEventEventArgs e)
        {
            try
            {
                _logger.LogDebug($"Key pressed: {e.KeyEvent.Key} (KeyValue: {e.KeyEvent.KeyValue})");
                
                // Handle search mode keys
                if (_currentMode == UiMode.Search)
                {
                    HandleSearchModeKey(e);
                    return;
                }
                
                // All keys are now configurable - check key bindings
                string keyString = ConvertKeyToString(e.KeyEvent.Key);
                _logger.LogDebug($"Converted key to string: {keyString}, IsEnabled: {_keyBindings.IsEnabled}");
                
                string? action = _keyBindings.GetActionForKey(keyString);
                _logger.LogDebug($"Action for key '{keyString}': {action ?? "null"}");
                
                if (action != null && ExecuteAction(action))
                {
                    e.Handled = true;
                    return;
                }
                
                // If no binding found, log it for debugging
                _logger.LogWarning($"No key binding found for: {keyString}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling key press");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles key press events when in search mode
        /// </summary>
        private void HandleSearchModeKey(View.KeyEventEventArgs e)
        {
            try
            {
                switch (e.KeyEvent.Key)
                {
                    case Key.Enter: // Exit search mode
                    case (Key)27: // Escape - exit search mode
                        ExitSearchMode();
                        e.Handled = true;
                        break;
                        
                    case Key.Space: // Mark and find next
                        HandleSearchMarkAndNext();
                        e.Handled = true;
                        break;
                        
                    case Key.CursorDown: // Find next match
                        HandleSearchNext();
                        e.Handled = true;
                        break;
                        
                    case Key.CursorUp: // Find previous match
                        HandleSearchPrevious();
                        e.Handled = true;
                        break;
                        
                    case Key.Backspace: // Remove last character from search pattern
                        HandleSearchBackspace();
                        e.Handled = true;
                        break;
                        
                    default:
                        // Handle regular character input
                        if (e.KeyEvent.KeyValue >= 32 && e.KeyEvent.KeyValue < 127)
                        {
                            char character = (char)e.KeyEvent.KeyValue;
                            HandleSearchInput(character);
                            e.Handled = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling search mode key");
                SetStatus($"Search error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Converts a Terminal.Gui Key to a string representation for key binding lookup
        /// </summary>
        private string ConvertKeyToString(Key key)
        {
            var parts = new List<string>();
            bool hasShift = false;
            
            // Check for modifiers
            if ((key & Key.ShiftMask) == Key.ShiftMask)
            {
                hasShift = true;
                parts.Add("Shift");
            }
            if ((key & Key.CtrlMask) == Key.CtrlMask)
                parts.Add("Ctrl");
            if ((key & Key.AltMask) == Key.AltMask)
                parts.Add("Alt");
            
            // Get the base key (remove modifiers)
            Key baseKey = key & ~(Key.ShiftMask | Key.CtrlMask | Key.AltMask);
            
            // Convert base key to string
            string keyName = baseKey switch
            {
                Key.Enter => "Enter",
                Key.Backspace => "Backspace",
                Key.Tab => "Tab",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "PageUp",
                Key.PageDown => "PageDown",
                Key.CursorUp => "Up",
                Key.CursorDown => "Down",
                Key.CursorLeft => "Left",
                Key.CursorRight => "Right",
                Key.Space => "Space",
                (Key)27 => "Escape",
                Key.F1 => "F1",
                Key.F2 => "F2",
                Key.F3 => "F3",
                Key.F4 => "F4",
                Key.F5 => "F5",
                Key.F6 => "F6",
                Key.F7 => "F7",
                Key.F8 => "F8",
                Key.F9 => "F9",
                Key.F10 => "F10",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                Key.D0 => "0",
                // Special characters
                (Key)'$' => "$",
                (Key)'@' => "@",
                (Key)':' => ":",
                (Key)'`' => "`",
                (Key)'~' => "~",
                (Key)'!' => "!",
                (Key)'#' => "#",
                (Key)'%' => "%",
                (Key)'^' => "^",
                (Key)'&' => "&",
                (Key)'*' => "*",
                (Key)'(' => "(",
                (Key)')' => ")",
                (Key)'-' => "-",
                (Key)'_' => "_",
                (Key)'=' => "=",
                (Key)'+' => "+",
                (Key)'[' => "[",
                (Key)']' => "]",
                (Key)'{' => "{",
                (Key)'}' => "}",
                (Key)'\\' => "\\",
                (Key)'|' => "|",
                (Key)';' => ";",
                (Key)'\'' => "'",
                (Key)'"' => "\"",
                (Key)',' => ",",
                (Key)'.' => ".",
                (Key)'<' => "<",
                (Key)'>' => ">",
                (Key)'/' => "/",
                (Key)'?' => "?",
                _ => baseKey >= Key.A && baseKey <= Key.Z ? ((char)baseKey).ToString() :
                     baseKey >= (Key)'a' && baseKey <= (Key)'z' ? ((char)baseKey).ToString().ToUpper() :
                     ((char)baseKey).ToString()
            };
            
            // Handle uppercase letters as Shift+Letter
            // Terminal.Gui sends uppercase letters WITHOUT ShiftMask when Shift is pressed
            if (baseKey >= Key.A && baseKey <= Key.Z && !hasShift && parts.Count == 0)
            {
                // This is an uppercase letter without explicit Shift modifier
                // Add Shift to the parts
                parts.Insert(0, "Shift");
            }
            
            parts.Add(keyName);
            
            return string.Join("+", parts);
        }
        
        /// <summary>
        /// Executes an action by name
        /// </summary>
        private bool ExecuteAction(string actionName)
        {
            try
            {
                switch (actionName)
                {
                    case "ShowHelp": ShowHelp(); return true;
                    case "EnterSearchMode": EnterSearchMode(); return true;
                    case "SwitchPane": SwitchPane(); return true;
                    case "HandleEnterKey": HandleEnterKey(); return true;
                    case "HandleShiftEnter": HandleShiftEnter(); return true;
                    case "HandleCtrlEnter": HandleCtrlEnter(); return true;
                    case "NavigateToParent": NavigateToParent(); return true;
                    case "InvertMarks": InvertMarks(); return true;
                    case "NavigateToRoot": NavigateToRoot(); return true;
                    case "MoveCursorUp": MoveCursorUp(); return true;
                    case "MoveCursorDown": MoveCursorDown(); return true;
                    case "SwitchToLeftPane": if (!_leftPaneActive) SwitchPane(); return true;
                    case "SwitchToRightPane": if (_leftPaneActive) SwitchPane(); return true;
                    case "PageUp":
                        var paneUp = GetActivePane();
                        paneUp.CursorPosition = Math.Max(0, paneUp.CursorPosition - 10);
                        RefreshPanes();
                        return true;
                    case "PageDown":
                        var paneDown = GetActivePane();
                        paneDown.CursorPosition = Math.Min(paneDown.Entries.Count - 1, paneDown.CursorPosition + 10);
                        RefreshPanes();
                        return true;
                    case "MoveCursorToFirst": MoveCursorToFirst(); return true;
                    case "MoveCursorToLast": MoveCursorToLast(); return true;
                    case "RefreshPane": LoadPaneDirectory(GetActivePane()); RefreshPanes(); SetStatus("Refreshed"); return true;
                    case "ToggleMarkAndMoveDown": ToggleMarkAndMoveDown(); return true;
                    case "ToggleMarkAndMoveUp": ToggleMarkAndMoveUp(); return true;
                    case "MarkRange": MarkRange(); return true;
                    case "DisplayMode1": HandleNumberKey(1); return true;
                    case "DisplayMode2": HandleNumberKey(2); return true;
                    case "DisplayMode3": HandleNumberKey(3); return true;
                    case "DisplayMode4": HandleNumberKey(4); return true;
                    case "DisplayMode5": HandleNumberKey(5); return true;
                    case "DisplayMode6": HandleNumberKey(6); return true;
                    case "DisplayMode7": HandleNumberKey(7); return true;
                    case "DisplayMode8": HandleNumberKey(8); return true;
                    case "DisplayModeDetailed": SetDisplayModeDetailed(); return true;
                    case "ShowWildcardMarkingDialog": ShowWildcardMarkingDialog(); return true;
                    case "HandleContextMenu": HandleContextMenu(); return true;
                    case "HandleCopyOperation": HandleCopyOperation(); return true;
                    case "HandleMoveOperation": HandleMoveOperation(); return true;
                    case "MoveToRegisteredFolder": MoveToRegisteredFolder(); return true;
                    case "HandleDeleteOperation": HandleDeleteOperation(); return true;
                    case "HandleCreateDirectory": HandleCreateDirectory(); return true;
                    case "HandleEditNewFile": HandleEditNewFile(); return true;
                    case "ShowDriveChangeDialog": ShowDriveChangeDialog(); return true;
                    case "JumpToPath": JumpToPath(); return true;
                    case "CycleSortMode": CycleSortMode(); return true;
                    case "ShowFileMaskDialog": ShowFileMaskDialog(); return true;
                    case "HandleCompressionOperation": HandleCompressionOperation(); return true;
                    case "ShowRegisteredFolderDialog": ShowRegisteredFolderDialog(); return true;
                    case "RegisterCurrentDirectory": RegisterCurrentDirectory(); return true;
                    case "ShowCustomFunctionsDialog": ShowCustomFunctionsDialog(); return true;
                    case "HandleSimpleRename": HandleSimpleRename(); return true;
                    case "HandlePatternRename": HandlePatternRename(); return true;
                    case "HandleFileComparison": HandleFileComparison(); return true;
                    case "HandleFileSplitOrJoin": HandleFileSplitOrJoin(); return true;
                    case "HandleLaunchConfigurationProgram": HandleLaunchConfigurationProgram(); return true;
                    case "ShowFileInfoForCursor": ShowFileInfoForCursor(); return true;
                    case "HandleArchiveExtraction": HandleArchiveExtraction(); return true;
                    case "ViewFileAsText": ViewFileAsText(); return true;
                    case "ViewFileAsHex": ViewFileAsHex(); return true;
                    case "MarkAll":
                        var allPane = GetActivePane();
                        for (int i = 0; i < allPane.Entries.Count; i++)
                            allPane.MarkedIndices.Add(i);
                        RefreshPanes();
                        SetStatus($"Marked all {allPane.Entries.Count} items");
                        return true;
                    case "ClearMarks":
                        var clearPane = GetActivePane();
                        clearPane.MarkedIndices.Clear();
                        RefreshPanes();
                        SetStatus("Marks cleared");
                        return true;
                    case "SyncPanes":
                        var activePane = GetActivePane();
                        var inactivePane = GetInactivePane();
                        inactivePane.CurrentPath = activePane.CurrentPath;
                        LoadPaneDirectory(inactivePane);
                        RefreshPanes();
                        SetStatus($"Synced panes to: {activePane.CurrentPath}");
                        return true;
                    case "SwapPanes":
                        SwapPanes();
                        return true;
                    case "ExitApplication": Application.RequestStop(); return true;
                    default:
                        // Check if action matches a custom function name
                        var customFunction = _customFunctionManager.GetFunctions()
                            .FirstOrDefault(f => f.Name.Equals(actionName, StringComparison.OrdinalIgnoreCase));
                        
                        if (customFunction != null)
                        {
                            _logger.LogDebug($"Executing custom function: {actionName}");
                            var cfActivePane = GetActivePane();
                            var cfInactivePane = GetInactivePane();
                            
                            bool success = _customFunctionManager.ExecuteFunction(
                                customFunction,
                                cfActivePane,
                                cfInactivePane,
                                _leftState,
                                _rightState
                            );
                            
                            if (success)
                            {
                                SetStatus($"Executed: {customFunction.Name}");
                                // Refresh panes in case files changed
                                LoadPaneDirectory(cfActivePane);
                                LoadPaneDirectory(cfInactivePane);
                                RefreshPanes();
                            }
                            else
                            {
                                SetStatus($"Failed: {customFunction.Name}");
                            }
                            
                            return true;
                        }
                        
                        _logger.LogWarning($"Unknown action: {actionName}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing action: {actionName}");
                SetStatus($"Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Loads directory contents for a pane
        /// </summary>
        private void LoadPaneDirectory(PaneState pane)
        {
            try
            {
                _logger.LogDebug($"Loading directory: {pane.CurrentPath}");
                
                // Get directory entries from FileSystemProvider
                var entries = _fileSystemProvider.GetDirectoryEntries(pane.CurrentPath);
                
                // Apply file mask filter
                if (!string.IsNullOrEmpty(pane.FileMask) && pane.FileMask != "*")
                {
                    entries = _fileSystemProvider.ApplyFileMask(entries, pane.FileMask);
                }
                
                // Apply sorting
                entries = SortEngine.Sort(entries, pane.SortMode);
                
                pane.Entries = entries;
                pane.CursorPosition = 0;
                pane.ScrollOffset = 0;
                pane.MarkedIndices.Clear();
                
                _logger.LogDebug($"Loaded {entries.Count} entries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load directory: {pane.CurrentPath}");
                SetStatus($"Error loading directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Runs the application main loop
        /// </summary>
        public void Run()
        {
            try
            {
                if (_mainWindow == null)
                {
                    throw new InvalidOperationException("MainController must be initialized before running");
                }
                
                _logger.LogInformation("Starting application main loop");
                Application.Top.Add(_mainWindow);
                
                // Set up periodic file list refresh timer (if enabled)
                var config = _configProvider.LoadConfiguration();
                if (config.Display.FileListRefreshIntervalMs > 0)
                {
                    _logger.LogInformation($"File list auto-refresh enabled: {config.Display.FileListRefreshIntervalMs}ms");
                    Application.MainLoop.AddTimeout(
                        TimeSpan.FromMilliseconds(config.Display.FileListRefreshIntervalMs),
                        (mainLoop) =>
                        {
                            // Only refresh if we're in normal mode (not in dialogs/viewers)
                            if (_currentMode == UiMode.Normal)
                            {
                                CheckAndRefreshFileList();
                            }
                            return true; // Continue timer
                        });
                }
                else
                {
                    _logger.LogInformation("File list auto-refresh disabled");
                }
                
                Application.Run();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in application main loop");
                throw;
            }
            finally
            {
                Shutdown();
            }
        }
        
        /// <summary>
        /// Shuts down the application and saves state
        /// </summary>
        private void Shutdown()
        {
            try
            {
                _logger.LogInformation("Shutting down application");
                
                // Save session state
                var config = _configProvider.LoadConfiguration();
                if (config.SaveSessionState)
                {
                    var sessionState = new SessionState
                    {
                        LeftPath = _leftState.CurrentPath,
                        RightPath = _rightState.CurrentPath,
                        LeftMask = _leftState.FileMask,
                        RightMask = _rightState.FileMask,
                        LeftSort = _leftState.SortMode,
                        RightSort = _rightState.SortMode
                    };
                    _configProvider.SaveSessionState(sessionState);
                    _logger.LogInformation("Session state saved");
                }
                
                Application.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during shutdown");
            }
        }
        
        /// <summary>
        /// Refreshes both panes to reflect current state
        /// </summary>
        public void RefreshPanes()
        {
            RefreshPane(_leftPane, _leftState, _leftPaneActive);
            RefreshPane(_rightPane, _rightState, !_leftPaneActive);
            
            // Update window title with paths (top border)
            UpdateWindowTitle();
            
            // Update status bar with drive usage (only if not in search mode)
            if (_currentMode != UiMode.Search)
            {
                UpdateStatusBar();
            }
            
            // Update bottom border with current filename
            UpdateBottomBorder();
            
            // Move cursor off-screen to prevent it from appearing on the pane view
            // This handles cursor artifacts from dialogs and viewers
            Application.Driver.Move(0, Application.Driver.Rows);
        }
        
        /// <summary>
        /// Lightweight display update without reloading data (for resize events)
        /// </summary>
        private void UpdateDisplay()
        {
            // Just redraw the panes without reloading file data
            _leftPane?.SetNeedsDisplay();
            _rightPane?.SetNeedsDisplay();
            
            // Update window title and status
            UpdateWindowTitle();
            UpdateStatusBar();
            UpdateBottomBorder();
        }
        
        /// <summary>
        /// Checks if file list has changed and refreshes if needed (for timer-based refresh)
        /// </summary>
        private void CheckAndRefreshFileList()
        {
            try
            {
                // Check if left pane directory has changed
                bool leftChanged = HasDirectoryChanged(_leftState);
                bool rightChanged = HasDirectoryChanged(_rightState);
                
                if (leftChanged || rightChanged)
                {
                    if (leftChanged)
                    {
                        LoadPaneDirectory(_leftState);
                    }
                    if (rightChanged)
                    {
                        LoadPaneDirectory(_rightState);
                    }
                    
                    // Update display
                    Application.MainLoop.Invoke(() => RefreshPanes());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking file list changes");
            }
        }
        
        /// <summary>
        /// Checks if a directory has changed since last load
        /// </summary>
        private bool HasDirectoryChanged(PaneState pane)
        {
            try
            {
                if (string.IsNullOrEmpty(pane.CurrentPath) || !Directory.Exists(pane.CurrentPath))
                {
                    return false;
                }
                
                // Get current directory info
                var dirInfo = new DirectoryInfo(pane.CurrentPath);
                var currentFiles = dirInfo.GetFileSystemInfos();
                
                // Simple check: compare count
                // More sophisticated check could compare timestamps or file names
                return currentFiles.Length != pane.Entries.Count;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Swaps the paths of left and right panes
        /// </summary>
        public void SwapPanes()
        {
            try
            {
                // Store the current paths
                string leftPath = _leftState.CurrentPath;
                string rightPath = _rightState.CurrentPath;
                
                // Swap the paths
                _leftState.CurrentPath = rightPath;
                _rightState.CurrentPath = leftPath;
                
                // Reload both panes with their new paths
                LoadPaneDirectory(_leftState);
                LoadPaneDirectory(_rightState);
                
                // Refresh display
                RefreshPanes();
                
                _logger.LogInformation("Swapped panes: Left='{LeftPath}', Right='{RightPath}'", rightPath, leftPath);
                SetStatus($"Swapped panes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error swapping panes: {Message}", ex.Message);
                SetStatus($"Error swapping panes: {ex.Message}");
            }
        }

        
        /// <summary>
        /// Refreshes a single pane display
        /// </summary>
        private void RefreshPane(PaneView? pane, PaneState state, bool isActive)
        {
            if (pane == null) return;
            
            // Update pane state and active status
            pane.State = state;
            pane.IsActive = isActive;
            
            // Trigger redraw
            pane.SetNeedsDisplay();
        }
        
        /// <summary>
        /// Switches focus between left and right panes
        /// </summary>
        public void SwitchPane()
        {
            _leftPaneActive = !_leftPaneActive;
            
            if (_leftPaneActive && _leftPane != null)
            {
                _leftPane.SetFocus();
            }
            else if (!_leftPaneActive && _rightPane != null)
            {
                _rightPane.SetFocus();
            }
            
            RefreshPanes();
            _logger.LogDebug($"Switched to {(_leftPaneActive ? "left" : "right")} pane");
        }
        
        /// <summary>
        /// Sets the status bar message
        /// </summary>
        public void SetStatus(string message)
        {
            if (_statusBar != null)
            {
                _statusBar.Text = message;
                _logger.LogDebug($"Status: {message}");
            }
        }
        
        /// <summary>
        /// Sets the message area text
        /// </summary>
        public void SetMessage(string message)
        {
            if (_messageArea != null)
            {
                _messageArea.Text = message;
            }
        }
        
        /// <summary>
        /// Gets the currently active pane state
        /// </summary>
        public PaneState GetActivePane()
        {
            return _leftPaneActive ? _leftState : _rightState;
        }
        
        /// <summary>
        /// Gets the inactive pane state
        /// </summary>
        public PaneState GetInactivePane()
        {
            return _leftPaneActive ? _rightState : _leftState;
        }
        
        /// <summary>
        /// Gets the current UI mode
        /// </summary>
        public UiMode GetCurrentMode()
        {
            return _currentMode;
        }
        
        /// <summary>
        /// Sets the current UI mode
        /// </summary>
        public void SetMode(UiMode mode)
        {
            _currentMode = mode;
            _logger.LogDebug($"UI mode changed to: {mode}");
        }
        
        /// <summary>
        /// Navigates to a directory in the active pane
        /// </summary>
        public void NavigateToDirectory(string path)
        {
            var activePane = GetActivePane();
            
            // Clear virtual folder state when navigating to a real directory
            activePane.IsInVirtualFolder = false;
            activePane.VirtualFolderArchivePath = null;
            activePane.VirtualFolderParentPath = null;
            
            activePane.CurrentPath = path;
            LoadPaneDirectory(activePane);
            RefreshPanes();
            _logger.LogDebug($"Navigated to: {path}");
        }
        
        /// <summary>
        /// Opens an archive file as a virtual folder
        /// </summary>
        private void OpenArchiveAsVirtualFolder(string archivePath)
        {
            var activePane = GetActivePane();
            
            try
            {
                _logger.LogDebug($"Opening archive as virtual folder: {archivePath}");
                
                // Get archive contents
                var archiveEntries = _archiveManager.ListArchiveContents(archivePath);
                
                // Store the parent directory path before entering virtual folder
                activePane.VirtualFolderParentPath = activePane.CurrentPath;
                activePane.VirtualFolderArchivePath = archivePath;
                activePane.IsInVirtualFolder = true;
                
                // Set the current path to indicate we're in an archive
                activePane.CurrentPath = $"[{Path.GetFileName(archivePath)}]";
                
                // Set the entries to the archive contents
                activePane.Entries = archiveEntries;
                activePane.CursorPosition = 0;
                activePane.ScrollOffset = 0;
                activePane.MarkedIndices.Clear();
                
                // Refresh display
                RefreshPanes();
                
                SetStatus($"Viewing archive: {Path.GetFileName(archivePath)} ({archiveEntries.Count} entries)");
                _logger.LogDebug($"Opened archive with {archiveEntries.Count} entries");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to open archive: {archivePath}");
                SetStatus($"Error opening archive: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles O key press - extracts archive to current directory
        /// </summary>
        public void HandleArchiveExtraction()
        {
            var activePane = GetActivePane();
            var currentEntry = activePane.GetCurrentEntry();
            
            if (currentEntry == null)
            {
                return;
            }
            
            // Check if this is an archive file
            if (!_archiveManager.IsArchive(currentEntry.FullPath))
            {
                SetStatus("Not an archive file");
                return;
            }
            
            try
            {
                _logger.LogDebug($"Extracting archive: {currentEntry.FullPath}");
                
                // Show confirmation dialog
                var confirmed = ShowConfirmationDialog(
                    "Extract Archive",
                    $"Extract '{currentEntry.Name}' to current directory?");
                
                if (!confirmed)
                {
                    SetStatus("Extraction cancelled");
                    return;
                }
                
                // Execute extraction with progress dialog
                var cancellationTokenSource = new CancellationTokenSource();
                var progressDialog = new Dialog("Extracting Archive", 70, 10);
                
                var statusLabel = new Label("Extracting...")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(statusLabel);
                
                var fileLabel = new Label($"Archive: {currentEntry.Name}")
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(fileLabel);
                
                var cancelButton = new Button("Cancel (ESC)")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(2)
                };
                
                cancelButton.Clicked += () =>
                {
                    cancellationTokenSource.Cancel();
                    statusLabel.Text = "Cancelling...";
                };
                
                progressDialog.Add(cancelButton);
                
                // Handle Escape key for cancellation
                progressDialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == (Key)27) // Escape
                    {
                        cancellationTokenSource.Cancel();
                        statusLabel.Text = "Cancelling...";
                        e.Handled = true;
                    }
                };
                
                // Execute extraction asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await _archiveManager.ExtractAsync(
                            currentEntry.FullPath,
                            activePane.CurrentPath,
                            cancellationTokenSource.Token);
                        
                        Application.MainLoop.Invoke(() =>
                        {
                            Application.RequestStop();
                            
                            if (result.Success)
                            {
                                SetStatus($"Extracted {result.FilesProcessed} file(s) from {currentEntry.Name}");
                                
                                // Refresh the pane to show extracted files
                                LoadPaneDirectory(activePane);
                                RefreshPanes();
                            }
                            else
                            {
                                SetStatus($"Extraction failed: {result.Message}");
                                
                                if (result.Errors.Count > 0)
                                {
                                    var errorMsg = string.Join("\n", result.Errors.Take(5));
                                    if (result.Errors.Count > 5)
                                    {
                                        errorMsg += $"\n... and {result.Errors.Count - 5} more errors";
                                    }
                                    ShowMessageDialog("Extraction Errors", errorMsg);
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.MainLoop.Invoke(() =>
                        {
                            Application.RequestStop();
                            SetStatus($"Extraction failed: {ex.Message}");
                            _logger.LogError(ex, "Archive extraction failed");
                        });
                    }
                });
                
                // Show the progress dialog
                Application.Run(progressDialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting archive: {currentEntry.FullPath}");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Navigates to the parent directory in the active pane
        /// Exits virtual folder if currently browsing an archive
        /// </summary>
        public void NavigateToParent()
        {
            var activePane = GetActivePane();
            
            // Check if we're in a virtual folder (archive)
            if (activePane.IsInVirtualFolder && activePane.VirtualFolderParentPath != null)
            {
                // Exit the virtual folder and return to the parent directory
                _logger.LogDebug($"Exiting virtual folder, returning to: {activePane.VirtualFolderParentPath}");
                
                string parentPath = activePane.VirtualFolderParentPath;
                
                // Clear virtual folder state
                activePane.IsInVirtualFolder = false;
                activePane.VirtualFolderArchivePath = null;
                activePane.VirtualFolderParentPath = null;
                
                // Navigate back to the parent directory
                activePane.CurrentPath = parentPath;
                LoadPaneDirectory(activePane);
                RefreshPanes();
                
                SetStatus($"Exited archive");
            }
            else
            {
                // Normal directory navigation
                var parentPath = Directory.GetParent(activePane.CurrentPath)?.FullName;
                
                if (parentPath != null)
                {
                    // Remember the current folder name to position cursor on it
                    string currentFolderName = Path.GetFileName(activePane.CurrentPath);
                    
                    NavigateToDirectory(parentPath);
                    
                    // Position cursor on the folder we just came from
                    if (!string.IsNullOrEmpty(currentFolderName))
                    {
                        PositionCursorOnEntry(activePane, currentFolderName);
                    }
                }
                else
                {
                    SetStatus("Already at root directory");
                }
            }
        }
        
        /// <summary>
        /// Positions the cursor on a specific entry by name
        /// </summary>
        private void PositionCursorOnEntry(PaneState pane, string entryName)
        {
            try
            {
                // Find the index of the entry with the matching name
                int index = pane.Entries.FindIndex(e => 
                    string.Equals(e.Name, entryName, StringComparison.OrdinalIgnoreCase));
                
                if (index >= 0)
                {
                    pane.CursorPosition = index;
                    _logger.LogDebug($"Positioned cursor on entry: {entryName} at index {index}");
                }
                else
                {
                    _logger.LogDebug($"Entry not found: {entryName}, cursor remains at position {pane.CursorPosition}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error positioning cursor on entry: {entryName}");
            }
        }
        
        /// <summary>
        /// Navigates to the root directory of the current drive
        /// </summary>
        public void NavigateToRoot()
        {
            var activePane = GetActivePane();
            var rootPath = Path.GetPathRoot(activePane.CurrentPath);
            
            if (!string.IsNullOrEmpty(rootPath))
            {
                NavigateToDirectory(rootPath);
            }
        }
        
        /// <summary>
        /// Navigates to a registered folder
        /// </summary>
        public void NavigateToRegisteredFolder(RegisteredFolder folder)
        {
            if (folder == null)
            {
                _logger.LogWarning("Attempted to navigate to null registered folder");
                SetStatus("Error: Invalid registered folder");
                return;
            }
            
            if (string.IsNullOrEmpty(folder.Path))
            {
                _logger.LogWarning($"Registered folder '{folder.Name}' has no path");
                SetStatus($"Error: Registered folder '{folder.Name}' has no path");
                return;
            }
            
            if (!Directory.Exists(folder.Path))
            {
                _logger.LogWarning($"Registered folder path does not exist: {folder.Path}");
                SetStatus($"Error: Path does not exist: {folder.Path}");
                return;
            }
            
            _logger.LogDebug($"Navigating to registered folder: {folder.Name} ({folder.Path})");
            NavigateToDirectory(folder.Path);
            SetStatus($"Navigated to: {folder.Name}");
        }
        
        /// <summary>
        /// Shows the registered folder selection dialog
        /// Handles I key press
        /// </summary>
        public void ShowRegisteredFolderDialog()
        {
            try
            {
                // Get registered folders from configuration
                var registeredFolders = _listProvider.GetJumpList();
                
                if (registeredFolders.Count == 0)
                {
                    SetStatus("No registered folders. Press Shift+B to register current directory.");
                    return;
                }
                
                // Create selection dialog
                var dialog = new Dialog("Registered Folders", 70, 20);
                
                var label = new Label("Select a folder to navigate to:")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                // Create list view with registered folders
                var folderList = new ListView()
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1),
                    Height = Dim.Fill(3),
                    AllowsMarking = false
                };
                
                var displayItems = registeredFolders.Select(f => $"{f.Name} - {f.Path}").ToList();
                folderList.SetSource(displayItems);
                dialog.Add(folderList);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 15,
                    Y = Pos.AnchorEnd(1),
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() - 3,
                    Y = Pos.AnchorEnd(1)
                };
                
                var deleteButton = new Button("Delete")
                {
                    X = Pos.Center() + 10,
                    Y = Pos.AnchorEnd(1)
                };
                
                bool okPressed = false;
                bool deletePressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                deletePressed = false;
                deleteButton.Clicked += () =>
                {
                    deletePressed = true;
                    Application.RequestStop();
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                dialog.Add(deleteButton);
                
                // Set focus to the list
                folderList.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Process the selection
                if (okPressed && folderList.SelectedItem >= 0 && folderList.SelectedItem < registeredFolders.Count)
                {
                    var selectedFolder = registeredFolders[folderList.SelectedItem];
                    NavigateToRegisteredFolder(selectedFolder);
                }
                else if (deletePressed && folderList.SelectedItem >= 0 && folderList.SelectedItem < registeredFolders.Count)
                {
                    var selectedFolder = registeredFolders[folderList.SelectedItem];
                    DeleteRegisteredFolder(selectedFolder);
                }
                else
                {
                    SetStatus("Cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing registered folder dialog");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows custom functions dialog and executes selected function
        /// </summary>
        public void ShowCustomFunctionsDialog()
        {
            try
            {
                var functions = _customFunctionManager.GetFunctions();
                
                if (functions.Count == 0)
                {
                    SetStatus("No custom functions defined. Create custom_functions.json in %APPDATA%\\TWF\\");
                    return;
                }
                
                // Show custom function selection dialog
                var dialog = new TWF.UI.CustomFunctionDialog(functions);
                Application.Run(dialog);
                
                // Execute selected function
                if (dialog.SelectedFunction != null)
                {
                    var activePane = GetActivePane();
                    var inactivePane = GetInactivePane();
                    
                    bool success = _customFunctionManager.ExecuteFunction(
                        dialog.SelectedFunction,
                        activePane,
                        inactivePane,
                        _leftState,
                        _rightState
                    );
                    
                    if (success)
                    {
                        SetStatus($"Executed: {dialog.SelectedFunction.Name}");
                        // Refresh panes in case files changed
                        LoadPaneDirectory(activePane);
                        LoadPaneDirectory(inactivePane);
                        RefreshPanes();
                    }
                    else
                    {
                        SetStatus($"Failed to execute: {dialog.SelectedFunction.Name}");
                    }
                }
                else
                {
                    SetStatus("Cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing custom functions dialog");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Registers the current directory as a registered folder
        /// Handles Shift+B key press
        /// </summary>
        public void RegisterCurrentDirectory()
        {
            try
            {
                var activePane = GetActivePane();
                var currentPath = activePane.CurrentPath;
                
                // Don't allow registering virtual folders
                if (activePane.IsInVirtualFolder)
                {
                    SetStatus("Cannot register virtual folders (archives)");
                    return;
                }
                
                // Create input dialog for folder name
                var dialog = new Dialog("Register Folder", 60, 10);
                
                var label = new Label("Enter a name for this folder:")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                var pathLabel = new Label($"Path: {currentPath}")
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1),
                    ColorScheme = new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
                    }
                };
                dialog.Add(pathLabel);
                
                var nameField = new TextField(Path.GetFileName(currentPath) ?? "Folder")
                {
                    X = 1,
                    Y = 3,
                    Width = Dim.Fill(1)
                };
                dialog.Add(nameField);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = 5,
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = 5
                };
                
                bool okPressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                // Set focus to the text field
                nameField.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Process the input if OK was pressed
                if (okPressed)
                {
                    string folderName = nameField.Text.ToString() ?? string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(folderName))
                    {
                        // Load configuration
                        var config = _configProvider.LoadConfiguration();
                        
                        // Check if this path is already registered
                        if (config.RegisteredFolders.Any(f => f.Path.Equals(currentPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            SetStatus("This folder is already registered");
                            return;
                        }
                        
                        // Create new registered folder
                        var newFolder = new RegisteredFolder
                        {
                            Name = folderName,
                            Path = currentPath,
                            SortOrder = config.RegisteredFolders.Count
                        };
                        
                        // Add to configuration
                        config.RegisteredFolders.Add(newFolder);
                        
                        // Save configuration
                        _configProvider.SaveConfiguration(config);
                        
                        SetStatus($"Registered folder: {folderName}");
                        _logger.LogInformation($"Registered folder: {folderName} -> {currentPath}");
                    }
                    else
                    {
                        SetStatus("No name entered");
                    }
                }
                else
                {
                    SetStatus("Registration cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering current directory");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Moves marked files to a selected registered folder
        /// Handles Shift+M key press
        /// </summary>
        public void MoveToRegisteredFolder()
        {
            try
            {
                var activePane = GetActivePane();
                
                // Get files to move (marked files or current file)
                var filesToMove = activePane.GetMarkedEntries();
                if (filesToMove.Count == 0)
                {
                    var currentEntry = activePane.GetCurrentEntry();
                    if (currentEntry != null)
                    {
                        filesToMove.Add(currentEntry);
                    }
                }
                
                if (filesToMove.Count == 0)
                {
                    SetStatus("No files to move");
                    return;
                }
                
                // Get registered folders
                var registeredFolders = _listProvider.GetJumpList();
                
                if (registeredFolders.Count == 0)
                {
                    SetStatus("No registered folders. Press Shift+B to register a folder.");
                    return;
                }
                
                // Create selection dialog
                var dialog = new Dialog("Move to Registered Folder", 70, 20);
                
                var label = new Label($"Select destination for {filesToMove.Count} file(s):")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                // Create list view with registered folders
                var folderList = new ListView()
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1),
                    Height = Dim.Fill(3),
                    AllowsMarking = false
                };
                
                var displayItems = registeredFolders.Select(f => $"{f.Name} - {f.Path}").ToList();
                folderList.SetSource(displayItems);
                dialog.Add(folderList);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = Pos.AnchorEnd(1),
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = Pos.AnchorEnd(1)
                };
                
                bool okPressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                // Set focus to the list
                folderList.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Process the selection
                if (okPressed && folderList.SelectedItem >= 0 && folderList.SelectedItem < registeredFolders.Count)
                {
                    var selectedFolder = registeredFolders[folderList.SelectedItem];
                    
                    if (!Directory.Exists(selectedFolder.Path))
                    {
                        SetStatus($"Error: Destination path does not exist: {selectedFolder.Path}");
                        return;
                    }
                    
                    // Perform move operation
                    PerformMoveOperation(filesToMove, selectedFolder.Path);
                }
                else
                {
                    SetStatus("Move cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to registered folder");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Deletes a registered folder from configuration
        /// </summary>
        private void DeleteRegisteredFolder(RegisteredFolder folder)
        {
            try
            {
                // Load configuration
                var config = _configProvider.LoadConfiguration();
                
                // Find and remove the folder
                var folderToRemove = config.RegisteredFolders.FirstOrDefault(f => 
                    f.Name == folder.Name && f.Path == folder.Path);
                
                if (folderToRemove != null)
                {
                    config.RegisteredFolders.Remove(folderToRemove);
                    
                    // Save configuration
                    _configProvider.SaveConfiguration(config);
                    
                    SetStatus($"Deleted registered folder: {folder.Name}");
                    _logger.LogInformation($"Deleted registered folder: {folder.Name}");
                }
                else
                {
                    SetStatus("Folder not found in configuration");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting registered folder");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Performs the actual move operation for files
        /// </summary>
        private async void PerformMoveOperation(List<FileEntry> files, string destination)
        {
            try
            {
                SetStatus($"Moving {files.Count} file(s)...");
                
                var result = await _fileOps.MoveAsync(files, destination, CancellationToken.None);
                
                if (result.Success)
                {
                    SetStatus($"Moved {result.FilesProcessed} file(s) successfully");
                    
                    // Refresh both panes
                    LoadPaneDirectory(_leftState);
                    LoadPaneDirectory(_rightState);
                    RefreshPanes();
                }
                else
                {
                    SetStatus($"Move failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing move operation");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Moves the cursor up in the active pane
        /// </summary>
        public void MoveCursorUp()
        {
            var activePane = GetActivePane();
            if (activePane.CursorPosition > 0)
            {
                activePane.CursorPosition--;
                //RefreshPanes();
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// Moves the cursor down in the active pane
        /// </summary>
        public void MoveCursorDown()
        {
            var activePane = GetActivePane();
            if (activePane.CursorPosition < activePane.Entries.Count - 1)
            {
                activePane.CursorPosition++;
                //RefreshPanes();
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// Moves the cursor to the first entry in the active pane
        /// </summary>
        public void MoveCursorToFirst()
        {
            var activePane = GetActivePane();
            if (activePane.Entries.Count > 0)
            {
                activePane.CursorPosition = 0;
                //RefreshPanes();
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// Moves the cursor to the last entry in the active pane
        /// </summary>
        public void MoveCursorToLast()
        {
            var activePane = GetActivePane();
            if (activePane.Entries.Count > 0)
            {
                activePane.CursorPosition = activePane.Entries.Count - 1;
                //RefreshPanes();
                UpdateDisplay();
            }
        }
        
        /// <summary>
        /// Handles Enter key press - navigates into directories, opens archives, or executes files
        /// </summary>
        public void HandleEnterKey()
        {
            var activePane = GetActivePane();
            var currentEntry = activePane.GetCurrentEntry();
            
            if (currentEntry == null)
            {
                return;
            }
            
            // Check if this is an archive file
            if (!currentEntry.IsDirectory && _archiveManager.IsArchive(currentEntry.FullPath))
            {
                // Open archive as virtual folder
                OpenArchiveAsVirtualFolder(currentEntry.FullPath);
            }
            else if (currentEntry.IsDirectory)
            {
                // Navigate into directory
                NavigateToDirectory(currentEntry.FullPath);
            }
            else if (IsTextFile(currentEntry.FullPath))
            {
                // Open text file in text viewer
                OpenTextFile(currentEntry.FullPath);
            }
            else if (IsImageFile(currentEntry.FullPath))
            {
                // Open image file in image viewer
                OpenImageFile(currentEntry.FullPath);
            }
            else
            {
                // Execute file with default handler or association
                ExecuteFile(currentEntry.FullPath, ExecutionMode.Default);
            }
        }
        
        /// <summary>
        /// Handles Shift+Enter key press - opens file with editor or extracts archive
        /// </summary>
        public void HandleShiftEnter()
        {
            var activePane = GetActivePane();
            var currentEntry = activePane.GetCurrentEntry();
            
            if (currentEntry == null)
            {
                return;
            }
            
            // Check if this is an archive file
            if (!currentEntry.IsDirectory && _archiveManager.IsArchive(currentEntry.FullPath))
            {
                // Extract archive
                HandleArchiveExtraction();
            }
            else if (!currentEntry.IsDirectory)
            {
                // Open file with text editor
                ExecuteFile(currentEntry.FullPath, ExecutionMode.Editor);
            }
            else
            {
                SetStatus("Cannot open directory with editor");
            }
        }
        
        /// <summary>
        /// Handles Ctrl+Enter key press - opens file with Explorer association
        /// </summary>
        public void HandleCtrlEnter()
        {
            var activePane = GetActivePane();
            var currentEntry = activePane.GetCurrentEntry();
            
            if (currentEntry == null)
            {
                return;
            }
            
            if (!currentEntry.IsDirectory)
            {
                // Open file with Windows Explorer association
                ExecuteFile(currentEntry.FullPath, ExecutionMode.ExplorerAssociation);
            }
            else
            {
                SetStatus("Cannot execute directory");
            }
        }
        
        /// <summary>
        /// Executes a file with the specified execution mode
        /// </summary>
        private void ExecuteFile(string filePath, ExecutionMode mode)
        {
            try
            {
                _logger.LogDebug($"Executing file: {filePath} with mode: {mode}");
                
                var config = _configProvider.LoadConfiguration();
                var result = _fileOps.ExecuteFile(filePath, config, mode);
                
                if (result.Success)
                {
                    SetStatus(result.Message);
                }
                else
                {
                    SetStatus($"Execution failed: {result.Message}");
                    _logger.LogError($"File execution failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing file: {filePath}");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determines if a file should be opened in the text viewer
        /// </summary>
        private bool IsTextFile(string filePath)
        {
            var textExtensions = new[]
            {
                ".txt", ".log", ".md", ".cs", ".json", ".xml", ".html", ".htm",
                ".css", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h",
                ".ini", ".cfg", ".conf", ".config", ".yaml", ".yml",
                ".sh", ".bat", ".cmd", ".ps1", ".sql", ".csv"
            };
            
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return textExtensions.Contains(extension);
        }
        
        /// <summary>
        /// Opens a text file in the text viewer
        /// </summary>
        private void OpenTextFile(string filePath)
        {
            try
            {
                _logger.LogDebug($"Opening text file: {filePath}");
                
                // Open the file in the viewer manager
                _viewerManager.OpenTextViewer(filePath);
                
                // Get the text viewer instance
                var textViewer = _viewerManager.CurrentTextViewer;
                if (textViewer == null)
                {
                    SetStatus("Error: Failed to open text viewer");
                    return;
                }
                
                // Change UI mode to TextViewer
                SetMode(UiMode.TextViewer);
                
                // Create and show the text viewer window
                var viewerWindow = new UI.TextViewerWindow(textViewer, _keyBindings);
                Application.Run(viewerWindow);
                
                // After viewer closes, return to normal mode
                SetMode(UiMode.Normal);
                _viewerManager.CloseCurrentViewer();
                
                // Refresh panes
                RefreshPanes();
                
                _logger.LogDebug("Text viewer closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening text file: {filePath}");
                SetStatus($"Error opening file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determines if a file should be opened in the image viewer
        /// </summary>
        private bool IsImageFile(string filePath)
        {
            var config = _configProvider.LoadConfiguration();
            var imageExtensions = config.Viewer.SupportedImageExtensions;
            
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return imageExtensions.Contains(extension);
        }
        
        /// <summary>
        /// Opens an image file in the image viewer
        /// </summary>
        private void OpenImageFile(string filePath)
        {
            try
            {
                _logger.LogDebug($"Opening image file: {filePath}");
                
                // Open the file in the viewer manager
                _viewerManager.OpenImageViewer(filePath);
                
                // Get the image viewer instance
                var imageViewer = _viewerManager.CurrentImageViewer;
                if (imageViewer == null)
                {
                    SetStatus("Error: Failed to open image viewer");
                    return;
                }
                
                // Change UI mode to ImageViewer
                SetMode(UiMode.ImageViewer);
                
                // Create and show the image viewer window
                var viewerWindow = new UI.ImageViewerWindow(imageViewer);
                Application.Run(viewerWindow);
                
                // After viewer closes, return to normal mode
                SetMode(UiMode.Normal);
                _viewerManager.CloseCurrentViewer();
                
                // Refresh panes
                RefreshPanes();
                
                _logger.LogDebug("Image viewer closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening image file: {filePath}");
                SetStatus($"Error opening file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Switches the display mode for the active pane
        /// Preserves cursor position and scroll offset
        /// </summary>
        public void SwitchDisplayMode(DisplayMode mode)
        {
            var activePane = GetActivePane();
            
            // Store current cursor position and scroll offset
            int previousCursorPosition = activePane.CursorPosition;
            int previousScrollOffset = activePane.ScrollOffset;
            
            // Change display mode
            activePane.DisplayMode = mode;
            
            // Preserve cursor position and scroll offset
            activePane.CursorPosition = previousCursorPosition;
            activePane.ScrollOffset = previousScrollOffset;
            
            // Refresh display
            RefreshPanes();
            
            // Update status to show current display mode
            string modeName = mode switch
            {
                DisplayMode.NameOnly => "Name Only",
                DisplayMode.Details => "Details",
                DisplayMode.Thumbnail => "Thumbnail",
                DisplayMode.Icon => "Icon",
                DisplayMode.OneColumn => "1 Column",
                DisplayMode.TwoColumns => "2 Columns",
                DisplayMode.ThreeColumns => "3 Columns",
                DisplayMode.FourColumns => "4 Columns",
                DisplayMode.FiveColumns => "5 Columns",
                DisplayMode.SixColumns => "6 Columns",
                DisplayMode.SevenColumns => "7 Columns",
                DisplayMode.EightColumns => "8 Columns",
                _ => "Unknown"
            };
            
            SetStatus($"Display mode: {modeName}");
            _logger.LogDebug($"Display mode changed to: {mode}");
        }
        
        /// <summary>
        /// Handles number key press (1-8) for display mode switching
        /// </summary>
        public void HandleNumberKey(int number)
        {
            if (number < 1 || number > 8)
            {
                _logger.LogWarning($"Invalid display mode number: {number}");
                return;
            }
            
            DisplayMode mode = number switch
            {
                1 => DisplayMode.OneColumn,
                2 => DisplayMode.TwoColumns,
                3 => DisplayMode.ThreeColumns,
                4 => DisplayMode.FourColumns,
                5 => DisplayMode.FiveColumns,
                6 => DisplayMode.SixColumns,
                7 => DisplayMode.SevenColumns,
                8 => DisplayMode.EightColumns,
                _ => DisplayMode.Details
            };
            
            SwitchDisplayMode(mode);
        }
        
        /// <summary>
        /// Toggles mark on current entry and moves cursor down
        /// Handles Space key press
        /// </summary>
        public void ToggleMarkAndMoveDown()
        {
            var activePane = GetActivePane();
            
            if (activePane.Entries.Count == 0)
            {
                return;
            }
            
            // Toggle mark at current cursor position
            _markingEngine.ToggleMark(activePane, activePane.CursorPosition);
            
            // Move cursor down
            MoveCursorDown();
            
            // Refresh display
            RefreshPanes();
            
            // Update status
            int markedCount = activePane.MarkedIndices.Count;
            SetStatus($"Marked: {markedCount} file(s)");
            
            _logger.LogDebug($"Toggled mark at index {activePane.CursorPosition}, marked count: {markedCount}");
        }
        
        /// <summary>
        /// Toggles mark on current entry and moves cursor up
        /// Handles Shift+Space key press
        /// </summary>
        public void ToggleMarkAndMoveUp()
        {
            var activePane = GetActivePane();
            
            if (activePane.Entries.Count == 0)
            {
                return;
            }
            
            // Toggle mark at current cursor position
            _markingEngine.ToggleMark(activePane, activePane.CursorPosition);
            
            // Move cursor up
            MoveCursorUp();
            
            // Refresh display
            RefreshPanes();
            
            // Update status
            int markedCount = activePane.MarkedIndices.Count;
            SetStatus($"Marked: {markedCount} file(s)");
            
            _logger.LogDebug($"Toggled mark at index {activePane.CursorPosition}, marked count: {markedCount}");
        }
        
        /// <summary>
        /// Marks range from last marked entry to current cursor position
        /// Handles Ctrl+Space key press
        /// </summary>
        public void MarkRange()
        {
            var activePane = GetActivePane();
            
            if (activePane.Entries.Count == 0)
            {
                return;
            }
            
            // Find the last marked index (or use 0 if none marked)
            int lastMarkedIndex = activePane.MarkedIndices.Count > 0 
                ? activePane.MarkedIndices.Max() 
                : 0;
            
            // Mark range from last marked to current cursor
            _markingEngine.MarkRange(activePane, lastMarkedIndex, activePane.CursorPosition);
            
            // Refresh display
            RefreshPanes();
            
            // Update status
            int markedCount = activePane.MarkedIndices.Count;
            SetStatus($"Marked range: {markedCount} file(s)");
            
            _logger.LogDebug($"Marked range from {lastMarkedIndex} to {activePane.CursorPosition}");
        }
        
        /// <summary>
        /// Inverts all marks in the active pane
        /// Handles Home or backtick key press
        /// </summary>
        public void InvertMarks()
        {
            var activePane = GetActivePane();
            
            if (activePane.Entries.Count == 0)
            {
                return;
            }
            
            // Store count before inversion
            int beforeCount = activePane.MarkedIndices.Count;
            
            // Invert all marks
            _markingEngine.InvertMarks(activePane);
            
            // Refresh display
            RefreshPanes();
            
            // Update status
            int afterCount = activePane.MarkedIndices.Count;
            SetStatus($"Inverted marks: {beforeCount} → {afterCount} file(s)");
            
            _logger.LogDebug($"Inverted marks: {beforeCount} → {afterCount}");
        }
        
        /// <summary>
        /// Shows wildcard marking dialog and applies pattern to mark files
        /// Handles @ key press
        /// Supports wildcard patterns with exclusions (colon prefix)
        /// Supports regex patterns with m/ syntax
        /// </summary>
        public void ShowWildcardMarkingDialog()
        {
            var activePane = GetActivePane();
            
            if (activePane.Entries.Count == 0)
            {
                SetStatus("No files to mark");
                return;
            }
            
            try
            {
                // Create input dialog
                var dialog = new Dialog("Wildcard Mark", 60, 8);
                
                var label = new Label("Enter pattern (* = any chars, ? = single char):")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                var patternField = new TextField("")
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                dialog.Add(patternField);
                
                var helpLabel = new Label("Prefix with : to exclude. Use m/ for regex.")
                {
                    X = 1,
                    Y = 3,
                    Width = Dim.Fill(1),
                    ColorScheme = new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black)
                    }
                };
                dialog.Add(helpLabel);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = 5,
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = 5
                };
                
                bool okPressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                // Set focus to the text field
                patternField.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Process the pattern if OK was pressed
                if (okPressed)
                {
                    string pattern = patternField.Text.ToString() ?? string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(pattern))
                    {
                        ApplyWildcardPattern(pattern);
                    }
                    else
                    {
                        SetStatus("No pattern entered");
                    }
                }
                else
                {
                    SetStatus("Wildcard marking cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing wildcard marking dialog");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies a wildcard or regex pattern to mark files
        /// Supports multiple patterns separated by spaces
        /// Patterns starting with colon (:) are exclusion patterns
        /// Patterns starting with m/ are treated as regex patterns
        /// </summary>
        private void ApplyWildcardPattern(string pattern)
        {
            var activePane = GetActivePane();
            
            try
            {
                // Clear existing marks before applying pattern
                _markingEngine.ClearMarks(activePane);
                
                // Check if this is a regex pattern
                if (pattern.TrimStart().StartsWith("m/"))
                {
                    // Remove m/ prefix and apply regex marking
                    string regexPattern = pattern.TrimStart().Substring(2);
                    _markingEngine.MarkByRegex(activePane, regexPattern);
                    
                    int markedCount = activePane.MarkedIndices.Count;
                    SetStatus($"Regex pattern applied: {markedCount} file(s) marked");
                    _logger.LogDebug($"Applied regex pattern '{regexPattern}': {markedCount} files marked");
                }
                else
                {
                    // Apply wildcard pattern
                    _markingEngine.MarkByWildcard(activePane, pattern);
                    
                    int markedCount = activePane.MarkedIndices.Count;
                    SetStatus($"Pattern applied: {markedCount} file(s) marked");
                    _logger.LogDebug($"Applied wildcard pattern '{pattern}': {markedCount} files marked");
                }
                
                // Refresh display to show marked files
                RefreshPanes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying pattern: {pattern}");
                SetStatus($"Error applying pattern: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles C key press - initiates copy operation
        /// Copies marked files or current file to opposite pane
        /// </summary>
        public void HandleCopyOperation()
        {
            var activePane = GetActivePane();
            var inactivePane = GetInactivePane();
            
            // Get files to copy (marked files or current file)
            var filesToCopy = activePane.GetMarkedEntries();
            if (filesToCopy.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToCopy = new List<FileEntry> { currentEntry };
                }
            }
            
            if (filesToCopy.Count == 0)
            {
                SetStatus("No files to copy");
                return;
            }
            
            // Execute copy operation with progress dialog
            ExecuteFileOperationWithProgress(
                "Copy",
                filesToCopy,
                inactivePane.CurrentPath,
                (files, dest, token) => _fileOps.CopyAsync(files, dest, token));
        }
        
        /// <summary>
        /// Handles M key press - initiates move operation
        /// Moves marked files or current file to opposite pane
        /// </summary>
        public void HandleMoveOperation()
        {
            var activePane = GetActivePane();
            var inactivePane = GetInactivePane();
            
            // Get files to move (marked files or current file)
            var filesToMove = activePane.GetMarkedEntries();
            if (filesToMove.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToMove = new List<FileEntry> { currentEntry };
                }
            }
            
            if (filesToMove.Count == 0)
            {
                SetStatus("No files to move");
                return;
            }
            
            // Execute move operation with progress dialog
            ExecuteFileOperationWithProgress(
                "Move",
                filesToMove,
                inactivePane.CurrentPath,
                (files, dest, token) => _fileOps.MoveAsync(files, dest, token));
        }
        
        /// <summary>
        /// Handles D key press - initiates delete operation with confirmation
        /// Deletes marked files or current file after user confirmation
        /// </summary>
        public void HandleDeleteOperation()
        {
            var activePane = GetActivePane();
            
            // Get files to delete (marked files or current file)
            var filesToDelete = activePane.GetMarkedEntries();
            if (filesToDelete.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToDelete = new List<FileEntry> { currentEntry };
                }
            }
            
            if (filesToDelete.Count == 0)
            {
                SetStatus("No files to delete");
                return;
            }
            
            // Show confirmation dialog
            var fileList = string.Join(", ", filesToDelete.Take(5).Select(f => f.Name));
            if (filesToDelete.Count > 5)
            {
                fileList += $" and {filesToDelete.Count - 5} more";
            }
            
            var confirmed = ShowConfirmationDialog(
                "Delete Confirmation",
                $"Delete {filesToDelete.Count} file(s)?\n{fileList}");
            
            if (!confirmed)
            {
                SetStatus("Delete cancelled");
                return;
            }
            
            // Execute delete operation with progress dialog
            ExecuteFileOperationWithProgress(
                "Delete",
                filesToDelete,
                string.Empty, // No destination for delete
                (files, dest, token) => _fileOps.DeleteAsync(files, token));
        }
        
        /// <summary>
        /// Handles R key press - simple rename of current file
        /// Shows input dialog with current filename, allows editing
        /// </summary>
        public void HandleSimpleRename()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                
                if (currentEntry == null)
                {
                    SetStatus("No file selected");
                    return;
                }
                
                if (currentEntry.IsDirectory)
                {
                    SetStatus("Cannot rename directories (use Shift+R for pattern rename)");
                    return;
                }
                
                // Show input dialog with current filename
                var dialog = new Dialog("Rename File", 60, 8);
                
                var label = new Label("New filename:")
                {
                    X = 1,
                    Y = 1
                };
                dialog.Add(label);
                
                var nameField = new TextField(currentEntry.Name)
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                dialog.Add(nameField);
                
                var okButton = new Button("OK", is_default: true);
                okButton.Clicked += () =>
                {
                    string newName = nameField.Text.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(newName) && newName != currentEntry.Name)
                    {
                        try
                        {
                            string oldPath = currentEntry.FullPath;
                            string newPath = Path.Combine(Path.GetDirectoryName(oldPath) ?? "", newName);
                            
                            if (File.Exists(newPath))
                            {
                                SetStatus($"File already exists: {newName}");
                            }
                            else
                            {
                                File.Move(oldPath, newPath);
                                LoadPaneDirectory(activePane);
                                RefreshPanes();
                                SetStatus($"Renamed to: {newName}");
                                _logger.LogInformation($"Renamed {currentEntry.Name} to {newName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            SetStatus($"Rename failed: {ex.Message}");
                            _logger.LogError(ex, "Error renaming file");
                        }
                    }
                    Application.RequestStop();
                };
                
                var cancelButton = new Button("Cancel");
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                dialog.AddButton(okButton);
                dialog.AddButton(cancelButton);
                
                Application.Run(dialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in simple rename");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles Shift+R key press - initiates pattern-based rename operation
        /// Shows dialog for pattern input, previews new names, and renames on confirmation
        /// </summary>
        public void HandlePatternRename()
        {
            var activePane = GetActivePane();
            
            // Get files to rename (marked files or current file)
            var filesToRename = activePane.GetMarkedEntries();
            if (filesToRename.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToRename = new List<FileEntry> { currentEntry };
                }
            }
            
            if (filesToRename.Count == 0)
            {
                SetStatus("No files to rename");
                return;
            }
            
            // Show pattern input dialog
            var (pattern, replacement, confirmed) = ShowPatternRenameDialog(filesToRename);
            
            if (!confirmed || string.IsNullOrEmpty(pattern))
            {
                SetStatus("Rename cancelled");
                return;
            }
            
            // Execute rename operation with progress dialog
            ExecuteFileOperationWithProgress(
                "Rename",
                filesToRename,
                string.Empty, // No destination for rename
                (files, dest, token) => _fileOps.RenameAsync(files, pattern, replacement ?? string.Empty));
        }
        
        /// <summary>
        /// Shows a dialog for pattern-based rename with preview
        /// Returns (pattern, replacement, confirmed)
        /// </summary>
        private (string pattern, string replacement, bool confirmed) ShowPatternRenameDialog(List<FileEntry> files)
        {
            string pattern = string.Empty;
            string replacement = string.Empty;
            bool confirmed = false;
            
            var dialog = new Dialog("Pattern-Based Rename", 80, 20);
            
            // Instructions
            var instructions = new Label(
                "Enter pattern and replacement:\n" +
                "  Simple: 'old' -> 'new'\n" +
                "  Regex: 's/pattern/replacement/'\n" +
                "  Transliterate: 'tr/abc/xyz/'")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = 4
            };
            dialog.Add(instructions);
            
            // Pattern input
            var patternLabel = new Label("Pattern:")
            {
                X = 1,
                Y = 5
            };
            dialog.Add(patternLabel);
            
            var patternField = new TextField("")
            {
                X = 12,
                Y = 5,
                Width = Dim.Fill(1)
            };
            dialog.Add(patternField);
            
            // Replacement input
            var replacementLabel = new Label("Replace:")
            {
                X = 1,
                Y = 6
            };
            dialog.Add(replacementLabel);
            
            var replacementField = new TextField("")
            {
                X = 12,
                Y = 6,
                Width = Dim.Fill(1)
            };
            dialog.Add(replacementField);
            
            // Preview label
            var previewLabel = new Label("Preview (first 5 files):")
            {
                X = 1,
                Y = 8
            };
            dialog.Add(previewLabel);
            
            // Preview list
            var previewList = new ListView()
            {
                X = 1,
                Y = 9,
                Width = Dim.Fill(1),
                Height = 5,
                AllowsMarking = false
            };
            dialog.Add(previewList);
            
            // Update preview when pattern or replacement changes
            void UpdatePreview()
            {
                var previewItems = new List<string>();
                var currentPattern = patternField.Text.ToString() ?? string.Empty;
                var currentReplacement = replacementField.Text.ToString() ?? string.Empty;
                
                if (string.IsNullOrEmpty(currentPattern))
                {
                    previewItems.Add("(Enter a pattern to see preview)");
                }
                else
                {
                    foreach (var file in files.Take(5))
                    {
                        try
                        {
                            var newName = ApplyRenamePatternPreview(file.Name, currentPattern, currentReplacement);
                            previewItems.Add($"{file.Name} -> {newName}");
                        }
                        catch
                        {
                            previewItems.Add($"{file.Name} -> (invalid pattern)");
                        }
                    }
                    
                    if (files.Count > 5)
                    {
                        previewItems.Add($"... and {files.Count - 5} more files");
                    }
                }
                
                previewList.SetSource(previewItems);
            }
            
            patternField.TextChanged += (oldText) => UpdatePreview();
            replacementField.TextChanged += (oldText) => UpdatePreview();
            
            // Buttons
            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = Pos.AnchorEnd(2),
                IsDefault = true
            };
            
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = Pos.AnchorEnd(2)
            };
            
            okButton.Clicked += () =>
            {
                pattern = patternField.Text.ToString() ?? string.Empty;
                replacement = replacementField.Text.ToString() ?? string.Empty;
                confirmed = true;
                Application.RequestStop();
            };
            
            cancelButton.Clicked += () =>
            {
                confirmed = false;
                Application.RequestStop();
            };
            
            dialog.Add(okButton);
            dialog.Add(cancelButton);
            
            // Set initial focus to pattern field
            patternField.SetFocus();
            
            // Initial preview update
            UpdatePreview();
            
            Application.Run(dialog);
            
            return (pattern, replacement, confirmed);
        }
        
        /// <summary>
        /// Applies rename pattern for preview purposes (same logic as FileOperations.ApplyRenamePattern)
        /// </summary>
        private string ApplyRenamePatternPreview(string filename, string pattern, string replacement)
        {
            // Support regex patterns with s/ syntax
            if (pattern.StartsWith("s/"))
            {
                var parts = pattern.Split('/');
                if (parts.Length >= 3)
                {
                    var searchPattern = parts[1];
                    var replacePattern = parts[2];
                    return System.Text.RegularExpressions.Regex.Replace(filename, searchPattern, replacePattern);
                }
            }

            // Support transliteration with tr/ syntax
            if (pattern.StartsWith("tr/"))
            {
                var parts = pattern.Split('/');
                if (parts.Length >= 3)
                {
                    var fromChars = parts[1];
                    var toChars = parts[2];
                    var result = filename.ToCharArray();
                    for (int i = 0; i < result.Length; i++)
                    {
                        int index = fromChars.IndexOf(result[i]);
                        if (index >= 0 && index < toChars.Length)
                        {
                            result[i] = toChars[index];
                        }
                    }
                    return new string(result);
                }
            }

            // Simple string replacement
            return filename.Replace(pattern, replacement);
        }
        
        /// <summary>
        /// Handles W key press - initiates file comparison operation
        /// Shows dialog to select comparison criteria, then marks matching files
        /// </summary>
        public void HandleFileComparison()
        {
            try
            {
                var leftPane = _leftState;
                var rightPane = _rightState;
                
                // Check if both panes have files
                if (leftPane.Entries.Count == 0 && rightPane.Entries.Count == 0)
                {
                    SetStatus("No files to compare");
                    return;
                }
                
                // Show comparison criteria selection dialog
                var (criteria, timestampTolerance, confirmed) = ShowComparisonDialog();
                
                if (!confirmed)
                {
                    SetStatus("Comparison cancelled");
                    return;
                }
                
                // Execute comparison
                var result = _fileOps.CompareFiles(leftPane, rightPane, criteria, timestampTolerance);
                
                if (result.Success)
                {
                    // Refresh panes to show marked files
                    RefreshPanes();
                    SetStatus(result.Message);
                    _logger.LogInformation($"File comparison completed: {result.Message}");
                }
                else
                {
                    SetStatus($"Comparison failed: {result.Message}");
                    _logger.LogError($"File comparison failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file comparison");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows a dialog for selecting file comparison criteria
        /// Returns (criteria, timestampTolerance, confirmed)
        /// </summary>
        private (ComparisonCriteria criteria, TimeSpan? timestampTolerance, bool confirmed) ShowComparisonDialog()
        {
            ComparisonCriteria selectedCriteria = ComparisonCriteria.Size;
            TimeSpan? timestampTolerance = TimeSpan.FromSeconds(2);
            bool confirmed = false;
            
            var dialog = new Dialog("File Comparison", 70, 18);
            
            // Instructions
            var instructions = new Label(
                "Select comparison criteria to mark matching files in both panes:")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            dialog.Add(instructions);
            
            // Radio group for comparison criteria
            var criteriaLabel = new Label("Comparison Criteria:")
            {
                X = 1,
                Y = 3
            };
            dialog.Add(criteriaLabel);
            
            var radioGroup = new RadioGroup(
                new NStack.ustring[] { "Size", "Timestamp", "Name" })
            {
                X = 3,
                Y = 4,
                Width = Dim.Fill(1),
                Height = 3,
                SelectedItem = 0
            };
            dialog.Add(radioGroup);
            
            // Timestamp tolerance input (only visible when Timestamp is selected)
            var toleranceLabel = new Label("Timestamp Tolerance (seconds):")
            {
                X = 1,
                Y = 8,
                Visible = false
            };
            dialog.Add(toleranceLabel);
            
            var toleranceField = new TextField("2")
            {
                X = 35,
                Y = 8,
                Width = 10,
                Visible = false
            };
            dialog.Add(toleranceField);
            
            var toleranceHint = new Label("(Files within this time difference are considered matching)")
            {
                X = 3,
                Y = 9,
                Width = Dim.Fill(1),
                Visible = false,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
                }
            };
            dialog.Add(toleranceHint);
            
            // Update visibility based on selection
            radioGroup.SelectedItemChanged += (args) =>
            {
                bool isTimestamp = args.SelectedItem == 1; // Timestamp option
                toleranceLabel.Visible = isTimestamp;
                toleranceField.Visible = isTimestamp;
                toleranceHint.Visible = isTimestamp;
            };
            
            // Description of each criteria
            var descriptionLabel = new Label("")
            {
                X = 1,
                Y = 11,
                Width = Dim.Fill(1),
                Height = 3,
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black)
                }
            };
            dialog.Add(descriptionLabel);
            
            // Update description based on selection
            void UpdateDescription()
            {
                switch (radioGroup.SelectedItem)
                {
                    case 0: // Size
                        descriptionLabel.Text = "Marks files with identical file sizes in both panes.";
                        break;
                    case 1: // Timestamp
                        descriptionLabel.Text = "Marks files with matching last modified timestamps\n(within the specified tolerance).";
                        break;
                    case 2: // Name
                        descriptionLabel.Text = "Marks files with identical names in both panes\n(case-insensitive comparison).";
                        break;
                }
            }
            
            radioGroup.SelectedItemChanged += (args) => UpdateDescription();
            UpdateDescription(); // Initial description
            
            // Buttons
            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = Pos.AnchorEnd(2),
                IsDefault = true
            };
            
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = Pos.AnchorEnd(2)
            };
            
            okButton.Clicked += () =>
            {
                // Map radio selection to enum
                selectedCriteria = radioGroup.SelectedItem switch
                {
                    0 => ComparisonCriteria.Size,
                    1 => ComparisonCriteria.Timestamp,
                    2 => ComparisonCriteria.Name,
                    _ => ComparisonCriteria.Size
                };
                
                // Parse timestamp tolerance if applicable
                if (selectedCriteria == ComparisonCriteria.Timestamp)
                {
                    var toleranceText = toleranceField.Text.ToString() ?? "2";
                    if (double.TryParse(toleranceText, out double seconds))
                    {
                        timestampTolerance = TimeSpan.FromSeconds(seconds);
                    }
                    else
                    {
                        timestampTolerance = TimeSpan.FromSeconds(2); // Default
                    }
                }
                else
                {
                    timestampTolerance = null;
                }
                
                confirmed = true;
                Application.RequestStop();
            };
            
            cancelButton.Clicked += () =>
            {
                confirmed = false;
                Application.RequestStop();
            };
            
            dialog.Add(okButton);
            dialog.Add(cancelButton);
            
            // Set initial focus to radio group
            radioGroup.SetFocus();
            
            Application.Run(dialog);
            
            return (selectedCriteria, timestampTolerance, confirmed);
        }
        
        /// <summary>
        /// Shows a confirmation dialog and returns user's choice
        /// </summary>
        private bool ShowConfirmationDialog(string title, string message)
        {
            var result = false;
            
            var dialog = new Dialog(title, 60, 10);
            
            var label = new Label(message)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(3)
            };
            dialog.Add(label);
            
            var yesButton = new Button("Yes")
            {
                X = Pos.Center() - 10,
                Y = Pos.AnchorEnd(2),
                IsDefault = true
            };
            
            var noButton = new Button("No")
            {
                X = Pos.Center() + 2,
                Y = Pos.AnchorEnd(2)
            };
            
            yesButton.Clicked += () =>
            {
                result = true;
                Application.RequestStop();
            };
            
            noButton.Clicked += () =>
            {
                result = false;
                Application.RequestStop();
            };
            
            dialog.Add(yesButton);
            dialog.Add(noButton);
            
            Application.Run(dialog);
            
            return result;
        }
        
        /// <summary>
        /// Executes a file operation with progress dialog and cancellation support
        /// </summary>
        private void ExecuteFileOperationWithProgress(
            string operationName,
            List<FileEntry> files,
            string destination,
            Func<List<FileEntry>, string, CancellationToken, Task<OperationResult>> operation)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var progressDialog = new Dialog($"{operationName} Progress", 70, 12);
            
            var statusLabel = new Label("Preparing...")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            progressDialog.Add(statusLabel);
            
            var fileLabel = new Label("")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            progressDialog.Add(fileLabel);
            
            var progressLabel = new Label("0%")
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1)
            };
            progressDialog.Add(progressLabel);
            
            var bytesLabel = new Label("")
            {
                X = 1,
                Y = 4,
                Width = Dim.Fill(1)
            };
            progressDialog.Add(bytesLabel);
            
            var cancelButton = new Button("Cancel (ESC)")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(2)
            };
            
            cancelButton.Clicked += () =>
            {
                cancellationTokenSource.Cancel();
                statusLabel.Text = "Cancelling...";
            };
            
            progressDialog.Add(cancelButton);
            
            // Handle Escape key for cancellation
            progressDialog.KeyPress += (e) =>
            {
                if (e.KeyEvent.Key == (Key)27) // Escape key code
                {
                    cancellationTokenSource.Cancel();
                    statusLabel.Text = "Cancelling...";
                    e.Handled = true;
                }
            };
            
            // Subscribe to progress events
            EventHandler<ProgressEventArgs>? progressHandler = (sender, e) =>
            {
                Application.MainLoop.Invoke(() =>
                {
                    fileLabel.Text = $"File: {e.CurrentFile} ({e.CurrentFileIndex}/{e.TotalFiles})";
                    progressLabel.Text = $"{e.PercentComplete:F1}%";
                    
                    if (e.TotalBytes > 0)
                    {
                        var mbProcessed = e.BytesProcessed / (1024.0 * 1024.0);
                        var mbTotal = e.TotalBytes / (1024.0 * 1024.0);
                        bytesLabel.Text = $"{mbProcessed:F2} MB / {mbTotal:F2} MB";
                    }
                });
            };
            
            _fileOps.ProgressChanged += progressHandler;
            
            // Execute operation asynchronously
            Task.Run(async () =>
            {
                try
                {
                    var result = await operation(files, destination, cancellationTokenSource.Token);
                    
                    Application.MainLoop.Invoke(() =>
                    {
                        _fileOps.ProgressChanged -= progressHandler;
                        Application.RequestStop();
                        
                        // Show result message
                        SetStatus(result.Message);
                        
                        if (result.Errors.Count > 0)
                        {
                            var errorMsg = string.Join("\n", result.Errors.Take(5));
                            if (result.Errors.Count > 5)
                            {
                                errorMsg += $"\n... and {result.Errors.Count - 5} more errors";
                            }
                            ShowMessageDialog("Operation Errors", errorMsg);
                        }
                        
                        // Refresh both panes
                        LoadPaneDirectory(_leftState);
                        LoadPaneDirectory(_rightState);
                        RefreshPanes();
                    });
                }
                catch (Exception ex)
                {
                    Application.MainLoop.Invoke(() =>
                    {
                        _fileOps.ProgressChanged -= progressHandler;
                        Application.RequestStop();
                        SetStatus($"{operationName} failed: {ex.Message}");
                        _logger.LogError(ex, $"{operationName} operation failed");
                    });
                }
            });
            
            // Show the progress dialog
            Application.Run(progressDialog);
        }
        
        /// <summary>
        /// Shows a message dialog
        /// </summary>
        private void ShowMessageDialog(string title, string message)
        {
            var dialog = new Dialog(title, 60, 15);
            
            var label = new Label(message)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                Height = Dim.Fill(3)
            };
            dialog.Add(label);
            
            var okButton = new Button("OK")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(2),
                IsDefault = true
            };
            
            okButton.Clicked += () =>
            {
                Application.RequestStop();
            };
            
            dialog.Add(okButton);
            
            Application.Run(dialog);
        }
        
        /// <summary>
        /// Handles J key press - shows directory creation dialog
        /// Creates a new directory in the active pane's current path
        /// Positions cursor on the newly created directory
        /// Handles Escape for cancellation
        /// </summary>
        public void HandleCreateDirectory()
        {
            var activePane = GetActivePane();
            
            try
            {
                // Create input dialog
                var dialog = new Dialog("Create Directory", 60, 8);
                
                var label = new Label("Enter directory name:")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                var nameField = new TextField("")
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                dialog.Add(nameField);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = 5,
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = 5
                };
                
                bool okPressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                // Handle Escape key for cancellation
                dialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == (Key)27) // Escape key code
                    {
                        Application.RequestStop();
                        e.Handled = true;
                    }
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                // Set focus to the text field
                nameField.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Process the directory name if OK was pressed
                if (okPressed)
                {
                    string directoryName = nameField.Text.ToString() ?? string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(directoryName))
                    {
                        CreateDirectory(directoryName);
                    }
                    else
                    {
                        SetStatus("No directory name entered");
                    }
                }
                else
                {
                    SetStatus("Directory creation cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing directory creation dialog");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a new directory in the active pane's current path
        /// and positions the cursor on the newly created directory
        /// </summary>
        private void CreateDirectory(string directoryName)
        {
            var activePane = GetActivePane();
            
            try
            {
                // Create the directory using FileOperations
                var result = _fileOps.CreateDirectory(activePane.CurrentPath, directoryName);
                
                if (result.Success)
                {
                    // Reload the directory to show the new folder
                    LoadPaneDirectory(activePane);
                    
                    // Find the newly created directory and position cursor on it
                    var newDirPath = Path.Combine(activePane.CurrentPath, directoryName);
                    var newDirIndex = activePane.Entries.FindIndex(e => 
                        e.FullPath.Equals(newDirPath, StringComparison.OrdinalIgnoreCase));
                    
                    if (newDirIndex >= 0)
                    {
                        activePane.CursorPosition = newDirIndex;
                    }
                    
                    // Refresh display
                    RefreshPanes();
                    
                    SetStatus($"Directory created: {directoryName}");
                    _logger.LogInformation($"Directory created: {newDirPath}");
                }
                else
                {
                    SetStatus($"Failed to create directory: {result.Message}");
                    _logger.LogWarning($"Failed to create directory '{directoryName}': {result.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating directory: {directoryName}");
                SetStatus($"Error creating directory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles E key press - shows dialog to create and edit a new file
        /// Creates a 0-byte file in the active pane's current path
        /// Opens the file with the associated program based on extension
        /// Positions cursor on the newly created file
        /// Handles Escape for cancellation
        /// </summary>
        public void HandleEditNewFile()
        {
            var activePane = GetActivePane();
            
            try
            {
                // Create input dialog
                var dialog = new Dialog("Create New File", 60, 8);
                
                var label = new Label("Enter new file name:")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                var nameField = new TextField("")
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                dialog.Add(nameField);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = 5,
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = 5
                };
                
                bool okPressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                // Handle Escape key for cancellation
                dialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == (Key)27) // Escape key code
                    {
                        Application.RequestStop();
                        e.Handled = true;
                    }
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                // Set focus to the text field
                nameField.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Process the file name if OK was pressed
                if (okPressed)
                {
                    string fileName = nameField.Text.ToString() ?? string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        CreateAndEditNewFile(fileName);
                    }
                    else
                    {
                        SetStatus("No file name entered");
                    }
                }
                else
                {
                    SetStatus("File creation cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing new file creation dialog");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a new 0-byte file in the active pane's current path,
        /// opens it with the associated program, and positions the cursor on it
        /// </summary>
        private void CreateAndEditNewFile(string fileName)
        {
            var activePane = GetActivePane();
            
            try
            {
                // Validate file name
                var invalidChars = Path.GetInvalidFileNameChars();
                if (fileName.IndexOfAny(invalidChars) >= 0)
                {
                    SetStatus("Invalid file name: contains invalid characters");
                    _logger.LogWarning($"Invalid file name attempted: {fileName}");
                    return;
                }
                
                // Build full path
                var fullPath = Path.Combine(activePane.CurrentPath, fileName);
                
                // Check if file already exists
                if (File.Exists(fullPath))
                {
                    SetStatus($"File already exists: {fileName}");
                    _logger.LogWarning($"File already exists: {fullPath}");
                    return;
                }
                
                // Create 0-byte file
                File.Create(fullPath).Dispose();
                _logger.LogInformation($"Created new file: {fullPath}");
                
                // Reload the directory to show the new file
                LoadPaneDirectory(activePane);
                
                // Find the newly created file and position cursor on it
                var newFileIndex = activePane.Entries.FindIndex(e => 
                    e.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
                
                if (newFileIndex >= 0)
                {
                    activePane.CursorPosition = newFileIndex;
                }
                
                // Refresh display
                RefreshPanes();
                
                // Open the file with associated program
                OpenFileWithAssociatedProgram(fullPath);
                
                SetStatus($"Created and opened: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating new file: {fileName}");
                SetStatus($"Error creating file: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Opens a file with the associated program from ExtensionAssociations config
        /// Falls back to default system association if no specific association is configured
        /// </summary>
        private void OpenFileWithAssociatedProgram(string filePath)
        {
            try
            {
                var config = _configProvider.LoadConfiguration();
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                string? programPath = null;
                
                // Look up associated program from configuration
                if (!string.IsNullOrEmpty(extension) && 
                    config.ExtensionAssociations.TryGetValue(extension, out var associatedProgram))
                {
                    programPath = associatedProgram;
                    _logger.LogDebug($"Found associated program for {extension}: {programPath}");
                }
                
                // Launch the file
                if (!string.IsNullOrEmpty(programPath))
                {
                    // Launch with specific program from config
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = programPath,
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    
                    System.Diagnostics.Process.Start(processStartInfo);
                    _logger.LogInformation($"Opened {filePath} with {programPath}");
                }
                else
                {
                    // Use default system association
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    
                    System.Diagnostics.Process.Start(processStartInfo);
                    _logger.LogInformation($"Opened {filePath} with default system association");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Program not found or no association
                _logger.LogWarning(ex, $"Could not open file with associated program: {filePath}");
                SetStatus($"Warning: Could not open file - {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening file: {filePath}");
                SetStatus($"Error opening file: {ex.Message}");
            }
        }
        

        /// <summary>
        /// Handles S key press - cycles through sort modes
        /// Updates pane display with new sort order
        /// Shows current sort mode in status area
        /// </summary>
        public void CycleSortMode()
        {
            var activePane = GetActivePane();
            
            try
            {
                // Define the sort mode cycle order
                var sortModes = new[]
                {
                    SortMode.NameAscending,
                    SortMode.NameDescending,
                    SortMode.ExtensionAscending,
                    SortMode.ExtensionDescending,
                    SortMode.SizeAscending,
                    SortMode.SizeDescending,
                    SortMode.DateAscending,
                    SortMode.DateDescending,
                    SortMode.Unsorted
                };
                
                // Find current mode index
                int currentIndex = Array.IndexOf(sortModes, activePane.SortMode);
                
                // Move to next mode (wrap around to beginning if at end)
                int nextIndex = (currentIndex + 1) % sortModes.Length;
                SortMode newSortMode = sortModes[nextIndex];
                
                // Update the pane's sort mode
                activePane.SortMode = newSortMode;
                
                // Re-sort the entries
                activePane.Entries = SortEngine.Sort(activePane.Entries, newSortMode);
                
                // Refresh the display
                RefreshPanes();
                
                // Get friendly name for status display
                string sortModeName = GetSortModeName(newSortMode);
                
                // Update status to show current sort mode
                SetStatus($"Sort mode: {sortModeName}");
                
                _logger.LogDebug($"Sort mode changed to: {newSortMode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cycling sort mode");
                SetStatus($"Error changing sort mode: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets a friendly display name for a sort mode
        /// </summary>
        private string GetSortModeName(SortMode mode)
        {
            return mode switch
            {
                SortMode.Unsorted => "Unsorted",
                SortMode.NameAscending => "Name (A-Z)",
                SortMode.NameDescending => "Name (Z-A)",
                SortMode.ExtensionAscending => "Extension (A-Z)",
                SortMode.ExtensionDescending => "Extension (Z-A)",
                SortMode.SizeAscending => "Size (Small-Large)",
                SortMode.SizeDescending => "Size (Large-Small)",
                SortMode.DateAscending => "Date (Old-New)",
                SortMode.DateDescending => "Date (New-Old)",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Handles colon key press - shows file mask input dialog
        /// Parses multiple masks with spaces
        /// Supports exclusion patterns with colon prefix
        /// Applies mask to filter file list
        /// </summary>
        public void ShowFileMaskDialog()
        {
            var activePane = GetActivePane();
            
            try
            {
                // Create input dialog
                var dialog = new Dialog("File Mask Filter", 60, 10);
                
                var label = new Label("Enter file mask (* = any chars, ? = single char):")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                var maskField = new TextField(activePane.FileMask)
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                dialog.Add(maskField);
                
                var helpLabel1 = new Label("Multiple patterns: *.txt *.doc")
                {
                    X = 1,
                    Y = 3,
                    Width = Dim.Fill(1),
                    ColorScheme = new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black)
                    }
                };
                dialog.Add(helpLabel1);
                
                var helpLabel2 = new Label("Exclusion: *.txt :temp*")
                {
                    X = 1,
                    Y = 4,
                    Width = Dim.Fill(1),
                    ColorScheme = new ColorScheme()
                    {
                        Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black)
                    }
                };
                dialog.Add(helpLabel2);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = 6,
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = 6
                };
                
                bool okPressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                // Handle Escape key for cancellation
                dialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == (Key)27) // Escape key code
                    {
                        Application.RequestStop();
                        e.Handled = true;
                    }
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                // Set focus to the text field
                maskField.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Process the mask if OK was pressed
                if (okPressed)
                {
                    string mask = maskField.Text.ToString() ?? string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(mask))
                    {
                        ApplyFileMask(mask);
                    }
                    else
                    {
                        // Empty mask means show all files
                        ApplyFileMask("*");
                    }
                }
                else
                {
                    SetStatus("File mask filter cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing file mask dialog");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies a file mask filter to the active pane
        /// Supports multiple patterns separated by spaces
        /// Patterns starting with colon (:) are exclusion patterns
        /// </summary>
        private void ApplyFileMask(string mask)
        {
            var activePane = GetActivePane();
            
            try
            {
                // Update the pane's file mask
                activePane.FileMask = mask;
                
                // Reload the directory with the new mask applied
                LoadPaneDirectory(activePane);
                
                // Refresh display
                RefreshPanes();
                
                // Update status
                int fileCount = activePane.Entries.Count(e => !e.IsDirectory);
                int dirCount = activePane.Entries.Count(e => e.IsDirectory);
                
                SetStatus($"File mask '{mask}' applied: {fileCount} file(s), {dirCount} dir(s)");
                _logger.LogDebug($"Applied file mask '{mask}': {fileCount} files, {dirCount} directories");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error applying file mask: {mask}");
                SetStatus($"Error applying file mask: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows path input dialog to jump to a specific path (\ key)
        /// </summary>
        public void JumpToPath()
        {
            try
            {
                var activePane = GetActivePane();
                
                // Show input dialog for path
                var dialog = new Dialog("Jump to Path", 70, 8);
                
                var label = new Label("Enter path:")
                {
                    X = 1,
                    Y = 1
                };
                dialog.Add(label);
                
                var pathField = new TextField(activePane.CurrentPath)
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                dialog.Add(pathField);
                
                var okButton = new Button("OK", is_default: true);
                okButton.Clicked += () =>
                {
                    string newPath = pathField.Text.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(newPath))
                    {
                        try
                        {
                            if (Directory.Exists(newPath))
                            {
                                activePane.CurrentPath = newPath;
                                LoadPaneDirectory(activePane);
                                RefreshPanes();
                                SetStatus($"Jumped to: {newPath}");
                                _logger.LogInformation($"Jumped to path: {newPath}");
                            }
                            else
                            {
                                SetStatus($"Path not found: {newPath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            SetStatus($"Invalid path: {ex.Message}");
                            _logger.LogError(ex, "Error jumping to path");
                        }
                    }
                    Application.RequestStop();
                };
                
                var cancelButton = new Button("Cancel");
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                dialog.AddButton(okButton);
                dialog.AddButton(cancelButton);
                
                Application.Run(dialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in jump to path");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows drive change dialog (L key)
        /// </summary>
        public void ShowDriveChangeDialog()
        {
            try
            {
                var drives = _listProvider.GetDriveList();
                
                if (drives.Count == 0)
                {
                    SetStatus("No drives available");
                    return;
                }
                
                // Create drive selection dialog
                var dialog = new Dialog("Select Drive", 60, 15);
                
                var driveList = new ListView()
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1),
                    Height = Dim.Fill(2),
                    AllowsMarking = false
                };
                
                var driveNames = drives.Select(d => 
                    $"{d.DriveLetter} - {d.VolumeLabel} ({d.DriveType})"
                ).ToList();
                
                driveList.SetSource(driveNames);
                dialog.Add(driveList);
                
                // Add keyboard handler for drive selection on the dialog
                dialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.KeyValue >= 'A' && e.KeyEvent.KeyValue <= 'Z' ||
                        e.KeyEvent.KeyValue >= 'a' && e.KeyEvent.KeyValue <= 'z')
                    {
                        char key = char.ToUpper((char)e.KeyEvent.KeyValue);
                        // Find drive that starts with this letter
                        for (int i = 0; i < drives.Count; i++)
                        {
                            if (drives[i].DriveLetter.StartsWith(key.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                driveList.SelectedItem = i;
                                driveList.SetNeedsDisplay();
                                e.Handled = true;
                                break;
                            }
                        }
                    }
                };
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = Pos.AnchorEnd(1),
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = Pos.AnchorEnd(1)
                };
                
                okButton.Clicked += () =>
                {
                    if (driveList.SelectedItem >= 0 && driveList.SelectedItem < drives.Count)
                    {
                        var selectedDrive = drives[driveList.SelectedItem];
                        NavigateToDirectory(selectedDrive.DriveLetter);
                        SetStatus($"Changed to drive {selectedDrive.DriveLetter}");
                    }
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                Application.Run(dialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing drive change dialog");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows file size summary (H key)
        /// </summary>
        /// <summary>
        /// Shows help dialog with key bindings (F1 key)
        /// </summary>
        public void ShowHelp()
        {
            try
            {
                _logger.LogInformation("ShowHelp called");
                SetStatus("Opening help...");
                
                string helpText = @"TWF - Two-pane Window Filer - Key Bindings

NAVIGATION:
  Tab         - Switch between panes
  Enter       - Open directory/file
  Backspace   - Go to parent directory
  Ctrl+Home   - Go to root directory
  Up/Down     - Move cursor
  Left/Right  - Switch to left/right pane
  PageUp/Down - Scroll page
  Ctrl+PgUp   - Jump to first entry
  Ctrl+PgDn   - Jump to last entry

FILE OPERATIONS:
  C           - Copy files
  M           - Move files
  D           - Delete files
  K           - Create directory
  Shift+R     - Rename with pattern
  P           - Compress to archive
  O           - Extract archive

MARKING:
  Space       - Toggle mark (move down)
  Shift+Space - Toggle mark (move up)
  Ctrl+Space  - Mark range
  Home        - Invert marks
  A           - Mark all
  End         - Clear marks
  @           - Wildcard mark

VIEW:
  1-8         - Switch display mode
  V           - View as text
  H           - Show file info
  S           - Cycle sort mode
  :           - Set file mask
  F           - Incremental search

OTHER:
  I/G         - Registered folders
  Shift+B     - Register current folder
  Shift+M     - Move to registered folder
  L           - Change drive
  W           - Compare files
  Shift+W     - Split/join files
  Y/Z         - Configuration
  E           - Sync panes
  Escape      - Exit

Press any key to close...";

                // Create dialog that fits the terminal size
                int dialogWidth = Math.Min(80, Application.Driver.Cols - 4);
                int dialogHeight = Math.Min(30, Application.Driver.Rows - 4);
                
                var dialog = new Dialog("Help", dialogWidth, dialogHeight)
                {
                    Modal = true
                };
                
                var textView = new TextView()
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(1),
                    ReadOnly = true,
                    Text = helpText
                };
                
                dialog.Add(textView);
                
                var closeButton = new Button("Close")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(0)
                };
                closeButton.Clicked += () => Application.RequestStop();
                dialog.Add(closeButton);
                
                Application.Run(dialog);
                
                // Refresh display after dialog closes
                RefreshPanes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing help");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows file info for file under cursor (H key)
        /// </summary>
        public void ShowFileInfoForCursor()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                
                if (currentEntry == null)
                {
                    SetStatus("No file selected");
                    return;
                }
                
                if (currentEntry.IsDirectory)
                {
                    SetStatus($"[DIR] {currentEntry.Name} - Modified: {currentEntry.LastModified:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    SetStatus($"{currentEntry.Name} - Size: {FormatFileSize(currentEntry.Size)} - Modified: {currentEntry.LastModified:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing file info");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Views file under cursor as text (V key)
        /// </summary>
        public void ViewFileAsText()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                
                if (currentEntry == null || currentEntry.IsDirectory)
                {
                    SetStatus("No file selected");
                    return;
                }
                
                // Open text viewer
                _viewerManager.OpenTextViewer(currentEntry.FullPath, Encoding.UTF8);
                _currentMode = UiMode.TextViewer;
                
                // Create and show the text viewer window
                var textViewer = _viewerManager.CurrentTextViewer;
                if (textViewer != null)
                {
                    var config = _configProvider.LoadConfiguration();
                    var viewerWindow = new TWF.UI.TextViewerWindow(textViewer, _keyBindings, config);
                    Application.Run(viewerWindow);
                    
                    // After viewer closes, return to normal mode
                    _currentMode = UiMode.Normal;
                    _viewerManager.CloseCurrentViewer();
                    
                    // Hide cursor after closing viewer
                    try
                    {
                        Console.CursorVisible = false;
                    }
                    catch
                    {
                        // Ignore errors hiding cursor
                    }
                    
                    // Refresh panes
                    RefreshPanes();
                    
                    _logger.LogDebug("Text viewer closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing file as text");
                SetStatus($"Error: {ex.Message}");
                _currentMode = UiMode.Normal;
            }
        }

        /// <summary>
        /// Views file under cursor as hex (binary view)
        /// </summary>
        public void ViewFileAsHex()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                
                if (currentEntry == null || currentEntry.IsDirectory)
                {
                    SetStatus("No file selected");
                    return;
                }
                
                // Open text viewer
                _viewerManager.OpenTextViewer(currentEntry.FullPath, Encoding.UTF8);
                _currentMode = UiMode.TextViewer;
                
                // Create and show the text viewer window in hex mode
                var textViewer = _viewerManager.CurrentTextViewer;
                if (textViewer != null)
                {
                    var config = _configProvider.LoadConfiguration();
                    var viewerWindow = new TWF.UI.TextViewerWindow(textViewer, _keyBindings, config, startInHexMode: true);
                    Application.Run(viewerWindow);
                    
                    // After viewer closes, return to normal mode
                    _currentMode = UiMode.Normal;
                    _viewerManager.CloseCurrentViewer();
                    
                    // Hide cursor after closing viewer
                    try
                    {
                        Console.CursorVisible = false;
                    }
                    catch
                    {
                        // Ignore errors hiding cursor
                    }
                    
                    // Refresh panes
                    RefreshPanes();
                    
                    _logger.LogDebug("Hex viewer closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing file as hex");
                SetStatus($"Error: {ex.Message}");
                _currentMode = UiMode.Normal;
            }
        }
        

        /// <summary>
        /// Handles P key press - shows compression dialog and compresses files
        /// Compresses marked files or cursor file to opposite pane
        /// Shows compression ratio after completion
        /// </summary>
        public void HandleCompressionOperation()
        {
            var activePane = GetActivePane();
            var inactivePane = GetInactivePane();
            
            // Get files to compress (marked files or current file)
            var filesToCompress = activePane.GetMarkedEntries();
            if (filesToCompress.Count == 0)
            {
                var currentEntry = activePane.GetCurrentEntry();
                if (currentEntry != null)
                {
                    filesToCompress = new List<FileEntry> { currentEntry };
                }
            }
            
            if (filesToCompress.Count == 0)
            {
                SetStatus("No files to compress");
                return;
            }
            
            try
            {
                // Show compression dialog to select format and archive name
                var (archiveFormat, archiveName, confirmed) = ShowCompressionDialog(filesToCompress);
                
                if (!confirmed)
                {
                    SetStatus("Compression cancelled");
                    return;
                }
                
                // Determine the full archive path in the opposite pane
                var archivePath = Path.Combine(inactivePane.CurrentPath, archiveName);
                
                // Calculate original size for compression ratio
                long originalSize = filesToCompress.Sum(f => f.IsDirectory ? 0 : f.Size);
                
                // Execute compression with progress dialog
                ExecuteCompressionWithProgress(filesToCompress, archivePath, archiveFormat, originalSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in compression operation");
                SetStatus($"Compression error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows a dialog to select archive format and enter archive name
        /// Returns the selected format, archive name, and whether the user confirmed
        /// </summary>
        private (ArchiveFormat format, string archiveName, bool confirmed) ShowCompressionDialog(List<FileEntry> filesToCompress)
        {
            ArchiveFormat selectedFormat = ArchiveFormat.ZIP;
            string archiveName = string.Empty;
            bool confirmed = false;
            
            var dialog = new Dialog("Compress Files", 70, 16);
            
            var infoLabel = new Label($"Compressing {filesToCompress.Count} file(s)")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            dialog.Add(infoLabel);
            
            var formatLabel = new Label("Select archive format:")
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1)
            };
            dialog.Add(formatLabel);
            
            // Create list view for archive formats
            var formatOptions = new List<string> { "ZIP", "TAR", "TGZ (TAR.GZ)", "7Z", "RAR", "LZH", "CAB", "BZ2", "XZ", "LZMA" };
            var formatListView = new ListView(formatOptions)
            {
                X = 1,
                Y = 4,
                Width = Dim.Fill(1),
                Height = 5,
                AllowsMarking = false,
                CanFocus = true
            };
            formatListView.SelectedItem = 0; // Default to ZIP
            dialog.Add(formatListView);
            
            var nameLabel = new Label("Archive name:")
            {
                X = 1,
                Y = 10,
                Width = Dim.Fill(1)
            };
            dialog.Add(nameLabel);
            
            // Suggest a default archive name based on first file or "archive"
            var defaultName = filesToCompress.Count == 1 
                ? Path.GetFileNameWithoutExtension(filesToCompress[0].Name) 
                : "archive";
            
            var nameField = new TextField(defaultName)
            {
                X = 1,
                Y = 11,
                Width = Dim.Fill(1)
            };
            dialog.Add(nameField);
            
            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 13,
                IsDefault = true
            };
            
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 13
            };
            
            okButton.Clicked += () =>
            {
                // Map list selection to ArchiveFormat
                selectedFormat = formatListView.SelectedItem switch
                {
                    0 => ArchiveFormat.ZIP,
                    1 => ArchiveFormat.TAR,
                    2 => ArchiveFormat.TGZ,
                    3 => ArchiveFormat.SevenZip,
                    4 => ArchiveFormat.RAR,
                    5 => ArchiveFormat.LZH,
                    6 => ArchiveFormat.CAB,
                    7 => ArchiveFormat.BZ2,
                    8 => ArchiveFormat.XZ,
                    9 => ArchiveFormat.LZMA,
                    _ => ArchiveFormat.ZIP
                };
                
                archiveName = nameField.Text.ToString() ?? string.Empty;
                
                // Add appropriate extension if not present
                var extension = GetArchiveExtension(selectedFormat);
                if (!archiveName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    archiveName += extension;
                }
                
                confirmed = true;
                Application.RequestStop();
            };
            
            cancelButton.Clicked += () =>
            {
                confirmed = false;
                Application.RequestStop();
            };
            
            // Handle Escape key for cancellation
            dialog.KeyPress += (e) =>
            {
                if (e.KeyEvent.Key == (Key)27) // Escape
                {
                    confirmed = false;
                    Application.RequestStop();
                    e.Handled = true;
                }
            };
            
            dialog.Add(okButton);
            dialog.Add(cancelButton);
            
            // Set focus to the name field
            nameField.SetFocus();
            
            // Show dialog
            Application.Run(dialog);
            
            return (selectedFormat, archiveName, confirmed);
        }
        
        /// <summary>
        /// Gets the file extension for an archive format
        /// </summary>
        private string GetArchiveExtension(ArchiveFormat format)
        {
            return format switch
            {
                ArchiveFormat.ZIP => ".zip",
                ArchiveFormat.TAR => ".tar",
                ArchiveFormat.TGZ => ".tar.gz",
                ArchiveFormat.SevenZip => ".7z",
                ArchiveFormat.RAR => ".rar",
                ArchiveFormat.LZH => ".lzh",
                ArchiveFormat.CAB => ".cab",
                ArchiveFormat.BZ2 => ".bz2",
                ArchiveFormat.XZ => ".xz",
                ArchiveFormat.LZMA => ".lzma",
                _ => ".zip"
            };
        }
        
        /// <summary>
        /// Executes compression operation with progress dialog
        /// </summary>
        private void ExecuteCompressionWithProgress(
            List<FileEntry> files,
            string archivePath,
            ArchiveFormat format,
            long originalSize)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var progressDialog = new Dialog("Compression Progress", 70, 12);
            
            var statusLabel = new Label("Compressing...")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1)
            };
            progressDialog.Add(statusLabel);
            
            var fileLabel = new Label($"Creating archive: {Path.GetFileName(archivePath)}")
            {
                X = 1,
                Y = 2,
                Width = Dim.Fill(1)
            };
            progressDialog.Add(fileLabel);
            
            var progressLabel = new Label("Processing...")
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1)
            };
            progressDialog.Add(progressLabel);
            
            var cancelButton = new Button("Cancel (ESC)")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(2)
            };
            
            cancelButton.Clicked += () =>
            {
                cancellationTokenSource.Cancel();
                statusLabel.Text = "Cancelling...";
            };
            
            progressDialog.Add(cancelButton);
            
            // Handle Escape key for cancellation
            progressDialog.KeyPress += (e) =>
            {
                if (e.KeyEvent.Key == (Key)27) // Escape
                {
                    cancellationTokenSource.Cancel();
                    statusLabel.Text = "Cancelling...";
                    e.Handled = true;
                }
            };
            
            // Execute compression asynchronously
            Task.Run(async () =>
            {
                try
                {
                    var result = await _archiveManager.CompressAsync(
                        files,
                        archivePath,
                        format,
                        cancellationTokenSource.Token);
                    
                    Application.MainLoop.Invoke(() =>
                    {
                        Application.RequestStop();
                        
                        if (result.Success)
                        {
                            // Calculate compression ratio
                            long compressedSize = 0;
                            if (File.Exists(archivePath))
                            {
                                compressedSize = new FileInfo(archivePath).Length;
                            }
                            
                            double compressionRatio = originalSize > 0 
                                ? (1.0 - (double)compressedSize / originalSize) * 100 
                                : 0;
                            
                            SetStatus($"Compressed {result.FilesProcessed} file(s) - Ratio: {compressionRatio:F1}%");
                            
                            // Refresh both panes to show the new archive
                            LoadPaneDirectory(_leftState);
                            LoadPaneDirectory(_rightState);
                            RefreshPanes();
                        }
                        else
                        {
                            SetStatus($"Compression failed: {result.Message}");
                            
                            if (result.Errors.Count > 0)
                            {
                                var errorMsg = string.Join("\n", result.Errors.Take(5));
                                if (result.Errors.Count > 5)
                                {
                                    errorMsg += $"\n... and {result.Errors.Count - 5} more errors";
                                }
                                ShowMessageDialog("Compression Errors", errorMsg);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Application.MainLoop.Invoke(() =>
                    {
                        Application.RequestStop();
                        SetStatus($"Compression failed: {ex.Message}");
                        _logger.LogError(ex, "Compression operation failed");
                    });
                }
            });
            
            // Show the progress dialog
            Application.Run(progressDialog);
        }
        
        /// <summary>
        /// Handles F key press - enters incremental search mode
        /// Updates cursor position as characters are typed
        /// Supports Migemo if available
        /// </summary>
        public void EnterSearchMode()
        {
            try
            {
                var activePane = GetActivePane();
                
                if (activePane.Entries.Count == 0)
                {
                    SetStatus("No files to search");
                    return;
                }
                
                // Enter search mode
                _currentMode = UiMode.Search;
                _searchPattern = string.Empty;
                _searchStartIndex = activePane.CursorPosition;
                
                // Update status to show search mode
                SetStatus("Search: ");
                SetMessage("Type to search, Space=mark+next, Arrows=next/prev, Enter/Esc=exit");
                
                _logger.LogDebug("Entered search mode");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error entering search mode");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles character input during search mode
        /// Updates cursor position to first matching file
        /// </summary>
        public void HandleSearchInput(char character)
        {
            try
            {
                var activePane = GetActivePane();
                
                // Add character to search pattern
                _searchPattern += character;
                
                // Find first match from current position
                bool useMigemo = _searchEngine != null && _searchPattern.Length > 0;
                int matchIndex = _searchEngine?.FindNext(
                    activePane.Entries,
                    _searchPattern,
                    _searchStartIndex - 1, // Start from before current position to include it
                    useMigemo: useMigemo) ?? -1;
                
                if (matchIndex >= 0)
                {
                    // Move cursor to match
                    activePane.CursorPosition = matchIndex;
                    RefreshPanes();
                    SetStatus($"Search: {_searchPattern} (found)");
                }
                else
                {
                    // No match found
                    SetStatus($"Search: {_searchPattern} (not found)");
                }
                
                _logger.LogDebug($"Search pattern: '{_searchPattern}', match: {matchIndex}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling search input");
                SetStatus($"Search error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles backspace during search mode
        /// Removes last character from search pattern
        /// </summary>
        public void HandleSearchBackspace()
        {
            try
            {
                if (_searchPattern.Length > 0)
                {
                    // Remove last character
                    _searchPattern = _searchPattern.Substring(0, _searchPattern.Length - 1);
                    
                    if (_searchPattern.Length > 0)
                    {
                        // Find match with updated pattern
                        var activePane = GetActivePane();
                        bool useMigemo = _searchEngine != null && _searchPattern.Length > 0;
                        int matchIndex = _searchEngine?.FindNext(
                            activePane.Entries,
                            _searchPattern,
                            _searchStartIndex - 1,
                            useMigemo: useMigemo) ?? -1;
                        
                        if (matchIndex >= 0)
                        {
                            activePane.CursorPosition = matchIndex;
                            RefreshPanes();
                            SetStatus($"Search: {_searchPattern} (found)");
                        }
                        else
                        {
                            SetStatus($"Search: {_searchPattern} (not found)");
                        }
                    }
                    else
                    {
                        // Pattern is now empty, return to start position
                        var activePane = GetActivePane();
                        activePane.CursorPosition = _searchStartIndex;
                        RefreshPanes();
                        SetStatus("Search: ");
                    }
                }
                
                _logger.LogDebug($"Search pattern after backspace: '{_searchPattern}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling search backspace");
                SetStatus($"Search error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles Space key during search mode
        /// Toggles mark on current file and finds next match
        /// </summary>
        public void HandleSearchMarkAndNext()
        {
            try
            {
                var activePane = GetActivePane();
                
                if (activePane.Entries.Count == 0 || string.IsNullOrEmpty(_searchPattern))
                {
                    return;
                }
                
                // Toggle mark on current entry
                _markingEngine.ToggleMark(activePane, activePane.CursorPosition);
                
                // Find next match
                bool useMigemo = _searchEngine != null && _searchPattern.Length > 0;
                int nextMatch = _searchEngine?.FindNext(
                    activePane.Entries,
                    _searchPattern,
                    activePane.CursorPosition,
                    useMigemo: useMigemo) ?? -1;
                
                if (nextMatch >= 0)
                {
                    activePane.CursorPosition = nextMatch;
                    RefreshPanes();
                    SetStatus($"Search: {_searchPattern} (marked, next found)");
                }
                else
                {
                    RefreshPanes();
                    SetStatus($"Search: {_searchPattern} (marked, no more matches)");
                }
                
                _logger.LogDebug($"Marked file and moved to next match: {nextMatch}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling search mark and next");
                SetStatus($"Search error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles arrow down key during search mode
        /// Finds next match
        /// </summary>
        public void HandleSearchNext()
        {
            try
            {
                var activePane = GetActivePane();
                
                if (activePane.Entries.Count == 0 || string.IsNullOrEmpty(_searchPattern))
                {
                    return;
                }
                
                // Find next match
                bool useMigemo = _searchEngine != null && _searchPattern.Length > 0;
                int nextMatch = _searchEngine?.FindNext(
                    activePane.Entries,
                    _searchPattern,
                    activePane.CursorPosition,
                    useMigemo: useMigemo) ?? -1;
                
                if (nextMatch >= 0)
                {
                    activePane.CursorPosition = nextMatch;
                    RefreshPanes();
                    SetStatus($"Search: {_searchPattern} (next found)");
                }
                else
                {
                    SetStatus($"Search: {_searchPattern} (no more matches)");
                }
                
                _logger.LogDebug($"Search next: {nextMatch}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling search next");
                SetStatus($"Search error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles arrow up key during search mode
        /// Finds previous match
        /// </summary>
        public void HandleSearchPrevious()
        {
            try
            {
                var activePane = GetActivePane();
                
                if (activePane.Entries.Count == 0 || string.IsNullOrEmpty(_searchPattern))
                {
                    return;
                }
                
                // Find previous match
                bool useMigemo = _searchEngine != null && _searchPattern.Length > 0;
                int prevMatch = _searchEngine?.FindPrevious(
                    activePane.Entries,
                    _searchPattern,
                    activePane.CursorPosition,
                    useMigemo: useMigemo) ?? -1;
                
                if (prevMatch >= 0)
                {
                    activePane.CursorPosition = prevMatch;
                    RefreshPanes();
                    SetStatus($"Search: {_searchPattern} (previous found)");
                }
                else
                {
                    SetStatus($"Search: {_searchPattern} (no previous matches)");
                }
                
                _logger.LogDebug($"Search previous: {prevMatch}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling search previous");
                SetStatus($"Search error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Exits search mode
        /// Handles Enter or Escape key press during search
        /// </summary>
        public void ExitSearchMode()
        {
            try
            {
                // Exit search mode
                _currentMode = UiMode.Normal;
                _searchPattern = string.Empty;
                _searchStartIndex = 0;
                
                // Update status
                var activePane = GetActivePane();
                int markedCount = activePane.MarkedIndices.Count;
                
                if (markedCount > 0)
                {
                    SetStatus($"Search exited - {markedCount} file(s) marked");
                }
                else
                {
                    SetStatus("Search exited");
                }
                
                SetMessage("TWF Ready - Press F1 for help");
                
                _logger.LogDebug("Exited search mode");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting search mode");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets whether the application is currently in search mode
        /// </summary>
        public bool IsInSearchMode()
        {
            return _currentMode == UiMode.Search;
        }
        
        /// <summary>
        /// Handles Shift+W key press - determines whether to split or join files
        /// </summary>
        public void HandleFileSplitOrJoin()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                
                if (currentEntry == null)
                {
                    SetStatus("No file selected");
                    return;
                }
                
                // Check if this is a split file part (e.g., filename.001, filename.002)
                if (IsSplitFilePart(currentEntry.FullPath))
                {
                    // This is a split file part - offer to join
                    HandleFileJoin();
                }
                else if (!currentEntry.IsDirectory)
                {
                    // This is a regular file - offer to split
                    HandleFileSplit();
                }
                else
                {
                    SetStatus("Cannot split or join directories");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling file split/join");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a file is a split file part (e.g., filename.001, filename.002)
        /// </summary>
        private bool IsSplitFilePart(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var extension = Path.GetExtension(fileName);
            
            // Check if extension is .001, .002, etc. (3 digits)
            if (extension.Length == 4 && extension[0] == '.')
            {
                return extension.Substring(1).All(char.IsDigit);
            }
            
            return false;
        }
        
        /// <summary>
        /// Handles file split operation
        /// Shows dialog to get split size and calls FileOperations.SplitAsync
        /// </summary>
        public void HandleFileSplit()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                
                if (currentEntry == null || currentEntry.IsDirectory)
                {
                    SetStatus("No file selected for splitting");
                    return;
                }
                
                _logger.LogDebug($"Splitting file: {currentEntry.FullPath}");
                
                // Show split dialog to get part size
                var (partSize, outputDirectory, confirmed) = ShowFileSplitDialog(currentEntry);
                
                if (!confirmed)
                {
                    SetStatus("Split cancelled");
                    return;
                }
                
                // Execute split operation with progress dialog
                var cancellationTokenSource = new CancellationTokenSource();
                var progressDialog = new Dialog("Splitting File", 70, 12);
                
                var statusLabel = new Label("Splitting file...")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(statusLabel);
                
                var fileLabel = new Label($"File: {currentEntry.Name}")
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(fileLabel);
                
                var progressLabel = new Label("Progress: 0%")
                {
                    X = 1,
                    Y = 3,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(progressLabel);
                
                var currentFileLabel = new Label("")
                {
                    X = 1,
                    Y = 4,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(currentFileLabel);
                
                var cancelButton = new Button("Cancel (ESC)")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(2)
                };
                
                cancelButton.Clicked += () =>
                {
                    cancellationTokenSource.Cancel();
                    statusLabel.Text = "Cancelling...";
                };
                
                progressDialog.Add(cancelButton);
                
                // Handle Escape key for cancellation
                progressDialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == (Key)27) // Escape
                    {
                        cancellationTokenSource.Cancel();
                        statusLabel.Text = "Cancelling...";
                        e.Handled = true;
                    }
                };
                
                // Subscribe to progress events
                EventHandler<ProgressEventArgs>? progressHandler = null;
                progressHandler = (sender, args) =>
                {
                    Application.MainLoop.Invoke(() =>
                    {
                        progressLabel.Text = $"Progress: {args.PercentComplete:F1}%";
                        currentFileLabel.Text = $"Creating part: {args.CurrentFile}";
                    });
                };
                
                _fileOps.ProgressChanged += progressHandler;
                
                // Execute split asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await _fileOps.SplitAsync(
                            currentEntry.FullPath,
                            partSize,
                            outputDirectory,
                            cancellationTokenSource.Token);
                        
                        Application.MainLoop.Invoke(() =>
                        {
                            _fileOps.ProgressChanged -= progressHandler;
                            Application.RequestStop();
                            
                            if (result.Success)
                            {
                                SetStatus($"Split into {result.FilesProcessed} part(s) successfully");
                                
                                // Refresh the pane to show split files
                                LoadPaneDirectory(activePane);
                                RefreshPanes();
                            }
                            else
                            {
                                SetStatus($"Split failed: {result.Message}");
                                
                                if (result.Errors.Count > 0)
                                {
                                    var errorMsg = string.Join("\n", result.Errors.Take(5));
                                    if (result.Errors.Count > 5)
                                    {
                                        errorMsg += $"\n... and {result.Errors.Count - 5} more errors";
                                    }
                                    ShowMessageDialog("Split Errors", errorMsg);
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.MainLoop.Invoke(() =>
                        {
                            _fileOps.ProgressChanged -= progressHandler;
                            Application.RequestStop();
                            SetStatus($"Split failed: {ex.Message}");
                            _logger.LogError(ex, "File split failed");
                        });
                    }
                });
                
                // Show the progress dialog
                Application.Run(progressDialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error splitting file");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows a dialog for file split configuration
        /// Returns (partSize, outputDirectory, confirmed)
        /// </summary>
        private (long partSize, string outputDirectory, bool confirmed) ShowFileSplitDialog(FileEntry file)
        {
            long partSize = 1024 * 1024; // Default 1MB
            string outputDirectory = GetInactivePane().CurrentPath;
            bool confirmed = false;
            
            var dialog = new Dialog("Split File", 70, 16);
            
            // File info
            var fileInfo = new Label($"File: {file.Name} ({FormatFileSize(file.Size)})")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
                }
            };
            dialog.Add(fileInfo);
            
            // Part size label
            var sizeLabel = new Label("Part size (bytes):")
            {
                X = 1,
                Y = 3
            };
            dialog.Add(sizeLabel);
            
            // Part size input
            var sizeField = new TextField("1048576") // 1MB default
            {
                X = 25,
                Y = 3,
                Width = 20
            };
            dialog.Add(sizeField);
            
            // Common size buttons
            var size1MB = new Button("1 MB")
            {
                X = 1,
                Y = 5
            };
            size1MB.Clicked += () => sizeField.Text = (1024 * 1024).ToString();
            dialog.Add(size1MB);
            
            var size10MB = new Button("10 MB")
            {
                X = 10,
                Y = 5
            };
            size10MB.Clicked += () => sizeField.Text = (10 * 1024 * 1024).ToString();
            dialog.Add(size10MB);
            
            var size100MB = new Button("100 MB")
            {
                X = 20,
                Y = 5
            };
            size100MB.Clicked += () => sizeField.Text = (100 * 1024 * 1024).ToString();
            dialog.Add(size100MB);
            
            var size1GB = new Button("1 GB")
            {
                X = 32,
                Y = 5
            };
            size1GB.Clicked += () => sizeField.Text = (1024 * 1024 * 1024).ToString();
            dialog.Add(size1GB);
            
            // Output directory label
            var dirLabel = new Label("Output directory:")
            {
                X = 1,
                Y = 7
            };
            dialog.Add(dirLabel);
            
            // Output directory field
            var dirField = new TextField(outputDirectory)
            {
                X = 1,
                Y = 8,
                Width = Dim.Fill(1)
            };
            dialog.Add(dirField);
            
            var hint = new Label("(Split parts will be created in the output directory)")
            {
                X = 1,
                Y = 9,
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black)
                }
            };
            dialog.Add(hint);
            
            // Buttons
            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 11,
                IsDefault = true
            };
            
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 11
            };
            
            okButton.Clicked += () =>
            {
                confirmed = true;
                Application.RequestStop();
            };
            
            cancelButton.Clicked += () =>
            {
                Application.RequestStop();
            };
            
            dialog.Add(okButton);
            dialog.Add(cancelButton);
            
            // Set focus to size field
            sizeField.SetFocus();
            
            // Show dialog
            Application.Run(dialog);
            
            // Parse the input
            if (confirmed)
            {
                if (long.TryParse(sizeField.Text.ToString(), out long parsedSize) && parsedSize > 0)
                {
                    partSize = parsedSize;
                }
                else
                {
                    confirmed = false;
                    SetStatus("Invalid part size");
                }
                
                outputDirectory = dirField.Text.ToString() ?? outputDirectory;
            }
            
            return (partSize, outputDirectory, confirmed);
        }
        
        /// <summary>
        /// Handles file join operation
        /// Shows dialog to select part files and calls FileOperations.JoinAsync
        /// </summary>
        public void HandleFileJoin()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                
                if (currentEntry == null)
                {
                    SetStatus("No file selected for joining");
                    return;
                }
                
                _logger.LogDebug($"Joining split files starting with: {currentEntry.FullPath}");
                
                // Find all related split file parts
                var partFiles = FindSplitFileParts(currentEntry.FullPath);
                
                if (partFiles.Count == 0)
                {
                    SetStatus("No split file parts found");
                    return;
                }
                
                // Show join dialog to get output file name
                var (outputFile, confirmed) = ShowFileJoinDialog(currentEntry, partFiles);
                
                if (!confirmed)
                {
                    SetStatus("Join cancelled");
                    return;
                }
                
                // Execute join operation with progress dialog
                var cancellationTokenSource = new CancellationTokenSource();
                var progressDialog = new Dialog("Joining Files", 70, 12);
                
                var statusLabel = new Label("Joining file parts...")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(statusLabel);
                
                var fileLabel = new Label($"Output: {Path.GetFileName(outputFile)}")
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(fileLabel);
                
                var progressLabel = new Label("Progress: 0%")
                {
                    X = 1,
                    Y = 3,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(progressLabel);
                
                var currentFileLabel = new Label("")
                {
                    X = 1,
                    Y = 4,
                    Width = Dim.Fill(1)
                };
                progressDialog.Add(currentFileLabel);
                
                var cancelButton = new Button("Cancel (ESC)")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(2)
                };
                
                cancelButton.Clicked += () =>
                {
                    cancellationTokenSource.Cancel();
                    statusLabel.Text = "Cancelling...";
                };
                
                progressDialog.Add(cancelButton);
                
                // Handle Escape key for cancellation
                progressDialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == (Key)27) // Escape
                    {
                        cancellationTokenSource.Cancel();
                        statusLabel.Text = "Cancelling...";
                        e.Handled = true;
                    }
                };
                
                // Subscribe to progress events
                EventHandler<ProgressEventArgs>? progressHandler = null;
                progressHandler = (sender, args) =>
                {
                    Application.MainLoop.Invoke(() =>
                    {
                        progressLabel.Text = $"Progress: {args.PercentComplete:F1}%";
                        currentFileLabel.Text = $"Processing: {args.CurrentFile}";
                    });
                };
                
                _fileOps.ProgressChanged += progressHandler;
                
                // Execute join asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await _fileOps.JoinAsync(
                            partFiles,
                            outputFile,
                            cancellationTokenSource.Token);
                        
                        Application.MainLoop.Invoke(() =>
                        {
                            _fileOps.ProgressChanged -= progressHandler;
                            Application.RequestStop();
                            
                            if (result.Success)
                            {
                                SetStatus($"Joined {result.FilesProcessed} part(s) successfully");
                                
                                // Refresh the pane to show joined file
                                LoadPaneDirectory(activePane);
                                RefreshPanes();
                            }
                            else
                            {
                                SetStatus($"Join failed: {result.Message}");
                                
                                if (result.Errors.Count > 0)
                                {
                                    var errorMsg = string.Join("\n", result.Errors.Take(5));
                                    if (result.Errors.Count > 5)
                                    {
                                        errorMsg += $"\n... and {result.Errors.Count - 5} more errors";
                                    }
                                    ShowMessageDialog("Join Errors", errorMsg);
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.MainLoop.Invoke(() =>
                        {
                            _fileOps.ProgressChanged -= progressHandler;
                            Application.RequestStop();
                            SetStatus($"Join failed: {ex.Message}");
                            _logger.LogError(ex, "File join failed");
                        });
                    }
                });
                
                // Show the progress dialog
                Application.Run(progressDialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error joining files");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Finds all split file parts related to a given split file
        /// </summary>
        private List<string> FindSplitFileParts(string splitFilePath)
        {
            var parts = new List<string>();
            
            try
            {
                var directory = Path.GetDirectoryName(splitFilePath);
                if (string.IsNullOrEmpty(directory))
                {
                    return parts;
                }
                
                var fileName = Path.GetFileName(splitFilePath);
                var extension = Path.GetExtension(fileName);
                
                // Get the base name without the .001, .002, etc. extension
                var baseName = fileName.Substring(0, fileName.Length - extension.Length);
                
                // Find all files matching the pattern
                var allFiles = Directory.GetFiles(directory, $"{baseName}.*");
                
                foreach (var file in allFiles)
                {
                    if (IsSplitFilePart(file))
                    {
                        parts.Add(file);
                    }
                }
                
                // Sort by file name to ensure correct order
                parts.Sort();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding split file parts for: {splitFilePath}");
            }
            
            return parts;
        }
        
        /// <summary>
        /// Shows a dialog for file join configuration
        /// Returns (outputFile, confirmed)
        /// </summary>
        private (string outputFile, bool confirmed) ShowFileJoinDialog(FileEntry file, List<string> partFiles)
        {
            string outputFile = string.Empty;
            bool confirmed = false;
            
            // Generate default output file name (remove .001 extension)
            var fileName = Path.GetFileName(file.FullPath);
            var extension = Path.GetExtension(fileName);
            var baseName = fileName.Substring(0, fileName.Length - extension.Length);
            var directory = GetInactivePane().CurrentPath;
            outputFile = Path.Combine(directory, baseName);
            
            var dialog = new Dialog("Join Split Files", 70, 18);
            
            // Part files info
            var infoLabel = new Label($"Found {partFiles.Count} part file(s) to join:")
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightCyan, Color.Black)
                }
            };
            dialog.Add(infoLabel);
            
            // List first few part files
            var partsList = new Label(string.Join("\n", partFiles.Take(5).Select(Path.GetFileName)))
            {
                X = 3,
                Y = 2,
                Width = Dim.Fill(1),
                Height = 5
            };
            dialog.Add(partsList);
            
            if (partFiles.Count > 5)
            {
                var moreLabel = new Label($"... and {partFiles.Count - 5} more")
                {
                    X = 3,
                    Y = 7,
                    Width = Dim.Fill(1)
                };
                dialog.Add(moreLabel);
            }
            
            // Output file label
            var outputLabel = new Label("Output file:")
            {
                X = 1,
                Y = 9
            };
            dialog.Add(outputLabel);
            
            // Output file field
            var outputField = new TextField(outputFile)
            {
                X = 1,
                Y = 10,
                Width = Dim.Fill(1)
            };
            dialog.Add(outputField);
            
            var hint = new Label("(The joined file will be created in the output location)")
            {
                X = 1,
                Y = 11,
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black)
                }
            };
            dialog.Add(hint);
            
            // Buttons
            var okButton = new Button("OK")
            {
                X = Pos.Center() - 10,
                Y = 13,
                IsDefault = true
            };
            
            var cancelButton = new Button("Cancel")
            {
                X = Pos.Center() + 2,
                Y = 13
            };
            
            okButton.Clicked += () =>
            {
                confirmed = true;
                Application.RequestStop();
            };
            
            cancelButton.Clicked += () =>
            {
                Application.RequestStop();
            };
            
            dialog.Add(okButton);
            dialog.Add(cancelButton);
            
            // Set focus to output field
            outputField.SetFocus();
            
            // Show dialog
            Application.Run(dialog);
            
            // Get the output file path
            if (confirmed)
            {
                outputFile = outputField.Text.ToString() ?? outputFile;
                
                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    confirmed = false;
                    SetStatus("Invalid output file name");
                }
            }
            
            return (outputFile, confirmed);
        }
        
        /// <summary>
        /// Handles backtick (`) key press - shows context menu dialog
        /// Displays menu items based on current selection and executes selected operation
        /// </summary>
        public void HandleContextMenu()
        {
            try
            {
                var activePane = GetActivePane();
                var currentEntry = activePane.GetCurrentEntry();
                bool hasMarkedFiles = activePane.MarkedIndices.Count > 0;
                
                _logger.LogDebug($"Opening context menu for entry: {currentEntry?.Name ?? "(none)"}, marked files: {hasMarkedFiles}");
                
                // Get context menu items from ListProvider
                var menuItems = _listProvider.GetContextMenu(currentEntry, hasMarkedFiles);
                
                if (menuItems.Count == 0)
                {
                    SetStatus("No menu items available");
                    return;
                }
                
                // Create context menu dialog
                var dialog = new Dialog("Context Menu", 60, Math.Min(menuItems.Count + 6, 25));
                
                var label = new Label("Select an operation:")
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1)
                };
                dialog.Add(label);
                
                // Create list view with menu items
                var menuList = new ListView()
                {
                    X = 1,
                    Y = 2,
                    Width = Dim.Fill(1),
                    Height = Dim.Fill(3),
                    AllowsMarking = false
                };
                
                // Format menu items for display (skip separators)
                var displayItems = new List<string>();
                var actionableItems = new List<TWF.Models.MenuItem>();
                
                foreach (var item in menuItems)
                {
                    if (item.IsSeparator)
                    {
                        displayItems.Add("─────────────────────────────────");
                        actionableItems.Add(item); // Keep separator in list for index alignment
                    }
                    else
                    {
                        var displayText = item.Label;
                        if (!string.IsNullOrEmpty(item.Shortcut))
                        {
                            displayText += $" ({item.Shortcut})";
                        }
                        displayItems.Add(displayText);
                        actionableItems.Add(item);
                    }
                }
                
                menuList.SetSource(displayItems);
                dialog.Add(menuList);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center() - 10,
                    Y = Pos.AnchorEnd(1),
                    IsDefault = true
                };
                
                var cancelButton = new Button("Cancel")
                {
                    X = Pos.Center() + 2,
                    Y = Pos.AnchorEnd(1)
                };
                
                bool okPressed = false;
                
                okButton.Clicked += () =>
                {
                    okPressed = true;
                    Application.RequestStop();
                };
                
                cancelButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                // Handle Escape key for cancellation
                dialog.KeyPress += (e) =>
                {
                    if (e.KeyEvent.Key == (Key)27) // Escape
                    {
                        Application.RequestStop();
                        e.Handled = true;
                    }
                };
                
                dialog.Add(okButton);
                dialog.Add(cancelButton);
                
                // Set focus to the list
                menuList.SetFocus();
                
                // Show dialog
                Application.Run(dialog);
                
                // Execute selected operation
                if (okPressed && menuList.SelectedItem >= 0 && menuList.SelectedItem < actionableItems.Count)
                {
                    var selectedItem = actionableItems[menuList.SelectedItem];
                    
                    // Skip if separator was selected
                    if (selectedItem.IsSeparator)
                    {
                        SetStatus("Invalid selection");
                        return;
                    }
                    
                    ExecuteContextMenuAction(selectedItem.Action);
                }
                else
                {
                    SetStatus("Context menu cancelled");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing context menu");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Executes the action associated with a context menu item
        /// Routes to existing handler methods based on action name
        /// </summary>
        private void ExecuteContextMenuAction(string action)
        {
            try
            {
                _logger.LogDebug($"Executing context menu action: {action}");
                
                switch (action)
                {
                    case "Navigate":
                        HandleEnterKey();
                        break;
                        
                    case "Execute":
                        HandleEnterKey();
                        break;
                        
                    case "OpenEditor":
                        HandleShiftEnter();
                        break;
                        
                    case "Copy":
                        HandleCopyOperation();
                        break;
                        
                    case "Move":
                        HandleMoveOperation();
                        break;
                        
                    case "Delete":
                        HandleDeleteOperation();
                        break;
                        
                    case "Rename":
                        HandlePatternRename();
                        break;
                        
                    case "ClearMarks":
                        var activePane = GetActivePane();
                        _markingEngine.ClearMarks(activePane);
                        RefreshPanes();
                        SetStatus("Marks cleared");
                        break;
                        
                    case "BrowseArchive":
                        HandleEnterKey();
                        break;
                        
                    case "ExtractArchive":
                        HandleArchiveExtraction();
                        break;
                        
                    case "Compress":
                        HandleCompressionOperation();
                        break;
                        
                    case "ViewText":
                        HandleEnterKey();
                        break;
                        
                    case "ViewImage":
                        HandleEnterKey();
                        break;
                        
                    case "Compare":
                        HandleFileComparison();
                        break;
                        
                    case "Split":
                        HandleFileSplitOrJoin();
                        break;
                        
                    case "Join":
                        HandleFileSplitOrJoin();
                        break;
                        
                    case "Refresh":
                        LoadPaneDirectory(GetActivePane());
                        RefreshPanes();
                        SetStatus("Refreshed");
                        break;
                        
                    case "CreateDirectory":
                        HandleCreateDirectory();
                        break;
                        
                    case "Properties":
                        ShowFileProperties();
                        break;
                        
                    default:
                        SetStatus($"Unknown action: {action}");
                        _logger.LogWarning($"Unknown context menu action: {action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing context menu action: {action}");
                SetStatus($"Error executing action: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Shows file properties dialog
        /// </summary>
        private void ShowFileProperties()
        {
            var activePane = GetActivePane();
            var currentEntry = activePane.GetCurrentEntry();
            
            if (currentEntry == null)
            {
                SetStatus("No file selected");
                return;
            }
            
            try
            {
                var dialog = new Dialog("File Properties", 70, 18);
                
                var properties = new StringBuilder();
                properties.AppendLine($"Name: {currentEntry.Name}");
                properties.AppendLine($"Path: {currentEntry.FullPath}");
                properties.AppendLine($"Type: {(currentEntry.IsDirectory ? "Directory" : "File")}");
                
                if (!currentEntry.IsDirectory)
                {
                    properties.AppendLine($"Size: {FormatFileSize(currentEntry.Size)} ({currentEntry.Size:N0} bytes)");
                    properties.AppendLine($"Extension: {currentEntry.Extension}");
                }
                
                properties.AppendLine($"Modified: {currentEntry.LastModified:yyyy-MM-dd HH:mm:ss}");
                properties.AppendLine($"Attributes: {currentEntry.Attributes}");
                
                if (currentEntry.IsArchive)
                {
                    properties.AppendLine("Archive: Yes");
                }
                
                var propertiesLabel = new Label(properties.ToString())
                {
                    X = 1,
                    Y = 1,
                    Width = Dim.Fill(1),
                    Height = Dim.Fill(3)
                };
                dialog.Add(propertiesLabel);
                
                var okButton = new Button("OK")
                {
                    X = Pos.Center(),
                    Y = Pos.AnchorEnd(1),
                    IsDefault = true
                };
                
                okButton.Clicked += () =>
                {
                    Application.RequestStop();
                };
                
                dialog.Add(okButton);
                
                Application.Run(dialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing file properties");
                SetStatus($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Formats a file size in bytes to a human-readable string
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
        
        /// <summary>
        /// Launches the external configuration program to edit the configuration file
        /// Handles Y key press
        /// </summary>
        public void HandleLaunchConfigurationProgram()
        {
            try
            {
                _logger.LogDebug("Launching configuration program");
                
                // Load configuration to get the program path
                var config = _configProvider.LoadConfiguration();
                var programPath = config.ConfigurationProgramPath;
                
                if (string.IsNullOrWhiteSpace(programPath))
                {
                    programPath = "notepad.exe"; // Default fallback
                    _logger.LogWarning("ConfigurationProgramPath is empty, using default: notepad.exe");
                }
                
                // Get the configuration file path
                var configFilePath = _configProvider.GetConfigFilePath();
                
                if (!File.Exists(configFilePath))
                {
                    SetStatus($"Configuration file not found: {configFilePath}");
                    _logger.LogWarning($"Configuration file not found: {configFilePath}");
                    return;
                }
                
                // Launch the external program with the config file as argument
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = programPath,
                    Arguments = $"\"{configFilePath}\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                
                System.Diagnostics.Process.Start(processStartInfo);
                
                SetStatus($"Launched {programPath} to edit configuration");
                _logger.LogInformation($"Successfully launched {programPath} with config file: {configFilePath}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Program not found or access denied
                _logger.LogError(ex, "Failed to launch configuration program");
                SetStatus($"Error: Program not found or access denied - {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error launching configuration program");
                SetStatus($"Error launching configuration program: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets the display mode to Details (the default detailed view)
        /// </summary>
        private void SetDisplayModeDetailed()
        {
            SwitchDisplayMode(DisplayMode.Details);
        }
    }
    
}


