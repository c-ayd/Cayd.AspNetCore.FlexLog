using Cayd.AspNetCore.FlexLog.Infrastructure;
using Cayd.AspNetCore.FlexLog.Logging;
using Cayd.AspNetCore.FlexLog.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cayd.AspNetCore.FlexLog.Middlewares
{
    public partial class FlexLogMiddleware
    {
        private static readonly string _headerLimitDropStrategy = "Drop";
        private static readonly string _headerLimitSliceStrategy = "Slice";
        private static readonly string _headerLimitDropText = "TOO LARGE";

        private static readonly int _defaultHeaderLimitLength = 512;
        private static readonly int _defaultQueryStringLimitLength = 512;
        private static readonly long _defaultRequestBodySizeLimit = 30720;      // 30 KB
        private static readonly string _requestBodySizeTooLargeText = "TOO LARGE";

        private static readonly string _queryStringTooLargeText = "TOO LARGE";

        private static readonly BidirectionalDictionary<string> _claimTypeAliases =
            new BidirectionalDictionary<string>(typeof(ClaimTypes)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(
                f => f.Name,
                f => (string)f.GetValue(null)!
            ), StringComparer.OrdinalIgnoreCase);

        private readonly RequestDelegate _next;

        private readonly List<string> _ignoredRoutes;

        private readonly bool _claimOptionEnabled;
        private readonly List<string> _includedClaimTypes;
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
        private readonly List<string> _ignoredRoutesForRequestBody;

        private readonly bool _responseBodyOptionEnabled;
        private readonly List<string> _ignoredRoutesForResponseBody;

        private readonly bool _queryStringOptionEnabled;
        private readonly bool _queryStringLimitOptionEnabled;
        private readonly int _queryStringLimitLength;
        private readonly List<string> _ignoredRoutesForQueryString;

        public FlexLogMiddleware(RequestDelegate next, IOptions<FlexLogOptions> loggingOptions)
        {
            _next = next;

            SetIgnoredRoutes(ignoredRoutes: ref _ignoredRoutes!,
                loggingOptions);

            SetClaimOptions(claimOptionEnabled: ref _claimOptionEnabled,
                includedClaimTypes: ref _includedClaimTypes!,
                ignoredClaimTypes: ref _ignoredClaimTypes!,
                ignoredRoutesForClaims: ref _ignoredRoutesForClaims!,
                loggingOptions);

            SetHeaderOptions(headerOptionEnabled: ref _headerOptionEnabled,
                correlationIdKey: ref _correlationIdKey,
                headerLimitOptionEnabled: ref _headerLimitOptionEnabled,
                headerLimitLength: ref _headerLimitLength,
                headerLimitDrop: ref _headerLimitDrop,
                includedHeaderKeys: ref _includedHeaderKeys!,
                ignoredHeaderKeys: ref _ignoredHeaderKeys!,
                ignoredRoutesForHeaders: ref _ignoredRoutesForHeaders!,
                loggingOptions);

            SetRequestBodyOptions(requestBodyOptionEnabled: ref _requestBodyOptionEnabled,
                requestBodySizeLimit: ref _requestBodySizeLimit,
                ignoredRoutesForRequestBody: ref _ignoredRoutesForRequestBody!,
                loggingOptions);

            SetResponseBodyOptions(responseBodyOptionEnabled: ref _responseBodyOptionEnabled,
                ignoredRoutesForResponseBody: ref _ignoredRoutesForResponseBody!,
                loggingOptions);

            SetQueryStringOptions(queryStringOptionEnabled: ref _queryStringOptionEnabled,
                queryStringLimitOptionEnabled: ref _queryStringLimitOptionEnabled,
                queryStringLimitLength: ref _queryStringLimitLength,
                ignoredRoutesForQueryString: ref _ignoredRoutesForQueryString!,
                loggingOptions);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsRouteIgnored(context, _ignoredRoutes))
            {
                await _next(context);
            }
            else
            {
                var logContext = new FlexLogContext();

                AddIdsToLogContext(context, logContext);
                AddProtocolToLogContext(context, logContext);

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
    }
}
