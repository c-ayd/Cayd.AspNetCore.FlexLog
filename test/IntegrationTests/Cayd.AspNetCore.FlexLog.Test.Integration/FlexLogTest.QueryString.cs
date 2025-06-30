using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        private string GetQueryStringForQueryStringEndpoint(bool empty)
            => empty ? string.Empty : "?myString=asd&myInt=5";
        private string GetShortQueryStringForQueryStringEndpoint()
            => "?id=5";

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueryStringEndpoint_WhenNoOptionIsSetAndThereIsQueryString_ShouldLogQueryString(bool emptyQueryString)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/QueryString/appsettings.QueryString.json", sink);
#else
            var (server, client) = CreateServer("Utilities/QueryString/appsettings.QueryString.json", sink);
#endif

            // Act
            var queryString = GetQueryStringForQueryStringEndpoint(emptyQueryString);
            var isSuccessful = (await client.GetAsync("/option/query-string" + queryString)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(queryString, buffer[0].QueryString);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task QueryStringEndpoint_WhenRouteIsIgnoredForQueryString_ShouldNotLogQueryString()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/QueryString/appsettings.QueryString.IgnoredRoutes.json", sink);
#else
            var (server, client) = CreateServer("Utilities/QueryString/appsettings.QueryString.IgnoredRoutes.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/query-string" + GetQueryStringForQueryStringEndpoint(false))).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Null(buffer[0].QueryString);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task QueryStringEndpoint_WhenQueryStringOptionIsNotEnabled_ShouldNotLogQueryString()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/QueryString/appsettings.QueryString.NotEnabled.json", sink);
#else
            var (server, client) = CreateServer("Utilities/QueryString/appsettings.QueryString.NotEnabled.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/query-string" + GetQueryStringForQueryStringEndpoint(false))).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Null(buffer[0].QueryString);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task QueryStringEndpoint_WhenQueryStringLimitOptionIsEnabled_ShouldAddOrSkipQueryStringDependingOnLength(bool inRange)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/QueryString/appsettings.QueryString.Limit.json", sink);
#else
            var (server, client) = CreateServer("Utilities/QueryString/appsettings.QueryString.Limit.json", sink);
#endif

            // Act
            var queryString = inRange ? GetShortQueryStringForQueryStringEndpoint() : GetQueryStringForQueryStringEndpoint(false);
            var isSuccessful = (await client.GetAsync("/option/query-string" + queryString)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);

            if (inRange)
            {
                Assert.Equal(queryString, buffer[0].QueryString);
            }
            else
            {
                Assert.Equal("TOO LARGE", buffer[0].QueryString);
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
