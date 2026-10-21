using NoiseVault.Application.Common.Media;

namespace NoiseVault.Application.Interfaces
{
    public interface IImageProcessor
    {
        Task<ImageProcessingResult> ProcessAsync(Stream input, EImagePreset preset, CancellationToken cancellationToken = default);
        Task<ImageProcessingResult> RotateAsync(Stream input, int degrees, CancellationToken cancellationToken = default);
    }
}
