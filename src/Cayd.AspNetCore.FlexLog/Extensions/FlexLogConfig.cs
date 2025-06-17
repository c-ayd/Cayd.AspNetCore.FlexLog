using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Extensions
{
    public class FlexLogConfig
    {
        private List<IFlexLogSink> _sinks = new List<IFlexLogSink>();
        private List<IFlexLogSink> _fallbackSinks = new List<IFlexLogSink>();

        public FlexLogConfig AddSink(IFlexLogSink sink)
        {
            _sinks.Add(sink);
            return this;
        }

        public FlexLogConfig AddFallbackSink(IFlexLogSink fallbackSink)
        {
            _fallbackSinks.Add(fallbackSink);
            return this;
        }

        public List<IFlexLogSink> GetSinks() => _sinks;
        public List<IFlexLogSink> GetFallbackSinks() => _fallbackSinks;
    }
}
