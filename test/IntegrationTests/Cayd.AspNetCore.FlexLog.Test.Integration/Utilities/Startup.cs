using Cayd.AspNetCore.FlexLog.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Text.Json;

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
                endpoints.MapGet("/", async context =>
                {
                    var flexLogger = context.RequestServices.GetRequiredService<IFlexLogger<Startup>>();
                    flexLogger.LogWarning("Test warning", new
                    {
                        Test = 123
                    });

                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Default Endpoint");
                });

                endpoints.MapGet("/exception", context =>
                {
                    throw new TestException("Test exception");
                });

                endpoints.MapPost("/stress", async context =>
                {
                    var body = await JsonSerializer.DeserializeAsync<StressTest.RequestModel>(context.Request.Body, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var flexLogger = context.RequestServices.GetRequiredService<IFlexLogger<Startup>>();
                    flexLogger.LogInformation("Test info", new
                    {
                        Id = body!.Id
                    });

                    var strs = new List<string>();
                    var ints = new List<int>();

                    for (int i = 0; i < 1000; i++)
                    {
                        strs.Add(i.ToString());
                        ints.Add(i);
                    }

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status201Created;
                    await context.Response.WriteAsJsonAsync(new StressTest.ResponseModel()
                    {
                        Str1 = "1",
                        Str2 = "2",
                        Str3 = "3",
                        Str4 = "4",
                        Str5 = "5",
                        Int1 = 6,
                        Int2 = 7,
                        Int3 = 8,
                        Int4 = 9,
                        Int5 = 10,
                        Nested = new StressTest.ResponseModel.NestedValue()
                        {
                            Strs = strs,
                            Ints = ints
                        }
                    });
                });

                endpoints.MapPost("/option/channel", async context =>
                {
                    var body = await JsonSerializer.DeserializeAsync<Channel.RequestModel>(context.Request.Body, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (body == null)
                    {
                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Channel Endpoint");
                    }
                    else
                    {
                        var flexLogger = context.RequestServices.GetRequiredService<IFlexLogger<Startup>>();
                        flexLogger.LogInformation(body.Number.ToString());

                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync("Channel Endpoint");
                    }
                });

                endpoints.MapGet("/option/frequency", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Frequency Endpoint");
                });

                endpoints.MapGet("/option/ignore", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Ignore Endpoint");
                });

                endpoints.MapGet("/option/claims", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Claims Endpoint");
                });

                endpoints.MapGet("/option/headers", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Headers Endpoint");
                });

                endpoints.MapPost("/option/request-body", async context =>
                {
                    var contentType = context.Request.ContentType;
                    if (contentType == null)
                    {
                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Request Body Endpoint");
                        return;
                    }

                    if (contentType.StartsWith("application/json"))
                    {
                        var body = await JsonSerializer.DeserializeAsync<RequestBody.RequestModel>(context.Request.Body, new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (body == null)
                        {
                            context.Response.ContentType = "text/plain";
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync("Request Body Endpoint");
                        }
                        else
                        {
                            context.Response.ContentType = "text/plain";
                            context.Response.StatusCode = StatusCodes.Status200OK;
                            await context.Response.WriteAsync("Request Body Endpoint");
                        }
                    }
                    else
                    {
                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync("Request Body Endpoint");
                    }
                });

                endpoints.MapPost("/option/response-body", async context =>
                {
                    var body = await JsonSerializer.DeserializeAsync<ResponseBody.RequestModel>(context.Request.Body, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (body == null)
                    {
                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Response Body Endpoint");
                    }
                    else
                    {
                        if (body.ReturnJson)
                        {
                            context.Response.ContentType = "application/json";
                            context.Response.StatusCode = body.StatusCode;
                            await context.Response.WriteAsJsonAsync(new ResponseBody.ResponseModel()
                            {
                                MyString = "asd",
                                MyInt = 5,
                                Nested = new ResponseBody.ResponseModel.NestedValue()
                                {
                                    MyString = "qwe"
                                }
                            });
                        }
                        else
                        {
                            context.Response.ContentType = "text/plain";
                            context.Response.StatusCode = body.StatusCode;
                            await context.Response.WriteAsync("Response Body Endpoint");
                        }
                    }
                });

                endpoints.MapGet("/option/query-string", async context =>
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Query String Endpoint");
                });
            });
        }
    }
}
