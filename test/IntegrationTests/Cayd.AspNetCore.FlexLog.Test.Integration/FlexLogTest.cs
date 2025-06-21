using Cayd.AspNetCore.FlexLog.DependencyInjection;
using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.Sinks;
using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using Cayd.AspNetCore.FlexLog.Test.Integration.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
#if NET6_0_OR_GREATER
        internal static async Task<(IHost host, HttpClient client)> CreateHost(string appsettingsPath, FlexLogSink sink, FlexLogSink? fallbackSink = null)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile(appsettingsPath);
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

        internal static async Task Dispose(IHost host, HttpClient client)
        {
            await host.StopAsync();
            host.Dispose();
            client.Dispose();
        }
#else
        internal static (TestServer server, HttpClient client) CreateServer(string appsettingsPath, FlexLogSink sink, FlexLogSink? fallbackSink = null)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(appsettingsPath)
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

        internal static void Dispose(TestServer server, HttpClient client)
        {
            server.Dispose();
            client.Dispose();
        }
#endif

        [Fact]
        public async Task DefaultEndpoint_WhenRequestIsMade_ShouldLog()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/appsettings.FastTimer.json", sink);
#else
            var (server, client) = CreateServer("Utilities/appsettings.FastTimer.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "The elapsed time is not calculated.");
            Assert.Equal(1, buffer[0].LogEntries.Count);
            Assert.Equal(ELogLevel.Warning, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
            Assert.Equal("Test warning", buffer[0].LogEntries[0].Message);
            Assert.Null(buffer[0].LogEntries[0].Exception);
            Assert.Equal(123, ((dynamic)buffer[0].LogEntries[0].Metadata!).Test);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task DefaultEndpoint_WhenApplicationShutsDown_ShouldFlushBuffer()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/appsettings.SlowTimer.json", sink);
#else
            var (server, client) = CreateServer("Utilities/appsettings.SlowTimer.json", sink);
#endif

            // Act
            var startTime = DateTime.UtcNow;
            var isSuccessful = (await client.GetAsync("/")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
            var endTime = DateTime.UtcNow;

            if ((endTime - startTime).TotalSeconds >= 5)
                Assert.Fail("It took longer than 5 seconds. The timer might have flushed the buffer.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "The elapsed time is not calculated.");
            Assert.Equal(1, buffer[0].LogEntries.Count);
            Assert.Equal(ELogLevel.Warning, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
            Assert.Equal("Test warning", buffer[0].LogEntries[0].Message);
            Assert.Null(buffer[0].LogEntries[0].Exception);
            Assert.Equal(123, ((dynamic)buffer[0].LogEntries[0].Metadata!).Test);
        }

        [Fact]
        public async Task DefaultEndpoint_WhenSinkIsFaulty_ShouldFlushBufferToFallbackSink()
        {
            // Arrange
            var sink = new TestFaultySink();
            var fallbackSink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/appsettings.FastTimer.json", sink, fallbackSink);
#else
            var (server, client) = CreateServer("Utilities/appsettings.FastTimer.json", sink, fallbackSink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await fallbackSink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "The elapsed time is not calculated.");
            Assert.Equal(1, buffer[0].LogEntries.Count);
            Assert.Equal(ELogLevel.Warning, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Test.Integration.Utilities.Startup", buffer[0].LogEntries[0].Category);
            Assert.Equal("Test warning", buffer[0].LogEntries[0].Message);
            Assert.Null(buffer[0].LogEntries[0].Exception);
            Assert.Equal(123, ((dynamic)buffer[0].LogEntries[0].Metadata!).Test);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ExceptionEndpoint_WhenExceptionIsThrown_ShouldLogException()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/appsettings.FastTimer.json", sink);
#else
            var (server, client) = CreateServer("Utilities/appsettings.FastTimer.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/exception")).IsSuccessStatusCode;
            if (isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.True(buffer[0].ElapsedTimeInMilliseconds > 0, "The elapsed time is not calculated.");
            Assert.Equal(1, buffer[0].LogEntries.Count);
            Assert.Equal(ELogLevel.Error, buffer[0].LogEntries[0].LogLevel);
            Assert.Equal("Cayd.AspNetCore.FlexLog.Middlewares.FlexLogMiddleware", buffer[0].LogEntries[0].Category);
            Assert.Equal("Test exception", buffer[0].LogEntries[0].Message);
            Assert.NotNull(buffer[0].LogEntries[0].Exception);
            Assert.IsType<TestException>(buffer[0].LogEntries[0].Exception);
            Assert.Null(buffer[0].LogEntries[0].Metadata);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
