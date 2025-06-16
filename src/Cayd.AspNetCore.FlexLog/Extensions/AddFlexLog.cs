using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Services;
using Cayd.AspNetCore.FlexLog.Sinks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
#endif

namespace Cayd.AspNetCore.FlexLog.Extensions
{
    public static partial class FlexLogExtensions
    {
#if NET6_0_OR_GREATER
        public static void AddFlexLog(this WebApplicationBuilder builder, Action<FlexLogConfig> configure)
        {
            RegisterFlexLog(builder.Services, builder.Configuration, configure);
        }
#endif
        public static void AddFlexLog(this IServiceCollection services, IConfiguration configuration, Action<FlexLogConfig> configure)
        {
            RegisterFlexLog(services, configuration, configure);
        }

        private static void RegisterFlexLog(IServiceCollection services, IConfiguration configuration, Action<FlexLogConfig> configure)
        {
            var config = new FlexLogConfig();
            configure(config);

            services.Configure<FlexLogOptions>(configuration.GetSection(FlexLogOptions.OptionKey));

            services.AddScoped<FlexLogContext>();
            services.AddScoped(typeof(IFlexLogger<>), typeof(FlexLogger<>));

            services.AddSingleton(new FlexLogChannel(new List<IFlexLogSink>(config.GetSinks())));
            services.AddHostedService<FlexLogBackgroundService>();
        }
    }
}
