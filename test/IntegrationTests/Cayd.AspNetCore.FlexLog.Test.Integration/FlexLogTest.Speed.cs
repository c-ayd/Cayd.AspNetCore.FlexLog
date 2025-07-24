using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        [Fact]
        public async Task FlexLogMiddleware_ShouldTakeLessThanTenMilliseconds()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/SpeedTest/appsettings.SpeedTest.json", sink);
#else
            var (server, client) = CreateServer("Utilities/SpeedTest/appsettings.SpeedTest.json", sink);
#endif

            await client.PostAsJsonAsync("/speed/log", new
            {
                Test = 123,
                Test2 = "abc"
            });
            await client.PostAsJsonAsync("/speed/no-log", new
            {
                Test = 123,
                Test2 = "abc"
            });

            // Act
            var numberOfTests = 100;

            var totalReferenceTime = 0.0;
            var minReferenceTime = double.MaxValue;

            var totalTime = 0.0;
            var minTime = double.MaxValue;

            for (int i = 0; i < numberOfTests; ++i)
            {
                var referenceStartTime = DateTime.UtcNow;
                var result = await client.PostAsJsonAsync("/speed/no-log", new
                {
                    Test = 123,
                    Test2 = "abc"
                });
                var referenceEndTime = DateTime.UtcNow;
                if (!result.IsSuccessStatusCode)
                    Assert.Fail("Something went wrong while making HTTP requests. (Reference)");

                var elapsedTime = (referenceEndTime - referenceStartTime).TotalMilliseconds;
                totalReferenceTime += elapsedTime;
                if (elapsedTime < minReferenceTime)
                {
                    minReferenceTime = elapsedTime;
                }

                var startTime = DateTime.UtcNow;
                result = await client.PostAsJsonAsync("/speed/log", new
                {
                    Test = 123,
                    Test2 = "abc"
                });
                var endTime = DateTime.UtcNow;
                if (!result.IsSuccessStatusCode)
                    Assert.Fail("Something went wrong while making HTTP requests. (Logging)");

                elapsedTime = (endTime - startTime).TotalMilliseconds;
                totalTime += elapsedTime;
                if (elapsedTime < minTime)
                {
                    minTime = elapsedTime;
                }
            }

            // Assert
            var averageReferenceElapsedTime = totalReferenceTime / numberOfTests;
            var averageElapsedTime = totalTime / numberOfTests;

            Assert.True(averageReferenceElapsedTime < averageElapsedTime, "The reference time is lower than the elapsed time. Rerun the test.");
            var difference = averageElapsedTime - averageReferenceElapsedTime;
            Assert.True(difference <= 10.0, $"The logging took more than 10 ms. Reference: {averageReferenceElapsedTime} - Elapsed: {averageElapsedTime}");

            _output.WriteLine($"Min Reference Time: {minReferenceTime} - Min Time: {minTime} - Average Difference: {difference}");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
