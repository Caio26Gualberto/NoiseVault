using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Interfaces;
using NoiseVault.Domain.Enums.Auth;

namespace NoiseVault.Infra.Data.ExternalLogin
{
    public class ExternalIdentityService : IExternalIdentityService
    {
        private const string GraphBase = "https://graph.facebook.com/v24.0/";
        private const string TicketPurpose = "NoiseVault.ExternalLogin.Ticket";
        private static readonly string[] GoogleIssuers = ["accounts.google.com", "https://accounts.google.com"];
        private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(15);

        private readonly HttpClient _http;
        private readonly ExternalLoginSettings _settings;
        private readonly GoogleSigningKeyCache _googleKeys;
        private readonly ITimeLimitedDataProtector _ticketProtector;
        private readonly IApplicationLogger _logger;

        public ExternalIdentityService(
            HttpClient http,
            ExternalLoginSettings settings,
            GoogleSigningKeyCache googleKeys,
            IDataProtectionProvider dataProtection,
            IApplicationLoggerFactory loggerFactory)
        {
            _http = http;
            _settings = settings;
            _googleKeys = googleKeys;
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

            var handler = new JsonWebTokenHandler();
            string keyId;

            try
            {
                keyId = handler.ReadJsonWebToken(token).Kid;
            }
            catch (ArgumentException)
            {
                return Rejected(EExternalProvider.Google, "malformed");
            }

            var keys = await _googleKeys.GetAsync(_http, keyId, cancellationToken);

            var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidIssuers = GoogleIssuers,
                ValidAudience = _settings.GoogleClientId,
                IssuerSigningKeys = keys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
            });

            if (!result.IsValid)
                return Rejected(EExternalProvider.Google, result.Exception?.GetType().Name ?? "invalid");

            var subject = Claim(result.Claims, "sub");

            if (string.IsNullOrWhiteSpace(subject))
                return Rejected(EExternalProvider.Google, "missing-sub");

            var email = Claim(result.Claims, "email");
            var emailVerified = result.Claims.TryGetValue("email_verified", out var verified)
                && (verified is true || string.Equals(verified?.ToString(), "true", StringComparison.OrdinalIgnoreCase));

            var name = Claim(result.Claims, "name");

            if (string.IsNullOrWhiteSpace(name))
                name = $"{Claim(result.Claims, "given_name")} {Claim(result.Claims, "family_name")}".Trim();

            return ExternalVerification.Valid(new ExternalIdentity(EExternalProvider.Google, subject, name ?? string.Empty, email, emailVerified));
        }

        private async Task<ExternalVerification> VerifyFacebookAsync(string token, CancellationToken cancellationToken)
        {
            if (!_settings.IsFacebookConfigured)
                return ExternalVerification.Of(EExternalVerificationStatus.Disabled);

            using var debug = await GetGraphAsync(
                $"{GraphBase}debug_token?input_token={Uri.EscapeDataString(token)}",
                $"{_settings.FacebookAppId}|{_settings.FacebookAppSecret}",
                cancellationToken);

            if (debug == null || !debug.RootElement.TryGetProperty("data", out var data))
                return Rejected(EExternalProvider.Facebook, "debug-token-refused");

            var isValid = data.TryGetProperty("is_valid", out var valid) && valid.ValueKind == JsonValueKind.True;
            var appId = data.TryGetProperty("app_id", out var app) ? app.ToString() : null;
            var userId = data.TryGetProperty("user_id", out var user) ? user.ToString() : null;

            if (!isValid || appId != _settings.FacebookAppId || string.IsNullOrWhiteSpace(userId))
                return Rejected(EExternalProvider.Facebook, isValid ? "other-app" : "invalid");

            using var me = await GetGraphAsync(
                $"{GraphBase}me?fields=id,name,email&appsecret_proof={AppSecretProof(token)}",
                token,
                cancellationToken);

            if (me == null)
                return Rejected(EExternalProvider.Facebook, "me-refused");

            var root = me.RootElement;
            var id = root.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;

            if (id != userId)
                return Rejected(EExternalProvider.Facebook, "user-mismatch");

            var name = root.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var email = root.TryGetProperty("email", out var emailElement) ? emailElement.GetString() : null;

            return ExternalVerification.Valid(new ExternalIdentity(
                EExternalProvider.Facebook,
                userId,
                name ?? string.Empty,
                email,
                EmailVerified: !string.IsNullOrWhiteSpace(email)));
        }

        private async Task<JsonDocument?> GetGraphAsync(string url, string accessToken, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _http.SendAsync(request, cancellationToken);

            if ((int)response.StatusCode >= 500)
                throw new HttpRequestException($"Graph API respondeu {(int)response.StatusCode}.");

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
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

        private static string? Claim(IDictionary<string, object> claims, string name) =>
            claims.TryGetValue(name, out var value) ? value?.ToString() : null;
    }
}
