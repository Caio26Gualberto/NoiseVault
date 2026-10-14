using NoiseVault.Application.Cities.Interfaces;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Archive.Models;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Archive
{
    public class ArchiveAppService : IArchiveAppService
    {
        private const int LineupNameMaxLength = 100;

        private readonly IArchiveRepository _archiveRepository;
        private readonly IRepository<ArchiveBandEntry> _bandEntryRepository;
        private readonly IRepository<ArchiveMedia> _archiveMediaRepository;
        private readonly IRepository<MediaUpload> _mediaRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IBandAppService _bandAppService;
        private readonly IMediaUploadAppService _mediaAppService;
        private readonly IPostAppService _postAppService;
        private readonly IFileStorage _fileStorage;
        private readonly INotificationAppService _notificationAppService;
        private readonly ICityAppService _cityAppService;
        private readonly IApplicationLogger _logger;

        public ArchiveAppService(
            IArchiveRepository archiveRepository,
            IRepository<ArchiveBandEntry> bandEntryRepository,
            IRepository<ArchiveMedia> archiveMediaRepository,
            IRepository<MediaUpload> mediaRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IPermissionChecker permissionChecker,
            IBandAppService bandAppService,
            IMediaUploadAppService mediaAppService,
            IPostAppService postAppService,
            IFileStorage fileStorage,
            INotificationAppService notificationAppService,
            ICityAppService cityAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _cityAppService = cityAppService;
            _archiveRepository = archiveRepository;
            _bandEntryRepository = bandEntryRepository;
            _archiveMediaRepository = archiveMediaRepository;
            _mediaRepository = mediaRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _permissionChecker = permissionChecker;
            _bandAppService = bandAppService;
            _mediaAppService = mediaAppService;
            _postAppService = postAppService;
            _fileStorage = fileStorage;
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Archive);
        }

        public Task<Result<PagedResultDto<ArchiveEntryDto>>> List(ArchiveListQueryDto query) =>
            PageAsync(query, filter => filter);

        public async Task<Result<PagedResultDto<ArchiveEntryDto>>> ByBand(string slug, ArchiveListQueryDto query)
        {
            var (bandId, _) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Result<PagedResultDto<ArchiveEntryDto>>.Fail(new Error("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound));

            return await PageAsync(query, filter => filter with { BandId = bandId });
        }

        public async Task<Result<List<ArchiveYearDto>>> Years()
        {
            var years = await _archiveRepository.YearsAsync();

            return Result<List<ArchiveYearDto>>.Ok(
                years.Select(y => new ArchiveYearDto { Year = y.Year, Count = y.Count }).ToList(),
                "ARCHIVE_YEARS_FOUND",
                "Archive years found");
        }

        public async Task<Result<ArchiveEntryDto>> Get(int id)
        {
            var row = await FindVisible(id);

            return row == null
                ? Fail(NotFound())
                : Result<ArchiveEntryDto>.Ok(await MapDetailAsync(row), "ARCHIVE_ENTRY_FOUND", "Archive entry found");
        }

        public async Task<Result<ArchiveEntryDto>> Create(ArchiveEntryInputDto input)
        {
            _logger.SetOperation("CreateArchiveEntry");

            var (data, error) = await ValidateAsync(input, new HashSet<int>());

            if (error != null)
                return Fail(error);

            var me = _currentUser.UserId;
            var entry = new ArchiveEntry { AuthorUserId = me };
            Apply(entry, data!);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _archiveRepository.AddAsync(entry);
                await AddBandsAsync(entry.Id, data!.Bands);
                await AddMediaAsync(entry.Id, data.Media);
                await _postAppService.CreateForArchive(entry.Id, me);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("ARCHIVE_ENTRY_CREATED", "Archive entry created", userId: me,
                extra: new { EntryId = entry.Id, entry.Year, Media = data!.Media.Count, Bands = data.Bands.Count });

            return Result<ArchiveEntryDto>.Ok(await MapDetailAsync((await FindVisible(entry.Id))!), "ARCHIVE_ENTRY_CREATED", "Archive entry published");
        }

        public async Task<Result<ArchiveEntryDto>> Update(int id, ArchiveEntryInputDto input)
        {
            _logger.SetOperation("UpdateArchiveEntry");

            var entry = await _archiveRepository.FirstOrDefaultAsync(new QueryOptions<ArchiveEntry> { Predicate = e => e.Id == id });

            if (entry == null)
                return Fail(NotFound());

            var currentMedia = await _archiveMediaRepository.ListAsync(new QueryOptions<ArchiveMedia> { Predicate = m => m.ArchiveEntryId == id });
            var (data, error) = await ValidateAsync(input, currentMedia.Select(m => m.MediaUploadId).ToHashSet());

            if (error != null)
                return Fail(error);

            Apply(entry, data!);

            var currentBands = await _bandEntryRepository.ListAsync(new QueryOptions<ArchiveBandEntry> { Predicate = b => b.ArchiveEntryId == id });
            var keptIds = data!.Media.Select(m => m.MediaId).ToHashSet();
            var removedMediaIds = currentMedia.Where(m => !keptIds.Contains(m.MediaUploadId)).Select(m => m.MediaUploadId).ToList();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _archiveRepository.UpdateAsync(entry);

                foreach (var band in currentBands)
                    await _bandEntryRepository.DeleteAsync(band);

                foreach (var media in currentMedia)
                    await _archiveMediaRepository.DeleteAsync(media);

                await AddBandsAsync(id, data.Bands);
                await AddMediaAsync(id, data.Media);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            await _mediaAppService.Discard(removedMediaIds);

            _logger.Info("ARCHIVE_ENTRY_UPDATED", "Archive entry updated", userId: _currentUser.UserId,
                extra: new { EntryId = id, Media = data.Media.Count, RemovedMedia = removedMediaIds.Count });

            return Result<ArchiveEntryDto>.Ok(await MapDetailAsync((await FindVisible(id))!), "ARCHIVE_ENTRY_UPDATED", "Archive entry updated");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteArchiveEntry");

            var entry = await _archiveRepository.FirstOrDefaultAsync(new QueryOptions<ArchiveEntry> { Predicate = e => e.Id == id });

            if (entry == null)
                return Result.Fail(NotFound());

            var media = await _archiveMediaRepository.ListAsync(new QueryOptions<ArchiveMedia>
            {
                Predicate = m => m.ArchiveEntryId == id,
                Includes = [m => m.MediaUpload!],
                AsNoTracking = true,
            });

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _archiveRepository.SoftDelete(entry);
                await _postAppService.DeleteForArchive(id);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            await _mediaAppService.DeleteStoredFiles(media.Select(m => m.MediaUpload!).ToList());

            _logger.Info("ARCHIVE_ENTRY_DELETED", "Archive entry deleted", userId: _currentUser.UserId, extra: new { EntryId = id, Media = media.Count });

            return Result.Ok("ARCHIVE_ENTRY_DELETED", "Archive entry deleted");
        }

        public Task<Result<ArchiveEntryDto>> Like(int id) =>
            ReactAsync(id, entry => _archiveRepository.AddLikeAsync(entry, _currentUser.UserId), ENotificationKind.ArchiveLiked, "ARCHIVE_LIKED", "Liked");

        public Task<Result<ArchiveEntryDto>> Unlike(int id) =>
            ReactAsync(id, async entry => { await _archiveRepository.RemoveLikeAsync(entry, _currentUser.UserId); return true; }, null, "ARCHIVE_UNLIKED", "Like removed");

        public Task<Result<ArchiveEntryDto>> Attend(int id) =>
            ReactAsync(id, entry => _archiveRepository.AddAttendanceAsync(entry, _currentUser.UserId), ENotificationKind.ArchiveAttended, "ARCHIVE_ATTENDED", "Marked as attended");

        public Task<Result<ArchiveEntryDto>> Unattend(int id) =>
            ReactAsync(id, async entry => { await _archiveRepository.RemoveAttendanceAsync(entry, _currentUser.UserId); return true; }, null, "ARCHIVE_UNATTENDED", "Attendance removed");

        public async Task<Result<PagedResultDto<ProfileSummaryDto>>> Attendees(int id, PagedRequestDto query)
        {
            query.Normalize();

            if (await FindVisible(id) == null)
                return Result<PagedResultDto<ProfileSummaryDto>>.Fail(NotFound());

            var (users, totalCount) = await _archiveRepository.PageAttendeesAsync(id, query.Skip, query.PageSize);

            return Result<PagedResultDto<ProfileSummaryDto>>.Ok(
                PagedResultDto<ProfileSummaryDto>.Create(users.Select(Summary).ToList(), query.Page, query.PageSize, totalCount),
                "ARCHIVE_ATTENDEES_FOUND",
                "Attendees found");
        }

        public Task<Result<MediaDto>> UploadPhoto(ImageUploadInput? file, CancellationToken cancellationToken = default) =>
            _mediaAppService.UploadPhoto(file, EMediaProfile.Archive, cancellationToken);

        public Task<Result<VideoUploadDto>> StartVideo(StartVideoUploadInputDto input) =>
            _mediaAppService.StartVideo(input, EMediaProfile.Archive);

        public Task<ArchiveReadModel?> FindVisible(int entryId) =>
            _archiveRepository.FindAsync(entryId, _currentUser.UserId);

        public Task<List<int>> AttendeeUserIds(int entryId) =>
            _archiveRepository.AttendeeUserIdsAsync(entryId);

        private async Task<Result<ArchiveEntryDto>> ReactAsync(int id, Func<int, Task<bool>> act, ENotificationKind? notify, string code, string message)
        {
            var entry = await FindVisible(id);

            if (entry == null)
                return Fail(NotFound());

            if (await act(id) && notify != null)
                await _notificationAppService.Notify(entry.AuthorUserId, new NotificationInput(notify.Value, _currentUser.UserId, ArchiveEntryId: id));

            return Result<ArchiveEntryDto>.Ok(await MapDetailAsync((await FindVisible(id))!), code, message);
        }

        private async Task<Result<PagedResultDto<ArchiveEntryDto>>> PageAsync(ArchiveListQueryDto query, Func<ArchiveFilter, ArchiveFilter> narrow)
        {
            query.Normalize();

            var kind = query.Kind?.ToLowerInvariant() switch
            {
                "photo" => EMediaKind.Photo,
                "video" => EMediaKind.Video,
                _ => (EMediaKind?)null,
            };

            var filter = narrow(new ArchiveFilter(
                Year: query.Year,
                Decade: query.Decade is { } decade ? decade - decade % 10 : null,
                Search: query.Search,
                CityId: query.CityId,
                Kind: kind,
                OldestFirst: string.Equals(query.Sort, "oldest", StringComparison.OrdinalIgnoreCase)));

            var (rows, totalCount) = await _archiveRepository.PageAsync(filter, _currentUser.UserId, query.Skip, query.PageSize);

            return Result<PagedResultDto<ArchiveEntryDto>>.Ok(
                PagedResultDto<ArchiveEntryDto>.Create(await MapListAsync(rows), query.Page, query.PageSize, totalCount),
                "ARCHIVE_ENTRIES_FOUND",
                "Archive entries found");
        }

        private sealed record ValidEntry(
            string Title,
            string? Description,
            int Year,
            int? Month,
            int? Day,
            string? Venue,
            int? CityId,
            string? CityName,
            List<ArchiveBandInputDto> Bands,
            List<ArchiveMediaInputDto> Media,
            int CoverMediaId);

        private async Task<(ValidEntry? Data, Error? Error)> ValidateAsync(ArchiveEntryInputDto input, IReadOnlySet<int> existingMediaIds)
        {
            var title = ProfileRules.NormalizeOptionalText(input.Title) ?? string.Empty;

            if (title.Length is < ArchiveRules.TitleMinLength or > ArchiveRules.TitleMaxLength)
                return Invalid("INVALID_ARCHIVE_TITLE", $"Title must have between {ArchiveRules.TitleMinLength} and {ArchiveRules.TitleMaxLength} characters");

            var description = ProfileRules.NormalizeOptionalText(input.Description);

            if (description is { Length: > ArchiveRules.DescriptionMaxLength })
                return Invalid("INVALID_ARCHIVE_DESCRIPTION", $"Description must have at most {ArchiveRules.DescriptionMaxLength} characters");

            if (!ArchiveRules.IsValidDate(input.Year, input.Month, input.Day))
                return Invalid("INVALID_ARCHIVE_DATE", $"Use a year from {ArchiveRules.MinYear} to now; the day needs the month");

            var venue = ProfileRules.NormalizeOptionalText(input.Venue);

            if (venue is { Length: < ArchiveRules.VenueMinLength or > ArchiveRules.VenueMaxLength })
                return Invalid("INVALID_ARCHIVE_VENUE", $"Venue must have between {ArchiveRules.VenueMinLength} and {ArchiveRules.VenueMaxLength} characters");

            var city = await _cityAppService.Resolve(input.CityId, input.CityName, required: false);

            if (city.Error != null)
                return (null, city.Error);

            var (bands, bandsError) = await ValidateBandsAsync(input.Bands ?? []);

            if (bandsError != null)
                return (null, bandsError);

            var media = (input.Media ?? []).ToList();

            if (media.Count == 0)
                return Invalid("ARCHIVE_MEDIA_REQUIRED", "Add at least one photo or video");

            if (media.Count > ArchiveRules.MaxMedia)
                return Invalid("TOO_MANY_ARCHIVE_MEDIA", $"An entry can have at most {ArchiveRules.MaxMedia} photos and videos");

            if (media.Select(m => m.MediaId).Distinct().Count() != media.Count)
                return Invalid("MEDIA_NOT_ATTACHABLE", "The same media appears twice");

            foreach (var item in media)
            {
                item.Caption = ProfileRules.NormalizeOptionalText(item.Caption);

                if (item.Caption is { Length: > ArchiveRules.CaptionMaxLength })
                    return Invalid("INVALID_ARCHIVE_CAPTION", $"Captions must have at most {ArchiveRules.CaptionMaxLength} characters");
            }

            var newIds = media.Select(m => m.MediaId).Where(id => !existingMediaIds.Contains(id)).ToList();

            if (newIds.Count > 0 && (await _mediaAppService.ListAttachable(newIds, _currentUser.UserId, EMediaProfile.Archive)).Count != newIds.Count)
                return (null, new Error("MEDIA_NOT_ATTACHABLE", "Some media is not available anymore; upload it again", ErrorType.Conflict));

            var cover = input.CoverMediaId is { } coverId && media.Any(m => m.MediaId == coverId) ? coverId : media[0].MediaId;

            return (new ValidEntry(title, description, input.Year, input.Month, input.Day, venue, city.CityId, city.CityName, bands!, media, cover), null);
        }

        private async Task<(List<ArchiveBandInputDto>? Bands, Error? Error)> ValidateBandsAsync(List<ArchiveBandInputDto> input)
        {
            if (input.Count > ArchiveRules.MaxBands)
                return (null, new Error("INVALID_LINEUP", $"An entry can list at most {ArchiveRules.MaxBands} bands", ErrorType.Validation));

            var bands = new List<ArchiveBandInputDto>();

            foreach (var entry in input)
            {
                var name = ProfileRules.NormalizeOptionalText(entry.Name);

                if (entry.BandId == null && name is not { Length: <= LineupNameMaxLength })
                    return (null, new Error("INVALID_LINEUP", $"Each band needs a registered band or a name up to {LineupNameMaxLength} characters", ErrorType.Validation));

                bands.Add(new ArchiveBandInputDto { BandId = entry.BandId, Name = entry.BandId == null ? name : null });
            }

            var bandIds = bands.Where(b => b.BandId != null).Select(b => b.BandId!.Value).ToList();

            if (bandIds.Count != bandIds.Distinct().Count())
                return (null, new Error("INVALID_LINEUP", "A band can appear only once", ErrorType.Validation));

            if (bandIds.Count > 0 && (await _bandAppService.SummariesByIds(bandIds)).Count != bandIds.Count)
                return (null, new Error("LINEUP_BAND_NOT_FOUND", "Some band does not exist anymore", ErrorType.Validation));

            return (bands, null);
        }

        private static void Apply(ArchiveEntry entry, ValidEntry data)
        {
            entry.Title = data.Title;
            entry.Description = data.Description;
            entry.Year = data.Year;
            entry.Month = data.Month;
            entry.Day = data.Day;
            entry.SortDate = ArchiveRules.SortDate(data.Year, data.Month, data.Day);
            entry.Venue = data.Venue;
            entry.CityId = data.CityId;
            entry.CityName = data.CityName;
            entry.CoverMediaUploadId = data.CoverMediaId;
        }

        private async Task AddBandsAsync(int entryId, List<ArchiveBandInputDto> bands)
        {
            if (bands.Count == 0)
                return;

            await _bandEntryRepository.AddRangeAsync(bands
                .Select((band, index) => new ArchiveBandEntry { ArchiveEntryId = entryId, BandId = band.BandId, Name = band.Name, Order = index })
                .ToList());
        }

        private Task AddMediaAsync(int entryId, List<ArchiveMediaInputDto> media) =>
            _archiveMediaRepository.AddRangeAsync(media
                .Select((item, index) => new ArchiveMedia { ArchiveEntryId = entryId, MediaUploadId = item.MediaId, Caption = item.Caption, Order = index })
                .ToList());

        private async Task<(bool CanEdit, bool CanDelete)> PermissionsAsync()
        {
            var me = _currentUser.UserId;
            return (await _permissionChecker.IsGrantedAsync(me, Permissions.Archive.Edit),
                await _permissionChecker.IsGrantedAsync(me, Permissions.Archive.Delete));
        }

        private async Task<List<ArchiveEntryDto>> MapListAsync(List<ArchiveReadModel> rows)
        {
            if (rows.Count == 0)
                return [];

            var coverIds = rows.Where(r => r.CoverMediaUploadId != null).Select(r => r.CoverMediaUploadId!.Value).ToList();
            var covers = (await _mediaRepository.ListAsync(new QueryOptions<MediaUpload>
            {
                Predicate = m => coverIds.Contains(m.Id),
                AsNoTracking = true,
            })).ToDictionary(m => m.Id);

            var bands = await BandsAsync(rows);
            var (canEdit, canDelete) = await PermissionsAsync();

            return rows.Select(r => Map(r, bands, canEdit, canDelete, r.CoverMediaUploadId != null && covers.TryGetValue(r.CoverMediaUploadId.Value, out var cover) ? _mediaAppService.Describe(cover) : null)).ToList();
        }

        private async Task<ArchiveEntryDto> MapDetailAsync(ArchiveReadModel row)
        {
            var media = await _archiveMediaRepository.ListAsync(new QueryOptions<ArchiveMedia>
            {
                Predicate = m => m.ArchiveEntryId == row.Id,
                Includes = [m => m.MediaUpload!],
                OrderBy = q => q.OrderBy(m => m.Order).ThenBy(m => m.Id),
                AsNoTracking = true,
            });

            var bands = await BandsAsync([row]);
            var (canEdit, canDelete) = await PermissionsAsync();
            var coverMedia = media.FirstOrDefault(m => m.MediaUploadId == row.CoverMediaUploadId) ?? media.FirstOrDefault();

            var dto = Map(row, bands, canEdit, canDelete, coverMedia != null ? _mediaAppService.Describe(coverMedia.MediaUpload!) : null);
            dto.Media = media.Select(m => new ArchiveMediaDto { Media = _mediaAppService.Describe(m.MediaUpload!), Caption = m.Caption }).ToList();

            var (attendees, _) = await _archiveRepository.PageAttendeesAsync(row.Id, 0, ArchiveRules.AttendeesPreview);
            dto.Attendees = attendees.Select(Summary).ToList();

            return dto;
        }

        private async Task<Dictionary<int, BandSummaryDto>> BandsAsync(IEnumerable<ArchiveReadModel> rows)
        {
            var ids = rows.SelectMany(r => r.Bands).Where(b => b.BandId != null).Select(b => b.BandId!.Value).Distinct().ToList();
            return await _bandAppService.SummariesByIds(ids);
        }

        private ArchiveEntryDto Map(ArchiveReadModel r, Dictionary<int, BandSummaryDto> bands, bool canEdit, bool canDelete, MediaDto? cover) => new()
        {
            Id = r.Id,
            Title = r.Title,
            Description = r.Description,
            Year = r.Year,
            Month = r.Month,
            Day = r.Day,
            Venue = r.Venue,
            City = r.City,
            CityName = r.CityName,
            Author = new ProfileSummaryDto
            {
                Id = r.AuthorUserId,
                Name = r.AuthorName,
                Nickname = r.AuthorNickname,
                AvatarUrl = _fileStorage.PublicUrl(r.AuthorAvatarKey),
            },
            Bands = r.Bands
                .Select(b => b.BandId != null && bands.TryGetValue(b.BandId.Value, out var band)
                    ? new ArchiveBandDto { Band = band, Name = band.Name }
                    : new ArchiveBandDto { Name = b.Name ?? string.Empty })
                .Where(b => b.Name.Length > 0)
                .ToList(),
            Cover = cover,
            PhotoCount = r.PhotoCount,
            VideoCount = r.VideoCount,
            LikeCount = r.LikeCount,
            LikedByMe = r.LikedByMe,
            AttendanceCount = r.AttendanceCount,
            AttendedByMe = r.AttendedByMe,
            CommentCount = r.CommentCount,
            CanEdit = canEdit,
            CanDelete = canDelete,
            CreatedAt = r.CreatedAt,
        };

        private ProfileSummaryDto Summary(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Nickname = user.Nickname,
            AvatarUrl = _fileStorage.PublicUrl(user.AvatarKey),
        };

        private static Result<ArchiveEntryDto> Fail(Error error) => Result<ArchiveEntryDto>.Fail(error);

        private static (ValidEntry? Data, Error? Error) Invalid(string code, string message) => (null, new Error(code, message, ErrorType.Validation));

        private static Error NotFound() => new("ARCHIVE_ENTRY_NOT_FOUND", "Archive entry not found", ErrorType.NotFound);
    }
}
