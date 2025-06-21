using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        private Utilities.ResponseBody.RequestModel CreateResponseBodyForResponseBodyEndpoint(int statusCode, bool returnJson)
            => new Utilities.ResponseBody.RequestModel() 
            { 
                StatusCode = statusCode,
                ReturnJson = returnJson 
            };

        private string GetResponseBodyAsString()
            => "{\"myString\":\"asd\",\"myInt\":5,\"nested\":{\"myString\":\"qwe\"}}";

        [Fact]
        public async Task ResponseBodyEndpoint_WhenNoOptionIsSet_ShouldLogResponseBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/ResponseBody/appsettings.ResponseBody.json", sink);
#else
            var (server, client) = CreateServer("Utilities/ResponseBody/appsettings.ResponseBody.json", sink);
#endif

            // Act
            var body = CreateResponseBodyForResponseBodyEndpoint(200, true);
            var isSuccessful = (await client.PostAsJsonAsync("/option/response-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
            Assert.Equal(200, buffer[0].ResponseStatusCode);
            Assert.Equal(GetResponseBodyAsString(), buffer[0].ResponseBody);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ResponseBodyEndpoint_WhenSpecificKeysAreRedactedForResponseBody_ShouldRedactThoseKeys()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/ResponseBody/appsettings.ResponseBody.RedactedKeys.json", sink);
#else
            var (server, client) = CreateServer("Utilities/ResponseBody/appsettings.ResponseBody.RedactedKeys.json", sink);
#endif

            // Act
            var body = CreateResponseBodyForResponseBodyEndpoint(201, true);
            var isSuccessful = (await client.PostAsJsonAsync("/option/response-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
            Assert.Equal(201, buffer[0].ResponseStatusCode);

            var responseBody = JsonSerializer.Deserialize<Utilities.ResponseBody.ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            })!;
            Assert.Equal("REDACTED", responseBody.MyString);
            Assert.Equal(5, responseBody.MyInt);
            Assert.Equal("REDACTED", responseBody.Nested.MyString);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ResponseBodyEndpoint_WhenRouteIsIgnoredForResponseBody_ShouldNotLogResponseBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/ResponseBody/appsettings.ResponseBody.IgnoredRoutes.json", sink);
#else
            var (server, client) = CreateServer("Utilities/ResponseBody/appsettings.ResponseBody.IgnoredRoutes.json", sink);
#endif

            // Act
            var body = CreateResponseBodyForResponseBodyEndpoint(200, true);
            var isSuccessful = (await client.PostAsJsonAsync("/option/response-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Null(buffer[0].ResponseBodyContentType);
            Assert.Null(buffer[0].ResponseStatusCode);
            Assert.Null(buffer[0].ResponseBodyRaw);
            Assert.Null(buffer[0].ResponseBody);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ResponseBodyEndpoint_WhenContentTypeIsNotJson_ShouldLogResponseBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/ResponseBody/appsettings.ResponseBody.json", sink);
#else
            var (server, client) = CreateServer("Utilities/ResponseBody/appsettings.ResponseBody.json", sink);
#endif

            // Act
            var body = CreateResponseBodyForResponseBodyEndpoint(200, false);
            var isSuccessful = (await client.PostAsJsonAsync("/option/response-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("text/plain", buffer[0].ResponseBodyContentType);
            Assert.Equal(200, buffer[0].ResponseStatusCode);
            Assert.Null(buffer[0].ResponseBodyRaw);
            Assert.Null(buffer[0].ResponseBody);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ResponseBodyEndpoint_WhenResponseBodyOptionIsNotEnabled_ShouldNotLogResponseBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/ResponseBody/appsettings.ResponseBody.NotEnabled.json", sink);
#else
            var (server, client) = CreateServer("Utilities/ResponseBody/appsettings.ResponseBody.NotEnabled.json", sink);
#endif

            // Act
            var body = CreateResponseBodyForResponseBodyEndpoint(200, true);
            var isSuccessful = (await client.PostAsJsonAsync("/option/response-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Null(buffer[0].ResponseBodyContentType);
            Assert.Null(buffer[0].ResponseStatusCode);
            Assert.Null(buffer[0].ResponseBodyRaw);
            Assert.Null(buffer[0].ResponseBody);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
