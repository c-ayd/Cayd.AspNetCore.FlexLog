using Cayd.AspNetCore.FlexLog.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Cayd.AspNetCore.FlexLog.Middlewares
{
    public partial class FlexLogMiddleware
    {
        private void SetIgnoredRoutes(ref List<string> ignoredRoutes,
            IOptions<FlexLogOptions> loggingOptions)
        {
            ignoredRoutes = loggingOptions.Value.IgnoredRoutes != null ?
                loggingOptions.Value.IgnoredRoutes :
                new List<string>();
        }

        private void SetClaimOptions(ref bool claimOptionEnabled,
            ref List<string> includedClaimTypes,
            ref HashSet<string> ignoredClaimTypes,
            ref List<string> ignoredRoutesForClaims,
            IOptions<FlexLogOptions> loggingOptions)
        {
            claimOptionEnabled = loggingOptions.Value.LogDetails?.Claims?.Enabled ?? true;
            if (claimOptionEnabled)
            {
                includedClaimTypes = loggingOptions.Value.LogDetails?.Claims?.IncludedTypes != null ?
                    new List<string>(loggingOptions.Value.LogDetails.Claims.IncludedTypes) :
                    new List<string>();
                ignoredClaimTypes = loggingOptions.Value.LogDetails?.Claims?.IgnoredTypes != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.Claims.IgnoredTypes, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
                ignoredRoutesForClaims = loggingOptions.Value.LogDetails?.Claims?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.Claims.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                includedClaimTypes = new List<string>();
                ignoredClaimTypes = new HashSet<string>();
                ignoredRoutesForClaims = new List<string>();
            }
        }

        private void SetHeaderOptions(ref bool headerOptionEnabled,
            ref string? correlationIdKey,
            ref bool headerLimitOptionEnabled,
            ref int headerLimitLength,
            ref bool headerLimitDrop,
            ref HashSet<string> includedHeaderKeys,
            ref HashSet<string> ignoredHeaderKeys,
            ref List<string> ignoredRoutesForHeaders,
            IOptions<FlexLogOptions> loggingOptions)
        {
            headerOptionEnabled = loggingOptions.Value.LogDetails?.Headers?.Enabled ?? true;
            if (headerOptionEnabled)
            {
                correlationIdKey = loggingOptions.Value.LogDetails?.Headers?.CorrelationIdKey;
                includedHeaderKeys = loggingOptions.Value.LogDetails?.Headers?.IncludedKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.Headers.IncludedKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
                ignoredHeaderKeys = loggingOptions.Value.LogDetails?.Headers?.IgnoredKeys != null ?
                    new HashSet<string>(loggingOptions.Value.LogDetails.Headers.IgnoredKeys, StringComparer.OrdinalIgnoreCase) :
                    new HashSet<string>();
                ignoredRoutesForHeaders = loggingOptions.Value.LogDetails?.Headers?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.Headers.IgnoredRoutes :
                    new List<string>();

                headerLimitOptionEnabled = loggingOptions.Value.LogDetails?.Headers?.Limit != null;
                if (headerLimitOptionEnabled)
                {
                    headerLimitLength = loggingOptions.Value.LogDetails?.Headers?.Limit?.Length != null ?
                        Math.Max(1, loggingOptions.Value.LogDetails.Headers.Limit.Length!.Value) : _defaultHeaderLimitLength;
                    headerLimitDrop = string.Equals(_headerLimitDropStrategy, loggingOptions.Value.LogDetails?.Headers?.Limit?.Strategy, StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                correlationIdKey = null;
                headerLimitOptionEnabled = false;
                includedHeaderKeys = new HashSet<string>();
                ignoredHeaderKeys = new HashSet<string>();
                ignoredRoutesForHeaders = new List<string>();
            }
        }

        private void SetRequestBodyOptions(ref bool requestBodyOptionEnabled,
            ref long requestBodySizeLimit,
            ref List<string> ignoredRoutesForRequestBody,
            IOptions<FlexLogOptions> loggingOptions)
        {
            requestBodyOptionEnabled = loggingOptions.Value.LogDetails?.RequestBody?.Enabled ?? true;
            if (requestBodyOptionEnabled)
            {
                requestBodySizeLimit = loggingOptions.Value.LogDetails?.RequestBody?.BodySizeLimitInBytes != null ?
                    Math.Max(1, loggingOptions.Value.LogDetails.RequestBody.BodySizeLimitInBytes.Value) : _defaultRequestBodySizeLimit;
                ignoredRoutesForRequestBody = loggingOptions.Value.LogDetails?.RequestBody?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.RequestBody.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                requestBodySizeLimit = _defaultRequestBodySizeLimit;
                ignoredRoutesForRequestBody = new List<string>();
            }
        }

        private void SetResponseBodyOptions(ref bool responseBodyOptionEnabled,
            ref List<string> ignoredRoutesForResponseBody,
            IOptions<FlexLogOptions> loggingOptions)
        {
            responseBodyOptionEnabled = loggingOptions.Value.LogDetails?.ResponseBody?.Enabled ?? true;
            if (responseBodyOptionEnabled)
            {
                ignoredRoutesForResponseBody = loggingOptions.Value.LogDetails?.ResponseBody?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.ResponseBody.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                ignoredRoutesForResponseBody = new List<string>();
            }
        }

        private void SetQueryStringOptions(ref bool queryStringOptionEnabled,
            ref bool queryStringLimitOptionEnabled,
            ref int queryStringLimitLength,
            ref List<string> ignoredRoutesForQueryString,
            IOptions<FlexLogOptions> loggingOptions)
        {
            queryStringOptionEnabled = loggingOptions.Value.LogDetails?.QueryString?.Enabled ?? true;
            if (queryStringOptionEnabled)
            {
                queryStringLimitOptionEnabled = loggingOptions.Value.LogDetails?.QueryString?.Limit != null;
                if (queryStringLimitOptionEnabled)
                {
                    queryStringLimitLength = loggingOptions.Value.LogDetails?.QueryString?.Limit?.Length != null ?
                        Math.Max(1, loggingOptions.Value.LogDetails.QueryString.Limit.Length!.Value) : _defaultQueryStringLimitLength;
                }

                ignoredRoutesForQueryString = loggingOptions.Value.LogDetails?.QueryString?.IgnoredRoutes != null ?
                    loggingOptions.Value.LogDetails.QueryString.IgnoredRoutes :
                    new List<string>();
            }
            else
            {
                queryStringLimitOptionEnabled = false;
                ignoredRoutesForQueryString = new List<string>();
            }
        }
    }
}
