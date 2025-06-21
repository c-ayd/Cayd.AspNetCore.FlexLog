using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        [Fact]
        public async Task IgnoreEndpoint_WhenEndpointIsInRouteFilter_ShouldNotLog()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/RouteFilter/appsettings.RouteFilter.json", sink);
#else
            var (server, client) = CreateServer("Utilities/RouteFilter/appsettings.RouteFilter.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/ignore")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            // Assert
            var bufferTask = sink.GetBuffer();
            if (await Task.WhenAny(bufferTask, Task.Delay(15 * 1000)) == bufferTask)
            {
                Assert.Fail("The buffer task is compeleted. It should not have been happened.");
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
