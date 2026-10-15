using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace NoiseVault.Infra.Data.Media
{
    public class ImageSharpProcessor : IImageProcessor
    {
        private const int Quality = 82;
        private const int AvatarSize = 512;
        private const int AvatarMinSize = 128;
        private const int BannerMaxWidth = 1600;
        private const int BannerMinWidth = 800;
        private const int PostMaxSide = 2048;
        private const int PostMinSide = 64;
        private const int ArchiveMaxSide = 3072;
        private const int FlyerMaxSide = 1200;
        private const int FlyerMinSide = 300;

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
                await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = Quality, FileFormat = WebpFileFormatType.Lossy, SkipMetadata = true }, cancellationToken);

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
            EImagePreset.Post or EImagePreset.Archive => image.Width < PostMinSide || image.Height < PostMinSide,
            EImagePreset.Flyer => image.Width < FlyerMinSide || image.Height < FlyerMinSide,
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
            EImagePreset.Post => new ResizeOptions
            {
                Size = new Size(Math.Min(image.Width, PostMaxSide), Math.Min(image.Height, PostMaxSide)),
                Mode = ResizeMode.Max,
            },
            EImagePreset.Archive => new ResizeOptions
            {
                Size = new Size(Math.Min(image.Width, ArchiveMaxSide), Math.Min(image.Height, ArchiveMaxSide)),
                Mode = ResizeMode.Max,
            },
            EImagePreset.Flyer => new ResizeOptions
            {
                Size = new Size(Math.Min(image.Width, FlyerMaxSide), Math.Min(image.Height, FlyerMaxSide)),
                Mode = ResizeMode.Max,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Preset de imagem desconhecido."),
        };
    }
}
