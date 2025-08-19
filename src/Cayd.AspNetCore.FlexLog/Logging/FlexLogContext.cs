using Cayd.AspNetCore.FlexLog.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Cayd.AspNetCore.FlexLog.Logging
{
    /// <summary>
    /// Holds information about the detailed log.
    /// </summary>
    public class FlexLogContext
    {
        /// <summary>
        /// Unique correlation ID that is genrated automatically. It can also be set automatically via the FlexLog option <c>'CorrelationIdKey'</c> if applicable.
        /// </summary>
        public Guid CorrelationId { get; set; }
        /// <summary>
        /// Protocol name of the request.
        /// </summary>
        public string Protocol { get; set; }
        /// <summary>
        /// Target endpoint that the request is made to.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Trace ID of the log coming from <see cref="HttpContext.TraceIdentifier"/> if applicable.
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// Timestamp when the log process is started.
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Elapsed time between the log process being started and being sent to <see cref="FlexLogChannel"/>.
        /// </summary>
        public double ElapsedTimeInMilliseconds { get; set; }

        /// <summary>
        /// List of captured <see cref="Claim"/> if applicable.
        /// </summary>
        public IDictionary<string, string?> Claims { get; set; }

        /// <summary>
        /// List of captured HTTP headers if applicable.
        /// </summary>
        public IDictionary<string, string?> Headers { get; set; }

        /// <summary>
        /// Query string of the request line.
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// Content type of the HTTP request.
        /// </summary>
        public string? RequestBodyContentType { get; set; }
        public byte[]? RequestBodyRaw { get; set; }
        /// <summary>
        /// JSON representation of the HTTP request body if available.
        /// </summary>
        public string? RequestBody { get; set; }
        /// <summary>
        /// Size of the HTTP request body in bytes. It is calculated if and only if the request's content type is JSON.
        /// </summary>
        public long? RequestBodySizeInBytes { get; set; }
        /// <summary>
        /// Whether the size of the HTTP request body exceeds the logging size limit. It is calculated if and only if the request's content type is JSON.
        /// </summary>
        public bool? IsRequestBodyTooLarge { get; set; }

        /// <summary>
        /// Content type of the HTTP response.
        /// </summary>
        public string? ResponseBodyContentType { get; set; }
        /// <summary>
        /// Status code of the HTTP response.
        /// </summary>
        public int? ResponseStatusCode { get; set; }
        public byte[]? ResponseBodyRaw { get; set; }
        /// <summary>
        /// JSON representation of the HTTP response body if available.
        /// </summary>
        public string? ResponseBody { get; set; }

        /// <summary>
        /// Log entries of the request that are added via <see cref="IFlexLogger{T}"/>.
        /// </summary>
        public IList<FlexLogEntry> LogEntries { get; set; }

        /// <summary>
        /// Creates a new log context.
        /// </summary>
        public FlexLogContext()
        {
            CorrelationId = Guid.NewGuid();
            Protocol = string.Empty;
            Endpoint = string.Empty;
            Timestamp = DateTime.UtcNow;
            Claims = new Dictionary<string, string?>();
            Headers = new Dictionary<string, string?>();
            LogEntries = new List<FlexLogEntry>();
        }
    }
}
