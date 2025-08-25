using Cayd.AspNetCore.FlexLog.Services;
using Cayd.AspNetCore.FlexLog.Logging;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Options
{
    /// <summary>
    /// Stores all FlexLog options
    /// </summary>
    public class FlexLogOptions
    {
        /// <summary>
        /// Configuration key of the FlexLog options
        /// </summary>
        public static readonly string OptionKey = "FlexLog";

        /// <summary>
        /// FlexLog's log channel behavior
        /// </summary>
        public ChannelOption? Channel { get; set; }
        /// <summary>
        /// How many logs trigger the flushing process
        /// </summary>
        public int? BufferLimit { get; set; }
        /// <summary>
        /// How much time should pass in seconds since the last log to flush the buffer
        /// </summary>
        public int? TimerInSeconds { get; set; }
        /// <summary>
        /// For which specific routes FlexLog should ignore logging. Given routes also cover all children routes
        /// </summary>
        public List<string>? IgnoredRoutes { get; set; }
        /// <summary>
        /// What are added to logs
        /// </summary>
        public LogDetailOption? LogDetails { get; set; }

        /// <summary>
        /// Options related to the <see cref="FlexLogChannel"/>
        /// </summary>
        public class ChannelOption
        {
            /// <summary>
            /// It can be either 'Unbounded' or 'DropWrite'. The 'Unbounded' option means that there is no limit of how many logs can be stored at any given time,
            /// whereas the 'DropWrite' option defines that the channel has a capacity and new logs are dropped until there is enough space again.
            /// </summary>
            public string? Strategy { get; set; }
            /// <summary>
            /// If the 'DropWrite' strategy is chosen, this option controls how many logs the channel can store.
            /// </summary>
            public int? Capacity { get; set; }
        }

        /// <summary>
        /// Options related to the <see cref="FlexLogContext"/>
        /// </summary>
        public class LogDetailOption
        {
            /// <summary>
            /// How claim types should be logged
            /// </summary>
            public ClaimOption? Claims { get; set; }
            /// <summary>
            /// How headers should be logged
            /// </summary>
            public HeaderOption? Headers { get; set; }
            /// <summary>
            /// How JSON request bodies should be logged
            /// </summary>
            public RequestBodyOption? RequestBody { get; set; }
            /// <summary>
            /// How JSON response bodies should be logged
            /// </summary>
            public ResponseBodyOption? ResponseBody { get; set; }
            /// <summary>
            /// How query string should be logged
            /// </summary>
            public QueryStringOption? QueryString { get; set; }

            /// <summary>
            /// Options related to how claim types should be logged
            /// </summary>
            public class ClaimOption
            {
                /// <summary>
                /// Whether FlexLog should log claims for any endpoint
                /// </summary>
                public bool? Enabled { get; set; }
                /// <summary>
                /// Which claim types FlexLog should log
                /// </summary>
                public List<string>? IncludedTypes { get; set; }
                /// <summary>
                /// Which claim types FlexLog should not log
                /// </summary>
                public List<string>? IgnoredTypes { get; set; }
                /// <summary>
                /// For which endpoints FlexLog should not log any type of claims
                /// </summary>
                public List<string>? IgnoredRoutes { get; set; }
            }

            /// <summary>
            /// Options related to how headers should be logged
            /// </summary>
            public class HeaderOption
            {
                /// <summary>
                /// Whether FlexLog should log headers for any endpoint
                /// </summary>
                public bool? Enabled { get; set; }
                /// <summary>
                /// The defined key in headers to set correlation IDs automatically
                /// </summary>
                public string? CorrelationIdKey { get; set; }
                /// <summary>
                /// If FlexLog should limit header values if they are too long
                /// </summary>
                public LimitOption? Limit { get; set; }
                /// <summary>
                /// Which headers FlexLog should log
                /// </summary>
                public List<string>? IncludedKeys { get; set; }
                /// <summary>
                /// Which headers FlexLog should not log
                /// </summary>
                public List<string>? IgnoredKeys { get; set; }
                /// <summary>
                /// For which endpoints FlexLog should not log any header
                /// </summary>
                public List<string>? IgnoredRoutes { get; set; }

                /// <summary>
                /// Options related to header limit
                /// </summary>
                public class LimitOption
                {
                    /// <summary>
                    /// Length of a header value to be considered too long in number of characters
                    /// </summary>
                    public int? Length { get; set; }
                    /// <summary>
                    /// It can be either 'Slice' or 'Drop'. The 'Slice' option means that header values are truncated according to the Length if they are too long,
                    /// whereas the 'Drop' option means that header values are replaced by a 'TOO LARGE' text if they are too long.
                    /// </summary>
                    public string? Strategy { get; set; }
                }
            }

            /// <summary>
            /// Options related to how JSON request bodies should be logged
            /// </summary>
            public class RequestBodyOption
            {
                /// <summary>
                /// Whether FlexLog should log JSON request bodies for any endpoint
                /// </summary>
                public bool? Enabled { get; set; }
                /// <summary>
                /// The size of JSON request bodies to be considered too large in bytes
                /// </summary>
                public long? BodySizeLimitInBytes { get; set; }
                /// <summary>
                /// Which JSON keys should be replaced by a 'REDACTED' text.
                /// </summary>
                public List<string>? RedactedKeys { get; set; }
                /// <summary>
                /// For which endpoints FlexLog should not log any JSON request body
                /// </summary>
                public List<string>? IgnoredRoutes { get; set; }
            }

            /// <summary>
            /// Options related to how JSON response bodies should be logged
            /// </summary>
            public class ResponseBodyOption
            {
                /// <summary>
                /// Whether FlexLog should log JSON response bodies for any endpoint
                /// </summary>
                public bool? Enabled { get; set; }
                /// <summary>
                /// Which JSON keys should be replaced by a 'REDACTED' text.
                /// </summary>
                public List<string>? RedactedKeys { get; set; }
                /// <summary>
                /// For which endpoints FlexLog should not log any JSON response body
                /// </summary>
                public List<string>? IgnoredRoutes { get; set; }
            }

            /// <summary>
            /// Options related to how query string should be logged
            /// </summary>
            public class QueryStringOption
            {
                /// <summary>
                /// Whether FlexLog should log query strings for any endpoint
                /// </summary>
                public bool? Enabled { get; set; }
                /// <summary>
                /// If FlexLog should limit query strings if they are too long
                /// </summary>
                public LimitOption? Limit { get; set; }
                /// <summary>
                /// For which endpoints FlexLog should not log any query string
                /// </summary>
                public List<string>? IgnoredRoutes { get; set; }

                /// <summary>
                /// Options related to query string limit
                /// </summary>
                public class LimitOption
                {
                    /// <summary>
                    /// Length of a query string to be considered too long in number of characters
                    /// </summary>
                    public int? Length { get; set; }
                }
            }
        }
    }
}
