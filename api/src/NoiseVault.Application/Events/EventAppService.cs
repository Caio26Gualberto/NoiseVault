using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Events.Models;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.PublicProfiles.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Events;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Events
{
    public class EventAppService : IEventAppService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRepository<EventLineupEntry> _lineupRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IBandAppService _bandAppService;
        private readonly IPublicProfileAppService _publicProfileAppService;
        private readonly IPostAppService _postAppService;
        private readonly IFileStorage _fileStorage;
        private readonly ImageReplacement _images;
        private readonly INotificationAppService _notificationAppService;
        private readonly IApplicationLogger _logger;

        public EventAppService(
            IEventRepository eventRepository,
            IRepository<EventLineupEntry> lineupRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IBandAppService bandAppService,
            IPublicProfileAppService publicProfileAppService,
            IPostAppService postAppService,
            IFileStorage fileStorage,
            IImageProcessor imageProcessor,
            INotificationAppService notificationAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _eventRepository = eventRepository;
            _lineupRepository = lineupRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _bandAppService = bandAppService;
            _publicProfileAppService = publicProfileAppService;
            _postAppService = postAppService;
            _fileStorage = fileStorage;
            _images = new ImageReplacement(fileStorage, imageProcessor);
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Events);
        }

        public Task<Result<PagedResultDto<EventDto>>> List(EventListQueryDto query) =>
            PageAsync(query, filter => filter);

        public async Task<Result<List<EventDto>>> Upcoming(int take)
        {
            var (rows, _) = await _eventRepository.PageAsync(
                new EventFilter(DateTime.UtcNow),
                _currentUser.UserId,
                0,
                Math.Clamp(take, 1, EventRules.MaxUpcomingTake));

            return Result<List<EventDto>>.Ok(await MapAsync(rows), "EVENTS_FOUND", "Events found");
        }

        public async Task<Result<PagedResultDto<EventDto>>> ByBand(string slug, EventListQueryDto query)
        {
            var (bandId, _) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Result<PagedResultDto<EventDto>>.Fail(new Error("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound));

            return await PageAsync(query, filter => filter with { BandId = bandId });
        }

        public async Task<Result<PagedResultDto<EventDto>>> ByUser(string nickname, EventListQueryDto query)
        {
            var userId = await _publicProfileAppService.FindUserId(nickname);

            if (userId == null)
                return Result<PagedResultDto<EventDto>>.Fail(new Error("PROFILE_NOT_FOUND", "Profile not found", ErrorType.NotFound));

            return await PageAsync(query, filter => filter with { OrganizerUserId = userId });
        }

        public async Task<Result<EventDto>> Get(int id)
        {
            var row = await FindVisible(id);

            return row == null
                ? Fail(EventNotFound())
                : Result<EventDto>.Ok(await MapOneAsync(row), "EVENT_FOUND", "Event found");
        }

        public async Task<Result<EventDto>> Create(CreateEventInputDto input)
        {
            _logger.SetOperation("CreateEvent");

            var now = DateTime.UtcNow;
            var (data, error) = await ValidateAsync(input, creating: true, now);

            if (error != null)
                return Fail(error);

            var me = _currentUser.UserId;
            int? bandId = null;

            if (!string.IsNullOrWhiteSpace(input.BandSlug))
            {
                var (foundBandId, role) = await _bandAppService.FindAccess(input.BandSlug);

                if (foundBandId == null)
                    return Fail(new Error("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound));

                if (role == null)
                    return Fail(new Error("NOT_BAND_MEMBER", "Only band members can publish as the band", ErrorType.Forbidden));

                bandId = foundBandId;
            }

            if (await _eventRepository.CountUpcomingByOrganizerAsync(me, now) >= EventRules.MaxActiveEventsPerUser)
                return Fail(new Error("EVENT_LIMIT_REACHED", $"You can have at most {EventRules.MaxActiveEventsPerUser} upcoming events", ErrorType.Conflict));

            var entity = new Event
            {
                OrganizerUserId = me,
                BandId = bandId,
                Status = EEventStatus.Published,
            };
            Apply(entity, data!);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _eventRepository.AddAsync(entity);
                await AddLineupAsync(entity.Id, data!.Lineup);
                await _postAppService.CreateForEvent(entity.Id, me, bandId);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("EVENT_CREATED", "Event created", userId: me, extra: new { EventId = entity.Id, BandId = bandId, Lineup = data!.Lineup.Count });

            await NotifyLineupAsync(entity.Id, data!.Lineup, []);

            return Result<EventDto>.Ok(await MapOneAsync((await FindVisible(entity.Id))!), "EVENT_CREATED", "Event published");
        }

        public async Task<Result<EventDto>> Update(int id, EventInputDto input)
        {
            _logger.SetOperation("UpdateEvent");

            var entity = await FindManagedAsync(id, tracked: true);

            if (entity == null)
                return Fail(await DeniedAsync(id));

            if (entity.Status == EEventStatus.Cancelled)
                return Fail(Cancelled());

            var (data, error) = await ValidateAsync(input, creating: false, DateTime.UtcNow);

            if (error != null)
                return Fail(error);

            Apply(entity, data!);

            var current = await _lineupRepository.ListAsync(new QueryOptions<EventLineupEntry> { Predicate = l => l.EventId == id });

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _eventRepository.UpdateAsync(entity);

                foreach (var entry in current)
                    await _lineupRepository.DeleteAsync(entry);

                await AddLineupAsync(id, data!.Lineup);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("EVENT_UPDATED", "Event updated", userId: _currentUser.UserId, extra: new { EventId = id });

            await NotifyLineupAsync(id, data!.Lineup, current.Where(l => l.BandId != null).Select(l => l.BandId!.Value).ToHashSet());

            return Result<EventDto>.Ok(await MapOneAsync((await FindVisible(id))!), "EVENT_UPDATED", "Event updated");
        }

        public async Task<Result<EventDto>> Cancel(int id)
        {
            _logger.SetOperation("CancelEvent");

            var entity = await FindManagedAsync(id, tracked: false);

            if (entity == null)
                return Fail(await DeniedAsync(id));

            if (!await _eventRepository.TryCancelAsync(id, DateTime.UtcNow))
                return Fail(Cancelled());

            _logger.Info("EVENT_CANCELLED", "Event cancelled", userId: _currentUser.UserId, extra: new { EventId = id });

            await _notificationAppService.NotifyMany(
                await _eventRepository.InterestedUserIdsAsync(id),
                new NotificationInput(ENotificationKind.EventCancelled, _currentUser.UserId, EventId: id));

            return Result<EventDto>.Ok(await MapOneAsync((await FindVisible(id))!), "EVENT_CANCELLED_OK", "Event cancelled");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteEvent");

            var entity = await FindManagedAsync(id, tracked: true);

            if (entity == null)
                return Result.Fail(await DeniedAsync(id));

            var bannerKey = entity.BannerKey;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _eventRepository.SoftDelete(entity);
                await _postAppService.DeleteForEvent(id);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            if (bannerKey != null)
                await _images.DeleteAsync(bannerKey);

            _logger.Info("EVENT_DELETED", "Event deleted", userId: _currentUser.UserId, extra: new { EventId = id, ByBandOwner = entity.OrganizerUserId != _currentUser.UserId });

            return Result.Ok("EVENT_DELETED", "Event deleted");
        }

        public Task<Result<EventDto>> AddInterest(int id) => ReactAsync(id, interested: true);

        public Task<Result<EventDto>> RemoveInterest(int id) => ReactAsync(id, interested: false);

        public async Task<Result<EventDto>> SetBanner(int id, ImageUploadInput? file, int? position, CancellationToken cancellationToken = default)
        {
            _logger.SetOperation("SetEventBanner");

            var bannerPosition = position ?? Event.DefaultBannerPosition;

            if (!ProfileRules.IsValidBannerPosition(bannerPosition))
                return InvalidBannerPosition();

            var entity = await FindManagedAsync(id, tracked: true);

            if (entity == null)
                return Fail(await DeniedAsync(id));

            var upload = await _images.UploadAsync(file, EImagePreset.Banner, $"events/{id}/banner", cancellationToken);

            if (!upload.Succeeded)
                return Fail(upload.Error!);

            var oldKey = entity.BannerKey;
            entity.BannerKey = upload.Key;
            entity.BannerPosition = bannerPosition;

            await _images.SaveAsync(upload.Key!, oldKey, () => _eventRepository.UpdateAsync(entity));

            _logger.Info("BANNER_UPDATED", "Event banner updated", userId: _currentUser.UserId, extra: new { EventId = id, upload.Key });

            return Result<EventDto>.Ok(await MapOneAsync((await FindVisible(id))!), "BANNER_UPDATED", "Banner updated successfully");
        }

        public async Task<Result<EventDto>> SetBannerPosition(int id, BannerPositionInputDto input)
        {
            if (!ProfileRules.IsValidBannerPosition(input.Position))
                return InvalidBannerPosition();

            var entity = await FindManagedAsync(id, tracked: true);

            if (entity == null)
                return Fail(await DeniedAsync(id));

            if (entity.BannerKey == null)
                return Fail(new Error("BANNER_NOT_FOUND", "There is no banner to reposition", ErrorType.NotFound));

            entity.BannerPosition = input.Position;
            await _eventRepository.UpdateAsync(entity);

            return Result<EventDto>.Ok(await MapOneAsync((await FindVisible(id))!), "BANNER_POSITION_UPDATED", "Banner position updated");
        }

        public async Task<Result<EventDto>> RemoveBanner(int id)
        {
            var entity = await FindManagedAsync(id, tracked: true);

            if (entity == null)
                return Fail(await DeniedAsync(id));

            var oldKey = entity.BannerKey;

            if (oldKey != null)
            {
                entity.BannerKey = null;
                entity.BannerPosition = Event.DefaultBannerPosition;
                await _eventRepository.UpdateAsync(entity);
                await _images.DeleteAsync(oldKey);
                _logger.Info("BANNER_REMOVED", "Event banner removed", userId: _currentUser.UserId, extra: new { EventId = id, Key = oldKey });
            }

            return Result<EventDto>.Ok(await MapOneAsync((await FindVisible(id))!), "BANNER_REMOVED", "Banner removed");
        }

        public Task<EventReadModel?> FindVisible(int eventId) =>
            _eventRepository.FindAsync(eventId, _currentUser.UserId);

        public async Task<int> RemindTomorrow()
        {
            var from = AppTime.TomorrowStartUtc;
            var to = AppTime.NextDayStartUtc(AppTime.Today.AddDays(1))!.Value;

            var events = await _eventRepository.InterestedStartingBetweenAsync(from, to);

            foreach (var (eventId, userIds) in events)
                await _notificationAppService.NotifyMany(userIds, new NotificationInput(ENotificationKind.EventTomorrow, null, EventId: eventId));

            _logger.Info("EVENT_REMINDERS_SENT", "Reminders for tomorrow's events sent", extra: new { Events = events.Count, Recipients = events.Sum(e => e.UserIds.Count) });

            return events.Count;
        }

        private async Task NotifyLineupAsync(int eventId, List<EventLineupInputDto> lineup, HashSet<int> previousBandIds)
        {
            var added = lineup
                .Where(l => l.BandId != null && !previousBandIds.Contains(l.BandId.Value))
                .Select(l => l.BandId!.Value)
                .Distinct();

            foreach (var bandId in added)
                await _notificationAppService.NotifyMany(
                    await _bandAppService.MemberUserIds(bandId),
                    new NotificationInput(ENotificationKind.EventLineup, _currentUser.UserId, EventId: eventId, BandId: bandId));
        }

        private async Task<Result<EventDto>> ReactAsync(int id, bool interested)
        {
            var row = await FindVisible(id);

            if (row == null)
                return Fail(EventNotFound());

            if (interested)
            {
                if (row.Status == EEventStatus.Cancelled)
                    return Fail(Cancelled());

                if (row.EndsAtOrDefault < DateTime.UtcNow)
                    return Fail(new Error("EVENT_ENDED", "This event already happened", ErrorType.Conflict));

                if (await _eventRepository.AddInterestAsync(id, _currentUser.UserId))
                    await _notificationAppService.Notify(row.OrganizerUserId, new NotificationInput(ENotificationKind.EventInterest, _currentUser.UserId, EventId: id));
            }
            else
            {
                await _eventRepository.RemoveInterestAsync(id, _currentUser.UserId);
            }

            return Result<EventDto>.Ok(
                await MapOneAsync((await FindVisible(id))!),
                interested ? "EVENT_INTEREST_ADDED" : "EVENT_INTEREST_REMOVED",
                interested ? "Marked as going" : "Removed from going");
        }

        private async Task<Result<PagedResultDto<EventDto>>> PageAsync(EventListQueryDto query, Func<EventFilter, EventFilter> narrow)
        {
            query.Normalize();

            var filter = narrow(new EventFilter(
                DateTime.UtcNow,
                Past: string.Equals(query.Scope, "past", StringComparison.OrdinalIgnoreCase),
                Genre: query.Genre,
                City: query.City,
                Search: query.Search,
                InterestedOnly: query.Interested));

            var (rows, totalCount) = await _eventRepository.PageAsync(filter, _currentUser.UserId, query.Skip, query.PageSize);

            return Result<PagedResultDto<EventDto>>.Ok(
                PagedResultDto<EventDto>.Create(await MapAsync(rows), query.Page, query.PageSize, totalCount),
                "EVENTS_FOUND",
                "Events found");
        }

        private Task<Event?> FindManagedAsync(int id, bool tracked)
        {
            var me = _currentUser.UserId;

            return _eventRepository.FirstOrDefaultAsync(new QueryOptions<Event>
            {
                Predicate = e => e.Id == id
                    && (e.OrganizerUserId == me
                        || (e.BandId != null && e.Band!.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner))),
                Includes = [e => e.Genres],
                AsNoTracking = !tracked,
            });
        }

        private async Task<Error> DeniedAsync(int id) =>
            await FindVisible(id) == null
                ? EventNotFound()
                : new Error("EVENT_FORBIDDEN", "Only the organizer can do this", ErrorType.Forbidden);

        private async Task AddLineupAsync(int eventId, List<EventLineupInputDto> lineup)
        {
            if (lineup.Count == 0)
                return;

            await _lineupRepository.AddRangeAsync(lineup
                .Select((entry, index) => new EventLineupEntry
                {
                    EventId = eventId,
                    BandId = entry.BandId,
                    Name = entry.BandId == null ? entry.Name : null,
                    Order = index,
                })
                .ToList());
        }

        private sealed record ValidEvent(
            string Title,
            string? Description,
            DateTime StartsAt,
            DateTime? EndsAt,
            string Venue,
            EAbcCity City,
            string? CityName,
            List<EMusicGenre> Genres,
            string? TicketsUrl,
            decimal? Price,
            int? Capacity,
            List<EventLineupInputDto> Lineup);

        private async Task<(ValidEvent? Data, Error? Error)> ValidateAsync(EventInputDto input, bool creating, DateTime now)
        {
            var title = ProfileRules.NormalizeOptionalText(input.Title) ?? string.Empty;

            if (title.Length is < EventRules.TitleMinLength or > EventRules.TitleMaxLength)
                return Invalid("INVALID_EVENT_TITLE", $"Title must have between {EventRules.TitleMinLength} and {EventRules.TitleMaxLength} characters");

            var description = ProfileRules.NormalizeOptionalText(input.Description);

            if (description is { Length: > EventRules.DescriptionMaxLength })
                return Invalid("INVALID_EVENT_DESCRIPTION", $"Description must have at most {EventRules.DescriptionMaxLength} characters");

            var startsAt = DateTime.SpecifyKind(input.StartsAt.ToUniversalTime(), DateTimeKind.Utc);
            DateTime? endsAt = input.EndsAt is { } end ? DateTime.SpecifyKind(end.ToUniversalTime(), DateTimeKind.Utc) : null;

            if (input.StartsAt == default || (endsAt != null && (endsAt <= startsAt || endsAt - startsAt > EventRules.MaxDuration)))
                return Invalid("INVALID_EVENT_DATES", "The event must end after it starts and last at most 7 days");

            if (creating && startsAt < now - EventRules.CreationTolerance)
                return Invalid("EVENT_IN_PAST", "The event must start in the future");

            var venue = ProfileRules.NormalizeOptionalText(input.Venue) ?? string.Empty;

            if (venue.Length is < EventRules.VenueMinLength or > EventRules.VenueMaxLength)
                return Invalid("INVALID_EVENT_VENUE", $"Venue must have between {EventRules.VenueMinLength} and {EventRules.VenueMaxLength} characters");

            if (!Enum.IsDefined(input.City))
                return Invalid("INVALID_CITY", "City is not in the supported list");

            var cityName = input.City == EAbcCity.Other ? ProfileRules.NormalizeOptionalText(input.CityName) : null;

            if (input.City == EAbcCity.Other && cityName is not { Length: >= EventRules.CityNameMinLength and <= EventRules.CityNameMaxLength })
                return Invalid("INVALID_EVENT_CITY_NAME", $"Tell the city name, between {EventRules.CityNameMinLength} and {EventRules.CityNameMaxLength} characters");

            var genres = input.Genres ?? [];

            if (!ProfileRules.IsValidGenres(genres))
                return Invalid("INVALID_GENRE", $"Choose up to {ProfileRules.MaxGenres} distinct genres from the supported list");

            var ticketsUrl = ProfileRules.NormalizeOptionalText(input.TicketsUrl);

            if (!EventRules.IsValidTicketsUrl(ticketsUrl))
                return Invalid("INVALID_TICKETS_URL", "Tickets link must be an http(s) address");

            if (input.Price is < 0 or > EventRules.MaxPrice)
                return Invalid("INVALID_EVENT_PRICE", $"Price must be between 0 and {EventRules.MaxPrice}");

            if (input.Capacity is < 1 or > EventRules.MaxCapacity)
                return Invalid("INVALID_EVENT_CAPACITY", $"Capacity must be between 1 and {EventRules.MaxCapacity}");

            var (lineup, lineupError) = await ValidateLineupAsync(input.Lineup ?? []);

            if (lineupError != null)
                return (null, lineupError);

            return (new ValidEvent(
                title,
                description,
                startsAt,
                endsAt,
                venue,
                input.City,
                cityName,
                genres,
                ticketsUrl,
                input.Price is { } price ? Math.Round(price, 2) : null,
                input.Capacity,
                lineup!), null);
        }

        private async Task<(List<EventLineupInputDto>? Lineup, Error? Error)> ValidateLineupAsync(List<EventLineupInputDto> input)
        {
            if (input.Count > EventRules.MaxLineup)
                return (null, InvalidError("INVALID_LINEUP", $"The line-up can have at most {EventRules.MaxLineup} entries"));

            var lineup = new List<EventLineupInputDto>();

            foreach (var entry in input)
            {
                var name = ProfileRules.NormalizeOptionalText(entry.Name);

                if (entry.BandId == null && name is not { Length: <= EventRules.LineupNameMaxLength })
                    return (null, InvalidError("INVALID_LINEUP", $"Each line-up entry needs a band or a name up to {EventRules.LineupNameMaxLength} characters"));

                lineup.Add(new EventLineupInputDto { BandId = entry.BandId, Name = entry.BandId == null ? name : null });
            }

            var bandIds = lineup.Where(l => l.BandId != null).Select(l => l.BandId!.Value).ToList();

            if (bandIds.Count != bandIds.Distinct().Count())
                return (null, InvalidError("INVALID_LINEUP", "A band can appear only once in the line-up"));

            if (bandIds.Count > 0 && (await _bandAppService.SummariesByIds(bandIds)).Count != bandIds.Count)
                return (null, new Error("LINEUP_BAND_NOT_FOUND", "Some line-up band does not exist anymore", ErrorType.Validation));

            return (lineup, null);
        }

        private static void Apply(Event entity, ValidEvent data)
        {
            entity.Title = data.Title;
            entity.Description = data.Description;
            entity.StartsAt = data.StartsAt;
            entity.EndsAt = data.EndsAt;
            entity.EndsAtOrDefault = EventRules.EndsAtOrDefault(data.StartsAt, data.EndsAt);
            entity.Venue = data.Venue;
            entity.City = data.City;
            entity.CityName = data.CityName;
            entity.TicketsUrl = data.TicketsUrl;
            entity.Price = data.Price;
            entity.Capacity = data.Capacity;

            entity.Genres.RemoveAll(g => !data.Genres.Contains(g.Genre));
            foreach (var genre in data.Genres.Where(g => entity.Genres.All(existing => existing.Genre != g)))
                entity.Genres.Add(new EventGenre { EventId = entity.Id, Genre = genre });
        }

        private async Task<EventDto> MapOneAsync(EventReadModel row) => (await MapAsync([row]))[0];

        private async Task<List<EventDto>> MapAsync(List<EventReadModel> rows)
        {
            if (rows.Count == 0)
                return [];

            var bandIds = rows
                .SelectMany(r => r.Lineup.Where(l => l.BandId != null).Select(l => l.BandId!.Value).Append(r.BandId ?? 0))
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var bands = await _bandAppService.SummariesByIds(bandIds);
            var me = _currentUser.UserId;
            var now = DateTime.UtcNow;

            return rows.Select(r => new EventDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                StartsAt = r.StartsAt,
                EndsAt = r.EndsAt,
                Venue = r.Venue,
                City = r.City,
                CityName = r.CityName,
                BannerUrl = _fileStorage.PublicUrl(r.BannerKey),
                BannerPosition = r.BannerPosition,
                Genres = r.Genres,
                TicketsUrl = r.TicketsUrl,
                Price = r.Price,
                Capacity = r.Capacity,
                Status = r.Status,
                CancelledAt = r.CancelledAt,
                HasEnded = r.EndsAtOrDefault < now,
                Organizer = new ProfileSummaryDto
                {
                    Id = r.OrganizerUserId,
                    Name = r.OrganizerName,
                    Nickname = r.OrganizerNickname,
                    AvatarUrl = _fileStorage.PublicUrl(r.OrganizerAvatarKey),
                },
                Band = r.BandId != null && bands.TryGetValue(r.BandId.Value, out var band) ? band : null,
                Lineup = r.Lineup
                    .Select(l => new EventLineupItemDto
                    {
                        Band = l.BandId != null && bands.TryGetValue(l.BandId.Value, out var lineupBand) ? lineupBand : null,
                        Name = l.BandId != null && bands.TryGetValue(l.BandId.Value, out var named) ? named.Name : l.Name,
                    })
                    .Where(l => l.Name != null)
                    .ToList(),
                InterestedCount = r.InterestedCount,
                Interested = r.Interested,
                CommentCount = r.CommentCount,
                CanManage = r.OrganizerUserId == me || r.ViewerOwnsBand,
                CreatedAt = r.CreatedAt,
            }).ToList();
        }

        private static Result<EventDto> Fail(Error error) => Result<EventDto>.Fail(error);

        private static (ValidEvent? Data, Error? Error) Invalid(string code, string message) => (null, InvalidError(code, message));

        private static Error InvalidError(string code, string message) => new(code, message, ErrorType.Validation);

        private static Result<EventDto> InvalidBannerPosition() =>
            Fail(InvalidError("INVALID_BANNER_POSITION", $"Banner position must be between {ProfileRules.MinBannerPosition} and {ProfileRules.MaxBannerPosition}"));

        private static Error EventNotFound() => new("EVENT_NOT_FOUND", "Event not found", ErrorType.NotFound);

        private static Error Cancelled() => new("EVENT_CANCELLED", "This event was cancelled", ErrorType.Conflict);
    }
}
