using System.Text;
using UnityEngine;

namespace Calliope.Infrastructure.Logging
{
    /// <summary>
    /// Logger implementation that routes to Unity's Debug.Log();
    /// Adds [Calliope] prefix by default to distinguish from other logs
    /// </summary>
    public class UnityLogger : ILogger
    {
        private readonly StringBuilder _messageBuilder;
        private readonly string _prefix;

        public UnityLogger(string prefix = "[Calliope]")
        {
            _messageBuilder = new StringBuilder();
            _prefix = prefix;
        }

        /// <summary>
        /// Logs a message with the specified severity level, using Unity's Debug.Log() system
        /// </summary>
        /// <param name="level">The severity level of the log message (e.g., Debug, Info, Warning, Error)</param>
        /// <param name="message">The message content to be logged</param>
        public void Log(LogLevel level, string message)
        {
            // Clear the message builder
            _messageBuilder.Clear();
            
            // Construct the full message
            _messageBuilder.Append(_prefix);
            _messageBuilder.Append(" ");
            _messageBuilder.Append(message);

            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(_messageBuilder.ToString());
                    break;
                
                case LogLevel.Warning:
                    Debug.LogWarning(_messageBuilder.ToString());
                    break;
                
                case LogLevel.Error:
                    Debug.LogError(_messageBuilder.ToString());
                    break;
            }
        }

        /// <summary>
        /// Logs a debug-level message using Unity's Debug.Log() system
        /// </summary>
        /// <param name="message">The debug message content to be logged</param>
        public void LogDebug(string message) => Log(LogLevel.Debug, message);

        /// <summary>
        /// Logs an informational message using Unity's Debug.Log() system
        /// </summary>
        /// <param name="message">The content of the informational message to be logged</param>
        public void LogInfo(string message) => Log(LogLevel.Info, message);

        /// <summary>
        /// Logs a warning message using Unity's Debug.LogWarning() system
        /// </summary>
        /// <param name="message">The message content to be logged as a warning</param>
        public void LogWarning(string message) => Log(LogLevel.Warning, message);

        /// <summary>
        /// Logs an error message using Unity's Debug.LogError() system
        /// </summary>
        /// <param name="message">The error message content to be logged</param>
        public void LogError(string message) => Log(LogLevel.Error, message);
    }
}