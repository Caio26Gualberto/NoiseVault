using Microsoft.Extensions.Configuration;

namespace NoiseVault.Infra.Data.Media
{
    public class VideoSettings
    {
        public string FfmpegPath { get; init; } = "ffmpeg";
        public string FfprobePath { get; init; } = "ffprobe";
        public int MaxShortSide { get; init; } = 1080;
        public int Crf { get; init; } = 23;
        public TimeSpan ProcessTimeout { get; init; } = TimeSpan.FromHours(4);
        public string Engine { get; init; } = FfmpegEngine;
        public string MediaConvertRoleArn { get; init; } = string.Empty;
        public string? MediaConvertQueueArn { get; init; }
        public string? MediaConvertEndpoint { get; init; }

        public const string FfmpegEngine = "Ffmpeg";
        public const string MediaConvertEngine = "MediaConvert";

        public bool UsesMediaConvert => string.Equals(Engine, MediaConvertEngine, StringComparison.OrdinalIgnoreCase);

        public static VideoSettings FromConfiguration(IConfiguration config)
        {
            var settings = Build(config);

            if (settings.UsesMediaConvert && string.IsNullOrWhiteSpace(settings.MediaConvertRoleArn))
                throw new InvalidOperationException(
                    "Video:Engine é MediaConvert mas MediaConvert:RoleArn está vazio. Defina MediaConvert__RoleArn com a role que o MediaConvert assume.");

            return settings;
        }

        private static VideoSettings Build(IConfiguration config) => new()
        {
            Engine = config["Video:Engine"] is { Length: > 0 } engine ? engine : FfmpegEngine,
            MediaConvertRoleArn = config["MediaConvert:RoleArn"] ?? string.Empty,
            MediaConvertQueueArn = config["MediaConvert:QueueArn"],
            MediaConvertEndpoint = config["MediaConvert:Endpoint"],
            FfmpegPath = config["Video:FfmpegPath"] is { Length: > 0 } ffmpeg ? ffmpeg : "ffmpeg",
            FfprobePath = config["Video:FfprobePath"] is { Length: > 0 } ffprobe ? ffprobe : "ffprobe",
            MaxShortSide = int.TryParse(config["Video:MaxShortSide"], out var side) ? side : 1080,
            Crf = int.TryParse(config["Video:Crf"], out var crf) ? crf : 23,
            ProcessTimeout = int.TryParse(config["Video:ProcessTimeoutMinutes"], out var minutes) ? TimeSpan.FromMinutes(minutes) : TimeSpan.FromHours(4),
        };
    }
}
