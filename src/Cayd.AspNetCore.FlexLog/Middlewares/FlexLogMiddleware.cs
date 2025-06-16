using Cayd.AspNetCore.FlexLog.Enums;
using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Middlewares
{
    public class FlexLogMiddleware
    {
        private static readonly long _defaultRequestBodySizeLimit = 30720;      // 30 KB
        private static readonly Dictionary<string, string> _claimTypeAliases = typeof(ClaimTypes)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(
                f => f.Name,
                f => (string)f.GetValue(null)!,
                StringComparer.OrdinalIgnoreCase
            );

        private readonly RequestDelegate _next;

        private readonly List<string> _ignoredRoutes;

        private readonly bool _claimOptionEnabled;
        private readonly HashSet<string> _includedClaimTypes;
        private readonly HashSet<string> _ignoredClaimTypes;

        private readonly bool _headerOptionEnabled;
        private readonly string? _correlationIdKey;
        private readonly HashSet<string> _includedHeaderKeys;
        private readonly HashSet<string> _ignoredHeaderKeys;

        private readonly bool _requestBodyOptionEnabled;
        private readonly long _requestBodySizeLimit;
        private readonly HashSet<string> _redactedKeysFromRequestBody;

        private readonly bool _responseBodyOptionEnabled;
        private readonly HashSet<string> _redactedKeysFromResponseBody;

        public FlexLogMiddleware(RequestDelegate next, IOptions<FlexLogOptions> loggingOptions)
        {
            _next = next;

            _ignoredRoutes = loggingOptions.Value.IgnoredRoutes != null ?
                new List<string>(loggingOptions.Value.IgnoredRoutes) :
                new List<string>();

            _claimOptionEnabled = loggingOptions.Value.LogDetails?.Claims?.Enabled ?? true;
            if (_claimOptionEnabled)
            {
                _includedClaimTypes = loggingOptions.Value.LogDetails?.Claims?.IncludedTypes != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.Claims.IncludedTypes, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
                _ignoredClaimTypes = loggingOptions.Value.LogDetails?.Claims?.IgnoredTypes != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.Claims.IgnoredTypes, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
            }
            else
            {
                _includedClaimTypes = new HashSet<string>();
                _ignoredClaimTypes = new HashSet<string>();
            }

            _headerOptionEnabled = loggingOptions.Value.LogDetails?.Headers?.Enabled ?? true;
            if (_headerOptionEnabled)
            {
                _correlationIdKey = loggingOptions.Value.LogDetails?.Headers?.CorrelationIdKey;
                _includedHeaderKeys = loggingOptions.Value.LogDetails?.Headers?.IncludedKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.Headers.IncludedKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
                _ignoredHeaderKeys = loggingOptions.Value.LogDetails?.Headers?.IgnoredKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.Headers.IgnoredKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
            }
            else
            {
                _includedHeaderKeys = new HashSet<string>();
                _ignoredHeaderKeys = new HashSet<string>();
            }

            _requestBodyOptionEnabled = loggingOptions.Value.LogDetails?.RequestBody?.Enabled ?? true;
            if (_requestBodyOptionEnabled)
            {
                _requestBodySizeLimit = loggingOptions.Value.LogDetails?.RequestBody?.BodySizeLimitInBytes ?? _defaultRequestBodySizeLimit;
                _redactedKeysFromRequestBody = loggingOptions.Value.LogDetails?.RequestBody?.RedactedKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.RequestBody.RedactedKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
            }
            else
            {
                _requestBodySizeLimit = _defaultRequestBodySizeLimit;
                _redactedKeysFromRequestBody = new HashSet<string>();
            }

            _responseBodyOptionEnabled = loggingOptions.Value.LogDetails?.ResponseBody?.Enabled ?? true;
            if (_responseBodyOptionEnabled)
            {
                _redactedKeysFromResponseBody = loggingOptions.Value.LogDetails?.ResponseBody?.RedactedKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.ResponseBody.RedactedKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
            }
            else
            {
                _redactedKeysFromResponseBody = new HashSet<string>();
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsRouteIgnored(context))
            {
                await _next(context);
            }
            else
            {
                var logContext = context.RequestServices.GetRequiredService<FlexLogContext>();

                SetLogId(context, logContext);

                AddClaimsToLogContext(context, logContext);
                AddHeadersToLogContext(context, logContext);

                await AddRequestBodyToLogContext(context, logContext);

                if (_requestBodyOptionEnabled)
                {
                    await AddResponseBodyToLogContext(context, logContext);
                }
                else
                {
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
                        AddLogContextToChannel(context, logContext);
                    }
                }
            }
        }

        private bool IsRouteIgnored(HttpContext context)
        {
            return _ignoredRoutes.Count > 0 &&
                _ignoredRoutes.Any(route => context.Request.Path.StartsWithSegments(route, StringComparison.OrdinalIgnoreCase));
        }

        private void SetLogId(HttpContext context, FlexLogContext logContext)
        {
            logContext.TraceId = context.TraceIdentifier;
            if (_correlationIdKey != null)
            {
                if (context.Request.Headers.TryGetValue(_correlationIdKey, out var correlationId))
                {
                    logContext.Id = correlationId.ToString();
                }
            }
        }

        private void AddClaimsToLogContext(HttpContext context, FlexLogContext logContext)
        {
            if (!_claimOptionEnabled)
                return;

            if (_includedClaimTypes.Count > 0)
            {
                foreach (var type in _includedClaimTypes)
                {
                    var claim = context.User.Claims
                        .Where(c => _claimTypeAliases.TryGetValue(type, out var claimType) ?
                            string.Equals(claimType, c.Type, StringComparison.OrdinalIgnoreCase) :
                            string.Equals(type, c.Type, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    logContext.Claims.Add(type, claim?.Value);
                }
            }
            else
            {
                foreach (var claim in context.User.Claims)
                {
                    if (_ignoredClaimTypes.Count > 0 &&
                        _ignoredClaimTypes.Any(t => _claimTypeAliases.TryGetValue(t, out var claimType) ?
                            string.Equals(claimType, claim.Type, StringComparison.OrdinalIgnoreCase) :
                            string.Equals(t, claim.Type, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    logContext.Claims.Add(claim.Type, claim?.Value);
                }
            }
        }

        private void AddHeadersToLogContext(HttpContext context, FlexLogContext logContext)
        {
            if (!_headerOptionEnabled)
                return;

            if (_includedHeaderKeys.Count > 0)
            {
                foreach (var headerKey in _includedHeaderKeys)
                {
                    logContext.Headers.Add(headerKey, context.Request.Headers[headerKey]);
                }
            }
            else
            {
                foreach (var header in context.Request.Headers)
                {
                    if (_ignoredHeaderKeys.Count > 0 &&
                        _ignoredHeaderKeys.Any(t => string.Equals(t, header.Key, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    logContext.Headers.Add(header.Key, header.Value);
                }
            }
        }

        private async Task AddRequestBodyToLogContext(HttpContext context, FlexLogContext logContext)
        {
            if (!_requestBodyOptionEnabled)
                return;

            logContext.RequestBodyContentType = context.Request.ContentType;
            if (!IsContentTypeJson(context.Request.ContentType))
                return;

            context.Request.EnableBuffering();
            using (var memoryStream = new MemoryStream())
            {
                await context.Request.Body.CopyToAsync(memoryStream);

                logContext.RequestBodySizeInBytes = memoryStream.Length;
                logContext.IsRequestBodyTooLarge = logContext.RequestBodySizeInBytes >= _requestBodySizeLimit;
                if (!logContext.IsRequestBodyTooLarge.Value)
                {
                    logContext.RequestBodyRaw = memoryStream.ToArray();
                }
            }

            context.Request.Body.Position = 0;
        }

        private async Task AddResponseBodyToLogContext(HttpContext context, FlexLogContext logContext)
        {
            var originalStream = context.Response.Body;
            using var memoryStream = new MemoryStream();

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
                logContext.ResponseBodyContentType = context.Response.ContentType;

                context.Response.Body = originalStream;

                if (IsContentTypeJson(context.Response.ContentType))
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

            var mediaType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            return mediaType.Equals(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase) ||
                mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
        }

        private void AddExceptionToLogContext(HttpContext context, FlexLogContext logContext, Exception exception)
        {
            var flexLogger = context.RequestServices.GetRequiredService<IFlexLogger<FlexLogMiddleware>>();
            flexLogger.Log(ELogLevel.Error, exception.Message, exception);
        }

        private void AddLogContextToChannel(HttpContext context, FlexLogContext logContext)
        {
            var logChannel = context.RequestServices.GetRequiredService<FlexLogChannel>();

            logContext.ElapsedTimeInMilliseconds = (DateTime.UtcNow - logContext.Timestamp).TotalMilliseconds;
            logChannel.AddLogContextToChannel(logContext);
        }
    }
}
