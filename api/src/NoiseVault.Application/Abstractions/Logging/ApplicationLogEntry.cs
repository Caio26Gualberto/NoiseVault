using System.Text.Json.Serialization;

namespace NoiseVault.Application.Abstractions.Logging
{
    public sealed class ApplicationLogEntry
    {
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("level")]
        public ApplicationLogLevel Level { get; init; }

        [JsonPropertyName("context")]
        public LogContext Context { get; init; }

        [JsonPropertyName("eventName")]
        public string EventName { get; init; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("operation")]
        public string? Operation { get; init; }

        [JsonPropertyName("application")]
        public string Application { get; init; } = ApplicationLogMetadata.DefaultApplication;

        [JsonPropertyName("environment")]
        public string Environment { get; init; } = string.Empty;

        [JsonPropertyName("userId")]
        public int? UserId { get; init; }

        [JsonPropertyName("data")]
        public IReadOnlyDictionary<string, object?>? Data { get; init; }

        [JsonPropertyName("extra")]
        public object? Extra { get; init; }

        [JsonPropertyName("exceptionType")]
        public string? ExceptionType { get; init; }

        [JsonPropertyName("exceptionMessage")]
        public string? ExceptionMessage { get; init; }

        [JsonPropertyName("causedBy")]
        public string? CausedBy { get; init; }

        [JsonPropertyName("stackTrace")]
        public string? StackTrace { get; init; }
    }
}
