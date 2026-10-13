using Microsoft.Extensions.Configuration;

namespace Headbanger.Infra.Data.Storage
{
    public class StorageSettings
    {
        public string Bucket { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public string ServiceUrl { get; init; } = string.Empty;
        public string PublicBaseUrl { get; init; } = string.Empty;
        public string AccessKey { get; init; } = string.Empty;
        public string SecretKey { get; init; } = string.Empty;

        public bool UsesCustomEndpoint => !string.IsNullOrWhiteSpace(ServiceUrl);
        public bool HasStaticCredentials => !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey);

        public static StorageSettings FromConfiguration(IConfiguration config)
        {
            var settings = new StorageSettings
            {
                Bucket = config["Storage:Bucket"] ?? string.Empty,
                Region = config["Storage:Region"] is { Length: > 0 } region ? region : "us-east-1",
                ServiceUrl = config["Storage:ServiceUrl"] ?? string.Empty,
                PublicBaseUrl = (config["Storage:PublicBaseUrl"] ?? string.Empty).TrimEnd('/'),
                AccessKey = config["Storage:AccessKey"] ?? string.Empty,
                SecretKey = config["Storage:SecretKey"] ?? string.Empty,
            };

            if (string.IsNullOrWhiteSpace(settings.Bucket) || string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
                throw new InvalidOperationException(
                    "Storage:Bucket e Storage:PublicBaseUrl são obrigatórios. Defina Storage__Bucket e Storage__PublicBaseUrl.");

            return settings;
        }
    }
}
