namespace NoiseVault.Application.Common.Media
{
    public sealed record VideoProbe(bool HasVideo, bool HasAudio, double DurationSeconds, int Width, int Height, double FrameRate, bool Interlaced = false);

    public sealed record VideoRendition(string Name, int Width, int Height);

    public sealed record TranscodedVideo(
        string OutputDirectory,
        string PlaylistFile,
        string? PosterPath,
        int Width,
        int Height,
        int DurationSeconds,
        IReadOnlyList<VideoRendition> Renditions,
        bool Deinterlaced);

    public enum RemoteTranscodeState
    {
        Running,
        Complete,
        Failed,
    }

    public sealed record RemoteTranscodeRequest(int MediaId, string InputKey, string OutputFolder);

    public sealed record RemoteTranscodeResult(
        RemoteTranscodeState State,
        int Width = 0,
        int Height = 0,
        int DurationSeconds = 0,
        string? Error = null);
}
