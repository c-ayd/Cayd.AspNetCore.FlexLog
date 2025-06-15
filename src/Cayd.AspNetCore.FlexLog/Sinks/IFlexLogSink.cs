using Cayd.AspNetCore.FlexLog.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Sinks
{
    public interface IFlexLogSink
    {
        Task FlushAsync(IReadOnlyCollection<FlexLogContext> buffer);
    }
}
