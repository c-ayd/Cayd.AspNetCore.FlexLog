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
        private static readonly string _dropNewestStrategy = "DropNewest";
        private static readonly string _dropOldestStrategy = "DropOldest";
        private static readonly string _dropWriteStrategy = "DropWrite";

        private static readonly int _defaultChannelCapacity = 10_000;

        public Channel<FlexLogContext> Logs { get; private set; }

        private readonly List<IFlexLogSink> _sinks;
        public IReadOnlyList<IFlexLogSink> Sinks => _sinks;

        private readonly List<IFlexLogSink> _fallbackSinks;
        public IReadOnlyList<IFlexLogSink> FallbackSinks => _fallbackSinks;

        public FlexLogChannel(FlexLogOptions? loggingOptions, ICollection<IFlexLogSink> sinks, ICollection<IFlexLogSink> fallbackSinks)
        {
            if (loggingOptions?.Channel?.Strategy == _dropNewestStrategy)
            {
                Logs = Channel.CreateBounded<FlexLogContext>(new BoundedChannelOptions(loggingOptions.Channel?.Capacity ?? _defaultChannelCapacity)
                {
                    AllowSynchronousContinuations = false,
                    Capacity = _defaultChannelCapacity,
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropNewest
                });
            }
            else if (loggingOptions?.Channel?.Strategy == _dropOldestStrategy)
            {
                Logs = Channel.CreateBounded<FlexLogContext>(new BoundedChannelOptions(loggingOptions.Channel?.Capacity ?? _defaultChannelCapacity)
                {
                    AllowSynchronousContinuations = false,
                    Capacity = _defaultChannelCapacity,
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                });
            }
            else if (loggingOptions?.Channel?.Strategy == _dropWriteStrategy)
            {
                Logs = Channel.CreateBounded<FlexLogContext>(new BoundedChannelOptions(loggingOptions.Channel?.Capacity ?? _defaultChannelCapacity)
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

            _sinks = new List<IFlexLogSink>(sinks);
            _fallbackSinks = new List<IFlexLogSink>(fallbackSinks);
        }

        public void AddLogContextToChannel(FlexLogContext logContext)
        {
            Logs.Writer.TryWrite(logContext);
        }
    }
}
