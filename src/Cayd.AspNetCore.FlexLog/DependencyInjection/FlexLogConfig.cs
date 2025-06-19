using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.DependencyInjection
{
    /// <summary>
    /// The sink builder class for FlexLog.
    /// </summary>
    public class FlexLogConfig
    {
        private List<FlexLogSink> _sinks = new List<FlexLogSink>();
        private List<FlexLogSink> _fallbackSinks = new List<FlexLogSink>();

        /// <summary>
        /// Adds a sink to FlexLog.
        /// </summary>
        /// <param name="sink">The custom sink implementation inheriting from <see cref="FlexLogSink"/> class to be added.</param>
        public FlexLogConfig AddSink(FlexLogSink sink)
        {
            _sinks.Add(sink);
            return this;
        }

        /// <summary>
        /// Adds a fallback sink to FlexLog.
        /// </summary>
        /// <param name="fallbackSink">The custom sink implementation inheriting from <see cref="FlexLogSink"/> class to be added.</param>
        public FlexLogConfig AddFallbackSink(FlexLogSink fallbackSink)
        {
            _fallbackSinks.Add(fallbackSink);
            return this;
        }

        public List<FlexLogSink> GetSinks() => _sinks;
        public List<FlexLogSink> GetFallbackSinks() => _fallbackSinks;
    }
}
