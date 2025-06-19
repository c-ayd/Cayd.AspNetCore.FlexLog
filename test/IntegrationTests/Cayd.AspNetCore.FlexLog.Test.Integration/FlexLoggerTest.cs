using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.DependencyInjection;
using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Sinks;
using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using Cayd.AspNetCore.FlexLog.Test.Integration.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public class FlexLoggerTest
    {
        public enum EAppsettingsConfig
        {
            None            =   0,
            Minimum         =   1,
            All             =   2,
            AllExtra        =   3
        }

        private string AppsettingsConfigEnumToString(EAppsettingsConfig appsettings)
        {
            return appsettings switch
            {
                EAppsettingsConfig.AllExtra => "appsettings.AllExtra.json",
                EAppsettingsConfig.All => "appsettings.All.json",
                EAppsettingsConfig.Minimum => "appsettings.Minimum.json",
                EAppsettingsConfig.None => "appsettings.None.json",
                _ => "appsettings.None.json",
            };
        }

#if NET6_0_OR_GREATER
        private async Task<(IHost host, HttpClient client)> CreateHost(EAppsettingsConfig appsettings, FlexLogSink sink, FlexLogSink? fallbackSink = null)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("Utilities/" + AppsettingsConfigEnumToString(appsettings), false);
                })
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseTestServer()
                        .UseStartup<Startup>()
                        .ConfigureServices((context, services) =>
                        {
                            services.AddFlexLog(context.Configuration, config =>
                            {
                                config.AddSink(sink);

                                if (fallbackSink != null)
                                {
                                    config.AddFallbackSink(fallbackSink);
                                }
                            });
                        });
                })
                .Build();

            await host.StartAsync();
            var client = host.GetTestClient();
            return (host, client);
        }

        private async Task Dispose(IHost host, HttpClient client)
        {
            await host.StopAsync();
            host.Dispose();
            client.Dispose();
        }
#else
        private (TestServer server, HttpClient client) CreateServer(EAppsettingsConfig appsettings, FlexLogSink sink, FlexLogSink? fallbackSink = null)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Utilities/" + AppsettingsConfigEnumToString(appsettings), optional: false)
                .Build();

            var server = new TestServer(new WebHostBuilder()
                .UseConfiguration(config)
                .UseStartup<Startup>()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton<IConfiguration>(config);

                    services.AddFlexLog(config, config =>
                    {
                        config.AddSink(sink);

                        if (fallbackSink != null)
                        {
                            config.AddFallbackSink(fallbackSink);
                        }
                    });
                }));

            var client = server.CreateClient();
            return (server, client);
        }

        private void Dispose(TestServer server, HttpClient client)
        {
            server.Dispose();
            client.Dispose();
        }
#endif

        private object CreateRequestBody()
            => new { Email = "test@test.com", Password = "123456" };

        [Theory]
        [InlineData(EAppsettingsConfig.AllExtra)]
        [InlineData(EAppsettingsConfig.All)]
        [InlineData(EAppsettingsConfig.Minimum)]
        [InlineData(EAppsettingsConfig.None)]
        public async Task Endpoint1_WhenRequestIsMade_ShouldSendLogToSink(EAppsettingsConfig appsettings)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettings, sink);
#else
            var (server, client) = CreateServer(appsettings, sink);
#endif

            // Act
            var result = (await client.PostAsJsonAsync("/", CreateRequestBody())).IsSuccessStatusCode;

            // Assert
            Assert.True(result, "Something happened while executing the endpoint 1.");

            var buffer = await sink.GetBuffer();
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "Elapsed time is zero.");
            Assert.True(buffer[0].LogEntries.Count > 0, "There is no log entry");
            Assert.Equal(ELogLevel.Information, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
            Assert.Equal("test info 1", buffer[0].LogEntries[0].Message);
            Assert.Null(buffer[0].LogEntries[0].Exception);
            Assert.NotNull(buffer[0].LogEntries[0].Metadata);

            RequestModel? request;
            ResponseModel? response;
            switch (appsettings)
            {
                case EAppsettingsConfig.AllExtra:
                    Assert.Equal("1234-5678", buffer[0].Id);

                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status200OK, buffer[0].ResponseStatusCode);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.All:
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status200OK, buffer[0].ResponseStatusCode);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.Minimum:
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Null(buffer[0].RequestBodyContentType);
                    Assert.Null(buffer[0].RequestBodyRaw);
                    Assert.Null(buffer[0].RequestBody);
                    Assert.Null(buffer[0].RequestBodySizeInBytes);
                    Assert.Null(buffer[0].IsRequestBodyTooLarge);

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Null(buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.None:
                default:
                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("123456", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status200OK, buffer[0].ResponseStatusCode);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    Assert.Equal(JsonValueKind.Number, response.Nested.Secret.RootElement.ValueKind);
                    Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    break;
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Theory]
        [InlineData(EAppsettingsConfig.AllExtra)]
        [InlineData(EAppsettingsConfig.All)]
        [InlineData(EAppsettingsConfig.Minimum)]
        [InlineData(EAppsettingsConfig.None)]
        public async Task IgnoreEndpoint_WhenRequestIsMade_ShouldIgnoreLogDependingOnSettings(EAppsettingsConfig appsettings)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettings, sink);
