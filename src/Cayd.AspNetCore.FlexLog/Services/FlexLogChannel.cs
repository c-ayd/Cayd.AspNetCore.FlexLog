using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Sinks;
using System;
using System.Collections.Generic;
using System.Threading.Channels;

namespace Cayd.AspNetCore.FlexLog.Services
{
    /// <summary>
    /// Manages sinks and the log channel.
    /// </summary>
    public class FlexLogChannel
    {
        private static readonly string _unboundedStrategy = "Unbounded";
        private static readonly string _dropWriteStrategy = "DropWrite";

        private static readonly int _defaultChannelCapacity = 10_000;

        public Channel<FlexLogContext> Logs { get; private set; }

        private readonly List<FlexLogSink> _sinks;
        public IReadOnlyList<FlexLogSink> Sinks => _sinks;

        private readonly List<FlexLogSink> _fallbackSinks;
        public IReadOnlyList<FlexLogSink> FallbackSinks => _fallbackSinks;

        public FlexLogChannel(FlexLogOptions? loggingOptions, ICollection<FlexLogSink> sinks, ICollection<FlexLogSink> fallbackSinks)
        {
            if (string.Equals(_dropWriteStrategy, loggingOptions?.Channel?.Strategy, System.StringComparison.OrdinalIgnoreCase))
            {
                var capacity = loggingOptions?.Channel?.Capacity != null ? Math.Max(1, loggingOptions.Channel.Capacity.Value) : _defaultChannelCapacity;
                Logs = Channel.CreateBounded<FlexLogContext>(new BoundedChannelOptions(capacity)
                {
                    AllowSynchronousContinuations = false,
                    Capacity = capacity,
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropWrite
                });
            }
            else
            {
                Logs = Channel.CreateUnbounded<FlexLogContext>(new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = false,
                    SingleWriter = false,
                    SingleReader = true
                });
            }

            _sinks = new List<FlexLogSink>(sinks);
            _fallbackSinks = new List<FlexLogSink>(fallbackSinks);
        }

        /// <summary>
        /// Adds a new log context to the log channel and calculates the elapsed time as well.
        /// </summary>
        /// <param name="logContext">New log context to be added.</param>
        public void AddLogContextToChannel(FlexLogContext logContext)
        {
            logContext.ElapsedTimeInMilliseconds = (DateTime.UtcNow - logContext.Timestamp).TotalMilliseconds;
            Logs.Writer.TryWrite(logContext);
        }
    }
}
