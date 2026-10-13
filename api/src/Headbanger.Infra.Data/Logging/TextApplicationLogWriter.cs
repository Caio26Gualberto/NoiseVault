using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Headbanger.Application.Abstractions.Logging;
using Headbanger.Application.Common.Time;

namespace Headbanger.Infra.Data.Logging
{
    public sealed class TextApplicationLogWriter : IApplicationLogWriter
    {
        private const int MaxPayloadDepth = 4;
        private const string UnserializablePayload = "<unserializable>";

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
                var line = Format(entry);

                lock (_gate)
                    Console.Out.WriteLine(line);
            }
            catch
            {
            }
        }

        private static string Format(ApplicationLogEntry entry)
        {
            var builder = new StringBuilder();

            builder.Append(LevelLabel(entry.Level).PadRight(5));
            builder.Append(' ');
            builder.Append(Timestamp(entry.Timestamp));
            builder.Append(" (");
            builder.Append(entry.Context.ToString().ToLowerInvariant());
            builder.Append(") ");

            if (!string.IsNullOrWhiteSpace(entry.EventName))
            {
                builder.Append(entry.EventName);
                builder.Append(": ");
            }

            builder.Append(Single(entry.Message));

            AppendSection(builder, ScopeParts(entry));
            AppendSection(builder, DataParts(entry.Data));
            AppendSection(builder, ExtraParts(entry.Extra));

            if (!string.IsNullOrWhiteSpace(entry.ExceptionType))
                AppendSection(builder, [$"error={Single(entry.ExceptionType)}: {Single(entry.ExceptionMessage)}"]);

            if (!string.IsNullOrWhiteSpace(entry.CausedBy))
                AppendSection(builder, [$"caused-by={Single(entry.CausedBy)}"]);

            if (!string.IsNullOrWhiteSpace(entry.StackTrace))
                AppendSection(builder, [$"stack={StackTrace(entry.StackTrace)}"]);

            return builder.ToString();
        }

        private static string LevelLabel(ApplicationLogLevel level) => level switch
        {
            ApplicationLogLevel.Information => "INFO",
            ApplicationLogLevel.Warning => "WARN",
            ApplicationLogLevel.Error => "ERROR",
            ApplicationLogLevel.Critical => "CRIT",
            _ => level.ToString().ToUpperInvariant(),
        };

        private static string Timestamp(DateTimeOffset timestamp) =>
            AppTime
                .ToLocal(timestamp.UtcDateTime)
                .ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        private static List<string> ScopeParts(ApplicationLogEntry entry)
        {
            var parts = new List<string>();

            if (entry.UserId.HasValue)
                parts.Add($"user={entry.UserId.Value}");

            if (!string.IsNullOrWhiteSpace(entry.Operation))
                parts.Add($"op={Single(entry.Operation)}");

            return parts;
        }

        private static List<string> DataParts(IReadOnlyDictionary<string, object?>? data)
        {
            var parts = new List<string>();
            if (data is null)
                return parts;

            foreach (var pair in data)
                AppendValue(parts, pair.Key, pair.Value);

            return parts;
        }

        private static List<string> ExtraParts(object? extra)
        {
            var parts = new List<string>();
            if (extra is null)
                return parts;

            AppendValue(parts, prefix: null, extra);

            return parts;
        }

        private static void AppendValue(List<string> parts, string? prefix, object? value)
        {
            JsonElement element;

            try
            {
                element = JsonSerializer.SerializeToElement(value, SerializerOptions);
            }
            catch
            {
                parts.Add($"{prefix ?? "extra"}={UnserializablePayload}");
                return;
            }

            AppendElement(parts, prefix, element, depth: 0);
        }

        private static void AppendElement(List<string> parts, string? prefix, JsonElement element, int depth)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object when depth < MaxPayloadDepth:
                    foreach (var property in element.EnumerateObject())
                        AppendElement(parts, Join(prefix, property.Name), property.Value, depth + 1);
                    return;

                case JsonValueKind.Array when depth < MaxPayloadDepth:
                    AppendArray(parts, prefix, element, depth);
                    return;

                default:
                    parts.Add($"{prefix ?? "value"}={Scalar(element)}");
                    return;
            }
        }

        private static void AppendArray(List<string> parts, string? prefix, JsonElement array, int depth)
        {
            var index = 0;
            var scalars = new List<string>();

            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    AppendElement(parts, Join(prefix, $"[{index}]"), item, depth + 1);
                else
                    scalars.Add(Scalar(item));

                index++;
            }

            if (scalars.Count > 0)
                parts.Add($"{prefix ?? "value"}=[{string.Join(", ", scalars)}]");
            else if (index == 0)
                parts.Add($"{prefix ?? "value"}=[]");
        }

        private static string Scalar(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => Single(element.GetString()),
            JsonValueKind.Null or JsonValueKind.Undefined => "null",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Object or JsonValueKind.Array => Single(element.GetRawText()),
            _ => element.GetRawText(),
        };

        private static string Join(string? prefix, string name) =>
            string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";

        private static void AppendSection(StringBuilder builder, IReadOnlyList<string> parts)
        {
            if (parts.Count == 0)
                return;

            builder.Append(" | ");
            builder.Append(string.Join(' ', parts));
        }

        private static string StackTrace(string stackTrace)
        {
            var frames = stackTrace
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(frame => frame.TrimEnd('\r'));

            return string.Join(" <- ", frames);
        }

        private static string Single(string? value) =>
            value is null ? string.Empty : value.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
    }
}
