using Microsoft.AspNetCore.Mvc.Controllers;

namespace NoiseVault.Api.Diagnostics
{
    public static class RequestDescriptor
    {
        public const string ErrorCodeItemKey = "__noisevault_error_code";
        public const string TraceIdHeader = "X-Trace-Id";

        private const string AppVersionHeader = "X-App-Version";
        private const string AppPlatformHeader = "X-App-Platform";
        private const string AccessTokenParameter = "access_token";

        public static string? ActionName(HttpContext context)
        {
            var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

            return descriptor is null
                ? context.GetEndpoint()?.DisplayName
                : $"{descriptor.ControllerName}.{descriptor.ActionName}";
        }

        public static void Describe(HttpContext context, IDictionary<string, object?> data)
        {
            data["Method"] = context.Request.Method;
            data["Path"] = context.Request.Path.Value;
            data["Action"] = ActionName(context);
            data["Status"] = context.Response.StatusCode;

            if (context.Request.QueryString.HasValue)
                data["Query"] = MaskedQuery(context.Request.Query);

            var routeValues = context.Request.RouteValues
                .Where(pair => pair.Key is not ("controller" or "action") && pair.Value is not null)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            if (routeValues.Count > 0)
                data["RouteValues"] = routeValues;

            var ip = context.Connection.RemoteIpAddress?.ToString();

            if (!string.IsNullOrWhiteSpace(ip))
                data["Ip"] = ip;

            if (context.Request.Headers.TryGetValue(AppVersionHeader, out var appVersion))
                data["AppVersion"] = appVersion.ToString();

            if (context.Request.Headers.TryGetValue(AppPlatformHeader, out var appPlatform))
                data["AppPlatform"] = appPlatform.ToString();
        }

        private static string MaskedQuery(IQueryCollection query) =>
            QueryString.Create(query.SelectMany(pair => pair.Value.Select(value =>
                new KeyValuePair<string, string?>(
                    pair.Key,
                    string.Equals(pair.Key, AccessTokenParameter, StringComparison.OrdinalIgnoreCase) ? "***" : value)))).Value ?? string.Empty;
    }
}
