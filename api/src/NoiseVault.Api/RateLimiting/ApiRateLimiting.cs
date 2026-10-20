using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NoiseVault.Api.ApiResponse;
using NoiseVault.Api.Diagnostics;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Domain.Constants;

namespace NoiseVault.Api.RateLimiting
{
    public static class ApiRateLimitPolicies
    {
        public const string Register = "register";
        public const string Login = "login";
        public const string RefreshToken = "refresh-token";
        public const string ConfirmEmail = "confirm-email";
        public const string ResendConfirmation = "resend-confirmation";
        public const string ForgotPassword = "forgot-password";
        public const string ResetPassword = "reset-password";
        public const string ProfileUpload = "profile-upload";
        public const string UserSearch = "user-search";
        public const string BandCreate = "band-create";
        public const string BandInvite = "band-invite";
        public const string MediaPhoto = "media-photo";
        public const string MediaVideo = "media-video";
        public const string PostCreate = "post-create";
        public const string CommentCreate = "comment-create";
        public const string EventCreate = "event-create";
        public const string SupportCreate = "support-create";
        public const string CitySearch = "city-search";
        public const string ArchiveSubmit = "archive-submit";
        public const string PhotoTag = "photo-tag";
        public const string SharePreview = "share-preview";
        public const string SeoPage = "seo-page";
        public const string OpeningCreate = "opening-create";
        public const string OpeningInterest = "opening-interest";
        public const string LocationUpdate = "location-update";
        public const string BandVerification = "band-verification";
        public const string DiscographyImport = "discography-import";
        public const string ExternalLogin = "external-login";
        public const string SignInMethod = "sign-in-method";
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
                AddFixedWindow(options, ApiRateLimitPolicies.ConfirmEmail, permitLimit: 10, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.ResendConfirmation, permitLimit: 3, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.ForgotPassword, permitLimit: 3, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.ResetPassword, permitLimit: 10, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.ProfileUpload, permitLimit: 20, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.UserSearch, permitLimit: 60, windowMinutes: 1);
                AddFixedWindow(options, ApiRateLimitPolicies.BandCreate, permitLimit: 5, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.BandInvite, permitLimit: 30, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.MediaPhoto, permitLimit: 60, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.MediaVideo, permitLimit: 10, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.PostCreate, permitLimit: 30, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.CommentCreate, permitLimit: 60, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.EventCreate, permitLimit: 10, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.SupportCreate, permitLimit: 5, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.CitySearch, permitLimit: 60, windowMinutes: 1);
                AddFixedWindow(options, ApiRateLimitPolicies.ArchiveSubmit, permitLimit: 10, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.PhotoTag, permitLimit: 60, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.SharePreview, permitLimit: 120, windowMinutes: 1);
                AddFixedWindow(options, ApiRateLimitPolicies.SeoPage, permitLimit: 300, windowMinutes: 1);
                AddFixedWindow(options, ApiRateLimitPolicies.OpeningCreate, permitLimit: 10, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.OpeningInterest, permitLimit: 20, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.LocationUpdate, permitLimit: 10, windowMinutes: 1440);
                AddFixedWindow(options, ApiRateLimitPolicies.BandVerification, permitLimit: 3, windowMinutes: 1440);
                AddFixedWindow(options, ApiRateLimitPolicies.DiscographyImport, permitLimit: 5, windowMinutes: 60);
                AddFixedWindow(options, ApiRateLimitPolicies.ExternalLogin, permitLimit: 20, windowMinutes: 5);
                AddFixedWindow(options, ApiRateLimitPolicies.SignInMethod, permitLimit: 10, windowMinutes: 60);

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
