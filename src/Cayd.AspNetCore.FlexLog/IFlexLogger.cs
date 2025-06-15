using Cayd.AspNetCore.FlexLog.Enums;
using System;

namespace Cayd.AspNetCore.FlexLog
{
    public interface IFlexLogger<T>
    {
        void Log(ELogLevel logLevel, string message, object? metadata = null);
        void Log(ELogLevel logLevel, string message, Exception exception, object? metadata = null);
        void LogTrace(string message, object? metadata = null);
        void LogTrace(string message, Exception exception, object? metadata = null);
        void LogDebug(string message, object? metadata = null);
        void LogDebug(string message, Exception exception, object? metadata = null);
        void LogInformation(string message, object? metadata = null);
        void LogInformation(string message, Exception exception, object? metadata = null);
        void LogWarning(string message, object? metadata = null);
        void LogWarning(string message, Exception exception, object? metadata = null);
        void LogError(string message, object? metadata = null);
        void LogError(string message, Exception exception, object? metadata = null);
        void LogCritical(string message, object? metadata = null);
        void LogCritical(string message, Exception exception, object? metadata = null);
    }
}
