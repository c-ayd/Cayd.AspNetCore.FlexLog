using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        [Fact]
        public async Task HeadersEndpoint_WhenNoOptionIsSet_ShouldLogAllHeaders()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Headers/appsettings.Headers.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Headers/appsettings.Headers.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/headers")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("*/*", buffer[0].Headers["Accept"]);
            Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
            Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);
            Assert.Equal("1234-5678", buffer[0].Headers["Correlation-Id"]);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task HeadersEndpoint_WhenCorrelationIdOptionIsSet_ShouldChangeCorrelationId()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Headers/appsettings.Headers.CorrelationId.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Headers/appsettings.Headers.CorrelationId.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/headers")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("*/*", buffer[0].Headers["Accept"]);
            Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
            Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);
            Assert.Equal("1234-5678", buffer[0].Headers["Correlation-Id"]);
            Assert.Equal("1234-5678", buffer[0].CorrelationId);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task HeadersEndpoint_WhenSpecificHeadersAreIncluded_ShouldLogOnlyThoseHeaders()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Headers/appsettings.Headers.IncludedKeys.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Headers/appsettings.Headers.IncludedKeys.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/headers")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("TestAgent", buffer[0].Headers["user-agent"]);
            Assert.False(buffer[0].Headers.TryGetValue("Accept", out var _), "The accept header is in the log.");
            Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "The connection header is in the log.");
            Assert.False(buffer[0].Headers.TryGetValue("Correlation-Id", out var _), "The correlation ID header is in the log.");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task HeadersEndpoint_WhenSpecificHeadersAreIgnored_ShouldNotLogThoseHeaders()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Headers/appsettings.Headers.IgnoredKeys.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Headers/appsettings.Headers.IgnoredKeys.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/headers")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("*/*", buffer[0].Headers["Accept"]);
            Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);
            Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "The user agent header is in the log.");
            Assert.False(buffer[0].Headers.TryGetValue("Correlation-Id", out var _), "The correlation ID header is in the log.");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task HeadersEndpoint_WhenRouteIsIgnoredForHeaders_ShouldNotLogAnyHeader()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Headers/appsettings.Headers.IgnoredRoutes.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Headers/appsettings.Headers.IgnoredRoutes.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/headers")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(0, buffer[0].Headers.Count);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task HeadersEndpoint_WhenHeaderOptionIsNotEnabled_ShouldNotLogAnyHeader()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Headers/appsettings.Headers.NotEnabled.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Headers/appsettings.Headers.NotEnabled.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/headers")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(0, buffer[0].Headers.Count);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Theory]
        [InlineData("Utilities/Headers/appsettings.Headers.Limit.Drop.json", true)]
        [InlineData("Utilities/Headers/appsettings.Headers.Limit.Slice.json", false)]
        public async Task HeadersEndpoint_WhenHeaderLimitOptionIsEnabled_ShouldManipulateHeader(string appsettingsPath, bool drop)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettingsPath, sink);
#else
            var (server, client) = CreateServer(appsettingsPath, sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/headers")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Arrange
            Assert.Equal(1, buffer.Count);

            if (drop)
            {
                Assert.Equal("*/*", buffer[0].Headers["Accept"]);
                Assert.Equal("TOO LARGE", buffer[0].Headers["User-Agent"]);
                Assert.Equal("TOO LARGE", buffer[0].Headers["Connection"]);
                Assert.Equal("TOO LARGE", buffer[0].Headers["Correlation-Id"]);
            }
            else
            {
                Assert.Equal("*/*", buffer[0].Headers["Accept"]);
                Assert.Equal("TestA", buffer[0].Headers["User-Agent"]);
                Assert.Equal("TestC", buffer[0].Headers["Connection"]);
                Assert.Equal("1234-", buffer[0].Headers["Correlation-Id"]);
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
