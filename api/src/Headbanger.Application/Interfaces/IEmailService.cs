namespace Headbanger.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendUserInviteEmail(string email, string name, string roleName, string temporaryPassword);
    }
}
