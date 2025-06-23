#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public class FlexLogStressTest
    {
        private Utilities.StressTest.RequestModel CreateRequestBodyForStressEndpoint(int id, List<string> strs, List<int> ints)
        {
            var body = new Utilities.StressTest.RequestModel();
            body.Id = id;
            body.Str1 = "1";
            body.Str2 = "2";
            body.Str3 = "3";
            body.Str4 = "4";
            body.Str5 = "5";
            body.Int1 = 6;
            body.Int2 = 7;
            body.Int3 = 8;
            body.Int4 = 9;
            body.Int5 = 10;
            body.Nested.Strs = new List<string>(strs);
            body.Nested.Ints = new List<int>(ints);
            return body;
        }

        private string CreateQueryStringForStressEndpoint()
            => "?page=10&pageSize=50";

        [Fact]
        public async Task StressEndpoint_WhenFiftyThousandRequestsAreMade_ShouldLogAll()
        {
            // Arrange
            var ids = new HashSet<int>();
            for (int i = 0; i < 50000; ++i)
            {
                ids.Add(i);
            }

            var strs = new List<string>();
            var ints = new List<int>();

            for (int i = 0; i < 1000; ++i)
            {
                strs.Add(i.ToString());
                ints.Add(i);
            }

            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await FlexLogTest.CreateHost("Utilities/StressTest/appsettings.StressTest.json", sink);
#else
            var (server, client) = FlexLogTest.CreateServer("Utilities/StressTest/appsettings.StressTest.json", sink);
#endif

            // Act
            var endpoint = "/stress" + CreateQueryStringForStressEndpoint();
            for (int i = 0; i < 50000; ++i)
            {
                var body = CreateRequestBodyForStressEndpoint(i, strs, ints);
                client.PostAsJsonAsync(endpoint, body);
            }

            // Assert
            for (int i = 0; i < 5; ++i)
            {
                var buffer = await sink.GetBuffer();

                Assert.Equal(10000, buffer.Count);

                Assert.Equal("1234-5678", buffer[0].CorrelationId);

                Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "The elapsed time is not calculated.");
                Assert.Equal(1, buffer[0].LogEntries.Count);
                Assert.Equal(ELogLevel.Information, buffer[0].LogEntries[0].LogLevel);
                Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
                Assert.Equal("Test info", buffer[0].LogEntries[0].Message);
                Assert.Null(buffer[0].LogEntries[0].Exception);

                int id = ((dynamic)buffer[0].LogEntries[0].Metadata!).Id;
                if (!ids.Remove(id))
                {
                    Assert.Fail($"ID #{id} was not found in the hash set.");
                }

                Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
                Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "The custom claim is in the log.");

                Assert.Equal("*/*", buffer[0].Headers["Accept"]);
                Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "The connection header is in the log.");
                Assert.Equal("1234-5678", buffer[0].Headers["Correlation-Id"]);

                Assert.Equal(CreateQueryStringForStressEndpoint(), buffer[0].QueryString);

                Assert.Equal("POST /stress", buffer[0].RequestLine);
                Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                Assert.True(buffer[0].RequestBodySizeInBytes > 0, "The request body size in the log is zero.");
                Assert.NotNull(buffer[0].IsRequestBodyTooLarge);
                Assert.False(buffer[0].IsRequestBodyTooLarge, "The request body size is too large according to the log.");

                var requestBody = JsonSerializer.Deserialize<Utilities.StressTest.ResponseModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                })!;
                Assert.Equal("1", requestBody.Str1);
                Assert.Equal("2", requestBody.Str2);
                Assert.Equal("REDACTED", requestBody.Str3);
                Assert.Equal("REDACTED", requestBody.Str4);
                Assert.Equal("5", requestBody.Str5);
                Assert.Equal(6, requestBody.Int1);
                Assert.Equal(7, requestBody.Int2);
                Assert.Equal(8, requestBody.Int3);
                Assert.Equal(9, requestBody.Int4);
                Assert.Equal(10, requestBody.Int5);
                Assert.Equal(strs, requestBody.Nested.Strs);
                Assert.Equal(ints, requestBody.Nested.Ints);

                Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                Assert.Equal(201, buffer[0].ResponseStatusCode);

                var responseBody = JsonSerializer.Deserialize<Utilities.StressTest.ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                })!;
                Assert.Equal("REDACTED", responseBody.Str1);
                Assert.Equal("REDACTED", responseBody.Str2);
                Assert.Equal("3", responseBody.Str3);
                Assert.Equal("4", responseBody.Str4);
                Assert.Equal("5", responseBody.Str5);
                Assert.Equal(6, responseBody.Int1);
                Assert.Equal(7, responseBody.Int2);
                Assert.Equal(8, responseBody.Int3);
                Assert.Equal(9, responseBody.Int4);
                Assert.Equal(10, responseBody.Int5);
                Assert.Equal(strs, responseBody.Nested.Strs);
                Assert.Equal(ints, responseBody.Nested.Ints);
            }

#if NET6_0_OR_GREATER
            await FlexLogTest.Dispose(host, client);
#else
            FlexLogTest.Dispose(server, client);
#endif
        }
    }
}
