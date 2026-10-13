using Headbanger.Application.Abstractions.Logging;
using Headbanger.Application.Common.Media;
using Headbanger.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Headbanger.Infra.Data.Media
{
    public class ImageSharpProcessor : IImageProcessor
    {
        private const int Quality = 82;
        private const int AvatarSize = 512;
        private const int AvatarMinSize = 128;
        private const int BannerMaxWidth = 1600;
        private const int BannerMinWidth = 800;

        private static readonly Configuration DecodeConfiguration = new(
            new JpegConfigurationModule(),
            new PngConfigurationModule(),
            new WebpConfigurationModule());

        private readonly IApplicationLogger _logger;

        public ImageSharpProcessor(IApplicationLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create(LogContext.Storage);
        }

        public async Task<ImageProcessingResult> ProcessAsync(Stream input, EImagePreset preset, CancellationToken cancellationToken = default)
        {
            Image image;

            try
            {
                image = await Image.LoadAsync(new DecoderOptions { Configuration = DecodeConfiguration }, input, cancellationToken);
            }
            catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
            {
                return Reject(preset, EImageRejection.Unsupported);
            }

            using (image)
            {
                image.Mutate(x => x.AutoOrient());

                if (IsTooSmall(image, preset))
                    return Reject(preset, EImageRejection.TooSmall);

                image.Mutate(x => x.Resize(ResizeOptionsFor(preset, image)));

                image.Metadata.ExifProfile = null;
                image.Metadata.IccProfile = null;
                image.Metadata.XmpProfile = null;
                image.Metadata.IptcProfile = null;

                using var output = new MemoryStream();
                await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = Quality, SkipMetadata = true }, cancellationToken);

                return ImageProcessingResult.Ok(new ProcessedImage(output.ToArray(), image.Width, image.Height, "image/webp", "webp"));
            }
        }

        private ImageProcessingResult Reject(EImagePreset preset, EImageRejection rejection)
        {
            _logger.Info("IMAGE_REJECTED", "Image rejected", extra: new { Preset = preset.ToString(), Reason = rejection.ToString() });
            return ImageProcessingResult.Reject(rejection);
        }

        private static bool IsTooSmall(Image image, EImagePreset preset) => preset switch
        {
            EImagePreset.Avatar => image.Width < AvatarMinSize || image.Height < AvatarMinSize,
            EImagePreset.Banner => image.Width < BannerMinWidth,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Preset de imagem desconhecido."),
        };

        private static ResizeOptions ResizeOptionsFor(EImagePreset preset, Image image) => preset switch
        {
            EImagePreset.Avatar => new ResizeOptions
            {
                Size = new Size(AvatarSize, AvatarSize),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
            },
            EImagePreset.Banner => new ResizeOptions
            {
                Size = new Size(Math.Min(image.Width, BannerMaxWidth), 0),
                Mode = ResizeMode.Max,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Preset de imagem desconhecido."),
        };
    }
}
