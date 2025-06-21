#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Cayd.AspNetCore.FlexLog.Exceptions;
using Cayd.AspNetCore.FlexLog.Test.Integration.Sinks;
using System.Threading.Tasks;
using Xunit;

namespace Cayd.AspNetCore.FlexLog.Test.Integration
{
    public partial class FlexLogTest
    {
        [Theory]
        [InlineData("Utilities/DependencyInjection/appsettings.InvalidRoute.json")]
        [InlineData("Utilities/DependencyInjection/appsettings.InvalidRoute.Claims.json")]
        [InlineData("Utilities/DependencyInjection/appsettings.InvalidRoute.Headers.json")]
        [InlineData("Utilities/DependencyInjection/appsettings.InvalidRoute.RequestBody.json")]
        [InlineData("Utilities/DependencyInjection/appsettings.InvalidRoute.ResponseBody.json")]
        [InlineData("Utilities/DependencyInjection/appsettings.InvalidRoute.QueryString.json")]
        public async Task AddFlexLog_WhenRouteValueDoesNotStartWithSlash_ShouldThrowException(string appsettingsPath)
        {
            // Arrange
            var sink = new TestSink();

            // Act
#if NET6_0_OR_GREATER
            var result = await Record.ExceptionAsync(async () =>
            {
                await CreateHost(appsettingsPath, sink);
            });
#else
            var result = Record.Exception(() =>
            {
                CreateServer(appsettingsPath, sink);
            });
#endif

            // Assert
            Assert.NotNull(result);
            Assert.IsType<InvalidRouteFormatException>(result);
        }
    }
}
