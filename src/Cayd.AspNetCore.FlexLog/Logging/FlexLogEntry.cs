using Cayd.AspNetCore.FlexLog.Enums;
using System;

namespace Cayd.AspNetCore.FlexLog.Logging
{
    public class FlexLogEntry
    {
        public ELogLevel LogLevel { get; set; }
        public string? Category { get; set; }
        public string? Message { get; set; }
        public Exception? Exception { get; set; }
        public object? Metadata { get; set; }
    }
}
