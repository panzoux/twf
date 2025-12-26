using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace TWF.Infrastructure
{
    /// <summary>
    /// Configures logging infrastructure for the application
    /// </summary>
    public static class LoggingConfiguration
    {
        private static ILoggerFactory? _loggerFactory;
        private static readonly object _lock = new object();
        private static LogLevel _minimumLogLevel = LogLevel.Information;

        /// <summary>
        /// Initializes the logging infrastructure
        /// </summary>
        /// <param name="logLevel">Minimum log level (None, Debug, Information, Warning, Error, Critical)</param>
        public static void Initialize(string logLevel = "Information")
        {
            lock (_lock)
            {
                if (_loggerFactory != null)
                {
                    return;
                }

                _minimumLogLevel = ParseLogLevel(logLevel);

                _loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddProvider(new FileLoggerProvider(_minimumLogLevel))
                        .SetMinimumLevel(_minimumLogLevel);
                });
            }
        }

        /// <summary>
        /// Parses a log level string into a LogLevel enum
        /// </summary>
        private static LogLevel ParseLogLevel(string logLevel)
        {
            var parsedLevel = logLevel?.ToLowerInvariant() switch
            {
                "none" => LogLevel.None,
                "trace" => LogLevel.Trace,
                "debug" => LogLevel.Debug,
                "information" => LogLevel.Information,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "critical" => LogLevel.Critical,
                _ => LogLevel.Information
            };

            return parsedLevel;
        }

        /// <summary>
        /// Gets a logger for the specified type
        /// </summary>
        public static ILogger<T> GetLogger<T>()
        {
            if (_loggerFactory == null)
            {
                Initialize();
            }

            return _loggerFactory!.CreateLogger<T>();
        }

        /// <summary>
        /// Gets a logger with the specified name
        /// </summary>
        public static ILogger GetLogger(string categoryName)
        {
            if (_loggerFactory == null)
            {
                Initialize();
            }

            return _loggerFactory!.CreateLogger(categoryName);
        }

        /// <summary>
        /// Changes the log level at runtime
        /// </summary>
        /// <param name="logLevel">New log level as string</param>
        public static void ChangeLogLevel(string logLevel)
        {
            lock (_lock)
            {
                var oldLogLevel = _minimumLogLevel;
                _minimumLogLevel = ParseLogLevel(logLevel);

                // Note: We can't easily change the log level of an existing LoggerFactory
                // The simplest approach is to shut down and reinitialize
                if (_loggerFactory != null)
                {
                    _loggerFactory.Dispose();
                    _loggerFactory = null;
                }

                // Reinitialize with new log level
                _loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder
                        .AddProvider(new FileLoggerProvider(_minimumLogLevel))
                        .SetMinimumLevel(_minimumLogLevel);
                });

                // Create a logger from the new factory to log the change
                var logger = _loggerFactory.CreateLogger(typeof(LoggingConfiguration).FullName ?? "LoggingConfiguration");
                logger.LogInformation("Log level changed from {OldLevel} to {NewLevel} ({NewLevelEnum})", oldLogLevel, logLevel, _minimumLogLevel);
            }
        }

        /// <summary>
        /// Shuts down the logging infrastructure
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                _loggerFactory?.Dispose();
                _loggerFactory = null;
            }
        }
    }

    /// <summary>
    /// Custom file logger provider
    /// </summary>
    internal class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;
        private readonly object _lock = new object();

        private readonly LogLevel _minimumLogLevel;

        public FileLoggerProvider(LogLevel minimumLogLevel)
        {
            _minimumLogLevel = minimumLogLevel;
            
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TWF"
            );

            Directory.CreateDirectory(logDirectory);
            _logFilePath = Path.Combine(logDirectory, "twf_errors.log");

            // Rotate log file if it exceeds 10MB
            if (File.Exists(_logFilePath))
            {
                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    var backupPath = Path.Combine(logDirectory, $"twf_errors_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                    File.Move(_logFilePath, backupPath);
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _logFilePath, _lock, _minimumLogLevel);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// Custom file logger
    /// </summary>
    internal class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private readonly object _lock;
        private readonly LogLevel _minimumLogLevel;

        public FileLogger(string categoryName, string logFilePath, object lockObject, LogLevel minimumLogLevel)
        {
            _categoryName = categoryName;
            _logFilePath = logFilePath;
            _lock = lockObject;
            _minimumLogLevel = minimumLogLevel;

            // Log the minimum log level being set for this logger
            try
            {
                var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Information] [FileLoggerProvider] FileLogger: Created logger for category '{categoryName}' with minimum log level: {minimumLogLevel}";
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minimumLogLevel;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}";

            if (exception != null)
            {
                logEntry += Environment.NewLine + exception.ToString();
            }

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Silently fail if we can't write to the log file
                }
            }
        }
    }
}
