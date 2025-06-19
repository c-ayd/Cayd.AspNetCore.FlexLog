using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.Logging;
using System;

namespace Cayd.AspNetCore.FlexLog
{
    /// <summary>
    /// Generic interface to log via FlexLog.
    /// </summary>
    /// <typeparam name="T">Category name.</typeparam>
    public interface IFlexLogger<T>
    {
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="logLevel">Log level of the log entry.</param>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void Log(ELogLevel logLevel, string message, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="logLevel">Log level of the log entry.</param>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="exception">Thrown exception.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void Log(ELogLevel logLevel, string message, Exception exception, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Trace"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogTrace(string message, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Trace"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="exception">Thrown exception.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogTrace(string message, Exception exception, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Debug"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogDebug(string message, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Debug"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="exception">Thrown exception.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogDebug(string message, Exception exception, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Information"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogInformation(string message, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Information"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="exception">Thrown exception.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogInformation(string message, Exception exception, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Warning"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogWarning(string message, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Warning"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="exception">Thrown exception.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogWarning(string message, Exception exception, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Error"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogError(string message, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Error"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="exception">Thrown exception.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogError(string message, Exception exception, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Critical"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogCritical(string message, object? metadata = null);
        /// <summary>
        /// Adds a new <see cref="FlexLogEntry"/> whose log level is <see cref="ELogLevel.Critical"/> to the current <see cref="FlexLogContext"/>.
        /// </summary>
        /// <param name="message">Message of the log entry.</param>
        /// <param name="exception">Thrown exception.</param>
        /// <param name="metadata">Extra data for the log entry.</param>
        void LogCritical(string message, Exception exception, object? metadata = null);
    }
}
