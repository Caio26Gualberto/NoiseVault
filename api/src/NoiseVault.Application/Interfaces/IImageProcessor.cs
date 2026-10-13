using NoiseVault.Application.Common.Media;

namespace NoiseVault.Application.Interfaces
{
    public interface IImageProcessor
    {
        Task<ImageProcessingResult> ProcessAsync(Stream input, EImagePreset preset, CancellationToken cancellationToken = default);
    }
}
