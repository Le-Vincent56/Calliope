namespace Calliope.Infrastructure.Logging
{
    /// <summary>
    /// Defines logging functionality for different severity levels
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the specified log level
        /// </summary>
        /// <param name="level">The severity level of the log message</param>
        /// <param name="message">The message to be logged</param>
        void Log(LogLevel level, string message);

        /// <summary>
        /// Logs a debug-level message
        /// </summary>
        /// <param name="message">The message to be logged</param>
        void LogDebug(string message);

        /// <summary>
        /// Logs an informational-level message
        /// </summary>
        /// <param name="message">The message to be logged</param>
        void LogInfo(string message);

        /// <summary>
        /// Logs a warning-level message
        /// </summary>
        /// <param name="message">The message to be logged</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error-level message
        /// </summary>
        /// <param name="message">The message to be logged</param>
        void LogError(string message);
    }
}