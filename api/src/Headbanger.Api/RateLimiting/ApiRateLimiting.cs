using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Headbanger.Api.ApiResponse;
using Headbanger.Api.Diagnostics;
using Headbanger.Application.Abstractions.Logging;
using Headbanger.Domain.Constants;

namespace Headbanger.Api.RateLimiting
{
    public static class ApiRateLimitPolicies
    {
        public const string Register = "register";
        public const string Login = "login";
        public const string RefreshToken = "refresh-token";
    }

    public static class ApiRateLimiting
    {
        private const string TooManyRequestsCode = "TOO_MANY_REQUESTS";

        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        PartitionKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 240,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                AddFixedWindow(options, ApiRateLimitPolicies.Register, permitLimit: 5, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.Login, permitLimit: 10, windowMinutes: 5);
                AddFixedWindow(options, ApiRateLimitPolicies.RefreshToken, permitLimit: 30, windowMinutes: 5);

                options.OnRejected = async (context, token) =>
                {
                    LogRejection(context.HttpContext);

                    context.HttpContext.Response.ContentType = "application/json";

                    var payload = EnvelopeJson.Serialize(new ApiResponse<object>
                    {
                        IsSuccess = false,
                        Code = TooManyRequestsCode,
                        Message = "Too many requests. Try again later."
                    });

                    await context.HttpContext.Response.WriteAsync(payload, token);
                };
            });

            return services;
        }

        private static void LogRejection(HttpContext context)
        {
            try
            {
                var writer = context.RequestServices.GetService<IApplicationLogWriter>();

                if (writer is null)
                    return;

                var metadata = context.RequestServices.GetRequiredService<ApplicationLogMetadata>();
                var policy = context.GetEndpoint()?.Metadata
                    .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "global";

                writer.Write(new ApplicationLogEntry
                {
                    Level = ApplicationLogLevel.Warning,
                    Context = LogContext.Api,
                    EventName = "RATE_LIMIT_REJECTED",
                    Message = "Request rejected by the rate limiter",
                    Application = metadata.Application,
                    Environment = metadata.Environment,
                    UserId = AccountId(context),
                    Data = new Dictionary<string, object?>
                    {
                        ["Policy"] = policy,
                        ["Partition"] = PartitionKey(context),
                        ["Method"] = context.Request.Method,
                        ["Path"] = context.Request.Path.Value,
                        ["Action"] = RequestDescriptor.ActionName(context),
                    },
                });
            }
            catch
            {
            }
        }

        private static int? AccountId(HttpContext context)
        {
            var raw = context.User.FindFirst(UserClaimTypes.UserId)?.Value;

            return int.TryParse(raw, out var userId) ? userId : null;
        }

        private static void AddFixedWindow(
            RateLimiterOptions options,
            string policyName,
            int permitLimit,
            int windowMinutes)
        {
            options.AddPolicy(policyName, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(windowMinutes),
                        QueueLimit = 0
                    }));
        }

        private static string PartitionKey(HttpContext context)
        {
            var userId = context.User.FindFirst(UserClaimTypes.UserId)?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
                return $"account:{userId}";

            return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        }
    }
}