#else
            var (server, client) = CreateServer(appsettings, sink);
#endif

            // Act
            var result = (await client.GetAsync("/ignore")).IsSuccessStatusCode;

            // Assert
            Assert.True(result, "Something happened while executing the endpoint 2.");

            IReadOnlyList<FlexLogContext>? buffer;
            switch (appsettings)
            {
                case EAppsettingsConfig.AllExtra:
                case EAppsettingsConfig.All:
                    var bufferTask = sink.GetBuffer();
                    if (await Task.WhenAny(bufferTask, Task.Delay(15 * 1000)) == bufferTask)
                    {
                        Assert.Fail("Buffer task is compeleted. It should not have been happened");
                    }
                    break;
                case EAppsettingsConfig.Minimum:
                    buffer = await sink.GetBuffer();
                    Assert.Equal(1, buffer.Count);
                    Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "Elapsed time is zero.");

                    Assert.True(buffer[0].LogEntries.Count > 0, "There is no log entry");
                    Assert.Equal(ELogLevel.Warning, buffer[0].LogEntries[0].LogLevel);
                    Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
                    Assert.Equal("test info 2", buffer[0].LogEntries[0].Message);
                    Assert.Null(buffer[0].LogEntries[0].Exception);
                    Assert.Null(buffer[0].LogEntries[0].Metadata);

                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    Assert.Equal("GET /ignore", buffer[0].RequestLine);
                    Assert.Null(buffer[0].RequestBodyContentType);
                    Assert.Null(buffer[0].RequestBodyRaw);
                    Assert.Null(buffer[0].RequestBody);
                    Assert.Null(buffer[0].RequestBodySizeInBytes);
                    Assert.Null(buffer[0].IsRequestBodyTooLarge);

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Null(buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.None:
                default:
                    buffer = await sink.GetBuffer();
                    Assert.Equal(1, buffer.Count);
                    Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "Elapsed time is zero.");

                    Assert.True(buffer[0].LogEntries.Count > 0, "There is no log entry");
                    Assert.Equal(ELogLevel.Warning, buffer[0].LogEntries[0].LogLevel);
                    Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
                    Assert.Equal("test info 2", buffer[0].LogEntries[0].Message);
                    Assert.Null(buffer[0].LogEntries[0].Exception);
                    Assert.Null(buffer[0].LogEntries[0].Metadata);

                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);

                    Assert.Equal("GET /ignore", buffer[0].RequestLine);
                    Assert.Null(buffer[0].RequestBodyContentType);
                    Assert.Null(buffer[0].RequestBodyRaw);
                    Assert.Null(buffer[0].RequestBody);
                    Assert.Null(buffer[0].RequestBodySizeInBytes);
                    Assert.Null(buffer[0].IsRequestBodyTooLarge);

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Equal("text/plain", buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status200OK, buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Theory]
        [InlineData(EAppsettingsConfig.AllExtra)]
        [InlineData(EAppsettingsConfig.All)]
        [InlineData(EAppsettingsConfig.Minimum)]
        [InlineData(EAppsettingsConfig.None)]
        public async Task ExceptionEndpoint_WhenRequestIsMade_ShouldRequestReturn500AndRelatedLog(EAppsettingsConfig appsettings)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettings, sink);
#else
            var (server, client) = CreateServer(appsettings, sink);
