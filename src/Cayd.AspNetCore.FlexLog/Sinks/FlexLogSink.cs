using Cayd.AspNetCore.FlexLog.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Sinks
{
    public abstract class FlexLogSink
    {
        public virtual Task InitializeAsync() => Task.CompletedTask;
        public virtual Task DisposeAsync() => Task.CompletedTask;
        public abstract Task FlushAsync(IReadOnlyCollection<FlexLogContext> buffer);
    }
}
