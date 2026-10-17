using System.Linq.Expressions;
using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Jobs;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Auth;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Interfaces.Authenticate;

namespace NoiseVault.Application.Bands
{
    public class BandInvitationAppService : IBandInvitationAppService
    {
        private readonly IBandInvitationRepository _invitationRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IBandAppService _bandAppService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly INotificationAppService _notificationAppService;
        private readonly IEnqueuedJobDispatcher _jobDispatcher;
        private readonly IEmailService _emailService;
        private readonly IAuthenticateService _authService;
        private readonly IApplicationLogger _logger;

        public BandInvitationAppService(
            IBandInvitationRepository invitationRepository,
            IRepository<User> userRepository,
            IBandAppService bandAppService,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            INotificationAppService notificationAppService,
            IEnqueuedJobDispatcher jobDispatcher,
            IEmailService emailService,
            IAuthenticateService authService,
            IApplicationLoggerFactory loggerFactory)
        {
            _invitationRepository = invitationRepository;
            _userRepository = userRepository;
            _bandAppService = bandAppService;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _notificationAppService = notificationAppService;
            _jobDispatcher = jobDispatcher;
            _emailService = emailService;
            _authService = authService;
            _logger = loggerFactory.Create(LogContext.Bands);
        }

        public async Task<Result<BandInvitationDto>> Invite(string slug, InviteMemberInputDto input)
        {
            _logger.SetOperation("InviteBandMember");

            var instrument = ProfileRules.NormalizeOptionalText(input.Instrument);

            if (instrument is { Length: > ProfileRules.InstrumentMaxLength })
                return Fail(Validation("INVALID_INSTRUMENT", $"Instrument must have at most {ProfileRules.InstrumentMaxLength} characters"));

            var bandId = await OwnedBandIdAsync(slug);

            if (bandId.Error != null)
                return Fail(bandId.Error);

            var nickname = NicknameRules.Normalize(input.Nickname ?? string.Empty);

            var invitee = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Nickname == nickname && u.IsActive,
                AsNoTracking = true,
            });

            if (invitee == null)
                return Fail(new Error("INVITEE_NOT_FOUND", "No active person with this nickname", ErrorType.NotFound));

            var me = _currentUser.UserId;

            if (invitee.Id == me)
                return Fail(Validation("CANNOT_INVITE_SELF", "You are already in the band"));

            if (await _bandAppService.IsMember(bandId.Value, invitee.Id))
                return Fail(new Error("ALREADY_BAND_MEMBER", "This person is already a member of the band", ErrorType.Conflict));

            var now = DateTime.UtcNow;
            await _invitationRepository.RetireExpiredAsync(bandId.Value, invitee.Id, now);

            if (await CountPendingAsync(bandId.Value, invitee.Id, now) > 0)
                return Fail(new Error("BAND_INVITATION_ALREADY_PENDING", "This person already has a pending invitation", ErrorType.Conflict));

            if (await CountPendingAsync(bandId.Value, null, now) >= BandRules.MaxPendingInvitationsPerBand)
                return Fail(new Error("BAND_INVITATION_LIMIT_REACHED", $"A band can have at most {BandRules.MaxPendingInvitationsPerBand} pending invitations", ErrorType.Conflict));

            var invitation = new BandInvitation
            {
                BandId = bandId.Value,
                InvitedUserId = invitee.Id,
                InvitedByUserId = me,
                Instrument = instrument,
                Status = EBandInvitationStatus.Pending,
                ExpiresAt = now.Add(BandRules.InvitationLifetime),
            };

            await _invitationRepository.AddAsync(invitation);

            _logger.Info("BAND_INVITATION_SENT", "Band invitation sent", userId: me, extra: new { InvitationId = invitation.Id, BandId = bandId.Value, InvitedUserId = invitee.Id });

            await _notificationAppService.Notify(invitee.Id, new NotificationInput(ENotificationKind.BandInvite, me, BandId: bandId.Value, BandInvitationId: invitation.Id));

            if (invitee.EmailOnBandInvite)
                EnqueueInviteEmail(invitation.Id);

