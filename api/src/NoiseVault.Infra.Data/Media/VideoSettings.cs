using Microsoft.Extensions.Configuration;

namespace NoiseVault.Infra.Data.Media
{
    public class VideoSettings
    {
        public string FfmpegPath { get; init; } = "ffmpeg";
        public string FfprobePath { get; init; } = "ffprobe";
        public int MaxShortSide { get; init; } = 1080;
        public int Crf { get; init; } = 23;
        public TimeSpan ProcessTimeout { get; init; } = TimeSpan.FromMinutes(30);

        public static VideoSettings FromConfiguration(IConfiguration config) => new()
        {
            FfmpegPath = config["Video:FfmpegPath"] is { Length: > 0 } ffmpeg ? ffmpeg : "ffmpeg",
            FfprobePath = config["Video:FfprobePath"] is { Length: > 0 } ffprobe ? ffprobe : "ffprobe",
            MaxShortSide = int.TryParse(config["Video:MaxShortSide"], out var side) ? side : 1080,
            Crf = int.TryParse(config["Video:Crf"], out var crf) ? crf : 23,
        };
    }
}
