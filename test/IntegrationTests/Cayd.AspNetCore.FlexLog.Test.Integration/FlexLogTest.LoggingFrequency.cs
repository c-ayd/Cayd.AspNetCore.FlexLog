using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        [Fact]
        public async Task FrequencyEndpoint_WhenBufferIsFull_ShouldFlushWithoutWaitingForTimer()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/LoggingFrequency/appsettings.LoggingFrequency.json", sink);
#else
            var (server, client) = CreateServer("Utilities/LoggingFrequency/appsettings.LoggingFrequency.json", sink);
#endif

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < 3; ++i)
            {
                var isSuccessful = (await client.GetAsync("/option/frequency")).IsSuccessStatusCode;
                if (!isSuccessful)
                    Assert.Fail("Something went wrong while making HTTP requests.");
            }
            var endTime = DateTime.UtcNow;

            if ((endTime - startTime).TotalSeconds >= 5)
                Assert.Fail("It took longer than 5 seconds. The timer might have flushed the buffer.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(3, buffer.Count);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task FrequencyEndpoint_WhenBufferIsFullForFirstTimeAndNotForSecondTime_ShouldFlushWithoutWaitingForTimerForFirstTimeAndWaitForTimerForSecondTime()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/LoggingFrequency/appsettings.LoggingFrequency.json", sink);
#else
            var (server, client) = CreateServer("Utilities/LoggingFrequency/appsettings.LoggingFrequency.json", sink);
#endif

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < 5; ++i)
            {
                var isSuccessful = (await client.GetAsync("/option/frequency")).IsSuccessStatusCode;
                if (!isSuccessful)
                    Assert.Fail("Something went wrong while making HTTP requests.");
            }
            var endTime = DateTime.UtcNow;

            if ((endTime - startTime).TotalSeconds >= 5)
                Assert.Fail("It took longer than 5 seconds. The timer might have flushed the buffer for the first time.");

            // Assert
            var buffer = await sink.GetBuffer();

            Assert.Equal(3, buffer.Count);

            buffer = await sink.GetBuffer();
            endTime = DateTime.UtcNow;

            Assert.Equal(2, buffer.Count);
            Assert.True((endTime - startTime).TotalSeconds > 5, "The second read process was faster than the timer.");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
