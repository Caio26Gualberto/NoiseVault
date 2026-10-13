namespace NoiseVault.Application.Common.Media
{
    public sealed record VideoProbe(bool HasVideo, double DurationSeconds, int Width, int Height);

    public sealed record TranscodedVideo(string VideoPath, string? PosterPath, int Width, int Height, int DurationSeconds);
}
