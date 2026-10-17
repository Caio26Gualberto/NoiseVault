using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;

namespace NoiseVault.Infra.Data.Media
{
    public class DisabledRemoteVideoTranscoder : IRemoteVideoTranscoder
    {
        public bool Enabled => false;

        public string PlaylistFile => "master.m3u8";

        public IReadOnlyList<string> PosterFiles { get; } = [];

        public Task<string> StartAsync(RemoteTranscodeRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Conversão remota desligada: Video:Engine não é MediaConvert.");

        public Task<RemoteTranscodeResult> CheckAsync(string jobId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Conversão remota desligada: Video:Engine não é MediaConvert.");
    }
}
