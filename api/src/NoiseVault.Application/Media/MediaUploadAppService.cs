using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Jobs;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Media;

namespace NoiseVault.Application.Media
{
    public class MediaUploadAppService : IMediaUploadAppService
    {
        private const string FailureUnsupported = "UNSUPPORTED";
        private const string FailureNoVideoStream = "NO_VIDEO_STREAM";
        private const string FailureTooLong = "TOO_LONG";
        private const string FailureTranscode = "TRANSCODE_FAILED";
        private const string FailureTimeout = "PROCESSING_TIMEOUT";

        private readonly IRepository<MediaUpload> _mediaRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly IImageProcessor _imageProcessor;
        private readonly IVideoTranscoder _videoTranscoder;
        private readonly IEnqueuedJobDispatcher _jobDispatcher;
        private readonly ImageReplacement _images;
        private readonly IApplicationLogger _logger;

        public MediaUploadAppService(
            IRepository<MediaUpload> mediaRepository,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            IImageProcessor imageProcessor,
            IVideoTranscoder videoTranscoder,
            IEnqueuedJobDispatcher jobDispatcher,
            IApplicationLoggerFactory loggerFactory)
        {
            _mediaRepository = mediaRepository;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _imageProcessor = imageProcessor;
            _videoTranscoder = videoTranscoder;
            _jobDispatcher = jobDispatcher;
            _images = new ImageReplacement(fileStorage, imageProcessor);
            _logger = loggerFactory.Create(LogContext.Media);
        }

        public async Task<Result<MediaDto>> UploadPhoto(ImageUploadInput? file, CancellationToken cancellationToken = default)
        {
            _logger.SetOperation("UploadPhoto");

            var me = _currentUser.UserId;
            var upload = await _images.UploadAsync(file, EImagePreset.Post, $"users/{me}/photos", cancellationToken);

            if (!upload.Succeeded)
                return Result<MediaDto>.Fail(upload.Error!);

            var media = new MediaUpload
            {
                OwnerUserId = me,
                Kind = EMediaKind.Photo,
                Status = EMediaStatus.Ready,
                StorageKey = upload.Key,
                Width = upload.Width,
                Height = upload.Height,
                SizeBytes = file!.Length,
                ExpiresAt = DateTime.UtcNow.Add(MediaRules.UnattachedLifetime),
            };

            await _images.SaveAsync(upload.Key!, null, () => _mediaRepository.AddAsync(media));

            return Result<MediaDto>.Ok(Describe(media), "PHOTO_UPLOADED", "Photo uploaded");
        }

        public async Task<Result<VideoUploadDto>> StartVideo(StartVideoUploadInputDto input)
        {
            _logger.SetOperation("StartVideoUpload");

            if (input.SizeBytes <= 0 || input.SizeBytes > MediaRules.VideoMaxBytes)
                return Result<VideoUploadDto>.Fail(new Error("VIDEO_TOO_LARGE", $"Video must have at most {MediaRules.VideoMaxBytes / (1024 * 1024)} MB", ErrorType.Validation));

            if (!MediaRules.IsVideoContentType(input.ContentType))
                return Result<VideoUploadDto>.Fail(new Error("VIDEO_UNSUPPORTED", "Send a video file", ErrorType.Validation));

            var me = _currentUser.UserId;
            var now = DateTime.UtcNow;

            var active = await _mediaRepository.CountAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.OwnerUserId == me
                    && m.Kind == EMediaKind.Video
                    && ((m.Status == EMediaStatus.Uploading && m.ExpiresAt > now) || m.Status == EMediaStatus.Processing),
            });

            if (active >= MediaRules.MaxActiveVideoUploads)
                return Result<VideoUploadDto>.Fail(new Error("VIDEO_UPLOAD_LIMIT_REACHED", $"Wait until your {MediaRules.MaxActiveVideoUploads} videos in progress finish", ErrorType.Conflict));

            var key = $"uploads/users/{me}/{Guid.NewGuid():N}";
            var uploadId = await _fileStorage.StartMultipartAsync(key, input.ContentType!);

            if (uploadId == null)
                return Result<VideoUploadDto>.Fail(StorageUnavailable());

            var media = new MediaUpload
            {
                OwnerUserId = me,
                Kind = EMediaKind.Video,
                Status = EMediaStatus.Uploading,
                OriginalKey = key,
                MultipartUploadId = uploadId,
                SizeBytes = input.SizeBytes,
                ExpiresAt = now.Add(MediaRules.UploadLifetime),
            };

            await _mediaRepository.AddAsync(media);

