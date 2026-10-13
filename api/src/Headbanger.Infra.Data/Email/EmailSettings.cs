using Microsoft.Extensions.Configuration;

namespace Headbanger.Infra.Data.Email
{
    public class EmailSettings
    {
        public const string SmtpProvider = "Smtp";

        public string Provider { get; init; } = string.Empty;
        public string FromAddress { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
        public string FrontendBaseUrl { get; init; } = string.Empty;
        public string SmtpHost { get; init; } = string.Empty;
        public int SmtpPort { get; init; }
        public bool SmtpUseStartTls { get; init; }
        public string SmtpUsername { get; init; } = string.Empty;
        public string SmtpPassword { get; init; } = string.Empty;

        public bool UsesSmtp => string.Equals(Provider, SmtpProvider, StringComparison.OrdinalIgnoreCase);

        public static EmailSettings FromConfiguration(IConfiguration config) => new()
        {
            Provider = config["Email:Provider"] ?? string.Empty,
            FromAddress = config["Email:From:Address"] ?? "nao-responda@headbanger.local",
            FromName = config["Email:From:Name"] ?? "Headbanger",
            FrontendBaseUrl = (config["Frontend:BaseUrl"] ?? string.Empty).TrimEnd('/'),
            SmtpHost = config["Email:Smtp:Host"] ?? string.Empty,
            SmtpPort = int.TryParse(config["Email:Smtp:Port"], out var port) ? port : 587,
            SmtpUseStartTls = !string.Equals(config["Email:Smtp:UseStartTls"], "false", StringComparison.OrdinalIgnoreCase),
            SmtpUsername = config["Email:Smtp:Username"] ?? string.Empty,
            SmtpPassword = config["Email:Smtp:Password"] ?? string.Empty,
        };
    }
}
