using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;
using System.Threading.Channels;

namespace Cayd.AspNetCore.FlexLog.Services
{
    public class FlexLogChannel
    {
        public Channel<FlexLogContext> Logs { get; private set; }

        private readonly List<IFlexLogSink> _sinks;
        public IReadOnlyList<IFlexLogSink> Sinks => _sinks;

        public FlexLogChannel(ICollection<IFlexLogSink> sinks)
        {
            Logs = Channel.CreateUnbounded<FlexLogContext>(new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = false,
                SingleWriter = false,
                SingleReader = true
            });

            _sinks = new List<IFlexLogSink>(sinks);
        }
    }
}
