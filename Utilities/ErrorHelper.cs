using System;

namespace TWF.Utilities
{
    /// <summary>
    /// Centralized error handling utility for consistent error reporting and logging
    /// </summary>
    public static class ErrorHelper
    {
        private static Action<string>? _statusCallback;
        private static Action<string, Exception>? _logCallback;

        /// <summary>
        /// Initializes the ErrorHelper with callbacks for status updates and logging
        /// </summary>
        /// <param name="statusCallback">Action to call for UI status updates</param>
        /// <param name="logCallback">Action to call for logging exceptions</param>
        public static void Initialize(Action<string> statusCallback, Action<string, Exception> logCallback)
        {
            _statusCallback = statusCallback;
            _logCallback = logCallback;
        }

        /// <summary>
        /// Handles an exception by updating the UI status and logging the error
        /// </summary>
        /// <param name="ex">The exception to handle</param>
        /// <param name="context">A description of what was happening when the error occurred</param>
        public static void Handle(Exception ex, string context)
        {
            if (ex == null) return;

            string statusMessage = $"{context}: {ex.Message}";
            
            _statusCallback?.Invoke(statusMessage);
            _logCallback?.Invoke(context, ex);
        }

        /// <summary>
        /// Shows a simple error message without an exception
        /// </summary>
        /// <param name="message">The error message to display</param>
        public static void Show(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            
            string statusMessage = message.StartsWith("Error", StringComparison.OrdinalIgnoreCase) 
                ? message 
                : $"Error: {message}";

            _statusCallback?.Invoke(statusMessage);
        }
    }
}
