using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.Logging;
using System;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Services
{
    public class FlexLogger<T> : IFlexLogger<T>
    {
        private readonly IList<FlexLogEntry> _logEntries;
        private readonly string _category;

        public FlexLogger(IList<FlexLogEntry> logEntries)
        {
            _logEntries = logEntries;
            _category = typeof(T).FullName ?? typeof(T).Name;
        }

        private void AddLogEntry(ELogLevel logLevel, string message, Exception? exception, object? metadata)
        {
            _logEntries.Add(new FlexLogEntry()
            {
                LogLevel = logLevel,
                Category = _category,
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
