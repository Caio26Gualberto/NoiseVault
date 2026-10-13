namespace Headbanger.Infra.Data.Email
{
    public interface IEmailSender
    {
        Task<bool> SendAsync(string to, string subject, string html, string text);
    }
}