            var parts = Enumerable.Range(1, MediaRules.PartCount(input.SizeBytes))
                .Select(partNumber => new VideoUploadPartDto
                {
                    PartNumber = partNumber,
                    Url = _fileStorage.PresignPartUrl(key, uploadId, partNumber, MediaRules.PartUrlLifetime),
                })
                .ToList();

            _logger.Info("VIDEO_UPLOAD_STARTED", "Video upload started", userId: me, extra: new { MediaId = media.Id, media.SizeBytes, Parts = parts.Count });

            return Result<VideoUploadDto>.Ok(new VideoUploadDto
            {
                Id = media.Id,
                PartSize = MediaRules.PartSize,
                Parts = parts,
                ExpiresAt = media.ExpiresAt,
            }, "VIDEO_UPLOAD_STARTED", "Upload the parts and then complete the upload");
        }

        public async Task<Result<MediaDto>> CompleteVideo(int id, CompleteVideoUploadInputDto input)
        {
            _logger.SetOperation("CompleteVideoUpload");

            var me = _currentUser.UserId;

            var media = await _mediaRepository.FirstOrDefaultAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.Id == id && m.OwnerUserId == me && m.Kind == EMediaKind.Video,
            });

            if (media == null)
                return Result<MediaDto>.Fail(MediaNotFound());

            if (media.Status != EMediaStatus.Uploading || media.ExpiresAt <= DateTime.UtcNow)
                return Result<MediaDto>.Fail(new Error("VIDEO_UPLOAD_NOT_PENDING", "This upload is not waiting for parts anymore", ErrorType.Conflict));

            var expected = MediaRules.PartCount(media.SizeBytes);
            var parts = (input.Parts ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p.ETag))
                .Select(p => new MultipartPart(p.PartNumber, p.ETag!))
                .ToList();

            var complete = parts.Count == expected
                && parts.Select(p => p.PartNumber).OrderBy(n => n).SequenceEqual(Enumerable.Range(1, expected));

            if (!complete || !await _fileStorage.CompleteMultipartAsync(media.OriginalKey!, media.MultipartUploadId!, parts))
                return Result<MediaDto>.Fail(Incomplete());

            var size = await _fileStorage.SizeAsync(media.OriginalKey!);

            if (size is null or <= 0 or > MediaRules.VideoMaxBytes)
            {
                await _fileStorage.DeleteAsync(media.OriginalKey!);
                await _mediaRepository.DeleteAsync(media);
                return Result<MediaDto>.Fail(size is null or <= 0
                    ? Incomplete()
                    : new Error("VIDEO_TOO_LARGE", $"Video must have at most {MediaRules.VideoMaxBytes / (1024 * 1024)} MB", ErrorType.Validation));
            }

            media.SizeBytes = size.Value;
            media.Status = EMediaStatus.Processing;
            media.MultipartUploadId = null;
            media.ExpiresAt = DateTime.UtcNow.Add(MediaRules.ProcessingLifetime);
            await _mediaRepository.UpdateAsync(media);

            try
            {
                _jobDispatcher.EnqueueVideoTranscode(media.Id);
            }
            catch (Exception ex)
            {
                _logger.Error("VIDEO_ENQUEUE_FAILED", "Failed to enqueue the video transcode; cleanup marks it as failed", exception: ex, userId: me, extra: new { MediaId = media.Id });
            }

            _logger.Info("VIDEO_UPLOAD_COMPLETED", "Video upload completed", userId: me, extra: new { MediaId = media.Id, media.SizeBytes });

            return Result<MediaDto>.Ok(Describe(media), "VIDEO_UPLOAD_COMPLETED", "Video uploaded; it is being processed");
        }

        public async Task<Result<MediaDto>> Get(int id)
        {
            var me = _currentUser.UserId;

            var media = await _mediaRepository.FirstOrDefaultAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.Id == id && m.OwnerUserId == me,
                AsNoTracking = true,
            });

            return media == null
                ? Result<MediaDto>.Fail(MediaNotFound())
                : Result<MediaDto>.Ok(Describe(media), "MEDIA_FOUND", "Media found");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteMedia");

            var me = _currentUser.UserId;

            var media = await _mediaRepository.FirstOrDefaultAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.Id == id && m.OwnerUserId == me,
                Includes = [m => m.Attachment!],
            });

            if (media == null)
                return Result.Fail(MediaNotFound());

            if (media.Attachment != null)
                return Result.Fail(new Error("MEDIA_IN_USE", "This media belongs to a post; delete the post instead", ErrorType.Conflict));

            await DiscardAsync(media);

            return Result.Ok("MEDIA_DELETED", "Media deleted");
        }

        public Task<List<MediaUpload>> ListAttachable(IReadOnlyCollection<int> ids, int ownerUserId)
        {
            var now = DateTime.UtcNow;

            return _mediaRepository.ListAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => ids.Contains(m.Id)
                    && m.OwnerUserId == ownerUserId
                    && m.Attachment == null
                    && ((m.Status == EMediaStatus.Ready && m.ExpiresAt > now) || m.Status == EMediaStatus.Processing),
                AsNoTracking = true,
            });
        }

        public MediaDto Describe(MediaUpload media) => new()
        {
            Id = media.Id,
            Kind = media.Kind,
            Status = media.Status,
            Url = media.Status == EMediaStatus.Ready ? _fileStorage.PublicUrl(media.StorageKey) : null,
            PosterUrl = media.Status == EMediaStatus.Ready ? _fileStorage.PublicUrl(media.PosterKey) : null,
            Width = media.Width,
            Height = media.Height,
            DurationSeconds = media.DurationSeconds,
        };

        public async Task DeleteStoredFiles(IReadOnlyCollection<MediaUpload> media)
        {
            foreach (var item in media)
            {
                foreach (var key in StoredKeys(item))
                    await _fileStorage.DeleteAsync(key);
            }
        }

        public async Task<EMediaStatus?> ProcessVideo(int mediaId, bool finalAttempt)
        {
            _logger.SetOperation("TranscodeVideo");
            _logger.Set("MediaId", mediaId);

            var media = await _mediaRepository.FirstOrDefaultAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.Id == mediaId && m.Kind == EMediaKind.Video,
            });

            if (media == null || media.Status != EMediaStatus.Processing)
                return media?.Status;

            var workDirectory = Path.Combine(Path.GetTempPath(), "noisevault-media", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDirectory);

            try
            {
                var inputPath = Path.Combine(workDirectory, "original");

                if (!await _fileStorage.DownloadToFileAsync(media.OriginalKey!, inputPath))
                    throw new InvalidOperationException("Não foi possível baixar o vídeo original do storage.");

                var probe = await _videoTranscoder.ProbeAsync(inputPath);

                if (probe == null)
                    return await RejectAsync(media, FailureUnsupported);

                if (!probe.HasVideo)
                    return await RejectAsync(media, FailureNoVideoStream);

                if (probe.DurationSeconds > MediaRules.VideoMaxDurationSeconds + MediaRules.VideoDurationToleranceSeconds)
                    return await RejectAsync(media, FailureTooLong);

                var video = await _videoTranscoder.TranscodeAsync(inputPath, workDirectory);
                var folder = $"users/{media.OwnerUserId}/videos/{Guid.NewGuid():N}";
                var videoKey = $"{folder}/video.mp4";

                if (!await _fileStorage.PutFileAsync(videoKey, video.VideoPath, "video/mp4"))
                    throw new InvalidOperationException("Não foi possível subir o vídeo convertido para o storage.");

                var posterKey = await UploadPosterAsync(video.PosterPath, folder);
                var originalKey = media.OriginalKey!;

                media.Status = EMediaStatus.Ready;
                media.StorageKey = videoKey;
                media.PosterKey = posterKey;
                media.OriginalKey = null;
                media.Width = video.Width;
                media.Height = video.Height;
                media.DurationSeconds = video.DurationSeconds;
                media.SizeBytes = new FileInfo(video.VideoPath).Length;
                media.ExpiresAt = DateTime.UtcNow.Add(MediaRules.UnattachedLifetime);
                await _mediaRepository.UpdateAsync(media);

                await _fileStorage.DeleteAsync(originalKey);

                _logger.Info("VIDEO_TRANSCODED", "Video transcoded", userId: media.OwnerUserId,
                    extra: new { MediaId = media.Id, media.Width, media.Height, media.DurationSeconds, media.SizeBytes });

                return EMediaStatus.Ready;
            }
            catch (Exception ex) when (finalAttempt)
            {
                _logger.Error("VIDEO_TRANSCODE_FAILED", "Video transcode failed on the last attempt", exception: ex, userId: media.OwnerUserId, extra: new { MediaId = media.Id });
                return await MarkFailedAsync(media, FailureTranscode);
            }
            catch (Exception ex)
            {
                _logger.Warning("VIDEO_TRANSCODE_RETRY", "Video transcode failed; Hangfire retries it", userId: media.OwnerUserId,
                    extra: new { MediaId = media.Id, Error = ex.GetType().Name, ex.Message });
                throw;
            }
            finally
            {
                TryDeleteDirectory(workDirectory);
            }
        }

        public async Task<int> Cleanup()
        {
            _logger.SetOperation("CleanupMedia");

            var now = DateTime.UtcNow;
            var cleaned = 0;

            var abandoned = await _mediaRepository.ListAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.Status == EMediaStatus.Uploading && m.ExpiresAt <= now,
            });

            foreach (var media in abandoned)
            {
                await DiscardAsync(media);
                cleaned++;
            }

            var stuck = await _mediaRepository.ListAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.Status == EMediaStatus.Processing && m.ExpiresAt <= now,
            });

            foreach (var media in stuck)
            {
                await MarkFailedAsync(media, FailureTimeout);
                cleaned++;
            }

            var unattached = await _mediaRepository.ListAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => (m.Status == EMediaStatus.Ready || m.Status == EMediaStatus.Failed)
                    && m.ExpiresAt <= now
                    && m.Attachment == null,
            });

            foreach (var media in unattached)
            {
                await DiscardAsync(media);
                cleaned++;
            }

            var ofDeletedPosts = await _mediaRepository.ListAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => m.Attachment != null
                    && m.Attachment.Post!.IsDeleted
                    && (m.StorageKey != null || m.PosterKey != null || m.OriginalKey != null),
            });

            foreach (var media in ofDeletedPosts)
            {
                await DeleteStoredFiles([media]);
                media.StorageKey = null;
                media.PosterKey = null;
                media.OriginalKey = null;
                await _mediaRepository.UpdateAsync(media);
                cleaned++;
            }

            if (cleaned > 0)
                _logger.Info("MEDIA_CLEANED", "Expired media cleaned", extra: new
                {
                    Abandoned = abandoned.Count,
                    TimedOut = stuck.Count,
                    Unattached = unattached.Count,
                    OfDeletedPosts = ofDeletedPosts.Count,
                });

            return cleaned;
        }

        private async Task<string?> UploadPosterAsync(string? posterPath, string folder)
        {
            if (posterPath == null)
                return null;

            await using var stream = File.OpenRead(posterPath);
            var processed = await _imageProcessor.ProcessAsync(stream, EImagePreset.Post);

            if (!processed.Succeeded)
                return null;

            var key = $"{folder}/poster.{processed.Image!.Extension}";

            return await _fileStorage.PutAsync(key, processed.Image.Content, processed.Image.ContentType) ? key : null;
        }

        private async Task<EMediaStatus> RejectAsync(MediaUpload media, string reason)
        {
            _logger.Info("VIDEO_REJECTED", "Video rejected", userId: media.OwnerUserId, extra: new { MediaId = media.Id, Reason = reason });
            return await MarkFailedAsync(media, reason);
        }

        private async Task<EMediaStatus> MarkFailedAsync(MediaUpload media, string reason)
        {
            var originalKey = media.OriginalKey;

            media.Status = EMediaStatus.Failed;
            media.FailureReason = reason;
            media.OriginalKey = null;
            media.ExpiresAt = DateTime.UtcNow.Add(MediaRules.UnattachedLifetime);
            await _mediaRepository.UpdateAsync(media);

            if (originalKey != null)
                await _fileStorage.DeleteAsync(originalKey);

            return EMediaStatus.Failed;
        }

        private async Task DiscardAsync(MediaUpload media)
        {
            if (media.Status == EMediaStatus.Uploading && media.OriginalKey != null && media.MultipartUploadId != null)
                await _fileStorage.AbortMultipartAsync(media.OriginalKey, media.MultipartUploadId);

            await DeleteStoredFiles([media]);
            await _mediaRepository.DeleteAsync(media);
        }

        private static IEnumerable<string> StoredKeys(MediaUpload media) =>
            new[] { media.StorageKey, media.PosterKey, media.OriginalKey }.OfType<string>();

        private void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.Warning("MEDIA_TEMP_CLEANUP_FAILED", "Temporary video folder was left behind", extra: new { Error = ex.GetType().Name, ex.Message });
            }
        }

        private static Error MediaNotFound() => new("MEDIA_NOT_FOUND", "Media not found", ErrorType.NotFound);

        private static Error Incomplete() => new("VIDEO_UPLOAD_INCOMPLETE", "Some parts of the video did not arrive; send them again", ErrorType.Validation);

        private static Error StorageUnavailable() => new("STORAGE_UNAVAILABLE", "File storage is unavailable, try again later", ErrorType.Unexpected);
    }
}
