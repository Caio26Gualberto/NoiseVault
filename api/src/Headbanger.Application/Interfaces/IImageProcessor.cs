using Headbanger.Application.Common.Media;

namespace Headbanger.Application.Interfaces
{
    public interface IImageProcessor
    {
        Task<ImageProcessingResult> ProcessAsync(Stream input, EImagePreset preset, CancellationToken cancellationToken = default);
    }
}
