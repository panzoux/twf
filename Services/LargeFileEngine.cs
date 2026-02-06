using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TWF.Models;

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
        private int _bomLength = 0;
        private Encoding _encoding = Encoding.UTF8;
        private int _currentEncodingIndex = 0;
        private List<Encoding> _supportedEncodings = new List<Encoding>();

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

        public void Initialize(ViewerSettings settings, Encoding? manualEncoding = null)
        {
            // Detect BOM length
            _bomLength = 0;
            try
            {
                byte[] bomBuffer = new byte[4];
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int count = fs.Read(bomBuffer, 0, 4);
                    if (count >= 3 && bomBuffer[0] == 0xEF && bomBuffer[1] == 0xBB && bomBuffer[2] == 0xBF)
                        _bomLength = 3;
                    else if (count >= 2 && bomBuffer[0] == 0xFF && bomBuffer[1] == 0xFE)
                        _bomLength = 2;
                    else if (count >= 2 && bomBuffer[0] == 0xFE && bomBuffer[1] == 0xFF)
                        _bomLength = 2;
                }
            }
            catch { }

            // Initialize supported encodings from priority list
            _supportedEncodings.Clear();
            foreach (var name in settings.EncodingPriority)
            {
                try { _supportedEncodings.Add(Encoding.GetEncoding(name)); } catch { }
            }

            // Fallback to reasonable defaults if list is empty
            if (_supportedEncodings.Count == 0)
            {
                _supportedEncodings.Add(Encoding.UTF8);
                _supportedEncodings.Add(Encoding.ASCII);
            }

            if (manualEncoding != null)
            {
                _encoding = manualEncoding;
            }
            else if (settings.AutoDetectEncoding)
            {
                _encoding = DetectEncoding();
            }
            else
            {
                _encoding = _supportedEncodings[0];
            }

            _currentEncodingIndex = _supportedEncodings.FindIndex(e => e.CodePage == _encoding.CodePage);
            if (_currentEncodingIndex < 0) _currentEncodingIndex = 0;

            // Open with FileShare.ReadWrite to allow viewing logs that are being written to
            _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize);
        }

        private Encoding DetectEncoding()
        {
            if (FileSize == 0) return _supportedEncodings[0];

            byte[] buffer = new byte[Math.Min(16384, (int)FileSize)];
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Read(buffer, 0, buffer.Length);
            }

            // 1. Check BOM
            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            {
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] BOM Detected: UTF-8");
                return Encoding.UTF8;
            }
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            {
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] BOM Detected: UTF-16LE");
                return Encoding.Unicode;
            }
            if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            {
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] BOM Detected: UTF-16BE");
                return Encoding.BigEndianUnicode;
            }

            // 2. Strict UTF-8 Validation
            bool isValidUtf8 = IsBufferValidUtf8(buffer, out bool hasMultiByte);
            System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] UTF-8 Check: Valid={{isValidUtf8}}, MultiByte={{hasMultiByte}}");
            
            if (isValidUtf8)
            {
                var result = hasMultiByte ? Encoding.UTF8 : (_supportedEncodings.Find(e => e is ASCIIEncoding) ?? Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] UTF-8/ASCII Selected: {{result.EncodingName}}");
                return result;
            }

            // 3. Heuristic Scoring for Japanese
            int sjisScore = CountShiftJisSequences(buffer);
            int eucScore = CountEucJpSequences(buffer);
            System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Heuristics: SJIS={{sjisScore}}, EUC={{eucScore}}");

            if (sjisScore > eucScore && sjisScore > 0)
            {
                System.Diagnostics.Debug.WriteLine("[LargeFileEngine] Heuristics Selected: Shift-JIS");
                return Encoding.GetEncoding("shift_jis");
            }
            if (eucScore > sjisScore && eucScore > 0)
            {
                System.Diagnostics.Debug.WriteLine("[LargeFileEngine] Heuristics Selected: EUC-JP");
                return Encoding.GetEncoding("euc-jp");
            }

            // 4. Fallback to first in priority list
            System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Inconclusive, Fallback: {{_supportedEncodings[0].EncodingName}}");
            return _supportedEncodings[0];
        }

        private bool IsBufferValidUtf8(byte[] buffer, out bool hasMultiByte)
        {
            int i = 0;
            hasMultiByte = false;
            while (i < buffer.Length)
            {
                byte b1 = buffer[i++];
                if (b1 < 0x80) continue; // ASCII is always valid

                hasMultiByte = true;
                if (b1 >= 0xC2 && b1 <= 0xDF) // 2-byte sequence
                {
                    if (i >= buffer.Length || (buffer[i++] & 0xC0) != 0x80) return false;
                }
                else if (b1 >= 0xE0 && b1 <= 0xEF) // 3-byte sequence
                {
                    if (i + 1 >= buffer.Length) return false;
                    byte b2 = buffer[i++];
                    byte b3 = buffer[i++];
                    if ((b2 & 0xC0) != 0x80 || (b3 & 0xC0) != 0x80) return false;
                    // Overlong and surrogate checks
                    if (b1 == 0xE0 && b2 < 0xA0) return false;
                    if (b1 == 0xED && b2 >= 0xA0) return false;
                }
                else if (b1 >= 0xF0 && b1 <= 0xF4) // 4-byte sequence
                {
                    if (i + 2 >= buffer.Length) return false;
                    byte b2 = buffer[i++];
                    byte b3 = buffer[i++];
                    byte b4 = buffer[i++];
                    if ((b2 & 0xC0) != 0x80 || (b3 & 0xC0) != 0x80 || (b4 & 0xC0) != 0x80) return false;
                    // Overlong and out-of-range checks
                    if (b1 == 0xF0 && b2 < 0x90) return false;
                    if (b1 == 0xF4 && b2 >= 0x90) return false;
                }
                else
                {
                    return false; // Illegal lead byte
                }
            }
            return true;
        }

        private int CountShiftJisSequences(byte[] buffer)
        {
            int score = 0;
            for (int i = 0; i < buffer.Length - 1; i++)
            {
                byte b1 = buffer[i];
                byte b2 = buffer[i + 1];
                if (((b1 >= 0x81 && b1 <= 0x9F) || (b1 >= 0xE0 && b1 <= 0xFC)) &&
                    ((b2 >= 0x40 && b2 <= 0x7E) || (b2 >= 0x80 && b2 <= 0xFC)))
                {
                    score++;
                    i++;
                }
            }
            return score;
        }

        private int CountEucJpSequences(byte[] buffer)
        {
            int score = 0;
            for (int i = 0; i < buffer.Length - 1; i++)
            {
                byte b1 = buffer[i];
                byte b2 = buffer[i + 1];
                if (b1 >= 0xA1 && b1 <= 0xFE && b2 >= 0xA1 && b2 <= 0xFE)
                {
                    score++;
                    i++;
                }
            }
            return score;
        }

        public void SetEncoding(Encoding encoding)
        {
            _encoding = encoding;
            _currentEncodingIndex = _supportedEncodings.FindIndex(e => e.CodePage == encoding.CodePage);
            if (_currentEncodingIndex < 0) _currentEncodingIndex = 0;
            StartIndexing();
        }

        public void CycleEncoding()
        {
            if (_supportedEncodings.Count == 0) return;
            _currentEncodingIndex = (_currentEncodingIndex + 1) % _supportedEncodings.Count;
            _encoding = _supportedEncodings[_currentEncodingIndex];
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
                _lineOffsets.Add(_bomLength);
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
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Starting indexing for {{_filePath}} (Size: {{FileSize}})");
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize))
                {
                    byte[] buffer = new byte[BufferSize];
                    long position = 0;
                    int bytesRead;

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
                            System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Indexing progress: {{_indexedLength}}/{{FileSize}} ({{IndexingProgress:P1}})");
                            IndexingProgressChanged?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Indexing error");
            }
            finally
            {
                _isIndexing = false;
                System.Diagnostics.Debug.WriteLine($"[LargeFileEngine] Indexing completed/stopped. Final lines: {{_lineOffsets.Count}}");
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
                if (startLine >= _lineOffsets.Count) return lines; 
                startOffset = _lineOffsets[startLine];

                int lastLineIndex = startLine + count;
                if (lastLineIndex < _lineOffsets.Count)
                {
                    endOffset = _lineOffsets[lastLineIndex];
                }
                else
                {
                    endOffset = FileSize; 
                }
            }

            int length = (int)(endOffset - startOffset);
            if (length < 0) return lines;
            if (length > 10 * 1024 * 1024) length = 10 * 1024 * 1024; 

            byte[] buffer = new byte[length];
            if (length > 0)
            {
                lock (_streamLock)
                {
                    _fileStream.Seek(startOffset, SeekOrigin.Begin);
                    _fileStream.Read(buffer, 0, length);
                }
            }

            string text = _encoding.GetString(buffer);
            using (var reader = new StringReader(text))
            {
                string? line;
                while ((line = reader.ReadLine()) != null && lines.Count < count)
                {
                    lines.Add(line);
                }
            }

            // Fill with empty strings if we didn't get enough lines but they should exist
            while (lines.Count < count && startLine + lines.Count < LineCount)
            {
                lines.Add(string.Empty);
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
        /// Synchronously finds all occurrences of a pattern. (Mainly for compatibility/tests)
        /// </summary>
        public List<int> Search(string pattern)
        {
            var matches = new List<int>();
            if (string.IsNullOrEmpty(pattern) || _fileStream == null) return matches;

            byte[] buffer = new byte[1024 * 1024]; 
            long position = 0;
            
            lock (_streamLock)
            {
                _fileStream.Seek(0, SeekOrigin.Begin);
            }

            while (position < FileSize)
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
                    matches.Add((int)GetLineNumberFromOffset(matchAbsOffset));
                    matchIndex = chunk.IndexOf(pattern, matchIndex + 1, StringComparison.OrdinalIgnoreCase);
                }
                position += bytesRead;
            }
            return matches;
        }

        /// <summary>
        /// Asynchronously finds the next occurrence of a pattern or regex.
        /// </summary>
        public async Task<HexSearchResult> FindNextAsync(string pattern, long startIndex, bool searchBackwards, bool useRegex, bool isHexMode, CancellationToken token)
        {
            if (string.IsNullOrEmpty(pattern) || _fileStream == null) return HexSearchResult.NotFound;

            if (isHexMode)
            {
                return await FindNextHexAsync(pattern, startIndex, searchBackwards, token);
            }

            return await Task.Run<HexSearchResult>(() => 
            {
                if (_fileStream == null) return HexSearchResult.NotFound;

                long startByteOffset = 0;
                lock (_lineOffsets)
                {
                    if (startIndex < _lineOffsets.Count)
                        startByteOffset = _lineOffsets[(int)startIndex];
                    else
                        startByteOffset = _lineOffsets[_lineOffsets.Count - 1];
                }

                byte[] buffer = new byte[1024 * 1024]; 
                long position = startByteOffset;
                int overlap = _encoding.GetMaxByteCount(pattern.Length);
                
                System.Text.RegularExpressions.Regex? regex = null;
                if (useRegex)
                {
                    try { regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase); } 
                    catch { return HexSearchResult.NotFound; }
                }

                if (!searchBackwards)
                {
                    while (position < FileSize)
                    {
                        if (token.IsCancellationRequested) return HexSearchResult.NotFound;

                        int bytesRead;
                        lock (_streamLock)
                        {
                            _fileStream.Seek(position, SeekOrigin.Begin);
                            bytesRead = _fileStream.Read(buffer, 0, buffer.Length);
                        }
                        if (bytesRead == 0) break;

                        string chunk = _encoding.GetString(buffer, 0, bytesRead);
                        int matchIndex = -1;

                        if (useRegex && regex != null)
                        {
                            var match = regex.Match(chunk);
                            if (match.Success) matchIndex = match.Index;
                        }
                        else
                        {
                            matchIndex = chunk.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                        }

                        if (matchIndex >= 0)
                        {
                            long matchAbsOffset = position + _encoding.GetByteCount(chunk.Substring(0, matchIndex));
                            return new HexSearchResult { Offset = GetLineNumberFromOffset(matchAbsOffset), MatchType = HexMatchType.Data };
                        }
                        position += Math.Max(1, bytesRead - overlap); 
                    }
                }
                else
                {
                    while (position > 0)
                    {
                        if (token.IsCancellationRequested) return HexSearchResult.NotFound;

                        long readStart = Math.Max(0, position - buffer.Length);
                        int bytesToRead = (int)(position - readStart);
                        
                        lock (_streamLock)
                        {
                            _fileStream.Seek(readStart, SeekOrigin.Begin);
                            _fileStream.Read(buffer, 0, bytesToRead);
                        }

                        string chunk = _encoding.GetString(buffer, 0, bytesToRead);
                        int matchIndex = -1;

                        if (useRegex && regex != null)
                        {
                            var matches = regex.Matches(chunk);
                            if (matches.Count > 0) matchIndex = matches[matches.Count - 1].Index; 
                        }
                        else
                        {
                            matchIndex = chunk.LastIndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                        }

                        if (matchIndex >= 0)
                        {
                            long matchAbsOffset = readStart + _encoding.GetByteCount(chunk.Substring(0, matchIndex));
                            return new HexSearchResult { Offset = GetLineNumberFromOffset(matchAbsOffset), MatchType = HexMatchType.Data };
                        }
                        if (readStart == 0) break;
                        position = readStart + Math.Min(bytesToRead - 1, overlap);
                    }
                }

                return HexSearchResult.NotFound;
            });
        }

        private async Task<HexSearchResult> FindNextHexAsync(string query, long startByteOffset, bool searchBackwards, CancellationToken token)
        {
            return await Task.Run<HexSearchResult>(() => 
            {
                if (_fileStream == null) return HexSearchResult.NotFound;

                byte[] pattern;
                bool isHexQuery = IsValidHexQuery(query, out pattern);
                
                if (!isHexQuery)
                {
                    // Treat as ASCII search if not valid hex
                    pattern = Encoding.ASCII.GetBytes(query);
                }

                bool canMatchAddress = query.Length <= 8 && IsHex(query);

                byte[] buffer = new byte[1024 * 1024];
                long position = startByteOffset;
                int overlap = Math.Max(0, pattern.Length - 1);

                if (!searchBackwards)
                {
                    while (position < FileSize)
                    {
                        if (token.IsCancellationRequested) return HexSearchResult.NotFound;

                        // 1. Check Address space first (every 16 bytes in this chunk)
                        if (canMatchAddress)
                        {
                            for (long addr = position; addr < position + BufferSize && addr < FileSize; addr += 16)
                            {
                                if (addr.ToString("X8").Contains(query, StringComparison.OrdinalIgnoreCase))
                                {
                                    return new HexSearchResult { Offset = addr, MatchType = HexMatchType.Address };
                                }
                            }
                        }

                        // 2. Check Data space
                        int bytesRead;
                        lock (_streamLock)
                        {
                            _fileStream.Seek(position, SeekOrigin.Begin);
                            bytesRead = _fileStream.Read(buffer, 0, buffer.Length);
                        }
                        if (bytesRead == 0) break;

                        int matchIndex = IndexOfBytes(buffer, bytesRead, pattern);
                        if (matchIndex >= 0)
                        {
                            return new HexSearchResult { Offset = position + matchIndex, MatchType = HexMatchType.Data };
                        }

                        position += Math.Max(1, bytesRead - overlap);
                    }
                }
                else
                {
                    // Backward search
                    while (position > 0)
                    {
                        if (token.IsCancellationRequested) return HexSearchResult.NotFound;

                        long readStart = Math.Max(0, position - buffer.Length);
                        int bytesToRead = (int)(position - readStart);
                        
                        lock (_streamLock)
                        {
                            _fileStream.Seek(readStart, SeekOrigin.Begin);
                            _fileStream.Read(buffer, 0, bytesToRead);
                        }

                        // Check Data space (Backwards)
                        int matchIndex = LastIndexOfBytes(buffer, bytesToRead, pattern);
                        if (matchIndex >= 0)
                        {
                            return new HexSearchResult { Offset = readStart + matchIndex, MatchType = HexMatchType.Data };
                        }

                        // Check Address space (Backwards)
                        if (canMatchAddress)
                        {
                            for (long addr = (position / 16) * 16; addr >= readStart; addr -= 16)
                            {
                                if (addr.ToString("X8").Contains(query, StringComparison.OrdinalIgnoreCase))
                                {
                                    return new HexSearchResult { Offset = addr, MatchType = HexMatchType.Address };
                                }
                            }
                        }

                        if (readStart == 0) break;
                        position = readStart + overlap;
                    }
                }

                return HexSearchResult.NotFound;
            });
        }

        private bool IsHex(string s)
        {
            foreach (char c in s)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == ' ' || c == '-'))
                    return false;
            }
            return true;
        }

        private bool IsValidHexQuery(string query, out byte[] bytes)
        {
            string clean = query.Replace(" ", "").Replace("-", "");
            if (clean.Length % 2 != 0 || clean.Length == 0)
            {
                bytes = Array.Empty<byte>();
                return false;
            }

            try
            {
                bytes = new byte[clean.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
                }
                return true;
            }
            catch
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }

        private int IndexOfBytes(byte[] buffer, int length, byte[] pattern)
        {
            for (int i = 0; i <= length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private int LastIndexOfBytes(byte[] buffer, int length, byte[] pattern)
        {
            for (int i = length - pattern.Length; i >= 0; i--)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private long GetLineNumberFromOffset(long offset)
        {
            long currentOffset = 0;
            long currentLine = 0;

            lock (_lineOffsets)
            {
                if (_lineOffsets.Count > 0 && Interlocked.Read(ref _indexedLength) >= offset)
                {
                    int index = _lineOffsets.BinarySearch(offset);
                    if (index < 0) index = ~index - 1;
                    return Math.Max(0, index);
                }

                if (_lineOffsets.Count > 0)
                {
                    currentLine = _lineOffsets.Count - 1;
                    currentOffset = _lineOffsets[(int)currentLine];
                }
            }

            if (offset <= currentOffset) return currentLine;

            try
            {
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize))
                {
                    fs.Seek(currentOffset, SeekOrigin.Begin);
                    byte[] buffer = new byte[BufferSize];
                    long pos = currentOffset;
                    
                        while (pos < offset)
                        {
                            int toRead = (int)Math.Min(buffer.Length, offset - pos);
                            int read = fs.Read(buffer, 0, toRead);
                            if (read <= 0) break;

                            for (int i = 0; i < read; i++)
                            {
                                if (buffer[i] == 10)
                                {
                                    currentLine++;
                                    lock (_lineOffsets)
                                    {
                                        long newOffset = pos + i + 1;
                                        if (!_lineOffsets.Contains(newOffset)) _lineOffsets.Add(newOffset);
                                    }
                                }
                            }
                            pos += read;
                        }
                        Interlocked.Exchange(ref _indexedLength, pos);
                    }
                }
            catch { }

            return currentLine;
        }

        public void Dispose()
        {
            CancelIndexing();
            _fileStream?.Dispose();
        }
    }
}