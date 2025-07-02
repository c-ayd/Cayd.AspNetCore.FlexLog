using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Sinks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Sinks
{
    public class TestSinkResource : FlexLogSink
    {
        public int Counter { get; private set; } = 0;

        public override async Task InitializeAsync()
        {
            ++Counter;
        }

        public override async Task DisposeAsync()
        {
            --Counter;
        }

        public override async Task WriteBatchAsync(IReadOnlyList<FlexLogContext> buffer)
        {
        }
    }
}
