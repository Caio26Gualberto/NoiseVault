using System.Linq.Expressions;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.BandOpenings.Dtos;
using NoiseVault.Application.BandOpenings.Interfaces;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Cities;
using NoiseVault.Application.Geo;
using NoiseVault.Application.Musicians;
using NoiseVault.Application.Musicians.Dtos;
using NoiseVault.Application.Musicians.Interfaces;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Ensaio;
using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.BandOpenings
{
    public class BandOpeningAppService : IBandOpeningAppService
    {
        private readonly IRepository<BandOpening> _openingRepository;
        private readonly IRepository<BandOpeningInterest> _interestRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IBandAppService _bandAppService;
        private readonly IBandInvitationAppService _invitationAppService;
        private readonly IMusicianAppService _musicianAppService;
        private readonly IPostAppService _postAppService;
        private readonly INotificationAppService _notificationAppService;
        private readonly IApplicationLogger _logger;

        public BandOpeningAppService(
            IRepository<BandOpening> openingRepository,
            IRepository<BandOpeningInterest> interestRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IBandAppService bandAppService,
            IBandInvitationAppService invitationAppService,
            IMusicianAppService musicianAppService,
            IPostAppService postAppService,
            INotificationAppService notificationAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _openingRepository = openingRepository;
            _interestRepository = interestRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _bandAppService = bandAppService;
            _invitationAppService = invitationAppService;
            _musicianAppService = musicianAppService;
            _postAppService = postAppService;
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Ensaio);
        }

        public async Task<Result<BandOpeningDto>> Create(string slug, BandOpeningInputDto input)
        {
            _logger.SetOperation("CreateBandOpening");

            var error = Validate(input, out var otherName, out var description);

            if (error != null)
                return Fail(error);

            var (bandId, role) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Fail(BandNotFound());

            if (role != EBandMemberRole.Owner)
                return Fail(BandForbidden());

            if (await _bandAppService.KindOf(bandId.Value) == EBandKind.Solo)
                return Fail(new Error("SOLO_ARTIST_NO_MEMBERS", "Solo artists do not have members, so they cannot open member openings", ErrorType.Conflict));

            var now = DateTime.UtcNow;
            var open = await _openingRepository.CountAsync(new QueryOptions<BandOpening>
            {
                Predicate = o => o.BandId == bandId && o.Status == EBandOpeningStatus.Open && o.ExpiresAt > now,
            });

            if (open >= BandOpeningRules.MaxOpenPerBand)
                return Fail(new Error("OPENING_LIMIT_REACHED", $"A band can have at most {BandOpeningRules.MaxOpenPerBand} open positions", ErrorType.Conflict));

            var me = _currentUser.UserId;
            var opening = new BandOpening
            {
                BandId = bandId.Value,
                Instrument = input.Instrument,
                OtherName = otherName,
                Description = description,
                Goal = input.Goal,
                Status = EBandOpeningStatus.Open,
                ExpiresAt = now.Add(BandOpeningRules.Lifetime),
            };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _openingRepository.AddAsync(opening);
                await _postAppService.CreateForOpening(opening.Id, me, bandId.Value);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("OPENING_CREATED", "Band opening created", userId: me, extra: new { OpeningId = opening.Id, BandId = bandId, opening.Instrument });

            return Ok(await MapOneAsync(opening.Id), "OPENING_CREATED", "Opening published");
        }

        public async Task<Result<BandOpeningDto>> Update(int id, BandOpeningInputDto input)
        {
            var error = Validate(input, out var otherName, out var description);

            if (error != null)
                return Fail(error);

            var opening = await FindManagedAsync(id, tracked: true);

            if (opening == null)
                return Fail(await DeniedAsync(id));

            if (opening.Status != EBandOpeningStatus.Open)
                return Fail(NotOpen());

            opening.Instrument = input.Instrument;
            opening.OtherName = otherName;
            opening.Description = description;
            opening.Goal = input.Goal;
            await _openingRepository.UpdateAsync(opening);

            return Ok(await MapOneAsync(id), "OPENING_UPDATED", "Opening updated");
        }

        public async Task<Result<BandOpeningDto>> Close(int id, CloseOpeningInputDto input)
        {
            _logger.SetOperation("CloseBandOpening");

            var opening = await FindManagedAsync(id, tracked: true);

            if (opening == null)
                return Fail(await DeniedAsync(id));

            if (opening.Status != EBandOpeningStatus.Open)
                return Fail(NotOpen());

            opening.Status = input.Filled ? EBandOpeningStatus.Filled : EBandOpeningStatus.Closed;
            opening.ClosedAt = DateTime.UtcNow;
            await _openingRepository.UpdateAsync(opening);

            _logger.Info("OPENING_CLOSED", "Band opening closed", userId: _currentUser.UserId, extra: new { OpeningId = id, input.Filled });

            return Ok(await MapOneAsync(id), "OPENING_CLOSED", "Opening closed");
        }

        public async Task<Result<BandOpeningDto>> Renew(int id)
        {
            var opening = await FindManagedAsync(id, tracked: true);

            if (opening == null)
                return Fail(await DeniedAsync(id));

            if (opening.Status != EBandOpeningStatus.Open)
                return Fail(NotOpen());

            opening.ExpiresAt = DateTime.UtcNow.Add(BandOpeningRules.Lifetime);
            await _openingRepository.UpdateAsync(opening);

            return Ok(await MapOneAsync(id), "OPENING_RENEWED", "Opening renewed");
        }

        public async Task<Result> Delete(int id)
        {
            var opening = await FindManagedAsync(id, tracked: true);

            if (opening == null)
                return Result.Fail(await DeniedAsync(id));

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _openingRepository.SoftDelete(opening);
                await _postAppService.DeleteForOpening(id);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("OPENING_DELETED", "Band opening deleted", userId: _currentUser.UserId, extra: new { OpeningId = id, opening.BandId });

            return Result.Ok("OPENING_DELETED", "Opening deleted");
        }

        public async Task<Result<BandOpeningDto>> Get(int id)
        {
            var exists = await _openingRepository.CountAsync(new QueryOptions<BandOpening> { Predicate = o => o.Id == id }) > 0;

            return exists
                ? Ok(await MapOneAsync(id), "OPENING_FOUND", "Opening found")
                : Fail(OpeningNotFound());
        }

        public async Task<Result<PagedResultDto<BandOpeningDto>>> Search(BandOpeningSearchQueryDto query)
        {
            query.Normalize();

            var now = DateTime.UtcNow;
            var instrument = query.Instrument;
            var genre = query.Genre;
            var origin = await _musicianAppService.ResolveOrigin(query.NearCityId);

            var byDistance = origin != null && query.MaxKm != null;
            var oLat = origin?.Latitude ?? 0;
            var oLon = origin?.Longitude ?? 0;
            var ky = GeoRules.KmPerDegreeLatitude;
            var kx = GeoRules.KmPerDegreeLongitude(oLat);
            var maxKm = Math.Clamp(query.MaxKm ?? GeoRules.MaxSearchKm, 1, GeoRules.MaxSearchKm);
            var max2 = (double)maxKm * maxKm;

            Func<IQueryable<BandOpening>, IOrderedQueryable<BandOpening>>? orderBy = byDistance
                ? q => q
                    .OrderBy(o => (o.Band!.City!.Latitude - oLat) * ky * ((o.Band.City!.Latitude - oLat) * ky) + (o.Band.City!.Longitude - oLon) * kx * ((o.Band.City!.Longitude - oLon) * kx))
                    .ThenByDescending(o => o.CreatedAt)
                    .ThenByDescending(o => o.Id)
                : null;

            return await PageAsync(
                o => o.Status == EBandOpeningStatus.Open
                    && o.ExpiresAt > now
                    && !o.Band!.IsDeleted
                    && (instrument == null || o.Instrument == instrument)
                    && (genre == null || o.Band.Genres.Any(g => g.Genre == genre))
                    && (!byDistance || (o.Band.City!.Latitude - oLat) * ky * ((o.Band.City!.Latitude - oLat) * ky) + (o.Band.City!.Longitude - oLon) * kx * ((o.Band.City!.Longitude - oLon) * kx) <= max2),
                query,
                origin,
                orderBy);
        }

        public async Task<Result<PagedResultDto<BandOpeningDto>>> ListForBand(string slug, PagedRequestDto query)
        {
            query.Normalize();

            var (bandId, role) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Result<PagedResultDto<BandOpeningDto>>.Fail(BandNotFound());

            var now = DateTime.UtcNow;
            var owner = role == EBandMemberRole.Owner;

            return await PageAsync(
                o => o.BandId == bandId && (owner || (o.Status == EBandOpeningStatus.Open && o.ExpiresAt > now)),
                query);
        }

        public async Task<Result<BandOpeningDto>> ExpressInterest(int id, OpeningInterestInputDto input)
        {
            _logger.SetOperation("ExpressOpeningInterest");

            var message = ProfileRules.NormalizeOptionalText(input.Message);

            if (message is { Length: > BandOpeningRules.InterestMessageMaxLength })
                return Fail(new Error("INVALID_INTEREST_MESSAGE", $"The message must have at most {BandOpeningRules.InterestMessageMaxLength} characters", ErrorType.Validation));

            var opening = await _openingRepository.FirstOrDefaultAsync(new QueryOptions<BandOpening> { Predicate = o => o.Id == id, AsNoTracking = true });

            if (opening == null)
                return Fail(OpeningNotFound());

            if (opening.Status != EBandOpeningStatus.Open || opening.ExpiresAt <= DateTime.UtcNow)
                return Fail(NotOpen());

            var me = _currentUser.UserId;

            if (!await _musicianAppService.HasProfile(me))
                return Fail(new Error("MUSICIAN_PROFILE_REQUIRED", "Create your musician profile first", ErrorType.Conflict));

            if (await _bandAppService.IsMember(opening.BandId, me))
                return Fail(new Error("ALREADY_BAND_MEMBER", "You are already a member of this band", ErrorType.Conflict));

            if (await _interestRepository.CountAsync(new QueryOptions<BandOpeningInterest> { Predicate = i => i.BandOpeningId == id && i.UserId == me }) > 0)
                return Fail(new Error("OPENING_INTEREST_DUPLICATE", "You already showed interest in this opening", ErrorType.Conflict));

            var interest = new BandOpeningInterest
            {
                BandOpeningId = id,
                UserId = me,
                Message = message,
                Status = EOpeningInterestStatus.Pending,
            };

            await _interestRepository.AddAsync(interest);

            _logger.Info("OPENING_INTEREST_SENT", "Interest sent to a band opening", userId: me, extra: new { OpeningId = id, opening.BandId, InterestId = interest.Id });

            await _notificationAppService.NotifyMany(
                await _bandAppService.MemberUserIds(opening.BandId, EBandMemberRole.Owner),
                new NotificationInput(ENotificationKind.BandOpeningInterest, me, BandId: opening.BandId, BandOpeningId: id));

            return Ok(await MapOneAsync(id), "OPENING_INTEREST_SENT", "Interest sent");
        }

        public async Task<Result<BandOpeningDto>> WithdrawInterest(int id)
        {
            var me = _currentUser.UserId;
            var interest = await _interestRepository.FirstOrDefaultAsync(new QueryOptions<BandOpeningInterest>
            {
                Predicate = i => i.BandOpeningId == id && i.UserId == me && i.Status == EOpeningInterestStatus.Pending,
            });

            if (interest == null)
                return Fail(InterestNotFound());

            await _interestRepository.SoftDelete(interest);

            return Ok(await MapOneAsync(id), "OPENING_INTEREST_WITHDRAWN", "Interest withdrawn");
        }

        public async Task<Result<PagedResultDto<OpeningInterestDto>>> ListInterests(int id, PagedRequestDto query)
        {
            query.Normalize();

            var opening = await FindManagedAsync(id, tracked: false);

            if (opening == null)
                return Result<PagedResultDto<OpeningInterestDto>>.Fail(await DeniedAsync(id));

            var options = new QueryOptions<BandOpeningInterest>
            {
                Predicate = i => i.BandOpeningId == id && i.User!.IsActive,
                OrderBy = q => q.OrderBy(i => i.Status).ThenByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var total = await _interestRepository.CountAsync(options);
            var rows = await _interestRepository.ListAsync(options);
            var cards = await _musicianAppService.CardsByUserIds(rows.Select(r => r.UserId).Distinct().ToList());

            return Result<PagedResultDto<OpeningInterestDto>>.Ok(
                PagedResultDto<OpeningInterestDto>.Create(
                    rows.Where(r => cards.ContainsKey(r.UserId)).Select(r => MapInterest(r, cards[r.UserId])).ToList(),
                    query.Page,
                    query.PageSize,
                    total),
                "OPENING_INTERESTS_FOUND",
                "Interests found");
        }

        public async Task<Result<OpeningInterestDto>> InviteInterested(int id, int interestId, InviteFromInterestInputDto input)
        {
            _logger.SetOperation("InviteFromOpeningInterest");

            var opening = await FindManagedAsync(id, tracked: false);

            if (opening == null)
                return Result<OpeningInterestDto>.Fail(await DeniedAsync(id));

            var interest = await FindInterestAsync(id, interestId);

            if (interest == null)
                return Result<OpeningInterestDto>.Fail(InterestNotFound());

            var invitation = await _invitationAppService.Invite(opening.Band!.Slug, new InviteMemberInputDto
            {
                Nickname = interest.User!.Nickname,
                Instrument = input.Instrument,
            });

            if (!invitation.IsSuccess)
                return Result<OpeningInterestDto>.Fail(invitation.Error!);

            interest.Status = EOpeningInterestStatus.Invited;
            interest.BandInvitationId = invitation.Data!.Id;
            await _interestRepository.UpdateAsync(interest);

            return await InterestResultAsync(interest, "OPENING_INTEREST_INVITED", "Invitation sent");
        }

        public async Task<Result<OpeningInterestDto>> DismissInterested(int id, int interestId)
        {
            var opening = await FindManagedAsync(id, tracked: false);

            if (opening == null)
                return Result<OpeningInterestDto>.Fail(await DeniedAsync(id));

            var interest = await FindInterestAsync(id, interestId);

            if (interest == null)
                return Result<OpeningInterestDto>.Fail(InterestNotFound());

            interest.Status = EOpeningInterestStatus.Dismissed;
            await _interestRepository.UpdateAsync(interest);

            return await InterestResultAsync(interest, "OPENING_INTEREST_DISMISSED", "Interest dismissed");
        }

        private Task<BandOpeningInterest?> FindInterestAsync(int openingId, int interestId) =>
            _interestRepository.FirstOrDefaultAsync(new QueryOptions<BandOpeningInterest>
            {
                Predicate = i => i.Id == interestId && i.BandOpeningId == openingId && i.Status == EOpeningInterestStatus.Pending,
                Includes = [i => i.User!],
            });

        private async Task<Result<OpeningInterestDto>> InterestResultAsync(BandOpeningInterest interest, string code, string message)
        {
            var cards = await _musicianAppService.CardsByUserIds([interest.UserId]);

            return Result<OpeningInterestDto>.Ok(MapInterest(interest, cards.GetValueOrDefault(interest.UserId) ?? new()), code, message);
        }

        private async Task<Result<PagedResultDto<BandOpeningDto>>> PageAsync(
            Expression<Func<BandOpening, bool>> predicate,
            PagedRequestDto query,
            GeoPoint? origin = null,
            Func<IQueryable<BandOpening>, IOrderedQueryable<BandOpening>>? orderBy = null)
        {
            var options = new QueryOptions<BandOpening>
            {
                Predicate = predicate,
                Includes = [o => o.Band!, o => o.Band!.City!, o => o.Band!.Members],
                OrderBy = orderBy ?? (q => q.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var total = await _openingRepository.CountAsync(options);
            var rows = await _openingRepository.ListAsync(options);

            return Result<PagedResultDto<BandOpeningDto>>.Ok(
                PagedResultDto<BandOpeningDto>.Create(await MapAsync(rows, origin), query.Page, query.PageSize, total),
                "OPENINGS_FOUND",
                "Openings found");
        }

        private async Task<BandOpeningDto> MapOneAsync(int id)
        {
            var row = await _openingRepository.FirstOrDefaultAsync(new QueryOptions<BandOpening>
            {
                Predicate = o => o.Id == id,
                Includes = [o => o.Band!, o => o.Band!.City!, o => o.Band!.Members],
                AsNoTracking = true,
            });

            return (await MapAsync([row!], await _musicianAppService.ResolveOrigin(null)))[0];
        }

        private async Task<List<BandOpeningDto>> MapAsync(List<BandOpening> rows, GeoPoint? origin)
        {
            if (rows.Count == 0)
                return [];

            var me = _currentUser.UserId;
            var now = DateTime.UtcNow;
            var ids = rows.Select(r => r.Id).ToList();
            var bands = await _bandAppService.SummariesByIds(rows.Select(r => r.BandId).Distinct().ToList());
            var managed = rows
                .Where(r => r.Band!.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner))
                .Select(r => r.Id)
                .ToHashSet();

            var interests = await _interestRepository.ListAsync(new QueryOptions<BandOpeningInterest>
            {
                Predicate = i => ids.Contains(i.BandOpeningId) && (i.UserId == me || i.Status == EOpeningInterestStatus.Pending),
                AsNoTracking = true,
            });

            var mine = interests.Where(i => i.UserId == me).ToDictionary(i => i.BandOpeningId);
            var pending = interests.Where(i => i.Status == EOpeningInterestStatus.Pending).GroupBy(i => i.BandOpeningId).ToDictionary(g => g.Key, g => g.Count());

            return rows.Select(r => new BandOpeningDto
            {
                Id = r.Id,
                Band = bands.GetValueOrDefault(r.BandId) ?? new BandSummaryDto { Id = r.BandId },
                City = r.Band!.City == null ? null : CityAppService.Map(r.Band.City),
                CityName = r.Band.CityName,
                Instrument = r.Instrument,
                OtherName = r.OtherName,
                Description = r.Description,
                Goal = r.Goal,
                Status = r.Status,
                IsExpired = r.ExpiresAt <= now,
                ExpiresAt = r.ExpiresAt,
                CreatedAt = r.CreatedAt,
                CanManage = managed.Contains(r.Id),
                MyInterest = mine.TryGetValue(r.Id, out var my) ? new OpeningMyInterestDto { Status = my.Status, Message = my.Message } : null,
                InterestCount = managed.Contains(r.Id) ? pending.GetValueOrDefault(r.Id) : null,
                DistanceKm = MusicianAppService.DistanceBucket(origin, CityAppService.Point(r.Band.City)),
            }).ToList();
        }

        private static OpeningInterestDto MapInterest(BandOpeningInterest interest, MusicianCardDto card) => new()
        {
            Id = interest.Id,
            Musician = card,
            Message = interest.Message,
            Status = interest.Status,
            CreatedAt = interest.CreatedAt,
        };

        private Task<BandOpening?> FindManagedAsync(int id, bool tracked)
        {
            var me = _currentUser.UserId;

            return _openingRepository.FirstOrDefaultAsync(new QueryOptions<BandOpening>
            {
                Predicate = o => o.Id == id && o.Band!.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner),
                Includes = [o => o.Band!],
                AsNoTracking = !tracked,
            });
        }

        private async Task<Error> DeniedAsync(int id) =>
            await _openingRepository.CountAsync(new QueryOptions<BandOpening> { Predicate = o => o.Id == id }) > 0
                ? BandForbidden()
                : OpeningNotFound();

        private static Error? Validate(BandOpeningInputDto input, out string? otherName, out string description)
        {
            otherName = input.Instrument == EInstrument.Other ? ProfileRules.NormalizeOptionalText(input.OtherName) : null;
            description = (input.Description ?? string.Empty).Trim();

            if (!MusicianRules.IsValidInstrument(input.Instrument, otherName))
                return new Error("INVALID_OPENING_INSTRUMENT", "Choose a supported instrument, or name it when it is other", ErrorType.Validation);

            if (input.Goal != null && !Enum.IsDefined(input.Goal.Value))
                return new Error("INVALID_MUSICIAN_PREFERENCES", "Goal must be a supported value", ErrorType.Validation);

            if (description.Length is < 1 or > BandOpeningRules.DescriptionMaxLength)
                return new Error("INVALID_OPENING_DESCRIPTION", $"Describe the opening in up to {BandOpeningRules.DescriptionMaxLength} characters", ErrorType.Validation);

            return null;
        }

        private static Error OpeningNotFound() => new("OPENING_NOT_FOUND", "Opening not found", ErrorType.NotFound);

        private static Error InterestNotFound() => new("OPENING_INTEREST_NOT_FOUND", "Interest not found", ErrorType.NotFound);

        private static Error NotOpen() => new("OPENING_NOT_OPEN", "This opening is no longer open", ErrorType.Conflict);

        private static Error BandNotFound() => new("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound);

        private static Error BandForbidden() => new("BAND_FORBIDDEN", "Only band owners can manage openings", ErrorType.Forbidden);

        private static Result<BandOpeningDto> Ok(BandOpeningDto dto, string code, string message) => Result<BandOpeningDto>.Ok(dto, code, message);

        private static Result<BandOpeningDto> Fail(Error error) => Result<BandOpeningDto>.Fail(error);
    }
}
