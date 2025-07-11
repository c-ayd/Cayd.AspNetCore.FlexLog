﻿#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.Logging;
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
        public enum ETestType
        {
            None,
            Claims,
            Headers,
            RequestBody,
            ResponseBody,
            QueryString,
            All
        }

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

        [Theory]
        [InlineData("Utilities/StressTest/appsettings.StressTest.None.json", ETestType.None)]
        [InlineData("Utilities/StressTest/appsettings.StressTest.Claims.json", ETestType.Claims)]
        [InlineData("Utilities/StressTest/appsettings.StressTest.Headers.json", ETestType.Headers)]
        [InlineData("Utilities/StressTest/appsettings.StressTest.RequestBody.json", ETestType.RequestBody)]
        [InlineData("Utilities/StressTest/appsettings.StressTest.ResponseBody.json", ETestType.ResponseBody)]
        [InlineData("Utilities/StressTest/appsettings.StressTest.QueryString.json", ETestType.QueryString)]
        [InlineData("Utilities/StressTest/appsettings.StressTest.All.json", ETestType.All)]
        public async Task StressEndpoint_WhenFiftyThousandRequestsAreMade_ShouldLogAll(string appsettingsPath, ETestType testType)
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
            var (host, client) = await FlexLogTest.CreateHost(appsettingsPath, sink);
#else
            var (server, client) = FlexLogTest.CreateServer(appsettingsPath, sink);
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

                Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "The elapsed time is not calculated.");
                Assert.Equal(2, buffer[0].LogEntries.Count);
                Assert.Equal(ELogLevel.Information, buffer[0].LogEntries[0].LogLevel);
                Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.TestService", buffer[0].LogEntries[0].Category);
                Assert.Equal("Test info", buffer[0].LogEntries[0].Message);
                Assert.Null(buffer[0].LogEntries[0].Exception);
                Assert.Null(buffer[0].LogEntries[0].Metadata);
                Assert.Equal(ELogLevel.Information, buffer[0].LogEntries[1].LogLevel);
                Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[1].Category);
                Assert.Equal("Test info", buffer[0].LogEntries[1].Message);
                Assert.Null(buffer[0].LogEntries[1].Exception);

                int id = ((dynamic)buffer[0].LogEntries[1].Metadata!).Id;
                if (!ids.Remove(id))
                {
                    Assert.Fail($"ID #{id} was not found in the hash set.");
                }

                switch (testType)
                {
                    case ETestType.Claims:
                        CheckClaims(buffer);
                        break;
                    case ETestType.Headers:
                        CheckHeaders(buffer);
                        break;
                    case ETestType.RequestBody:
                        CheckRequestBody(buffer, strs, ints);
                        break;
                    case ETestType.ResponseBody:
                        CheckResponseBody(buffer, strs, ints);
                        break;
                    case ETestType.QueryString:
                        CheckQueryString(buffer);
                        break;
                    case ETestType.All:
                        CheckClaims(buffer);
                        CheckHeaders(buffer);
                        CheckRequestBody(buffer, strs, ints);
                        CheckResponseBody(buffer, strs, ints);
                        CheckQueryString(buffer);
                        break;
                    case ETestType.None:
                    default:
                        break;
                }
            }

#if NET6_0_OR_GREATER
            await FlexLogTest.Dispose(host, client);
#else
            FlexLogTest.Dispose(server, client);
#endif
        }

        private void CheckClaims(IReadOnlyList<FlexLogContext> buffer)
        {
            Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
            Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
            Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
            Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "The custom claim is in the log.");
        }

        private void CheckHeaders(IReadOnlyList<FlexLogContext> buffer)
        {
            Assert.Equal("1234-5678", buffer[0].CorrelationId);

            Assert.Equal("*/*", buffer[0].Headers["Accept"]);
            Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
            Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "The connection header is in the log.");
            Assert.Equal("1234-5678", buffer[0].Headers["Correlation-Id"]);
        }

        private void CheckRequestBody(IReadOnlyList<FlexLogContext> buffer, List<string> strs, List<int> ints)
        {
            Assert.Equal("POST /stress", buffer[0].Endpoint);
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
        }

        private void CheckResponseBody(IReadOnlyList<FlexLogContext> buffer, List<string> strs, List<int> ints)
        {
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

        private void CheckQueryString(IReadOnlyList<FlexLogContext> buffer)
        {
            Assert.Equal(CreateQueryStringForStressEndpoint(), buffer[0].QueryString);
        }
    }
}
