using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Cayd.AspNetCore.FlexLog.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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
        private static readonly string _headerLimitDropStrategy = "Drop";
        private static readonly string _headerLimitSliceStrategy = "Slice";
        private static readonly string _headerLimitDropText = "TOO LARGE";

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
        private readonly List<string> _ignoredRoutesForClaims;

        private readonly bool _headerOptionEnabled;
        private readonly string? _correlationIdKey;
        private readonly bool _headerLimitOptionEnabled;
        private readonly int _headerLimitLength;
        private readonly bool _headerLimitDrop;
        private readonly HashSet<string> _includedHeaderKeys;
        private readonly HashSet<string> _ignoredHeaderKeys;
        private readonly List<string> _ignoredRoutesForHeaders;

        private readonly bool _requestBodyOptionEnabled;
        private readonly long _requestBodySizeLimit;
        private readonly HashSet<string> _redactedKeysFromRequestBody;
        private readonly List<string> _ignoredRoutesForRequestBody;

        private readonly bool _responseBodyOptionEnabled;
        private readonly HashSet<string> _redactedKeysFromResponseBody;
        private readonly List<string> _ignoredRoutesForResponseBody;

        private readonly bool _queryStringOptionEnabled;
        private readonly bool _queryStringLimitOptionEnabled;
        private readonly int _queryStringLimitLength;
        private readonly List<string> _ignoredRoutesForQueryString;

        public FlexLogMiddleware(RequestDelegate next, IOptions<FlexLogOptions> loggingOptions)
        {
            _next = next;

            _ignoredRoutes = loggingOptions.Value.IgnoredRoutes != null ?
                loggingOptions.Value.IgnoredRoutes :
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
                _ignoredRoutesForClaims = loggingOptions.Value.LogDetails?.Claims?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.Claims.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                _includedClaimTypes = new HashSet<string>();
                _ignoredClaimTypes = new HashSet<string>();
                _ignoredRoutesForClaims = new List<string>();
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
                _ignoredRoutesForHeaders = loggingOptions.Value.LogDetails?.Headers?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.Headers.IgnoredRoutes :
                    new List<string>();

                _headerLimitOptionEnabled = loggingOptions.Value.LogDetails?.Headers?.Limit != null;
                if (_headerLimitOptionEnabled)
                {
                    _headerLimitLength = loggingOptions.Value.LogDetails?.Headers?.Limit?.Length ?? 512;
                    _headerLimitDrop = string.Equals(_headerLimitDropStrategy, loggingOptions.Value.LogDetails?.Headers?.Limit?.Strategy, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                _correlationIdKey = null;
                _headerLimitOptionEnabled = false;
                _includedHeaderKeys = new HashSet<string>();
                _ignoredHeaderKeys = new HashSet<string>();
                _ignoredRoutesForHeaders = new List<string>();
            }

            _requestBodyOptionEnabled = loggingOptions.Value.LogDetails?.RequestBody?.Enabled ?? true;
            if (_requestBodyOptionEnabled)
            {
                _requestBodySizeLimit = loggingOptions.Value.LogDetails?.RequestBody?.BodySizeLimitInBytes ?? _defaultRequestBodySizeLimit;
                _redactedKeysFromRequestBody = loggingOptions.Value.LogDetails?.RequestBody?.RedactedKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.RequestBody.RedactedKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
                _ignoredRoutesForRequestBody = loggingOptions.Value.LogDetails?.RequestBody?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.RequestBody.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                _requestBodySizeLimit = _defaultRequestBodySizeLimit;
                _redactedKeysFromRequestBody = new HashSet<string>();
                _ignoredRoutesForRequestBody = new List<string>();
            }

            _responseBodyOptionEnabled = loggingOptions.Value.LogDetails?.ResponseBody?.Enabled ?? true;
            if (_responseBodyOptionEnabled)
            {
                _redactedKeysFromResponseBody = loggingOptions.Value.LogDetails?.ResponseBody?.RedactedKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.ResponseBody.RedactedKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
                _ignoredRoutesForResponseBody = loggingOptions.Value.LogDetails?.ResponseBody?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.ResponseBody.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                _redactedKeysFromResponseBody = new HashSet<string>();
                _ignoredRoutesForResponseBody = new List<string>();
            }

            _queryStringOptionEnabled = loggingOptions.Value.LogDetails?.QueryString?.Enabled ?? true;
            if (_queryStringOptionEnabled)
            {
                _queryStringLimitOptionEnabled = loggingOptions.Value.LogDetails?.QueryString?.Limit != null;
                if (_queryStringLimitOptionEnabled)
                {
                    _queryStringLimitLength = loggingOptions.Value.LogDetails?.QueryString?.Limit?.Length ?? 512;
                }
                
                _ignoredRoutesForQueryString = loggingOptions.Value.LogDetails?.QueryString?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.QueryString.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                _queryStringLimitOptionEnabled = false;
                _ignoredRoutesForQueryString = new List<string>();
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsRouteIgnored(context, _ignoredRoutes))
            {
                await _next(context);
            }
            else
            {
                var logContext = context.RequestServices.GetRequiredService<FlexLogContext>();

                SetLogId(context, logContext);

                AddClaimsToLogContext(context, logContext, _ignoredRoutesForClaims);
                AddHeadersToLogContext(context, logContext, _ignoredRoutesForHeaders);
                AddRequestLineToLogContext(context, logContext);
                AddQueryStringToLogContext(context, logContext, _ignoredRoutesForQueryString);

                await AddRequestBodyToLogContext(context, logContext, _ignoredRoutesForRequestBody);

                if (_responseBodyOptionEnabled && !IsRouteIgnored(context, _ignoredRoutesForResponseBody))
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

        private bool IsRouteIgnored(HttpContext context, ICollection<string> ignoredRoutes)
        {
            return ignoredRoutes.Count > 0 &&
                ignoredRoutes.Any(route => context.Request.Path.StartsWithSegments(route, StringComparison.OrdinalIgnoreCase));
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

        private void AddClaimsToLogContext(HttpContext context, FlexLogContext logContext, ICollection<string> ignoredRoutes)
        {
            if (!_claimOptionEnabled || IsRouteIgnored(context, ignoredRoutes))
                return;

            if (_includedClaimTypes.Count > 0)
            {
                foreach (var type in _includedClaimTypes)
                {
                    _claimTypeAliases.TryGetValue(type, out var claimType);

                    var claim = context.User.Claims
                        .Where(c => claimType != null ?
                            string.Equals(claimType, c.Type, StringComparison.OrdinalIgnoreCase) :
                            string.Equals(type, c.Type, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    logContext.Claims.Add(claimType ?? type, claim?.Value);
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
                        (_ignoredHeaderKeys.Count > 0 && _ignoredHeaderKeys.Any(t => string.Equals(t, kvHeader.Key, StringComparison.OrdinalIgnoreCase))))
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
            logContext.RequestLine = $"{context.Request.Method} {context.Request.Path}";
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
            var logChannel = context.RequestServices.GetRequiredService<FlexLogChannel>();

            logContext.ElapsedTimeInMilliseconds = (DateTime.UtcNow - logContext.Timestamp).TotalMilliseconds;
            logChannel.AddLogContextToChannel(logContext);
        }
    }
}
