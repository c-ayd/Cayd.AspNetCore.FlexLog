using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Test.Integration.Utilities
{
    public class AddClaimMiddleware
    {
        private readonly RequestDelegate _next;

        public AddClaimMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, "TestUser"),
                new Claim(ClaimTypes.Email, "test@test.com"),
                new Claim(ClaimTypes.Name, "TestName"),
                new Claim("CustomClaim", "CustomValue")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }
    }
}
