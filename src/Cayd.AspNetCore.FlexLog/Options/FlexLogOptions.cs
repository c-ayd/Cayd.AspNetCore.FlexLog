using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Options
{
    public class FlexLogOptions
    {
        public static readonly string OptionKey = "FlexLog";

        public ChannelOption? Channel { get; set; }
        public int? BufferLimit { get; set; }
        public int? TimerInSeconds { get; set; }
        public List<string>? IgnoredRoutes { get; set; }
        public LogDetailOption? LogDetails { get; set; }

        public class ChannelOption
        {
            public string? Strategy { get; set; }
            public int? Capacity { get; set; }
        }

        public class LogDetailOption
        {
            public ClaimOption? Claims { get; set; }
            public HeaderOption? Headers { get; set; }
            public RequestBodyOption? RequestBody { get; set; }
            public ResponseBodyOption? ResponseBody { get; set; }
            public QueryStringOption? QueryString { get; set; }

            public class ClaimOption
            {
                public bool? Enabled { get; set; }
                public List<string>? IncludedTypes { get; set; }
                public List<string>? IgnoredTypes { get; set; }
                public List<string>? IgnoredRoutes { get; set; }
            }

            public class HeaderOption
            {
                public bool? Enabled { get; set; }
                public string? CorrelationIdKey { get; set; }
                public List<string>? IncludedKeys { get; set; }
                public List<string>? IgnoredKeys { get; set; }
                public List<string>? IgnoredRoutes { get; set; }
            }

            public class RequestBodyOption
            {
                public bool? Enabled { get; set; }
                public long? BodySizeLimitInBytes { get; set; }
                public List<string>? RedactedKeys { get; set; }
                public List<string>? IgnoredRoutes { get; set; }
            }

            public class ResponseBodyOption
            {
                public bool? Enabled { get; set; }
                public List<string>? RedactedKeys { get; set; }
                public List<string>? IgnoredRoutes { get; set; }
            }

            public class QueryStringOption
            {
                public bool? Enabled { get; set; }
                public List<string>? IgnoredRoutes { get; set; }
            }
        }
    }
}