#endif

            // Act
            var result = (await client.PostAsJsonAsync("/exception", CreateRequestBody())).IsSuccessStatusCode;

            // Assert
            Assert.False(result, "Endpoint 3 was successful.");

            var buffer = await sink.GetBuffer();
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "Elapsed time is zero.");
            Assert.True(buffer[0].LogEntries.Count > 0, "There is no log entry");
            Assert.Equal(ELogLevel.Error, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Middlewares.FlexLogMiddleware", buffer[0].LogEntries[0].Category);
            Assert.Equal("test exception", buffer[0].LogEntries[0].Message);
            Assert.NotNull(buffer[0].LogEntries[0].Exception);
            Assert.IsType<ArgumentException>(buffer[0].LogEntries[0].Exception);
            Assert.Null(buffer[0].LogEntries[0].Metadata);

            RequestModel? request;
            switch (appsettings)
            {
                case EAppsettingsConfig.AllExtra:
                    Assert.Equal("1234-5678", buffer[0].Id);

                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /exception", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status500InternalServerError, buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.All:
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /exception", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status500InternalServerError, buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.Minimum:
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    Assert.Equal("POST /exception", buffer[0].RequestLine);
                    Assert.Null(buffer[0].RequestBodyContentType);
                    Assert.Null(buffer[0].RequestBodyRaw);
                    Assert.Null(buffer[0].RequestBody);
                    Assert.Null(buffer[0].RequestBodySizeInBytes);
                    Assert.Null(buffer[0].IsRequestBodyTooLarge);

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status500InternalServerError, buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.None:
                default:
                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("123456", request.Password);
                    Assert.Equal("POST /exception", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status500InternalServerError, buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Theory]
        [InlineData(EAppsettingsConfig.AllExtra)]
        [InlineData(EAppsettingsConfig.All)]
        [InlineData(EAppsettingsConfig.Minimum)]
        [InlineData(EAppsettingsConfig.None)]
        public async Task Endpoint1_WhenSinkIsFaulty_ShouldSendBufferToFallbackSink(EAppsettingsConfig appsettings)
        {
            // Arrange
            var faultySink = new TestFaultySink();
            var fallbackSink = new TestFallbackSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettings, faultySink, fallbackSink);
#else
            var (server, client) = CreateServer(appsettings, faultySink, fallbackSink);
#endif

            // Act
            var result = (await client.PostAsJsonAsync("/", CreateRequestBody())).IsSuccessStatusCode;

            // Assert
            Assert.True(result, "Something happened while executing the endpoint 1.");

            var buffer = await fallbackSink.GetBuffer();
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "Elapsed time is zero.");
            Assert.True(buffer[0].LogEntries.Count > 0, "There is no log entry");
            Assert.Equal(ELogLevel.Information, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
            Assert.Equal("test info 1", buffer[0].LogEntries[0].Message);
            Assert.Null(buffer[0].LogEntries[0].Exception);
            Assert.NotNull(buffer[0].LogEntries[0].Metadata);

            RequestModel? request;
            ResponseModel? response;
            switch (appsettings)
            {
                case EAppsettingsConfig.AllExtra:
                    Assert.Equal("1234-5678", buffer[0].Id);

                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.All:
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.Minimum:
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Null(buffer[0].RequestBodyContentType);
                    Assert.Null(buffer[0].RequestBodyRaw);
                    Assert.Null(buffer[0].RequestBody);
                    Assert.Null(buffer[0].RequestBodySizeInBytes);
                    Assert.Null(buffer[0].IsRequestBodyTooLarge);

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.None:
                default:
                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("123456", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    Assert.Equal(JsonValueKind.Number, response.Nested.Secret.RootElement.ValueKind);
                    Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    break;
            }

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Theory]
        [InlineData(EAppsettingsConfig.AllExtra)]
        [InlineData(EAppsettingsConfig.All)]
        [InlineData(EAppsettingsConfig.Minimum)]
        [InlineData(EAppsettingsConfig.None)]
        public async Task Endpoint1_WhenApplicationShutsDown_ShouldFlushSink(EAppsettingsConfig appsettings)
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettings, sink);
#else
            var (server, client) = CreateServer(appsettings, sink);
#endif

            // Act
            var result = (await client.PostAsJsonAsync("/", CreateRequestBody())).IsSuccessStatusCode;

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif

            // Assert
            Assert.True(result, "Something happened while executing the endpoint 1.");

            var buffer = await sink.GetBuffer();
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "Elapsed time is zero.");
            Assert.True(buffer[0].LogEntries.Count > 0, "There is no log entry");
            Assert.Equal(ELogLevel.Information, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
            Assert.Equal("test info 1", buffer[0].LogEntries[0].Message);
            Assert.Null(buffer[0].LogEntries[0].Exception);
            Assert.NotNull(buffer[0].LogEntries[0].Metadata);

            RequestModel? request;
            ResponseModel? response;
            switch (appsettings)
            {
                case EAppsettingsConfig.AllExtra:
                    Assert.Equal("1234-5678", buffer[0].Id);

                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status200OK, buffer[0].ResponseStatusCode);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.All:
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status200OK, buffer[0].ResponseStatusCode);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.Minimum:
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Null(buffer[0].RequestBodyContentType);
                    Assert.Null(buffer[0].RequestBodyRaw);
                    Assert.Null(buffer[0].RequestBody);
                    Assert.Null(buffer[0].RequestBodySizeInBytes);
                    Assert.Null(buffer[0].IsRequestBodyTooLarge);

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Null(buffer[0].ResponseStatusCode);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.None:
                default:
                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("123456", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(StatusCodes.Status200OK, buffer[0].ResponseStatusCode);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    Assert.Equal(JsonValueKind.Number, response.Nested.Secret.RootElement.ValueKind);
                    Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    break;
            }
        }

        [Theory]
        [InlineData(EAppsettingsConfig.AllExtra)]
        [InlineData(EAppsettingsConfig.All)]
        [InlineData(EAppsettingsConfig.Minimum)]
        [InlineData(EAppsettingsConfig.None)]
        public async Task Endpoint1_WhenApplicationShutsDownAndSinkIsFault_ShouldFlushFallbackSink(EAppsettingsConfig appsettings)
        {
            // Arrange
            var faultySink = new TestFaultySink();
            var fallbackSink = new TestFallbackSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost(appsettings, faultySink, fallbackSink);
#else
            var (server, client) = CreateServer(appsettings, faultySink, fallbackSink);
#endif

            // Act
            var result = (await client.PostAsJsonAsync("/", CreateRequestBody())).IsSuccessStatusCode;

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif

            // Assert
            Assert.True(result, "Something happened while executing the endpoint 1.");

            var buffer = await fallbackSink.GetBuffer();
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "Elapsed time is zero.");
            Assert.True(buffer[0].LogEntries.Count > 0, "There is no log entry");
            Assert.Equal(ELogLevel.Information, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
            Assert.Equal("test info 1", buffer[0].LogEntries[0].Message);
            Assert.Null(buffer[0].LogEntries[0].Exception);
            Assert.NotNull(buffer[0].LogEntries[0].Metadata);

            RequestModel? request;
            ResponseModel? response;
            switch (appsettings)
            {
                case EAppsettingsConfig.AllExtra:
                    Assert.Equal("1234-5678", buffer[0].Id);

                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.All:
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("REDACTED", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");
                    Assert.False(buffer[0].IsRequestBodyTooLarge, "Request body size is too large.");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    if (response.Nested.Secret.RootElement.ValueKind == JsonValueKind.Number)
                    {
                        Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    }
                    else
                    {
                        Assert.Equal("REDACTED", response.Nested.Secret.RootElement.GetString());
                    }
                    break;
                case EAppsettingsConfig.Minimum:
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "NameIdentifier claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Email, out var _), "Email claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.Name, out var _), "Name claim type is included in the claims.");
                    Assert.False(buffer[0].Claims.TryGetValue("CustomClaim", out var _), "CustomClaim claim type is included in the claims.");

                    Assert.False(buffer[0].Headers.TryGetValue("User-Agent", out var _), "User-Agent header is included in the header.");
                    Assert.False(buffer[0].Headers.TryGetValue("Connection", out var _), "Connection header is included in the header.");

                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Null(buffer[0].RequestBodyContentType);
                    Assert.Null(buffer[0].RequestBodyRaw);
                    Assert.Null(buffer[0].RequestBody);
                    Assert.Null(buffer[0].RequestBodySizeInBytes);
                    Assert.Null(buffer[0].IsRequestBodyTooLarge);

                    Assert.Null(buffer[0].ResponseBody);
                    Assert.Null(buffer[0].ResponseBodyContentType);
                    Assert.Null(buffer[0].ResponseBodyRaw);
                    Assert.Null(buffer[0].ResponseBody);
                    break;
                case EAppsettingsConfig.None:
                default:
                    Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
                    Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
                    Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
                    Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);

                    Assert.Equal("TestAgent", buffer[0].Headers["User-Agent"]);
                    Assert.Equal("TestConnection", buffer[0].Headers["Connection"]);

                    request = JsonSerializer.Deserialize<RequestModel>(buffer[0].RequestBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(request);
                    Assert.Equal("test@test.com", request.Email);
                    Assert.Equal("123456", request.Password);
                    Assert.Equal("POST /", buffer[0].RequestLine);
                    Assert.Equal("application/json", buffer[0].RequestBodyContentType);
                    Assert.NotNull(buffer[0].RequestBodySizeInBytes);
                    Assert.True(buffer[0].RequestBodySizeInBytes > 0, "Request body size is zero");

                    response = JsonSerializer.Deserialize<ResponseModel>(buffer[0].ResponseBody!, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Assert.NotNull(response);
                    Assert.Equal("application/json", buffer[0].ResponseBodyContentType);
                    Assert.Equal(JsonValueKind.Number, response.Value.RootElement.ValueKind);
                    Assert.Equal(456, response.Value.RootElement.GetInt64());
                    Assert.Equal(JsonValueKind.Number, response.Nested.Secret.RootElement.ValueKind);
                    Assert.Equal(789, response.Nested.Secret.RootElement.GetInt64());
                    break;
            }
        }
    }
}
