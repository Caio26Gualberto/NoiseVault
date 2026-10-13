using Amazon.S3;
using Amazon.S3.Model;
using Headbanger.Application.Abstractions.Logging;
using Headbanger.Application.Interfaces;

namespace Headbanger.Infra.Data.Storage
{
    public class S3FileStorage : IFileStorage
    {
        private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

        private readonly IAmazonS3 _client;
        private readonly StorageSettings _settings;
        private readonly IApplicationLogger _logger;

        public S3FileStorage(IAmazonS3 client, StorageSettings settings, IApplicationLoggerFactory loggerFactory)
        {
            _client = client;
            _settings = settings;
            _logger = loggerFactory.Create(LogContext.Storage);
        }

        public async Task<bool> PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = new MemoryStream(content, writable: false);

                var request = new PutObjectRequest
                {
                    BucketName = _settings.Bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                };
                request.Headers.CacheControl = ImmutableCacheControl;

                await _client.PutObjectAsync(request, cancellationToken);

                _logger.Info("STORAGE_UPLOADED", "Object stored", extra: new { Key = key, Bytes = content.Length });
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("STORAGE_UPLOAD_FAILED", "Failed to store object", exception: ex, extra: new { Key = key });
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.DeleteObjectAsync(_settings.Bucket, key, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning("STORAGE_DELETE_FAILED", "Failed to delete object; it is now orphaned",
                    extra: new { Key = key, Error = ex.GetType().Name, ex.Message });
                return false;
            }
        }

        public string? PublicUrl(string? key) =>
            string.IsNullOrWhiteSpace(key) ? null : $"{_settings.PublicBaseUrl}/{key}";
    }
}
