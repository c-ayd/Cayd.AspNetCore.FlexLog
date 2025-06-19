using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
#endif

namespace Cayd.AspNetCore.FlexLog.DependencyInjection
{
    public static partial class FlexLogDependencyInjection
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
            var loggingOptions = configuration.GetSection(FlexLogOptions.OptionKey).Get<FlexLogOptions>();

            services.AddScoped<FlexLogContext>();
            services.AddScoped(typeof(IFlexLogger<>), typeof(FlexLogger<>));

            services.AddSingleton(new FlexLogChannel(loggingOptions, config.GetSinks(), config.GetFallbackSinks()));
            services.AddHostedService<FlexLogBackgroundService>();
        }
    }
}
