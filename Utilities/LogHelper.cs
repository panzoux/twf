using System;
using System.IO;
using System.Collections.Generic;

namespace TWF.Utilities
{
    /// <summary>
    /// Utility for log file rotation and retention management
    /// </summary>
    public static class LogHelper
    {
        private const long MaxLogSize = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Rotates the log file if it exceeds the size limit and cleans up old compressed/rotated logs
        /// </summary>
        /// <param name="logPath">Path to the active log file</param>
        /// <param name="maxFiles">Maximum number of rotated log files to keep (0 = keep all)</param>
        public static void RotateAndCleanup(string logPath, int maxFiles)
        {
            try
            {
                if (!File.Exists(logPath)) return;

                var fileInfo = new FileInfo(logPath);
                if (fileInfo.Length >= MaxLogSize)
                {
                    string dir = Path.GetDirectoryName(logPath) ?? string.Empty;
                    string name = Path.GetFileNameWithoutExtension(logPath);
                    string ext = Path.GetExtension(logPath);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string backupPath = Path.Combine(dir, $"{name}_{timestamp}{ext}");

                    File.Move(logPath, backupPath);
                }

                if (maxFiles > 0)
                {
                    CleanupOldLogs(logPath, maxFiles);
                }
            }
            catch
            {
                // Silently fail to avoid crashing the app due to logging issues
            }
        }

        /// <summary>
        /// Deletes old rotated logs, keeping only the specified number of most recent ones
        /// </summary>
        private static void CleanupOldLogs(string logPath, int maxFiles)
        {
            try
            {
                string dir = Path.GetDirectoryName(logPath) ?? string.Empty;
                if (!Directory.Exists(dir)) return;

                string name = Path.GetFileNameWithoutExtension(logPath);
                string ext = Path.GetExtension(logPath);
                string pattern = $"{name}_*{ext}";

                var files = Directory.GetFiles(dir, pattern);
                var oldLogs = new List<FileInfo>(files.Length);
                foreach (var f in files)
                {
                    oldLogs.Add(new FileInfo(f));
                }

                // Sort descending by LastWriteTime
                oldLogs.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));

                if (oldLogs.Count > maxFiles)
                {
                    for (int i = maxFiles; i < oldLogs.Count; i++)
                    {
                        try { oldLogs[i].Delete(); } catch { }
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
