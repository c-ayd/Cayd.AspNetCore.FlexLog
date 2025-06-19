using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class AddHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public AddHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.Headers["User-Agent"] = "TestAgent";
            context.Request.Headers["Connection"] = "TestConnection";
            context.Request.Headers["Correlation-Id"] = "1234-5678";

            await _next(context);
        }
    }
}
