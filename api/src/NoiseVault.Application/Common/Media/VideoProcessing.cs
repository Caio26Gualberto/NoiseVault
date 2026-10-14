namespace NoiseVault.Application.Common.Media
{
    public sealed record VideoProbe(bool HasVideo, bool HasAudio, double DurationSeconds, int Width, int Height, double FrameRate);

    public sealed record VideoRendition(string Name, int Width, int Height);

    public sealed record TranscodedVideo(
        string OutputDirectory,
        string PlaylistFile,
        string? PosterPath,
        int Width,
        int Height,
        int DurationSeconds,
        IReadOnlyList<VideoRendition> Renditions);
}
