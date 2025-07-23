#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Sinks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Sinks
{
    public class TestFaultySink : FlexLogSink
    {
        public override async Task SaveLogsAsync(IReadOnlyList<FlexLogContext> buffer)
        {
            throw new Exception("Test exception");
        }
    }
}
