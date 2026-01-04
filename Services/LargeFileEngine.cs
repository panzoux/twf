using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TWF.Services
{
    public enum FileViewMode
    {
        Text,
        Hex
    }

    /// <summary>
    /// Engine for handling large files with lazy loading and asynchronous indexing.
    /// </summary>
    public class LargeFileEngine : IDisposable
    {
        private readonly string _filePath;
        private FileStream? _fileStream;
        private readonly object _streamLock = new object();
        private const int BufferSize = 64 * 1024; // 64KB read buffer

        // Text Mode State
        private readonly List<long> _lineOffsets = new List<long>();
        private CancellationTokenSource? _indexingCts;
        private bool _isIndexing = false;
        private long _indexedLength = 0;
        private Encoding _encoding = Encoding.UTF8;
        
        // Stats
        public string FilePath => _filePath;
        public long FileSize { get; private set; }
        public int LineCount { get { lock (_lineOffsets) return _lineOffsets.Count; } }
        public bool IsIndexing => _isIndexing;
        public double IndexingProgress => FileSize > 0 ? (double)Interlocked.Read(ref _indexedLength) / FileSize : 0;
        public Encoding CurrentEncoding => _encoding;

        public event EventHandler? IndexingProgressChanged;
        public event EventHandler? IndexingCompleted;

        public LargeFileEngine(string filePath)
        {
            _filePath = filePath;
            FileSize = new FileInfo(filePath).Length;
            _lineOffsets.Add(0); // Line 0 starts at offset 0
        }

        public void Initialize(Encoding? encoding = null)
        {
            if (encoding != null) _encoding = encoding;

            // Open with FileShare.ReadWrite to allow viewing logs that are being written to
            _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize);
        }

        public void SetEncoding(Encoding encoding)
        {
            _encoding = encoding;
            // Re-indexing might be required if encoding changes line endings or char widths significantly?
            // For UTF-8/ASCII/Latin1, newline byte 0x0A is usually stable. 
            // For UTF-16, it's 0x000A.
            // For simplicity, we restart indexing if encoding changes.
            StartIndexing();
        }

        /// <summary>
        /// Starts or restarts the line indexing background task.
        /// </summary>
        public void StartIndexing()
        {
            CancelIndexing();

            _indexingCts = new CancellationTokenSource();
            _isIndexing = true;
            lock (_lineOffsets)
            {
                _lineOffsets.Clear();
                _lineOffsets.Add(0);
            }
            Interlocked.Exchange(ref _indexedLength, 0);

            Task.Run(() => IndexFileWorker(_indexingCts.Token));
        }

        public void CancelIndexing()
        {
            if (_indexingCts != null)
            {
                _indexingCts.Cancel();
                _indexingCts.Dispose();
                _indexingCts = null;
            }
            _isIndexing = false;
        }

        private void IndexFileWorker(CancellationToken token)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Starting indexing for {_filePath} (Size: {FileSize})");
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize))
                {
                    byte[] buffer = new byte[BufferSize];
                    long position = 0;
                    int bytesRead;

                    // Simple scan for '\n' (0x0A). 
                    // This works for UTF-8, ASCII, Latin1. 
                    // TODO: Handle UTF-16/32 appropriately if needed.
                    
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (token.IsCancellationRequested) 
                        {
                            System.Diagnostics.Debug.WriteLine("[LargeFileEngine] Indexing cancelled.");
                            break;
                        }

                        for (int i = 0; i < bytesRead; i++)
                        {
                            if (buffer[i] == 10) // \n
                            {
                                lock (_lineOffsets)
                                {
                                    _lineOffsets.Add(position + i + 1);
                                }
                            }
                        }

                        position += bytesRead;
                        Interlocked.Exchange(ref _indexedLength, position);

                        // Throttle progress updates
                        if (position % (1024 * 1024) == 0) // Every 1MB
                        {
                            System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Indexing progress: {_indexedLength}/{FileSize} ({IndexingProgress:P1})");
                            IndexingProgressChanged?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Indexing error: {ex.Message}");
            }
            finally
            {
                _isIndexing = false;
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Indexing completed/stopped. Final lines: {_lineOffsets.Count}");
                IndexingCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Reads lines for text view.
        /// </summary>
        public List<string> GetTextLines(int startLine, int count)
        {
            var lines = new List<string>();
            if (_fileStream == null) return lines;

            long startOffset = 0;
            long endOffset = 0;

            lock (_lineOffsets)
            {
                if (startLine >= _lineOffsets.Count) return lines; // Requesting beyond indexed area
                startOffset = _lineOffsets[startLine];

                int lastLineIndex = startLine + count;
                if (lastLineIndex < _lineOffsets.Count)
                {
                    endOffset = _lineOffsets[lastLineIndex];
                }
                else
                {
                    // Reading past the last known line (or end of file)
                    // If indexing finished, use FileSize. If not, use last known offset?
                    // Better: just read to FileSize or some reasonable limit
                    endOffset = FileSize; // Or _fileStream.Length if it grew
                }
            }

            int length = (int)(endOffset - startOffset);
            if (length <= 0) return lines;
            if (length > 10 * 1024 * 1024) length = 10 * 1024 * 1024; // Cap read at 10MB just in case

            byte[] buffer = new byte[length];
            lock (_streamLock)
            {
                _fileStream.Seek(startOffset, SeekOrigin.Begin);
                _fileStream.Read(buffer, 0, length);
            }

            // Decode
            string text = _encoding.GetString(buffer);
            
            // Split into lines. 
            // Note: GetString might create one huge string.
            // Using a StreamParser would be more efficient for memory but complex.
            // For now, this is much better than reading the *whole* file.
            
            // Split handles \r\n, \r, \n.
            // We just need to be careful: the buffer might end in the middle of a newline or line.
            // But our offsets point to starts of lines (after \n). 
            // So startOffset is valid. endOffset is start of line (startLine + count).
            // So buffer contains exactly 'count' lines, except maybe the last one if EOF.
            
            using (var reader = new StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null && lines.Count < count)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        /// <summary>
        /// Reads bytes for hex view.
        /// </summary>
        public byte[] GetBytes(long offset, int length)
        {
            if (_fileStream == null) return Array.Empty<byte>();

            byte[] buffer = new byte[length];
            int read = 0;
            lock (_streamLock)
            {
                if (offset >= _fileStream.Length) return Array.Empty<byte>();
                _fileStream.Seek(offset, SeekOrigin.Begin);
                read = _fileStream.Read(buffer, 0, length);
            }

            if (read < length)
            {
                Array.Resize(ref buffer, read);
            }
            return buffer;
        }

        /// <summary>
        /// Searches for a pattern in the file and returns a list of line numbers.
        /// This is a simple implementation; a robust one would use Boyer-Moore and handle cross-buffer matches.
        /// </summary>
        public List<int> Search(string pattern)
        {
            var matches = new List<int>();
            if (string.IsNullOrEmpty(pattern) || _fileStream == null) return matches;

            // Wait for indexing to complete or at least use what we have? 
            // For correct line numbers, we need indexing.
            // If indexing is running, we might miss lines or return wrong line numbers if we scan ahead.
            // For now, assume we search only what is indexed or we just scan the text content we can read.
            
            // Naive line-by-line search using the index
            // This allows us to reuse GetTextLines logic and Encoding
            // But reading line-by-line is slow for 1GB.
            
            // Better: Read chunks, find string, map to line number.
            // Simplified approach for the prototype:
            // Read 1MB chunks, search string in string (using current encoding).
            
            // NOTE: This simple implementation might miss matches crossing chunk boundaries.
            
            byte[] buffer = new byte[1024 * 1024]; // 1MB
            long position = 0;
            long fileLength = FileSize;
            
            // Convert pattern to bytes for searching in byte stream? 
            // Or convert buffer to string? Converting buffer to string is easier for "Text" search (case ignore etc).
            
            lock (_streamLock)
            {
                _fileStream.Seek(0, SeekOrigin.Begin);
            }

            while (position < fileLength)
            {
                int bytesRead;
                lock (_streamLock)
                {
                    _fileStream.Seek(position, SeekOrigin.Begin);
                    bytesRead = _fileStream.Read(buffer, 0, buffer.Length);
                }
                
                if (bytesRead == 0) break;

                string chunk = _encoding.GetString(buffer, 0, bytesRead);
                int matchIndex = chunk.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                
                while (matchIndex >= 0)
                {
                    long matchAbsOffset = position + _encoding.GetByteCount(chunk.Substring(0, matchIndex));
                    
                    // Map offset to line number
                    // Binary search in _lineOffsets
                    int lineIndex = -1;
                    lock (_lineOffsets)
                    {
                        lineIndex = _lineOffsets.BinarySearch(matchAbsOffset);
                        if (lineIndex < 0) lineIndex = ~lineIndex - 1;
                    }
                    
                    if (lineIndex >= 0 && !matches.Contains(lineIndex))
                    {
                        matches.Add(lineIndex);
                    }

                    matchIndex = chunk.IndexOf(pattern, matchIndex + 1, StringComparison.OrdinalIgnoreCase);
                }

                // Overlap chunks to handle boundary matches? (Length of pattern)
                // For this prototype, we skip overlap logic.
                
                position += bytesRead;
            }

            return matches;
        }

        public void Dispose()
        {
            CancelIndexing();
            _fileStream?.Dispose();
        }
    }
}
