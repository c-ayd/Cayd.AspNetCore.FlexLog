using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Sinks
{
    public class TestDelayedSink : FlexLogSink
    {
        private int _delay = 0;
        private int _counter = 0;
        private TaskCompletionSource<IReadOnlyList<FlexLogContext>> _tcs = new TaskCompletionSource<IReadOnlyList<FlexLogContext>>();
        public async Task<IReadOnlyList<FlexLogContext>> GetBuffer() => await _tcs.Task;

        public TestDelayedSink(int delay)
        {
            _delay = delay;
        }

        public override async Task SaveLogsAsync(IReadOnlyList<FlexLogContext> buffer)
        {
            if (_counter == 0)
            {
                await Task.Delay(_delay);
                ++_counter;
                return;
            }

            _tcs.SetResult(buffer);
        }
    }
}
