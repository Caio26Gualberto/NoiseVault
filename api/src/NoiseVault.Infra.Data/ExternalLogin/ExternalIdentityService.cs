using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Interfaces;
using NoiseVault.Domain.Enums.Auth;

namespace NoiseVault.Infra.Data.ExternalLogin
{
    public class ExternalIdentityService : IExternalIdentityService
    {
        private const string GoogleTokenInfoUrl = "https://oauth2.googleapis.com/tokeninfo";
        private const string GoogleUserInfoUrl = "https://openidconnect.googleapis.com/v1/userinfo";
        private const string GraphBase = "https://graph.facebook.com/v24.0/";
        private const string TicketPurpose = "NoiseVault.ExternalLogin.Ticket";
        private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(15);

        private readonly HttpClient _http;
        private readonly ExternalLoginSettings _settings;
        private readonly ITimeLimitedDataProtector _ticketProtector;
        private readonly IApplicationLogger _logger;

        public ExternalIdentityService(
            HttpClient http,
            ExternalLoginSettings settings,
            IDataProtectionProvider dataProtection,
            IApplicationLoggerFactory loggerFactory)
        {
            _http = http;
            _settings = settings;
            _ticketProtector = dataProtection.CreateProtector(TicketPurpose).ToTimeLimitedDataProtector();
            _logger = loggerFactory.Create(LogContext.Auth);
        }

        public async Task<ExternalVerification> VerifyAsync(EExternalProvider provider, string token, CancellationToken cancellationToken = default)
        {
            try
            {
                return provider switch
                {
                    EExternalProvider.Google => await VerifyGoogleAsync(token, cancellationToken),
                    EExternalProvider.Facebook => await VerifyFacebookAsync(token, cancellationToken),
                    _ => ExternalVerification.Of(EExternalVerificationStatus.Invalid),
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.Error(
                    "EXTERNAL_PROVIDER_UNAVAILABLE",
                    "External identity provider did not respond as expected",
                    exception: ex,
                    extra: new { Provider = provider.ToString() });

                return ExternalVerification.Of(EExternalVerificationStatus.Unavailable);
            }
        }

        public string IssueTicket(ExternalIdentity identity) =>
            _ticketProtector.Protect(JsonSerializer.Serialize(identity), TicketLifetime);

        public ExternalIdentity? ReadTicket(string? ticket)
        {
            if (string.IsNullOrWhiteSpace(ticket))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ExternalIdentity>(_ticketProtector.Unprotect(ticket));
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException)
            {
                return null;
            }
        }

        private async Task<ExternalVerification> VerifyGoogleAsync(string token, CancellationToken cancellationToken)
        {
            if (!_settings.IsGoogleConfigured)
                return ExternalVerification.Of(EExternalVerificationStatus.Disabled);

            using var tokenInfo = await SendAsync(
                new HttpRequestMessage(HttpMethod.Post, GoogleTokenInfoUrl)
                {
                    Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("access_token", token)]),
                },
                cancellationToken);

            if (tokenInfo == null)
                return Rejected(EExternalProvider.Google, "tokeninfo-refused");

            var info = tokenInfo.RootElement;
            var audience = Text(info, "aud");
            var subject = Text(info, "sub");

            if (audience != _settings.GoogleClientId || string.IsNullOrWhiteSpace(subject))
                return Rejected(EExternalProvider.Google, audience != _settings.GoogleClientId ? "other-app" : "missing-sub");

            using var userInfo = await SendAsync(Bearer(GoogleUserInfoUrl, token), cancellationToken);

            if (userInfo == null)
                return Rejected(EExternalProvider.Google, "userinfo-refused");

            var user = userInfo.RootElement;

            if (Text(user, "sub") != subject)
                return Rejected(EExternalProvider.Google, "user-mismatch");

            var name = Text(user, "name");

            if (string.IsNullOrWhiteSpace(name))
                name = $"{Text(user, "given_name")} {Text(user, "family_name")}".Trim();

            var emailVerified = user.TryGetProperty("email_verified", out var verified)
                && (verified.ValueKind == JsonValueKind.True || string.Equals(verified.ToString(), "true", StringComparison.OrdinalIgnoreCase));

            return ExternalVerification.Valid(new ExternalIdentity(EExternalProvider.Google, subject, name, Text(user, "email"), emailVerified));
        }

        private async Task<ExternalVerification> VerifyFacebookAsync(string token, CancellationToken cancellationToken)
        {
            if (!_settings.IsFacebookConfigured)
                return ExternalVerification.Of(EExternalVerificationStatus.Disabled);

            using var debug = await SendAsync(
                Bearer($"{GraphBase}debug_token?input_token={Uri.EscapeDataString(token)}", $"{_settings.FacebookAppId}|{_settings.FacebookAppSecret}"),
                cancellationToken);

            if (debug == null || !debug.RootElement.TryGetProperty("data", out var data))
                return Rejected(EExternalProvider.Facebook, "debug-token-refused");

            var isValid = data.TryGetProperty("is_valid", out var valid) && valid.ValueKind == JsonValueKind.True;
            var appId = data.TryGetProperty("app_id", out var app) ? app.ToString() : null;
            var userId = data.TryGetProperty("user_id", out var user) ? user.ToString() : null;

            if (!isValid || appId != _settings.FacebookAppId || string.IsNullOrWhiteSpace(userId))
                return Rejected(EExternalProvider.Facebook, isValid ? "other-app" : "invalid");

            using var me = await SendAsync(
                Bearer($"{GraphBase}me?fields=id,name,email&appsecret_proof={AppSecretProof(token)}", token),
                cancellationToken);

            if (me == null)
                return Rejected(EExternalProvider.Facebook, "me-refused");

            var root = me.RootElement;

            if (Text(root, "id") != userId)
                return Rejected(EExternalProvider.Facebook, "user-mismatch");

            var email = Text(root, "email");

            return ExternalVerification.Valid(new ExternalIdentity(
                EExternalProvider.Facebook,
                userId,
                Text(root, "name") ?? string.Empty,
                email,
                EmailVerified: !string.IsNullOrWhiteSpace(email)));
        }

        private static HttpRequestMessage Bearer(string url, string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return request;
        }

        private async Task<JsonDocument?> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (request)
            using (var response = await _http.SendAsync(request, cancellationToken))
            {
                if ((int)response.StatusCode >= 500)
                    throw new HttpRequestException($"Provedor respondeu {(int)response.StatusCode}.");

                if (!response.IsSuccessStatusCode)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
        }

        private string AppSecretProof(string token)
        {
            var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(_settings.FacebookAppSecret), Encoding.UTF8.GetBytes(token));
            return Convert.ToHexStringLower(hash);
        }

        private ExternalVerification Rejected(EExternalProvider provider, string reason)
        {
            _logger.Warning(
                "EXTERNAL_TOKEN_REJECTED",
                "External sign-in token was rejected",
                extra: new { Provider = provider.ToString(), Reason = reason });

            return ExternalVerification.Of(EExternalVerificationStatus.Invalid);
        }

        private static string? Text(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.ToString() : null;
    }
}
