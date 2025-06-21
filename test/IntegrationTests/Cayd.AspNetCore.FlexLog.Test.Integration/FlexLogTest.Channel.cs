using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        [Fact]
        public async Task ChannelEndpoint_WhenChannelIsUnbounded_ShouldAcceptAllLogs()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Channel/appsettings.Channel.Unbounded.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Channel/appsettings.Channel.Unbounded.json", sink);
#endif

            // Act
            for (int i = 0; i < 10000; ++i)
            {
                var isSuccessful = (await client.PostAsJsonAsync("/option/channel", new
                {
                    Number = i
                }))
                .IsSuccessStatusCode;

                if (!isSuccessful)
                    Assert.Fail("Something went wrong while making HTTP requests.");
            }

            var buffer = await sink.GetBuffer();

            // Arrange
            Assert.Equal(10000, buffer.Count);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ChannelEndpoint_WhenChannelIsDropWrite_ShouldNotLogIncomingLog()
        {
            // Arrange
            var sink = new TestDelayedSink(10000);
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Channel/appsettings.Channel.DropWrite.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Channel/appsettings.Channel.DropWrite.json", sink);
#endif

            // Act
            for (int i = 0; i < 21; ++i)
            {
                var isSuccessful = (await client.PostAsJsonAsync("/option/channel", new
                {
                    Number = i
                }))
                .IsSuccessStatusCode;

                if (!isSuccessful)
                    Assert.Fail("Something went wrong while making HTTP requests.");
            }

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(10, buffer.Count);
            Assert.False(buffer.Any(l => l.LogEntries.Any(e => e.Message == "20")),
                "The last log is in the buffer. It should have been dropped.");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
