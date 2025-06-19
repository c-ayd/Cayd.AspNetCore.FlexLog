using System;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Logging
{
    public class FlexLogContext
    {
        public string Id { get; set; }
        public string? TraceId { get; set; }

        public DateTime Timestamp { get; set; }
        public double ElapsedTimeInMilliseconds { get; set; }

        public IDictionary<string, string?> Claims { get; set; }

        public IDictionary<string, string?> Headers { get; set; }

        public string? QueryString { get; set; }

        public string RequestLine { get; set; }
        public string? RequestBodyContentType { get; set; }
        public byte[]? RequestBodyRaw { get; set; }
        public string? RequestBody { get; set; }
        public long? RequestBodySizeInBytes { get; set; }
        public bool? IsRequestBodyTooLarge { get; set; }

        public string? ResponseBodyContentType { get; set; }
        public int? ResponseStatusCode { get; set; }
        public byte[]? ResponseBodyRaw { get; set; }
        public string? ResponseBody { get; set; }

        public IList<FlexLogEntry> LogEntries { get; set; }

        public FlexLogContext()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.UtcNow;
            Claims = new Dictionary<string, string?>();
            Headers = new Dictionary<string, string?>();
            RequestLine = string.Empty;
            LogEntries = new List<FlexLogEntry>();
        }
    }
}
