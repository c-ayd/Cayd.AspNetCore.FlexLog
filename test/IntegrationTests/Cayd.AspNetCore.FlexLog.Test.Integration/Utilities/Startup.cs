using Cayd.AspNetCore.FlexLog.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseMiddleware<AddClaimMiddleware>();
            app.UseMiddleware<AddHeaderMiddleware>();

            app.UseFlexLog();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/", async context =>
                {
                    var flexLogger = context.RequestServices.GetRequiredService<IFlexLogger<Startup>>();
                    flexLogger.LogInformation("test info 1", new
                    {
                        Test = 123
                    });

                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Value = 456,
                        Nested = new
                        {
                            Secret = 789
                        }
                    });
                });

                endpoints.MapGet("/ignore", async context =>
                {
                    var flexLogger = context.RequestServices.GetRequiredService<IFlexLogger<Startup>>();
                    flexLogger.LogWarning("test info 2");

                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Endpoint2");
                });

                endpoints.MapPost("/exception", context =>
                {
                    throw new ArgumentException("test exception");
                });
            });
        }
    }
}
