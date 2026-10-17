using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using NoiseVault.Application.Abstractions.Logging;

namespace NoiseVault.Infra.Data.Email
{
    public class SesEmailSender : IEmailSender
    {
        private const string Charset = "UTF-8";

        private readonly IAmazonSimpleEmailServiceV2 _client;
        private readonly EmailSettings _settings;
        private readonly IApplicationLogger _logger;

        public SesEmailSender(IAmazonSimpleEmailServiceV2 client, EmailSettings settings, IApplicationLoggerFactory loggerFactory)
        {
            _client = client;
            _settings = settings;
            _logger = loggerFactory.Create(LogContext.Email);
        }

        public async Task<bool> SendAsync(string to, string subject, string html, string text)
        {
            var request = new SendEmailRequest
            {
                FromEmailAddress = $"{_settings.FromName} <{_settings.FromAddress}>",
                Destination = new Destination { ToAddresses = [to] },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content { Data = subject, Charset = Charset },
                        Body = new Body
                        {
                            Html = new Content { Data = html, Charset = Charset },
                            Text = new Content { Data = text, Charset = Charset },
                        },
                    },
                },
            };

            try
            {
                var response = await _client.SendEmailAsync(request);

                _logger.Info(
                    eventName: "EMAIL_SENT",
                    message: "E-mail accepted by SES",
                    extra: new { response.MessageId, Subject = subject });

                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    eventName: "EMAIL_SEND_FAILED",
                    message: "SES delivery failed",
                    extra: new { Subject = subject, Error = ex.GetType().Name, ex.Message });

                return false;
            }
        }
    }
}
