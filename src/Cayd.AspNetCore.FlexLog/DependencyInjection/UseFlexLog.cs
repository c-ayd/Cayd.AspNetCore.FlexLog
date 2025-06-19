using Cayd.AspNetCore.FlexLog.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Cayd.AspNetCore.FlexLog.DependencyInjection
{
    public static partial class FlexLogDependencyInjection
    {
        public static void UseFlexLog(this IApplicationBuilder app)
        {
            app.UseMiddleware<FlexLogMiddleware>();
        }
    }
}
