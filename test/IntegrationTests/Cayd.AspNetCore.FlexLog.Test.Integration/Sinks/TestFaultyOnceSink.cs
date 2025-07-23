using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Sinks;
using System;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Sinks
{
    public class TestFaultyOnceSink : FlexLogSink
    {
        private SemaphoreSlim _readSemaphore = new SemaphoreSlim(0, 1);
        private SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1, 1);
        private List<FlexLogContext> _buffer = new List<FlexLogContext>();

        private bool isFaulty = true;

        public async Task<IReadOnlyList<FlexLogContext>> GetBuffer()
        {
            await _readSemaphore.WaitAsync();

            var buffer = new List<FlexLogContext>(_buffer);

            _writeSemaphore.Release();

            return _buffer;
        }

        public override async Task SaveLogsAsync(IReadOnlyList<FlexLogContext> buffer)
        {
            if (isFaulty)
            {
                isFaulty = false;
                throw new Exception("Test exception");
            }

            await _writeSemaphore.WaitAsync();

            _buffer = new List<FlexLogContext>(buffer);

            _readSemaphore.Release();
        }
    }
}
