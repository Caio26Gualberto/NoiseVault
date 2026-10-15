using NoiseVault.Application.Auth.Dtos;
using NoiseVault.Application.Bands.Dtos;

namespace NoiseVault.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailConfirmationAsync(EmailConfirmationEmailDto email);
        Task<bool> SendPasswordResetAsync(PasswordResetEmailDto email);
        Task<bool> SendUserInviteEmail(string email, string name, string roleName, string temporaryPassword);
        Task<bool> SendBandInviteAsync(BandInviteEmailDto email);
        Task<bool> SendBandVerificationRequestedAsync(BandVerificationEmailDto email);
    }
}
