using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Response;

namespace NoiseVault.Application.Common.Media
{
    public sealed record ImageUpload(string? Key, Error? Error, int Width = 0, int Height = 0)
    {
        public bool Succeeded => Key is not null;
    }

    public sealed class ImageReplacement
    {
        private readonly IFileStorage _fileStorage;
        private readonly IImageProcessor _imageProcessor;

        public ImageReplacement(IFileStorage fileStorage, IImageProcessor imageProcessor)
        {
            _fileStorage = fileStorage;
            _imageProcessor = imageProcessor;
        }

        public async Task<ImageUpload> UploadAsync(ImageUploadInput? file, EImagePreset preset, string folder, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return Reject("IMAGE_REQUIRED", "Send an image file");

            var maxBytes = ImagePresets.MaxBytes(preset);

            if (file.Length > maxBytes)
                return Reject("IMAGE_TOO_LARGE", $"Image must have at most {maxBytes / (1024 * 1024)} MB");

            var processed = await _imageProcessor.ProcessAsync(file.Content, preset, cancellationToken);

            if (!processed.Succeeded)
                return processed.Rejection == EImageRejection.TooSmall
                    ? Reject("IMAGE_TOO_SMALL", "Image is too small for this use")
                    : Reject("IMAGE_UNSUPPORTED", "Send a JPEG, PNG or WebP image");

            var image = processed.Image!;
            var key = $"{folder}/{Guid.NewGuid():N}.{image.Extension}";

            if (!await _fileStorage.PutAsync(key, image.Content, image.ContentType, cancellationToken))
                return new ImageUpload(null, new Error("STORAGE_UNAVAILABLE", "File storage is unavailable, try again later", ErrorType.Unexpected));

            return new ImageUpload(key, null, image.Width, image.Height);
        }

        public async Task SaveAsync(string newKey, string? oldKey, Func<Task> persist)
        {
            try
            {
                await persist();
            }
            catch
            {
                await _fileStorage.DeleteAsync(newKey, CancellationToken.None);
                throw;
            }

            if (oldKey != null)
                await _fileStorage.DeleteAsync(oldKey, CancellationToken.None);
        }

        public Task DeleteAsync(string key) => _fileStorage.DeleteAsync(key, CancellationToken.None);

        private static ImageUpload Reject(string code, string message) =>
            new(null, new Error(code, message, ErrorType.Validation));
    }
}
