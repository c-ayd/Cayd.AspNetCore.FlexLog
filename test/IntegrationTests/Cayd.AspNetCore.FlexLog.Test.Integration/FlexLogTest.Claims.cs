using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        [Fact]
        public async Task ClaimsEndpoint_WhenNoOptionIsSet_ShouldLogAllClaims()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Claims/appsettings.Claims.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Claims/appsettings.Claims.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/claims")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(5, buffer[0].Claims.Count);
            Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
            Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
            Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
            Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);
            Assert.Equal("Role1,Role2", buffer[0].Claims[ClaimTypes.Role]);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ClaimsEndpoint_WhenSpecificTypesAreIncluded_ShouldLogOnlyThoseClaims()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Claims/appsettings.Claims.IncludedTypes.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Claims/appsettings.Claims.IncludedTypes.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/claims")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(3, buffer[0].Claims.Count);
            Assert.Equal("TestUser", buffer[0].Claims[ClaimTypes.NameIdentifier]);
            Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
            Assert.Equal("CustomValue", buffer[0].Claims["CustomClaim"]);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ClaimsEndpoint_WhenSpecificTypesAreIgnored_ShouldNotLogThoseClaims()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Claims/appsettings.Claims.IgnoredTypes.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Claims/appsettings.Claims.IgnoredTypes.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/claims")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(3, buffer[0].Claims.Count);
            Assert.Equal("test@test.com", buffer[0].Claims[ClaimTypes.Email]);
            Assert.Equal("TestName", buffer[0].Claims[ClaimTypes.Name]);
            Assert.False(buffer[0].Claims.TryGetValue(ClaimTypes.NameIdentifier, out var _), "The name identifier claim is in the log.");
            Assert.False(buffer[0].Claims.TryGetValue("Customclaim", out var _), "The custom claim is in the log.");
            Assert.Equal("Role1,Role2", buffer[0].Claims[ClaimTypes.Role]);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ClaimsEndpoint_WhenRouteIsIgnoredForClaims_ShouldNotLogAnyClaim()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Claims/appsettings.Claims.IgnoredRoutes.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Claims/appsettings.Claims.IgnoredRoutes.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/claims")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(0, buffer[0].Claims.Count);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }

        [Fact]
        public async Task ClaimsEndpoint_WhenClaimOptionIsNotEnabled_ShouldNotLogAnyClaim()
        {
            // Arrange
            var sink = new TestSink();
#if NET6_0_OR_GREATER
            var (host, client) = await CreateHost("Utilities/Claims/appsettings.Claims.NotEnabled.json", sink);
#else
            var (server, client) = CreateServer("Utilities/Claims/appsettings.Claims.NotEnabled.json", sink);
#endif

            // Act
            var isSuccessful = (await client.GetAsync("/option/claims")).IsSuccessStatusCode;
            if (!isSuccessful)
                Assert.Fail("Something went wrong while making HTTP requests.");

            var buffer = await sink.GetBuffer();

            // Assert
            Assert.Equal(1, buffer.Count);
            Assert.Equal(0, buffer[0].Claims.Count);

#if NET6_0_OR_GREATER
            await Dispose(host, client);
#else
            Dispose(server, client);
#endif
        }
    }
}
