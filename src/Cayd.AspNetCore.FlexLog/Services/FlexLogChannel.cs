using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;
using System.Threading.Channels;

namespace Cayd.AspNetCore.FlexLog.Services
{
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
                Logs = Channel.CreateBounded<FlexLogContext>(new BoundedChannelOptions(loggingOptions?.Channel?.Capacity ?? _defaultChannelCapacity)
                {
                    AllowSynchronousContinuations = false,
                    Capacity = _defaultChannelCapacity,
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

        public void AddLogContextToChannel(FlexLogContext logContext)
        {
            Logs.Writer.TryWrite(logContext);
        }
    }
}
