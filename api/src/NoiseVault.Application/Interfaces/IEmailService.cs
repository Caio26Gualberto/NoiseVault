using NoiseVault.Application.Auth.Dtos;

namespace NoiseVault.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailConfirmationAsync(EmailConfirmationEmailDto email);
        Task<bool> SendPasswordResetAsync(PasswordResetEmailDto email);
        Task<bool> SendUserInviteEmail(string email, string name, string roleName, string temporaryPassword);
    }
}
