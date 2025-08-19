using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class AddHeaderMiddleware
    {
        public static readonly Guid CorrelationId = Guid.NewGuid();

        private readonly RequestDelegate _next;

        public AddHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.Headers["Accept"] = "*/*";
            context.Request.Headers["User-Agent"] = "TestAgent";
            context.Request.Headers["Connection"] = "TestConnection";
            context.Request.Headers["Correlation-Id"] = CorrelationId.ToString();

            await _next(context);
        }
    }
}
