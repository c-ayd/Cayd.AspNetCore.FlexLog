using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Services;
using System;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Testing
{
    /// <summary>
    /// A mock version of <see cref="FlexLogger{T}"/> for testing.
    /// <para>
    /// Its <see cref="LogEntries"/> and <see cref="Category"/> name can be accessed publicly.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Category type</typeparam>
    public class FlexLoggerTest<T> : IFlexLogger<T>
    {
        /// <summary>
        /// Log entries that are added.
        /// </summary>
        public List<FlexLogEntry> LogEntries { get; private set; }
        /// <summary>
        /// Name of the class that added the <see cref="LogEntries"/>.
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// Creates a test instance of <see cref="FlexLogger{T}"/>.
        /// </summary>
        public FlexLoggerTest()
        {
            LogEntries = new List<FlexLogEntry>();
            Category = typeof(T).FullName ?? typeof(T).Name;
        }

        private void AddLogEntry(ELogLevel logLevel, string message, Exception? exception, object? metadata)
        {
            LogEntries.Add(new FlexLogEntry()
            {
                LogLevel = logLevel,
                Category = Category,
                Message = message,
                Exception = exception,
                Metadata = metadata
            });
        }

        public void Log(ELogLevel logLevel, string message, object? metadata = null)
            => AddLogEntry(logLevel, message, null, metadata);

        public void Log(ELogLevel logLevel, string message, Exception exception, object? metadata = null)
            => AddLogEntry(logLevel, message, exception, metadata);

        public void LogTrace(string message, object? metadata = null)
            => AddLogEntry(ELogLevel.Trace, message, null, metadata);

        public void LogTrace(string message, Exception exception, object? metadata = null)
            => AddLogEntry(ELogLevel.Trace, message, exception, metadata);

        public void LogDebug(string message, object? metadata = null)
            => AddLogEntry(ELogLevel.Debug, message, null, metadata);

        public void LogDebug(string message, Exception exception, object? metadata = null)
            => AddLogEntry(ELogLevel.Debug, message, exception, metadata);

        public void LogInformation(string message, object? metadata = null)
            => AddLogEntry(ELogLevel.Information, message, null, metadata);

        public void LogInformation(string message, Exception exception, object? metadata = null)
            => AddLogEntry(ELogLevel.Information, message, exception, metadata);

        public void LogWarning(string message, object? metadata = null)
            => AddLogEntry(ELogLevel.Warning, message, null, metadata);

        public void LogWarning(string message, Exception exception, object? metadata = null)
            => AddLogEntry(ELogLevel.Warning, message, exception, metadata);

        public void LogError(string message, object? metadata = null)
            => AddLogEntry(ELogLevel.Error, message, null, metadata);

        public void LogError(string message, Exception exception, object? metadata = null)
            => AddLogEntry(ELogLevel.Error, message, exception, metadata);

        public void LogCritical(string message, object? metadata = null)
            => AddLogEntry(ELogLevel.Critical, message, null, metadata);

        public void LogCritical(string message, Exception exception, object? metadata = null)
            => AddLogEntry(ELogLevel.Critical, message, exception, metadata);
    }
}
