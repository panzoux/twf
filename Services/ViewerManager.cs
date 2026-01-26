using System.Text;
namespace TWF.Services
{
    /// <summary>
    /// Manages text viewers
    /// </summary>
    public class ViewerManager
    {
        private LargeFileEngine? _currentFileEngine;
        private readonly SearchEngine _searchEngine;

        public ViewerManager(SearchEngine searchEngine)
        {
            _searchEngine = searchEngine ?? throw new ArgumentNullException(nameof(searchEngine));
        }

        /// <summary>
        /// Opens a text file in the text viewer (using LargeFileEngine)
        /// </summary>
        /// <param name="filePath">Path to the text file</param>
        /// <param name="settings">Viewer settings for auto-detection</param>
        /// <param name="encoding">Initial encoding to use</param>
        public void OpenTextViewer(string filePath, TWF.Models.ViewerSettings settings, Encoding? encoding = null)
        {
            CloseCurrentViewer();
            _currentFileEngine = new LargeFileEngine(filePath);
            _currentFileEngine.Initialize(settings, encoding);
            // Start indexing in background
            _currentFileEngine.StartIndexing();
        }

        /// <summary>
        /// Closes the currently open viewer
        /// </summary>
        public void CloseCurrentViewer()
        {
            if (_currentFileEngine != null)
            {
                _currentFileEngine.Dispose();
                _currentFileEngine = null;
            }
        }

        /// <summary>
        /// Gets the current file engine if one is open
        /// </summary>
        public LargeFileEngine? CurrentTextViewer => _currentFileEngine;

        /// <summary>
        /// Gets the search engine
        /// </summary>
        public SearchEngine SearchEngine => _searchEngine;
    }

    // TextViewer class removed in favor of LargeFileEngine
}
