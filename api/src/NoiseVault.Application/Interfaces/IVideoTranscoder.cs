using NoiseVault.Application.Common.Media;

namespace NoiseVault.Application.Interfaces
{
    public interface IVideoTranscoder
    {
        Task<VideoProbe?> ProbeAsync(string inputPath, CancellationToken cancellationToken = default);
        Task<TranscodedVideo> TranscodeAsync(string inputPath, string outputDirectory, CancellationToken cancellationToken = default);
    }
}
