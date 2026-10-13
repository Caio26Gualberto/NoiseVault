using System.Text.Json;
using System.Text.Json.Serialization;
using NoiseVault.Application.Abstractions.Logging;

namespace NoiseVault.Infra.Data.Logging
{
    public sealed class ConsoleApplicationLogWriter : IApplicationLogWriter
    {
        private const string SerializationFailedKey = "LogSerializationFailed";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly Lock _gate = new();

        public void Write(ApplicationLogEntry entry)
        {
            try
            {
                var line = Serialize(entry);
                if (line is null)
                    return;

                lock (_gate)
                    Console.Out.WriteLine(line);
            }
            catch
            {
            }
        }

        private static string? Serialize(ApplicationLogEntry entry)
        {
            try
            {
                return JsonSerializer.Serialize(entry, SerializerOptions);
            }
            catch
            {
            }

            try
            {
                return JsonSerializer.Serialize(WithoutPayload(entry), SerializerOptions);
            }
            catch
            {
            }

            try
            {
                return JsonSerializer.Serialize(
                    new
                    {
                        timestamp = DateTimeOffset.UtcNow,
                        level = entry.Level.ToString(),
                        context = entry.Context.ToString(),
                        eventName = entry.EventName,
                        message = entry.Message,
                        application = entry.Application,
                        logSerializationFailed = true,
                    },
                    SerializerOptions);
            }
            catch
            {
                return null;
            }
        }

        private static ApplicationLogEntry WithoutPayload(ApplicationLogEntry entry) => new()
        {
            Timestamp = entry.Timestamp,
            Level = entry.Level,
            Context = entry.Context,
            EventName = entry.EventName,
            Message = entry.Message,
            Operation = entry.Operation,
            Application = entry.Application,
            Environment = entry.Environment,
            UserId = entry.UserId,
            Data = new Dictionary<string, object?> { [SerializationFailedKey] = true },
            Extra = null,
            ExceptionType = entry.ExceptionType,
            ExceptionMessage = entry.ExceptionMessage,
            CausedBy = entry.CausedBy,
            StackTrace = entry.StackTrace,
        };
    }
}
