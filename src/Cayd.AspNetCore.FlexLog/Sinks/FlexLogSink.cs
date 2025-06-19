using Cayd.AspNetCore.FlexLog.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Sinks
{
    /// <summary>
    /// Marks the class as a sink for FlexLog.
    /// </summary>
    public abstract class FlexLogSink
    {
        /// <summary>
        /// Called once when the application start to initialize resources.
        /// </summary>
        public virtual Task InitializeAsync() => Task.CompletedTask;
        /// <summary>
        /// Called once when the application shuts down to release resources.
        /// </summary>
        /// <returns></returns>
        public virtual Task DisposeAsync() => Task.CompletedTask;
        /// <summary>
        /// Called by FlexLog to provide the logs in the buffer.
        /// </summary>
        /// <param name="buffer">List of logs to handle.</param>
        public abstract Task FlushAsync(IReadOnlyList<FlexLogContext> buffer);
    }
}
