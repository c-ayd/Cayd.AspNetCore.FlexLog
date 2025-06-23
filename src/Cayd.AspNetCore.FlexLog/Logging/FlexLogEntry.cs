using Cayd.AspNetCore.FlexLog.Enums;
using System;

namespace Cayd.AspNetCore.FlexLog.Logging
{
    /// <summary>
    /// A log entry added via <see cref="IFlexLogger{T}"/>.
    /// </summary>
    public class FlexLogEntry
    {
        /// <summary>
        /// Log level of this log entry.
        /// </summary>
        public ELogLevel LogLevel { get; set; }
        /// <summary>
        /// Name of the class that added this log entry.
        /// </summary>
        public string? Category { get; set; }
        /// <summary>
        /// Log message of this log entry.
        /// </summary>
        public string? Message { get; set; }
        /// <summary>
        /// Thrown exception if available.
        /// </summary>
        public Exception? Exception { get; set; }
        /// <summary>
        /// Extra data related to this log entry.
        /// </summary>
        public object? Metadata { get; set; }
    }
}
