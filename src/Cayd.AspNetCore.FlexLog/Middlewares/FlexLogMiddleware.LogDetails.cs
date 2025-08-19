using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using System;
using System.Linq;
using Cayd.AspNetCore.FlexLog.Logging;
using Microsoft.Extensions.DependencyInjection;
using Cayd.AspNetCore.FlexLog.Services;

namespace Cayd.AspNetCore.FlexLog.Middlewares
{
    public partial class FlexLogMiddleware
    {
        private bool IsRouteIgnored(HttpContext context, ICollection<string> ignoredRoutes)
        {
            return ignoredRoutes.Count > 0 &&
                ignoredRoutes.Any(route => context.Request.Path.StartsWithSegments(route, StringComparison.OrdinalIgnoreCase));
        }

        private void AddIdsToLogContext(HttpContext context, FlexLogContext logContext)
        {
            logContext.TraceId = context.TraceIdentifier;
            if (_correlationIdKey != null)
            {
                if (context.Request.Headers.TryGetValue(_correlationIdKey, out var correlationIdStrValue))
                {
                    if (Guid.TryParse(correlationIdStrValue.ToString(), out var correlationId))
                    {
                        logContext.CorrelationId = correlationId;
                    }
                }
            }
        }

        private void AddProtocolToLogContext(HttpContext context, FlexLogContext logContext)
        {
            logContext.Protocol = context.Request.Protocol;
        }

