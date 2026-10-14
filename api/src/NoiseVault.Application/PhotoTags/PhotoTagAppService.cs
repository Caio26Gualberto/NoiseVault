using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.PhotoTags.Dtos;
using NoiseVault.Application.PhotoTags.Interfaces;
using NoiseVault.Application.PhotoTags.Models;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.PublicProfiles.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.PhotoTags
{
    public class PhotoTagAppService : IPhotoTagAppService
    {
        private readonly IPhotoTagRepository _tagRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IPublicProfileAppService _publicProfileAppService;
        private readonly INotificationAppService _notificationAppService;
        private readonly IFileStorage _fileStorage;
        private readonly IApplicationLogger _logger;

        public PhotoTagAppService(
            IPhotoTagRepository tagRepository,
            ICurrentUser currentUser,
            IPermissionChecker permissionChecker,
            IPublicProfileAppService publicProfileAppService,
            INotificationAppService notificationAppService,
            IFileStorage fileStorage,
            IApplicationLoggerFactory loggerFactory)
        {
            _tagRepository = tagRepository;
            _currentUser = currentUser;
            _permissionChecker = permissionChecker;
            _publicProfileAppService = publicProfileAppService;
            _notificationAppService = notificationAppService;
            _fileStorage = fileStorage;
            _logger = loggerFactory.Create(LogContext.Media);
        }

        public async Task<Result<List<PhotoTagDto>>> Add(int mediaId, PhotoTagInputDto input)
        {
            _logger.SetOperation("AddPhotoTag");

            if (!PhotoTagRules.IsValidPosition(input.X) || !PhotoTagRules.IsValidPosition(input.Y))
                return Fail(Invalid("INVALID_PHOTO_TAG_POSITION", "The tag position must be inside the photo"));

            var nickname = ProfileRules.NormalizeOptionalText(input.Nickname);
            var name = ProfileRules.NormalizeOptionalText(input.Name);

            if ((nickname == null) == (name == null))
                return Fail(Invalid("PHOTO_TAG_TARGET_REQUIRED", "Tag a person with an account or write a name, not both"));

            if (name is { Length: < PhotoTagRules.NameMinLength or > PhotoTagRules.NameMaxLength })
                return Fail(Invalid("INVALID_PHOTO_TAG_NAME", $"The name must have between {PhotoTagRules.NameMinLength} and {PhotoTagRules.NameMaxLength} characters"));

            var target = await _tagRepository.FindTargetAsync(mediaId);

            if (target == null || target.Kind != EMediaKind.Photo || target.Status != EMediaStatus.Ready || (target.PostId == null && target.ArchiveEntryId == null))
                return Fail(new Error("PHOTO_NOT_TAGGABLE", "Only published photos of posts and archive entries can be tagged", ErrorType.Conflict));

            int? taggedUserId = null;

            if (nickname != null)
            {
                taggedUserId = await _publicProfileAppService.FindUserId(nickname);

                if (taggedUserId == null)
                    return Fail(new Error("PROFILE_NOT_FOUND", "Profile not found", ErrorType.NotFound));

                if (await _tagRepository.CountAsync(new QueryOptions<PhotoTag> { Predicate = t => t.MediaUploadId == mediaId && t.TaggedUserId == taggedUserId }) > 0)
                    return Fail(new Error("PHOTO_TAG_DUPLICATE", "This person is already tagged in the photo", ErrorType.Conflict));
            }

            if (await _tagRepository.CountAsync(new QueryOptions<PhotoTag> { Predicate = t => t.MediaUploadId == mediaId }) >= PhotoTagRules.MaxPerPhoto)
                return Fail(new Error("TOO_MANY_PHOTO_TAGS", $"A photo can have at most {PhotoTagRules.MaxPerPhoto} tags", ErrorType.Conflict));

            var me = _currentUser.UserId;
            var tag = new PhotoTag
            {
                MediaUploadId = mediaId,
                X = Math.Round(input.X, 4),
                Y = Math.Round(input.Y, 4),
                TaggedUserId = taggedUserId,
                Name = taggedUserId == null ? name : null,
                TaggedByUserId = me,
            };

            await _tagRepository.AddAsync(tag);

            _logger.Info("PHOTO_TAG_CREATED", "Photo tag created", userId: me,
                extra: new { TagId = tag.Id, MediaId = mediaId, target.PostId, target.ArchiveEntryId, WithAccount = taggedUserId != null });

            if (taggedUserId != null)
                await _notificationAppService.Notify(taggedUserId.Value, new NotificationInput(ENotificationKind.PhotoTagged, me, PostId: target.PostId, ArchiveEntryId: target.ArchiveEntryId));

            var tags = await ForMedia([mediaId]);

            return Result<List<PhotoTagDto>>.Ok(tags.GetValueOrDefault(mediaId) ?? [], "PHOTO_TAG_CREATED", "Person tagged");
        }

        public async Task<Result> Remove(int tagId)
        {
            _logger.SetOperation("RemovePhotoTag");

            var row = await _tagRepository.FindRowAsync(tagId);

            if (row == null)
                return Result.Fail(new Error("PHOTO_TAG_NOT_FOUND", "Tag not found", ErrorType.NotFound));

            if (!CanRemove(row, await CanCurateAsync()))
                return Result.Fail(new Error("PHOTO_TAG_FORBIDDEN", "Only the tagged person, who tagged, the author or the curators can remove this tag", ErrorType.Forbidden));

            var tag = await _tagRepository.FirstOrDefaultAsync(new QueryOptions<PhotoTag> { Predicate = t => t.Id == tagId });
            await _tagRepository.SoftDelete(tag!);

            var me = _currentUser.UserId;

            _logger.Info("PHOTO_TAG_DELETED", "Photo tag removed", userId: me,
                extra: new { TagId = tagId, MediaId = row.MediaUploadId, BySelf = row.TaggedUserId == me });

            return Result.Ok("PHOTO_TAG_DELETED", "Tag removed");
        }

        public async Task<Dictionary<int, List<PhotoTagDto>>> ForMedia(IReadOnlyCollection<int> mediaIds)
        {
            if (mediaIds.Count == 0)
                return [];

            var rows = await _tagRepository.ListByMediaAsync(mediaIds);

            if (rows.Count == 0)
                return [];

            var canCurate = rows.Any(r => r.InArchive) && await CanCurateAsync();

            return rows
                .GroupBy(r => r.MediaUploadId)
                .ToDictionary(g => g.Key, g => g.Select(r => Map(r, canCurate)).ToList());
        }

        private PhotoTagDto Map(PhotoTagRow row, bool canCurate) => new()
        {
            Id = row.Id,
            X = row.X,
            Y = row.Y,
            User = row.TaggedUserId == null
                ? null
                : new ProfileSummaryDto
                {
                    Id = row.TaggedUserId.Value,
                    Name = row.TaggedUserName ?? string.Empty,
                    Nickname = row.TaggedUserNickname ?? string.Empty,
                    AvatarUrl = _fileStorage.PublicUrl(row.TaggedUserAvatarKey),
                },
            Name = row.Name,
            CanRemove = CanRemove(row, canCurate),
        };

        private bool CanRemove(PhotoTagRow row, bool canCurate)
        {
            var me = _currentUser.UserId;
            return row.TaggedUserId == me || row.TaggedByUserId == me || row.ContentAuthorUserId == me || (row.InArchive && canCurate);
        }

        private Task<bool> CanCurateAsync() => _permissionChecker.IsGrantedAsync(_currentUser.UserId, Permissions.Archive.Edit);

        private static Result<List<PhotoTagDto>> Fail(Error error) => Result<List<PhotoTagDto>>.Fail(error);

        private static Error Invalid(string code, string message) => new(code, message, ErrorType.Validation);
    }
}
