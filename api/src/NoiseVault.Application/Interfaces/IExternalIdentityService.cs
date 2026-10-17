using NoiseVault.Domain.Enums.Auth;

namespace NoiseVault.Application.Interfaces
{
    public sealed record ExternalIdentity(
        EExternalProvider Provider,
        string ProviderKey,
        string Name,
        string? Email,
        bool EmailVerified);

    public enum EExternalVerificationStatus
    {
        Valid = 0,
        Invalid = 1,
        Disabled = 2,
        Unavailable = 3,
    }

    public sealed record ExternalVerification(EExternalVerificationStatus Status, ExternalIdentity? Identity)
    {
        public static ExternalVerification Valid(ExternalIdentity identity) => new(EExternalVerificationStatus.Valid, identity);
        public static ExternalVerification Of(EExternalVerificationStatus status) => new(status, null);
    }

    public interface IExternalIdentityService
    {
        Task<ExternalVerification> VerifyAsync(EExternalProvider provider, string token, CancellationToken cancellationToken = default);
        string IssueTicket(ExternalIdentity identity);
        ExternalIdentity? ReadTicket(string? ticket);
    }
}
