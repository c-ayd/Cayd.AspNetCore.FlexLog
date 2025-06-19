using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.DependencyInjection
{
    public class FlexLogConfig
    {
        private List<FlexLogSink> _sinks = new List<FlexLogSink>();
        private List<FlexLogSink> _fallbackSinks = new List<FlexLogSink>();

        public FlexLogConfig AddSink(FlexLogSink sink)
        {
            _sinks.Add(sink);
            return this;
        }

        public FlexLogConfig AddFallbackSink(FlexLogSink fallbackSink)
        {
            _fallbackSinks.Add(fallbackSink);
            return this;
        }

        public List<FlexLogSink> GetSinks() => _sinks;
        public List<FlexLogSink> GetFallbackSinks() => _fallbackSinks;
    }
}
