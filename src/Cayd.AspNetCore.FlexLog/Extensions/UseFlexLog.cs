using Cayd.AspNetCore.FlexLog.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Cayd.AspNetCore.FlexLog.Extensions
{
    public static partial class FlexLogExtensions
    {
        public static void UseFlexLog(this IApplicationBuilder app)
        {
            app.UseMiddleware<FlexLogMiddleware>();
        }
    }
}
