using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        private Utilities.RequestBody.RequestModel CreateRequestBodyForRequestBodyEndpoint()
            => new Utilities.RequestBody.RequestModel()
            {
                Email = "test@test.com",
                Password = "123456",
                Nested = new Utilities.RequestBody.RequestModel.NestedValue()
                {
                    MyString = "asd"
                }
            };

        private string GetRequestBodyAsString()
            => "{\"email\":\"test@test.com\",\"password\":\"123456\",\"nested\":{\"myString\":\"asd\"}}";

        [Fact]
        public async Task RequestBodyEndpoint_WhenNoOptionIsSet_ShouldLogRequestBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/RequestBody/appsettings.RequestBody.json", sink);
#else
            var (server, client) = CreateServer("Utilities/RequestBody/appsettings.RequestBody.json", sink);
#endif

            // Act
            var body = CreateRequestBodyForRequestBodyEndpoint();
            var isSuccessful = (await client.PostAsJsonAsync("/option/request-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("POST /option/request-body", buffer[0].RequestLine);
            Assert.Equal("application/json", buffer[0].RequestBodyContentType);
            Assert.Equal(GetRequestBodyAsString(), buffer[0].RequestBody);
            Assert.NotNull(buffer[0].RequestBodySizeInBytes);
            Assert.True(buffer[0].RequestBodySizeInBytes > 0, "The body size in the log is zero.");
            Assert.NotNull(buffer[0].IsRequestBodyTooLarge);
            Assert.False(buffer[0].IsRequestBodyTooLarge, "The request body size is too large according to the log.");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Theory]
        [InlineData("Utilities/RequestBody/appsettings.RequestBody.SizeSmall.json", false)]
        [InlineData("Utilities/RequestBody/appsettings.RequestBody.SizeLarge.json", true)]
        public async Task RequestBodyEndpoint_WhenRequestBodySizeIsSet_ShouldOrShouldNotLogRequestBodyInDetailDependingOnWhetherLimitIsLargeOrNot(string appsettingsPath, bool isLarge)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettingsPath, sink);
#else
            var (server, client) = CreateServer(appsettingsPath, sink);
#endif

            // Act
            var body = CreateRequestBodyForRequestBodyEndpoint();
            var isSuccessful = (await client.PostAsJsonAsync("/option/request-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("POST /option/request-body", buffer[0].RequestLine);
            Assert.Equal("application/json", buffer[0].RequestBodyContentType);

            if (isLarge)
            {
                Assert.Equal(GetRequestBodyAsString(), buffer[0].RequestBody);
                Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                Assert.True(buffer[0].RequestBodySizeInBytes > 0, "The body size in the log is zero.");
                Assert.NotNull(buffer[0].IsRequestBodyTooLarge);
                Assert.False(buffer[0].IsRequestBodyTooLarge, "The request body size is too large according to the log.");
            }
            else
            {
                Assert.Null(buffer[0].RequestBodyRaw);
                Assert.True(string.IsNullOrEmpty(buffer[0].RequestBody), "The request body is logged.");
                Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                Assert.True(buffer[0].RequestBodySizeInBytes > 0, "The body size in the log is zero.");
                Assert.NotNull(buffer[0].IsRequestBodyTooLarge);
                Assert.True(buffer[0].IsRequestBodyTooLarge, "The request body size is not large according to the log.");
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task RequestBodyEndpoint_WhenSpecificKeysAreRedactedForRequestBody_ShouldRedactOnlyThoseKeys()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/RequestBody/appsettings.RequestBody.RedactedKeys.json", sink);
#else
            var (server, client) = CreateServer("Utilities/RequestBody/appsettings.RequestBody.RedactedKeys.json", sink);
#endif

            // Act
            var body = CreateRequestBodyForRequestBodyEndpoint();
            var isSuccessful = (await client.PostAsJsonAsync("/option/request-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("POST /option/request-body", buffer[0].RequestLine);
            Assert.Equal("application/json", buffer[0].RequestBodyContentType);

            var requestBody = JsonSerializer.Deserialize<Utilities.RequestBody.RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            })!;
            Assert.Equal(body.Email, requestBody.Email);
            Assert.Equal("REDACTED", requestBody.Password);
            Assert.Equal("REDACTED", requestBody.Nested.MyString);

            Assert.NotNull(buffer[0].RequestBodySizeInBytes);
            Assert.True(buffer[0].RequestBodySizeInBytes > 0, "The body size in the log is zero.");
            Assert.NotNull(buffer[0].IsRequestBodyTooLarge);
            Assert.False(buffer[0].IsRequestBodyTooLarge, "The request body size is too large according to the log.");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task RequestBodyEndpoint_WhenRouteIsIgnoredForRequestBody_ShouldNotLogRequestBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/RequestBody/appsettings.RequestBody.IgnoredRoutes.json", sink);
#else
            var (server, client) = CreateServer("Utilities/RequestBody/appsettings.RequestBody.IgnoredRoutes.json", sink);
#endif

            // Act
            var body = CreateRequestBodyForRequestBodyEndpoint();
            var isSuccessful = (await client.PostAsJsonAsync("/option/request-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("POST /option/request-body", buffer[0].RequestLine);
            Assert.Null(buffer[0].RequestBodyContentType);
            Assert.Null(buffer[0].RequestBodyRaw);
            Assert.Null(buffer[0].RequestBody);
            Assert.Null(buffer[0].RequestBodySizeInBytes);
            Assert.Null(buffer[0].IsRequestBodyTooLarge);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task RequestBodyEndpoint_WhenContentTypeIsNotJson_ShouldNotLogRequestBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/RequestBody/appsettings.RequestBody.json", sink);
#else
            var (server, client) = CreateServer("Utilities/RequestBody/appsettings.RequestBody.json", sink);
#endif

            // Act
            var content = new StringContent("test", Encoding.UTF8, "text/plain");
            var isSuccessful = (await client.PostAsync("/option/request-body", content)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("POST /option/request-body", buffer[0].RequestLine);
            Assert.Equal("text/plain", buffer[0].RequestBodyContentType);
            Assert.Null(buffer[0].RequestBodyRaw);
            Assert.Null(buffer[0].RequestBody);
            Assert.Null(buffer[0].RequestBodySizeInBytes);
            Assert.Null(buffer[0].IsRequestBodyTooLarge);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task RequestBodyEndpoint_WhenRequestBodyOptionIsNotEnabled_ShouldNotLogRequestBody()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/RequestBody/appsettings.RequestBody.NotEnabled.json", sink);
#else
            var (server, client) = CreateServer("Utilities/RequestBody/appsettings.RequestBody.NotEnabled.json", sink);
#endif

            // Act
            var body = CreateRequestBodyForRequestBodyEndpoint();
            var isSuccessful = (await client.PostAsJsonAsync("/option/request-body", body)).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal("POST /option/request-body", buffer[0].RequestLine);
            Assert.Null(buffer[0].RequestBodyContentType);
            Assert.Null(buffer[0].RequestBodyRaw);
            Assert.Null(buffer[0].RequestBody);
            Assert.Null(buffer[0].RequestBodySizeInBytes);
            Assert.Null(buffer[0].IsRequestBodyTooLarge);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
