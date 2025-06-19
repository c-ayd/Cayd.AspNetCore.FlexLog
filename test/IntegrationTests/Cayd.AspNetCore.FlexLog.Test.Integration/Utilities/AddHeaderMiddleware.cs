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

            await _next(context);
        }
    }
}
