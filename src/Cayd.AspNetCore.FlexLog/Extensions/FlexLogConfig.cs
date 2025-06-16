using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Extensions
{
    public class FlexLogConfig
    {
        private List<IFlexLogSink> _sinks = new List<IFlexLogSink>();

        public FlexLogConfig AddSink(IFlexLogSink sink)
        {
            _sinks.Add(sink);
            return this;
        }

        public IReadOnlyList<IFlexLogSink> GetSinks() => _sinks;
    }
}
