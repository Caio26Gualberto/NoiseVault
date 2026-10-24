using System.Linq.Expressions;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.Bands
{
    public class BandPartnershipAppService : IBandPartnershipAppService
    {
        private readonly IBandPartnershipRepository _partnershipRepository;
        private readonly IRepository<Band> _bandRepository;
        private readonly IBandAppService _bandAppService;
        private readonly ICurrentUser _currentUser;
        private readonly INotificationAppService _notificationAppService;
        private readonly IApplicationLogger _logger;

        public BandPartnershipAppService(
            IBandPartnershipRepository partnershipRepository,
            IRepository<Band> bandRepository,
            IBandAppService bandAppService,
            ICurrentUser currentUser,
            INotificationAppService notificationAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _partnershipRepository = partnershipRepository;
            _bandRepository = bandRepository;
            _bandAppService = bandAppService;
            _currentUser = currentUser;
            _notificationAppService = notificationAppService;
            _logger = loggerFactory.Create(LogContext.Bands);
        }

        public async Task<Result<BandPartnershipDto>> Invite(string slug, InvitePartnerInputDto input)
        {
            _logger.SetOperation("InviteBandPartner");

            var requesterId = await OwnedBandIdAsync(slug);

            if (requesterId.Error != null)
                return Fail(requesterId.Error);

            var targetSlug = BandRules.NormalizeSlug(input.TargetSlug);

            var target = await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Slug == targetSlug,
                AsNoTracking = true,
            });

            if (target == null)
                return Fail(new Error("BAND_PARTNER_NOT_FOUND", "No band or solo artist with this address", ErrorType.NotFound));

            var requester = requesterId.Value;

            if (target.Id == requester)
                return Fail(new Error("CANNOT_PARTNER_SELF", "A page cannot partner with itself", ErrorType.Validation));

            var (low, high) = Pair(requester, target.Id);
            var now = DateTime.UtcNow;

            await _partnershipRepository.RetireExpiredAsync(low, high, now);

            var existing = await _partnershipRepository.FirstOrDefaultAsync(new QueryOptions<BandPartnership>
            {
                Predicate = p => p.LowBandId == low && p.HighBandId == high
                    && (p.Status == EBandPartnershipStatus.Pending || p.Status == EBandPartnershipStatus.Accepted),
                AsNoTracking = true,
            });

            if (existing?.Status == EBandPartnershipStatus.Accepted)
                return Fail(new Error("ALREADY_PARTNERS", "These pages already play together", ErrorType.Conflict));

            if (existing != null)
                return Fail(new Error("BAND_PARTNERSHIP_ALREADY_PENDING", "There is already a pending partnership invitation between these pages", ErrorType.Conflict));

            var cooldownStart = now - BandRules.PartnershipCooldown;

            if (await _partnershipRepository.CountAsync(new QueryOptions<BandPartnership>
                {
                    Predicate = p => p.RequesterBandId == requester && p.TargetBandId == target.Id
                        && p.Status == EBandPartnershipStatus.Declined && p.RespondedAt > cooldownStart,
                }) > 0)
                return Fail(new Error("BAND_PARTNERSHIP_COOLDOWN", $"This page declined recently; wait {BandRules.PartnershipCooldown.Days} days before inviting again", ErrorType.Conflict));

            if (await _partnershipRepository.CountAsync(new QueryOptions<BandPartnership> { Predicate = PendingSent(requester, now) }) >= BandRules.MaxPendingPartnershipsPerBand)
                return Fail(new Error("BAND_PARTNERSHIP_LIMIT_REACHED", $"A page can have at most {BandRules.MaxPendingPartnershipsPerBand} pending partnership invitations", ErrorType.Conflict));

            if (await CountPartnersAsync(requester) >= BandRules.MaxPartnersPerBand)
                return Fail(PartnerLimit());

            var me = _currentUser.UserId;
            var partnership = new BandPartnership
            {
                RequesterBandId = requester,
                TargetBandId = target.Id,
                LowBandId = low,
                HighBandId = high,
                InvitedByUserId = me,
                Status = EBandPartnershipStatus.Pending,
                ExpiresAt = now.Add(BandRules.PartnershipLifetime),
            };

            await _partnershipRepository.AddAsync(partnership);

            _logger.Info("BAND_PARTNERSHIP_SENT", "Band partnership invitation sent", userId: me, extra: new { PartnershipId = partnership.Id, RequesterBandId = requester, TargetBandId = target.Id });

            await _notificationAppService.NotifyMany(
                await _bandAppService.MemberUserIds(target.Id, EBandMemberRole.Owner),
                new NotificationInput(ENotificationKind.BandPartnershipInvited, me, BandId: requester, BandPartnershipId: partnership.Id));

            return Ok(await MapOneAsync(partnership.Id, requester), "BAND_PARTNERSHIP_SENT", "Partnership invitation sent");
        }

        public async Task<Result<PagedResultDto<BandPartnershipDto>>> ListAccepted(string slug, PagedRequestDto query)
        {
            query.Normalize();

            var (bandId, _) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Result<PagedResultDto<BandPartnershipDto>>.Fail(BandNotFound());

            var id = bandId.Value;

            return await PageAsync(id, p => p.Status == EBandPartnershipStatus.Accepted
                && ((p.RequesterBandId == id && !p.TargetBand!.IsDeleted) || (p.TargetBandId == id && !p.RequesterBand!.IsDeleted)),
                q => q.OrderByDescending(p => p.RespondedAt).ThenByDescending(p => p.Id),
                query);
        }

        public async Task<Result<PagedResultDto<BandPartnershipDto>>> ListPending(string slug, PagedRequestDto query)
        {
            query.Normalize();

            var bandId = await OwnedBandIdAsync(slug);

            if (bandId.Error != null)
                return Result<PagedResultDto<BandPartnershipDto>>.Fail(bandId.Error);

            var id = bandId.Value;
            var now = DateTime.UtcNow;
            var cooldownStart = now - BandRules.PartnershipCooldown;

            return await PageAsync(id, p =>
                    (p.Status == EBandPartnershipStatus.Pending && p.ExpiresAt > now && (p.RequesterBandId == id || p.TargetBandId == id))
                    || (p.Status == EBandPartnershipStatus.Declined && p.RequesterBandId == id && p.RespondedAt > cooldownStart),
                q => q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
                query);
        }

        public async Task<Result<BandPartnershipDto>> Accept(int partnershipId)
        {
            _logger.SetOperation("AcceptBandPartnership");

            var me = _currentUser.UserId;
            var partnership = await FindForTargetOwnerAsync(partnershipId, me);

            if (partnership == null)
                return Fail(PartnershipNotFound());

            if (await CountPartnersAsync(partnership.TargetBandId) >= BandRules.MaxPartnersPerBand
                || await CountPartnersAsync(partnership.RequesterBandId) >= BandRules.MaxPartnersPerBand)
                return Fail(PartnerLimit());

            if (!await _partnershipRepository.TryRespondAsync(partnershipId, me, EBandPartnershipStatus.Accepted, DateTime.UtcNow))
                return Fail(NotPending());

            _logger.Info("BAND_PARTNERSHIP_ACCEPTED", "Band partnership accepted", userId: me, extra: new { PartnershipId = partnershipId, partnership.RequesterBandId, partnership.TargetBandId });

            await MarkReadForOwnersAsync(partnership.TargetBandId, partnershipId);
            await _notificationAppService.NotifyMany(
                await _bandAppService.MemberUserIds(partnership.RequesterBandId, EBandMemberRole.Owner),
                new NotificationInput(ENotificationKind.BandPartnershipAccepted, me, BandId: partnership.TargetBandId, BandPartnershipId: partnershipId));

            return Ok(await MapOneAsync(partnershipId, partnership.TargetBandId), "BAND_PARTNERSHIP_ACCEPTED", "You now play together");
        }

        public async Task<Result<BandPartnershipDto>> Decline(int partnershipId)
        {
            _logger.SetOperation("DeclineBandPartnership");

            var me = _currentUser.UserId;
            var partnership = await FindForTargetOwnerAsync(partnershipId, me);

            if (partnership == null)
                return Fail(PartnershipNotFound());

            if (!await _partnershipRepository.TryRespondAsync(partnershipId, me, EBandPartnershipStatus.Declined, DateTime.UtcNow))
                return Fail(NotPending());

            _logger.Info("BAND_PARTNERSHIP_DECLINED", "Band partnership declined", userId: me, extra: new { PartnershipId = partnershipId });

            await MarkReadForOwnersAsync(partnership.TargetBandId, partnershipId);

            return Ok(await MapOneAsync(partnershipId, partnership.TargetBandId), "BAND_PARTNERSHIP_DECLINED", "Partnership invitation declined");
        }

        public async Task<Result> Remove(int partnershipId)
        {
            _logger.SetOperation("RemoveBandPartnership");

            var me = _currentUser.UserId;

            var partnership = await _partnershipRepository.FirstOrDefaultAsync(new QueryOptions<BandPartnership>
            {
                Predicate = p => p.Id == partnershipId
                    && (p.RequesterBand!.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner)
                        || p.TargetBand!.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner)),
                AsNoTracking = true,
            });

            if (partnership == null)
                return Result.Fail(PartnershipNotFound());

            if (!await _partnershipRepository.TryEndAsync(partnershipId, me, DateTime.UtcNow))
                return Result.Fail(new Error("BAND_PARTNERSHIP_NOT_REMOVABLE", "Only the inviting page cancels a pending invitation; an accepted partnership can be undone by either page", ErrorType.Conflict));

            _logger.Info("BAND_PARTNERSHIP_REMOVED", "Band partnership removed", userId: me, extra: new { PartnershipId = partnershipId, From = partnership.Status });

            return Result.Ok("BAND_PARTNERSHIP_REMOVED", "Partnership removed");
        }

        private async Task MarkReadForOwnersAsync(int bandId, int partnershipId)
        {
            foreach (var owner in await _bandAppService.MemberUserIds(bandId, EBandMemberRole.Owner))
                await _notificationAppService.MarkPartnershipRead(owner, partnershipId);
        }

        private Task<BandPartnership?> FindForTargetOwnerAsync(int partnershipId, int me) =>
            _partnershipRepository.FirstOrDefaultAsync(new QueryOptions<BandPartnership>
            {
                Predicate = p => p.Id == partnershipId
                    && p.TargetBand!.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner),
                AsNoTracking = true,
            });

        private Task<int> CountPartnersAsync(int bandId) =>
            _partnershipRepository.CountAsync(new QueryOptions<BandPartnership>
            {
                Predicate = p => p.Status == EBandPartnershipStatus.Accepted && (p.RequesterBandId == bandId || p.TargetBandId == bandId),
            });

        private static Expression<Func<BandPartnership, bool>> PendingSent(int bandId, DateTime now) =>
            p => p.RequesterBandId == bandId && p.Status == EBandPartnershipStatus.Pending && p.ExpiresAt > now;

        private static (int Low, int High) Pair(int a, int b) => a < b ? (a, b) : (b, a);

        private async Task<(int Value, Error? Error)> OwnedBandIdAsync(string slug)
        {
            var (bandId, role) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return (0, BandNotFound());

            if (role != EBandMemberRole.Owner)
                return (0, new Error("BAND_FORBIDDEN", "Only band owners can do this", ErrorType.Forbidden));

            return (bandId.Value, null);
        }

        private async Task<Result<PagedResultDto<BandPartnershipDto>>> PageAsync(
            int bandId,
            Expression<Func<BandPartnership, bool>> predicate,
            Func<IQueryable<BandPartnership>, IOrderedQueryable<BandPartnership>> orderBy,
            PagedRequestDto query)
        {
            var options = new QueryOptions<BandPartnership>
            {
                Predicate = predicate,
                OrderBy = orderBy,
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _partnershipRepository.CountAsync(options);
            var partnerships = await _partnershipRepository.ListAsync(options);
            var items = await MapAsync(partnerships, bandId);

            return Result<PagedResultDto<BandPartnershipDto>>.Ok(
                PagedResultDto<BandPartnershipDto>.Create(items, query.Page, query.PageSize, totalCount),
                "BAND_PARTNERSHIPS_FOUND",
                "Partnerships found");
        }

        private async Task<BandPartnershipDto> MapOneAsync(int partnershipId, int viewpointBandId)
        {
            var partnership = await _partnershipRepository.FirstOrDefaultAsync(new QueryOptions<BandPartnership>
            {
                Predicate = p => p.Id == partnershipId,
                AsNoTracking = true,
            });

            return (await MapAsync([partnership!], viewpointBandId))[0];
        }

        private async Task<List<BandPartnershipDto>> MapAsync(List<BandPartnership> partnerships, int viewpointBandId)
        {
            var partnerIds = partnerships.Select(p => PartnerOf(p, viewpointBandId)).Distinct().ToList();
            var bands = await _bandAppService.SummariesByIds(partnerIds);

            return partnerships.Select(p =>
            {
                var partnerId = PartnerOf(p, viewpointBandId);

                return new BandPartnershipDto
                {
                    Id = p.Id,
                    Partner = bands.GetValueOrDefault(partnerId) ?? new BandSummaryDto { Id = partnerId },
                    Direction = p.RequesterBandId == viewpointBandId ? EBandPartnershipDirection.Outgoing : EBandPartnershipDirection.Incoming,
                    Status = p.Status,
                    ExpiresAt = p.ExpiresAt,
                    CreatedAt = p.CreatedAt,
                    RespondedAt = p.RespondedAt,
                };
            }).ToList();
        }

        private static int PartnerOf(BandPartnership partnership, int viewpointBandId) =>
            partnership.RequesterBandId == viewpointBandId ? partnership.TargetBandId : partnership.RequesterBandId;

        private static Result<BandPartnershipDto> Ok(BandPartnershipDto dto, string code, string message) =>
            Result<BandPartnershipDto>.Ok(dto, code, message);

        private static Result<BandPartnershipDto> Fail(Error error) => Result<BandPartnershipDto>.Fail(error);

        private static Error BandNotFound() => new("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound);

        private static Error PartnershipNotFound() => new("BAND_PARTNERSHIP_NOT_FOUND", "Partnership not found", ErrorType.NotFound);

        private static Error PartnerLimit() =>
            new("BAND_PARTNER_LIMIT_REACHED", $"A page can play with at most {BandRules.MaxPartnersPerBand} other pages", ErrorType.Conflict);

        private static Error NotPending() =>
            new("BAND_PARTNERSHIP_NOT_PENDING", "This invitation was already answered, cancelled or has expired", ErrorType.Conflict);
    }
}
