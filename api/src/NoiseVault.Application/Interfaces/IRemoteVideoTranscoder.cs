using NoiseVault.Application.Common.Media;

namespace NoiseVault.Application.Interfaces
{
    public interface IRemoteVideoTranscoder
    {
        bool Enabled { get; }
        string PlaylistFile { get; }
        IReadOnlyList<string> PosterFiles { get; }

        Task<string> StartAsync(RemoteTranscodeRequest request, CancellationToken cancellationToken = default);
        Task<RemoteTranscodeResult> CheckAsync(string jobId, CancellationToken cancellationToken = default);
    }
}
