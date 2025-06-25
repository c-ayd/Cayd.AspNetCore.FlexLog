using Cayd.AspNetCore.FlexLog.Exceptions;
using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
#endif

namespace Cayd.AspNetCore.FlexLog.DependencyInjection
{
    public static partial class FlexLogDependencyInjection
    {
#if NET6_0_OR_GREATER
        /// <summary>
        /// Registers all services of FlexLog.
        /// </summary>
        /// <param name="configure">Adds sinks and fallback sinks for FlexLog.</param>
        public static void AddFlexLog(this WebApplicationBuilder builder, Action<FlexLogConfig> configure)
        {
            RegisterFlexLog(builder.Services, builder.Configuration, configure);
        }
#endif
        /// <summary>
        /// Registers all services of FlexLog.
        /// </summary>
        /// <param name="configuration">Application configuration properties.</param>
        /// <param name="configure">Adds sinks and fallback sinks for FlexLog.</param>
        public static void AddFlexLog(this IServiceCollection services, IConfiguration configuration, Action<FlexLogConfig> configure)
        {
            RegisterFlexLog(services, configuration, configure);
        }

        private static void RegisterFlexLog(IServiceCollection services, IConfiguration configuration, Action<FlexLogConfig> configure)
        {
            var config = new FlexLogConfig();
            configure(config);

            var loggingOptions = configuration.GetSection(FlexLogOptions.OptionKey).Get<FlexLogOptions>();
            CheckRouteOptions(loggingOptions);

            services.Configure<FlexLogOptions>(configuration.GetSection(FlexLogOptions.OptionKey));

            services.AddScoped<IList<FlexLogEntry>>(sp => new List<FlexLogEntry>());
            services.AddScoped(typeof(IFlexLogger<>), typeof(FlexLogger<>));
            services.AddSingleton(new FlexLogChannel(loggingOptions, config.GetSinks(), config.GetFallbackSinks()));
            services.AddHostedService<FlexLogBackgroundService>();
        }

        private static void CheckRouteOptions(FlexLogOptions? options)
        {
            if (options == null)
                return;

            if (options.IgnoredRoutes != null)
            {
                foreach (var route in options.IgnoredRoutes)
                {
                    if (!route.StartsWith('/'))
                    {
                        throw new InvalidRouteFormatException("IgnoredRoutes", route);
                    }
                }
            }

            if (options.LogDetails?.Claims?.IgnoredRoutes != null)
            {
                foreach (var route in options.LogDetails.Claims.IgnoredRoutes)
                {
                    if (!route.StartsWith('/'))
                    {
                        throw new InvalidRouteFormatException("LogDetails:Claims:IgnoredRoutes", route);
                    }
                }
            }

            if (options.LogDetails?.Headers?.IgnoredRoutes != null)
            {
                foreach (var route in options.LogDetails.Headers.IgnoredRoutes)
                {
                    if (!route.StartsWith('/'))
                    {
                        throw new InvalidRouteFormatException("LogDetails:Headers:IgnoredRoutes", route);
                    }
                }
            }

            if (options.LogDetails?.RequestBody?.IgnoredRoutes != null)
            {
                foreach (var route in options.LogDetails.RequestBody.IgnoredRoutes)
                {
                    if (!route.StartsWith('/'))
                    {
                        throw new InvalidRouteFormatException("LogDetails:RequestBody:IgnoredRoutes", route);
                    }
                }
            }

            if (options.LogDetails?.ResponseBody?.IgnoredRoutes != null)
            {
                foreach (var route in options.LogDetails.ResponseBody.IgnoredRoutes)
                {
                    if (!route.StartsWith('/'))
                    {
                        throw new InvalidRouteFormatException("LogDetails:ResponseBody:IgnoredRoutes", route);
                    }
                }
            }

            if (options.LogDetails?.QueryString?.IgnoredRoutes != null)
            {
                foreach (var route in options.LogDetails.QueryString.IgnoredRoutes)
                {
                    if (!route.StartsWith('/'))
                    {
                        throw new InvalidRouteFormatException("LogDetails:QueryString:IgnoredRoutes", route);
                    }
                }
            }
        }
    }
}
