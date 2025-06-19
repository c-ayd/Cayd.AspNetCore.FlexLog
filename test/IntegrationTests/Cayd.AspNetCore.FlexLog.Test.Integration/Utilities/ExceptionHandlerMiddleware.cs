using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch
            {
                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    Status = 500,
                    Error = "Internal Server Error",
                    Message = "Something happened"
                });
            }
        }
    }
}