        private void AddClaimsToLogContext(HttpContext context, FlexLogContext logContext, ICollection<string> ignoredRoutes)
        {
            if (!_claimOptionEnabled || IsRouteIgnored(context, ignoredRoutes))
                return;

            if (_includedClaimTypes.Count > 0)
            {
                foreach (var type in _includedClaimTypes)
                {
                    string claimTypeName = type;
                    var claim = context.User.Claims
                        .Where(c => string.Equals(type, c.Type, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    if (claim == null)
                    {
                        _claimTypeAliases.TryGetValue(type, out var claimTypeAlias);
                        claim = context.User.Claims
                            .Where(c => string.Equals(claimTypeAlias, c.Type, StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault();

                        if (claim == null)
                            continue;

                        claimTypeName = claimTypeAlias!;
                    }

                    if (logContext.Claims.ContainsKey(claimTypeName))
                    {

                    }
                    else
                    {
                        logContext.Claims.Add(claimTypeName, claim.Value);
                    }
                }
            }
            else
            {
                foreach (var claim in context.User.Claims)
                {
                    if (_ignoredClaimTypes.Count > 0)
                    {
                        if (_ignoredClaimTypes.Contains(claim.Type))
                            continue;

                        _claimTypeAliases.TryGetValue(claim.Type, out var claimTypeAlias);
                        if (claimTypeAlias != null && _ignoredClaimTypes.Contains(claimTypeAlias))
                            continue;
                    }

                    if (logContext.Claims.ContainsKey(claim.Type))
                    {
                        logContext.Claims[claim.Type] += "," + claim.Value;
                    }
                    else
                    {
                        logContext.Claims.Add(claim.Type, claim.Value);
                    }
                }
            }
        }

        private void AddHeadersToLogContext(HttpContext context, FlexLogContext logContext, ICollection<string> ignoredRoutes)
        {
            if (!_headerOptionEnabled || IsRouteIgnored(context, ignoredRoutes))
                return;

            if (_includedHeaderKeys.Count > 0)
            {
                foreach (var headerKey in _includedHeaderKeys)
                {
                    if (!context.Request.Headers.TryGetValue(headerKey, out var headerValue))
                        continue;

                    logContext.Headers.Add(headerKey, GetHeaderString(headerValue));
                }
            }
            else
            {
                foreach (var kvHeader in context.Request.Headers)
                {
                    if (!context.Request.Headers.TryGetValue(kvHeader.Key, out var headerValue) ||
                        (_ignoredHeaderKeys.Count > 0 && _ignoredHeaderKeys.Contains(kvHeader.Key)))
                        continue;

                    logContext.Headers.Add(kvHeader.Key, GetHeaderString(headerValue));
                }
            }
        }

        private string GetHeaderString(StringValues headerValue)
        {
            var value = headerValue.ToString();
            if (_headerLimitOptionEnabled && value.Length > _headerLimitLength)
            {
                if (_headerLimitDrop)
                {
                    value = _headerLimitDropText;
                }
                else
                {
                    value = value.Substring(0, _headerLimitLength);
                }
            }

            return value;
        }

        private void AddRequestLineToLogContext(HttpContext context, FlexLogContext logContext)
        {
            logContext.Endpoint = $"{context.Request.Method} {context.Request.Path}";
        }

        private void AddQueryStringToLogContext(HttpContext context, FlexLogContext logContext, ICollection<string> ignoredRoutes)
        {
            if (!_queryStringOptionEnabled || IsRouteIgnored(context, ignoredRoutes))
                return;

            var value = context.Request.QueryString.Value;
            if (value == null)
                return;

            if (_queryStringLimitOptionEnabled)
            {
                if (value.Length <= _queryStringLimitLength)
                {
                    logContext.QueryString = value;
                }
                else
                {
                    logContext.QueryString = _queryStringTooLargeText;
                }
            }
            else
            {
                logContext.QueryString = value;
            }
        }

        private async Task AddRequestBodyToLogContext(HttpContext context, FlexLogContext logContext, ICollection<string> ignoredRoutes)
        {
            if (!_requestBodyOptionEnabled || IsRouteIgnored(context, ignoredRoutes))
                return;

            logContext.RequestBodyContentType = context.Request.ContentType?.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            if (!IsContentTypeJson(logContext.RequestBodyContentType))
                return;

            context.Request.EnableBuffering();
            using (var memoryStream = new MemoryStream())
            {
                await context.Request.Body.CopyToAsync(memoryStream);

                logContext.RequestBodySizeInBytes = memoryStream.Length;
                logContext.IsRequestBodyTooLarge = logContext.RequestBodySizeInBytes >= _requestBodySizeLimit;
                if (logContext.IsRequestBodyTooLarge.Value)
                {
                    logContext.RequestBody = _requestBodySizeTooLargeText;
                }
                else
                {
                    logContext.RequestBodyRaw = memoryStream.ToArray();
                }
            }

            context.Request.Body.Position = 0;
        }

        private async Task AddResponseBodyToLogContext(HttpContext context, FlexLogContext logContext)
        {
            var originalStream = context.Response.Body;
            await using var memoryStream = new MemoryStream();

            context.Response.Body = memoryStream;

            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                AddExceptionToLogContext(context, logContext, exception);
                throw;
            }
            finally
            {
                logContext.ResponseBodyContentType = context.Response.ContentType?.Split(';', 2, StringSplitOptions.TrimEntries)[0];

                if (logContext.ResponseStatusCode == null)
                {
                    logContext.ResponseStatusCode = context.Response.StatusCode;
                }

                context.Response.Body = originalStream;

                if (IsContentTypeJson(logContext.ResponseBodyContentType))
                {
                    memoryStream.Position = 0;
                    logContext.ResponseBodyRaw = memoryStream.ToArray();
                }

                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalStream);

                AddLogContextToChannel(context, logContext);
            }
        }

        private bool IsContentTypeJson(string? contentType)
        {
            if (contentType == null)
                return false;

            return contentType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase) ||
                contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
        }

        private void AddExceptionToLogContext(HttpContext context, FlexLogContext logContext, Exception exception)
        {
            logContext.ResponseStatusCode = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300 ?
                StatusCodes.Status500InternalServerError : context.Response.StatusCode;

            var flexLogger = context.RequestServices.GetRequiredService<IFlexLogger<FlexLogMiddleware>>();
            flexLogger.LogError(exception.Message, exception);
        }

        private void AddLogContextToChannel(HttpContext context, FlexLogContext logContext)
        {
            var logEntries = context.RequestServices.GetRequiredService<IList<FlexLogEntry>>();
            logContext.LogEntries = new List<FlexLogEntry>(logEntries);

            var logChannel = context.RequestServices.GetRequiredService<FlexLogChannel>();
            logChannel.AddLogContextToChannel(logContext);
        }
    }
}