            return Ok(await MapOneAsync(invitation.Id), "BAND_INVITATION_SENT", "Invitation sent");
        }

        public async Task<Result<PagedResultDto<BandInvitationDto>>> ListForBand(string slug, PagedRequestDto query)
        {
            query.Normalize();

            var bandId = await OwnedBandIdAsync(slug);

            if (bandId.Error != null)
                return Result<PagedResultDto<BandInvitationDto>>.Fail(bandId.Error);

            return await PageAsync(bandId.Value, null, query);
        }

        public async Task<Result<PagedResultDto<BandInvitationDto>>> ListMine(PagedRequestDto query)
        {
            query.Normalize();

            var me = _currentUser.UserId;

            return await PageAsync(null, me, query);
        }

        public async Task<Result<BandInvitationDto>> Accept(int invitationId)
        {
            _logger.SetOperation("AcceptBandInvitation");

            var me = _currentUser.UserId;

            var invitation = await _invitationRepository.FirstOrDefaultAsync(new QueryOptions<BandInvitation>
            {
                Predicate = i => i.Id == invitationId && i.InvitedUserId == me,
                AsNoTracking = true,
            });

            if (invitation == null)
                return Fail(InvitationNotFound());

            if (await _bandAppService.IsMember(invitation.BandId, me))
                return Fail(new Error("ALREADY_BAND_MEMBER", "You are already a member of this band", ErrorType.Conflict));

            if (await _bandAppService.CountMemberships(me) >= BandRules.MaxBandsPerUser)
                return Fail(new Error("BAND_LIMIT_REACHED", $"You can be part of at most {BandRules.MaxBandsPerUser} bands", ErrorType.Conflict));

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (!await _invitationRepository.TryRespondAsync(invitationId, me, EBandInvitationStatus.Accepted, DateTime.UtcNow))
                {
                    await _unitOfWork.RollbackAsync();
                    return Fail(NotPending());
                }

                await _bandAppService.AddMember(invitation.BandId, me, invitation.Instrument);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("BAND_MEMBER_JOINED", "Band invitation accepted", userId: me, extra: new { InvitationId = invitationId, invitation.BandId });

            await _notificationAppService.MarkInvitationRead(me, invitationId);
            await _notificationAppService.Notify(invitation.InvitedByUserId, new NotificationInput(ENotificationKind.BandInviteAccepted, me, BandId: invitation.BandId, BandInvitationId: invitationId));

            return Ok(await MapOneAsync(invitationId), "BAND_INVITATION_ACCEPTED", "Welcome to the band");
        }

        public async Task<Result<BandInvitationDto>> Decline(int invitationId)
        {
            _logger.SetOperation("DeclineBandInvitation");

            var me = _currentUser.UserId;

            if (!await _invitationRepository.TryRespondAsync(invitationId, me, EBandInvitationStatus.Declined, DateTime.UtcNow))
                return Fail(await _invitationRepository.CountAsync(new QueryOptions<BandInvitation>
                {
                    Predicate = i => i.Id == invitationId && i.InvitedUserId == me,
                }) > 0 ? NotPending() : InvitationNotFound());

            _logger.Info("BAND_INVITATION_DECLINED", "Band invitation declined", userId: me, extra: new { InvitationId = invitationId });

            var declined = await _invitationRepository.FirstOrDefaultAsync(new QueryOptions<BandInvitation>
            {
                Predicate = i => i.Id == invitationId,
                AsNoTracking = true,
            });

            await _notificationAppService.MarkInvitationRead(me, invitationId);
            await _notificationAppService.Notify(declined!.InvitedByUserId, new NotificationInput(ENotificationKind.BandInviteDeclined, me, BandId: declined.BandId, BandInvitationId: invitationId));

            return Ok(await MapOneAsync(invitationId), "BAND_INVITATION_DECLINED", "Invitation declined");
        }

        public async Task<Result<BandInvitationDto>> Revoke(int invitationId)
        {
            _logger.SetOperation("RevokeBandInvitation");

            var invitation = await _invitationRepository.FirstOrDefaultAsync(new QueryOptions<BandInvitation>
            {
                Predicate = i => i.Id == invitationId,
                Includes = [i => i.Band!],
                AsNoTracking = true,
            });

            if (invitation?.Band == null)
                return Fail(InvitationNotFound());

            var (_, role) = await _bandAppService.FindAccess(invitation.Band.Slug);

            if (role != EBandMemberRole.Owner)
                return Fail(new Error("BAND_FORBIDDEN", "Only band owners can do this", ErrorType.Forbidden));

            if (!await _invitationRepository.TryRevokeAsync(invitationId, invitation.BandId, DateTime.UtcNow))
                return Fail(NotPending());

            _logger.Info("BAND_INVITATION_REVOKED", "Band invitation revoked", userId: _currentUser.UserId, extra: new { InvitationId = invitationId, invitation.BandId });

            await _notificationAppService.MarkInvitationRead(invitation.InvitedUserId, invitationId);

            return Ok(await MapOneAsync(invitationId), "BAND_INVITATION_REVOKED", "Invitation revoked");
        }

        public async Task SendInviteEmail(int invitationId)
        {
            _logger.SetOperation("SendBandInviteEmail");

            var invitation = await _invitationRepository.FirstOrDefaultAsync(new QueryOptions<BandInvitation>
            {
                Predicate = i => i.Id == invitationId,
                Includes = [i => i.Band!, i => i.InvitedUser!, i => i.InvitedByUser!],
                AsNoTracking = true,
            });

            if (invitation == null
                || invitation.Status != EBandInvitationStatus.Pending
                || invitation.ExpiresAt <= DateTime.UtcNow
                || invitation.InvitedUser is not { IsActive: true, EmailOnBandInvite: true }
                || !await _authService.IsEmailConfirmed(invitation.InvitedUser.Email))
                return;

            var sent = await _emailService.SendBandInviteAsync(new BandInviteEmailDto
            {
                To = invitation.InvitedUser.Email,
                Name = invitation.InvitedUser.Name,
                InvitedByName = invitation.InvitedByUser!.Name,
                BandName = invitation.Band!.Name,
                Instrument = invitation.Instrument,
                ExpiresAt = invitation.ExpiresAt,
            });

            if (!sent)
            {
                _logger.Warning("BAND_INVITE_EMAIL_FAILED", "Band invite email was not accepted by the transport", extra: new { InvitationId = invitationId });
                throw new InvalidOperationException($"E-mail do convite {invitationId} não foi aceito pelo transporte.");
            }

            _logger.Info("BAND_INVITE_EMAIL_SENT", "Band invite email sent", userId: invitation.InvitedUserId, extra: new { InvitationId = invitationId });
        }

        private void EnqueueInviteEmail(int invitationId)
        {
            try
            {
                _jobDispatcher.EnqueueBandInviteEmail(invitationId);
            }
            catch (Exception ex)
            {
                _logger.Error("BAND_INVITE_EMAIL_ENQUEUE_FAILED", "Band invite email could not be enqueued", exception: ex, extra: new { InvitationId = invitationId });
            }
        }

        private async Task<(int Value, Error? Error)> OwnedBandIdAsync(string slug)
        {
            var (bandId, role) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return (0, new Error("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound));

            if (role != EBandMemberRole.Owner)
                return (0, new Error("BAND_FORBIDDEN", "Only band owners can do this", ErrorType.Forbidden));

            return (bandId.Value, null);
        }

        private async Task<Result<PagedResultDto<BandInvitationDto>>> PageAsync(int? bandId, int? invitedUserId, PagedRequestDto query)
        {
            var now = DateTime.UtcNow;

            var options = new QueryOptions<BandInvitation>
            {
                Predicate = PendingPredicate(bandId, invitedUserId, now),
                Includes = [i => i.InvitedUser!, i => i.InvitedByUser!],
                OrderBy = q => q.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _invitationRepository.CountAsync(options);
            var invitations = await _invitationRepository.ListAsync(options);
            var items = await MapAsync(invitations);

            return Result<PagedResultDto<BandInvitationDto>>.Ok(
                PagedResultDto<BandInvitationDto>.Create(items, query.Page, query.PageSize, totalCount),
                "BAND_INVITATIONS_FOUND",
                "Invitations found");
        }

        private Task<int> CountPendingAsync(int bandId, int? invitedUserId, DateTime now) =>
            _invitationRepository.CountAsync(new QueryOptions<BandInvitation> { Predicate = PendingPredicate(bandId, invitedUserId, now) });

        private static Expression<Func<BandInvitation, bool>> PendingPredicate(int? bandId, int? invitedUserId, DateTime now) =>
            i => (bandId == null || i.BandId == bandId)
                && (invitedUserId == null || i.InvitedUserId == invitedUserId)
                && i.Status == EBandInvitationStatus.Pending
                && i.ExpiresAt > now;

        private async Task<BandInvitationDto> MapOneAsync(int invitationId)
        {
            var invitation = await _invitationRepository.FirstOrDefaultAsync(new QueryOptions<BandInvitation>
            {
                Predicate = i => i.Id == invitationId,
                Includes = [i => i.InvitedUser!, i => i.InvitedByUser!],
                AsNoTracking = true,
            });

            return (await MapAsync([invitation!]))[0];
        }

        private async Task<List<BandInvitationDto>> MapAsync(List<BandInvitation> invitations)
        {
            var bands = await _bandAppService.SummariesByIds(invitations.Select(i => i.BandId).Distinct().ToList());

            return invitations.Select(i => new BandInvitationDto
            {
                Id = i.Id,
                Band = bands.GetValueOrDefault(i.BandId) ?? new BandSummaryDto { Id = i.BandId },
                InvitedUser = Summary(i.InvitedUser!),
                InvitedBy = Summary(i.InvitedByUser!),
                Instrument = i.Instrument,
                Status = i.Status,
                ExpiresAt = i.ExpiresAt,
                CreatedAt = i.CreatedAt,
            }).ToList();
        }

        private ProfileSummaryDto Summary(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Nickname = user.Nickname,
            AvatarUrl = _fileStorage.PublicUrl(user.AvatarKey),
        };

        private static Result<BandInvitationDto> Ok(BandInvitationDto dto, string code, string message) =>
            Result<BandInvitationDto>.Ok(dto, code, message);

        private static Result<BandInvitationDto> Fail(Error error) => Result<BandInvitationDto>.Fail(error);

        private static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

        private static Error InvitationNotFound() => new("BAND_INVITATION_NOT_FOUND", "Invitation not found", ErrorType.NotFound);

        private static Error NotPending() => new("BAND_INVITATION_NOT_PENDING", "This invitation was already answered, revoked or has expired", ErrorType.Conflict);
    }
}
