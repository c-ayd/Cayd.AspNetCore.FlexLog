#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Sinks
{
    public class TestFallbackSink : FlexLogSink
    {
        private TaskCompletionSource<IReadOnlyList<FlexLogContext>> _tcs = new TaskCompletionSource<IReadOnlyList<FlexLogContext>>();
        public async Task<IReadOnlyList<FlexLogContext>> GetBuffer() => await _tcs.Task;

        public TestFallbackSink()
        {
            _tcs = new TaskCompletionSource<IReadOnlyList<FlexLogContext>>();
        }

        public override async Task FlushAsync(IReadOnlyList<FlexLogContext> buffer)
        {
            _tcs.SetResult(new List<FlexLogContext>(buffer));
        }
    }
}
