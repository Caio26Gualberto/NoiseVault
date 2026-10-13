using Amazon.S3;
using Amazon.S3.Model;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Interfaces;

namespace NoiseVault.Infra.Data.Storage
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
            using var stream = new MemoryStream(content, writable: false);
            return await PutStreamAsync(key, stream, content.Length, contentType, cancellationToken);
        }

        public async Task<bool> PutFileAsync(string key, string path, string contentType, CancellationToken cancellationToken = default)
        {
            await using var stream = File.OpenRead(path);
            return await PutStreamAsync(key, stream, stream.Length, contentType, cancellationToken);
        }

        public async Task<bool> DownloadToFileAsync(string key, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _client.GetObjectAsync(_settings.Bucket, key, cancellationToken);
                await using var output = File.Create(path);
                await response.ResponseStream.CopyToAsync(output, cancellationToken);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Error("STORAGE_DOWNLOAD_FAILED", "Failed to download object", exception: ex, extra: new { Key = key });
                return false;
            }
        }

        public async Task<long?> SizeAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var metadata = await _client.GetObjectMetadataAsync(_settings.Bucket, key, cancellationToken);
                return metadata.ContentLength;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
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

        public async Task<string?> StartMultipartAsync(string key, string contentType, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
                {
                    BucketName = _settings.Bucket,
                    Key = key,
                    ContentType = contentType,
                }, cancellationToken);

                return response.UploadId;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Error("STORAGE_MULTIPART_START_FAILED", "Failed to start multipart upload", exception: ex, extra: new { Key = key });
                return null;
            }
        }

        public string PresignPartUrl(string key, string uploadId, int partNumber, TimeSpan lifetime) =>
            _client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _settings.Bucket,
                Key = key,
                Verb = HttpVerb.PUT,
                UploadId = uploadId,
                PartNumber = partNumber,
                Expires = DateTime.UtcNow.Add(lifetime),
                Protocol = _settings.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTP : Protocol.HTTPS,
            });

        public async Task<bool> CompleteMultipartAsync(string key, string uploadId, IReadOnlyList<MultipartPart> parts, CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
                {
                    BucketName = _settings.Bucket,
                    Key = key,
                    UploadId = uploadId,
                    PartETags = parts
                        .OrderBy(p => p.PartNumber)
                        .Select(p => new PartETag(p.PartNumber, p.ETag))
                        .ToList(),
                }, cancellationToken);

                return true;
            }
            catch (AmazonS3Exception ex)
            {
                _logger.Warning("STORAGE_MULTIPART_COMPLETE_FAILED", "Failed to complete multipart upload",
                    extra: new { Key = key, ex.ErrorCode, ex.Message });
                return false;
            }
        }

        public async Task AbortMultipartAsync(string key, string uploadId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.AbortMultipartUploadAsync(_settings.Bucket, key, uploadId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.Warning("STORAGE_MULTIPART_ABORT_FAILED", "Failed to abort multipart upload; the bucket lifecycle rule cleans it",
                    extra: new { Key = key, Error = ex.GetType().Name, ex.Message });
            }
        }

        private async Task<bool> PutStreamAsync(string key, Stream stream, long length, string contentType, CancellationToken cancellationToken)
        {
            try
            {
                var request = new PutObjectRequest
                {
                    BucketName = _settings.Bucket,
                    Key = key,
                    InputStream = stream,
                    ContentType = contentType,
                };
                request.Headers.CacheControl = ImmutableCacheControl;

                await _client.PutObjectAsync(request, cancellationToken);

                _logger.Info("STORAGE_UPLOADED", "Object stored", extra: new { Key = key, Bytes = length });
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error("STORAGE_UPLOAD_FAILED", "Failed to store object", exception: ex, extra: new { Key = key });
                return false;
            }
        }
    }
}
