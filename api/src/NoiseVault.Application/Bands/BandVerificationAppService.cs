using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Application.Abstractions.Jobs;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Notifications;

namespace NoiseVault.Application.Bands
{
    public class BandVerificationAppService : IBandVerificationAppService
    {
        private readonly IBandVerificationRepository _requestRepository;
        private readonly IRepository<Band> _bandRepository;
        private readonly IRepository<BandMember> _memberRepository;
        private readonly IBandAppService _bandAppService;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionChecker _permissionChecker;
        private readonly INotificationAppService _notificationAppService;
        private readonly IEmailService _emailService;
        private readonly IEnqueuedJobDispatcher _jobDispatcher;
        private readonly IFileStorage _fileStorage;
        private readonly IApplicationLogger _logger;

        public BandVerificationAppService(
            IBandVerificationRepository requestRepository,
            IRepository<Band> bandRepository,
            IRepository<BandMember> memberRepository,
            IBandAppService bandAppService,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork,
            IPermissionChecker permissionChecker,
            INotificationAppService notificationAppService,
            IEmailService emailService,
            IEnqueuedJobDispatcher jobDispatcher,
            IFileStorage fileStorage,
            IApplicationLoggerFactory loggerFactory)
        {
            _requestRepository = requestRepository;
            _bandRepository = bandRepository;
            _memberRepository = memberRepository;
            _bandAppService = bandAppService;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _permissionChecker = permissionChecker;
            _notificationAppService = notificationAppService;
            _emailService = emailService;
            _jobDispatcher = jobDispatcher;
            _fileStorage = fileStorage;
            _logger = loggerFactory.Create(LogContext.Bands);
        }

        public async Task<Result<BandVerificationDto>> Request(string slug, BandVerificationRequestInputDto input)
        {
            _logger.SetOperation("RequestBandVerification");

            var message = ProfileRules.NormalizeOptionalText(input.Message) ?? string.Empty;

            if (message.Length is < BandVerificationRules.MessageMinLength or > BandVerificationRules.MessageMaxLength)
                return Fail(new Error("INVALID_VERIFICATION_MESSAGE", $"Tell us who you are in the band and how we can confirm it, with {BandVerificationRules.MessageMinLength} to {BandVerificationRules.MessageMaxLength} characters", ErrorType.Validation));

            var phone = ProfileRules.NormalizeOptionalText(input.ContactPhone);

            if (phone is { Length: > BandVerificationRules.ContactPhoneMaxLength })
                return Fail(new Error("INVALID_VERIFICATION_PHONE", $"Contact phone must have at most {BandVerificationRules.ContactPhoneMaxLength} characters", ErrorType.Validation));

            var (bandId, role) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Fail(BandNotFound());

            if (role != EBandMemberRole.Owner)
                return Fail(new Error("BAND_FORBIDDEN", "Only band owners can do this", ErrorType.Forbidden));

            var band = (await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band> { Predicate = b => b.Id == bandId, AsNoTracking = true }))!;

            if (band.IsVerified)
                return Fail(new Error("BAND_ALREADY_VERIFIED", "This band is already verified", ErrorType.Conflict));

            var memberCount = await _memberRepository.CountAsync(new QueryOptions<BandMember> { Predicate = m => m.BandId == bandId && m.User!.IsActive });
            var missing = MissingRequirements(band, memberCount);

            if (missing.Count > 0)
                return Fail(new Error("BAND_VERIFICATION_REQUIREMENTS", $"Before asking for verification the band needs: {string.Join(", ", missing)}", ErrorType.Validation));

            if (await _requestRepository.CountAsync(new QueryOptions<BandVerificationRequest> { Predicate = r => r.BandId == bandId && r.Status == EBandVerificationStatus.Pending }) > 0)
                return Fail(new Error("BAND_VERIFICATION_ALREADY_PENDING", "This band already has a verification request waiting for review", ErrorType.Conflict));

            var me = _currentUser.UserId;
            var request = new BandVerificationRequest
            {
                BandId = bandId.Value,
                RequestedByUserId = me,
                Message = message,
                ContactPhone = phone,
                Status = EBandVerificationStatus.Pending,
            };

            await _requestRepository.AddAsync(request);

            _logger.Info("BAND_VERIFICATION_REQUESTED", "Band verification requested", userId: me, extra: new { RequestId = request.Id, BandId = bandId });

            await NotifyReviewersAsync(bandId.Value, me);
            EnqueueRequestedEmail(request.Id);

            return Result<BandVerificationDto>.Ok(await MapAsync((await FindAsync(request.Id))!), "BAND_VERIFICATION_REQUESTED", "Verification requested; the team will review it");
        }

        public async Task<Result<PagedResultDto<BandVerificationDto>>> List(BandVerificationQueryDto query)
        {
            query.Normalize();

            if (query.Status != null && !Enum.IsDefined(query.Status.Value))
                return Result<PagedResultDto<BandVerificationDto>>.Fail(new Error("INVALID_VERIFICATION_STATUS", "Unknown verification status", ErrorType.Validation));

            var (items, totalCount) = await _requestRepository.PageAsync(query.Status, query.Search, query.Skip, query.PageSize);
            var mapped = new List<BandVerificationDto>(items.Count);

            foreach (var item in items)
                mapped.Add(await MapAsync(item));

            return Result<PagedResultDto<BandVerificationDto>>.Ok(
                PagedResultDto<BandVerificationDto>.Create(mapped, query.Page, query.PageSize, totalCount),
                "BAND_VERIFICATIONS_FOUND",
                "Verification requests found");
        }

        public async Task<Result<BandVerificationSummaryDto>> Summary()
        {
            var counts = await _requestRepository.CountByStatusAsync();

            return Result<BandVerificationSummaryDto>.Ok(new BandVerificationSummaryDto
            {
                Pending = counts.GetValueOrDefault(EBandVerificationStatus.Pending),
                Approved = counts.GetValueOrDefault(EBandVerificationStatus.Approved),
                Rejected = counts.GetValueOrDefault(EBandVerificationStatus.Rejected),
            }, "BAND_VERIFICATION_SUMMARY_FOUND", "Verification summary found");
        }

        public async Task<Result<BandVerificationDto>> Approve(int id)
        {
            _logger.SetOperation("ApproveBandVerification");

            var request = await FindAsync(id);

            if (request == null)
                return Fail(NotFound());

            var band = request.Band!;
            var normalized = band.Name.ToLowerInvariant();
            var homonym = await _bandRepository.CountAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Id != band.Id && b.IsVerified && b.Name.ToLower() == normalized,
            });

            if (homonym > 0)
                return Fail(new Error("BAND_NAME_ALREADY_VERIFIED", "Another verified band already uses this name", ErrorType.Conflict));

            var me = _currentUser.UserId;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (!await _requestRepository.TryReviewAsync(id, EBandVerificationStatus.Approved, me, null, DateTime.UtcNow))
                {
                    await _unitOfWork.RollbackAsync();
                    return Fail(NotPending());
                }

                var tracked = (await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band> { Predicate = b => b.Id == band.Id }))!;
                tracked.IsVerified = true;
                await _bandRepository.UpdateAsync(tracked);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("BAND_VERIFICATION_APPROVED", "Band verification approved", userId: me, extra: new { RequestId = id, BandId = band.Id });

            await NotifyOwnersAsync(band.Id, ENotificationKind.BandVerificationApproved, me);

            return Result<BandVerificationDto>.Ok(await MapAsync((await FindAsync(id))!), "BAND_VERIFICATION_APPROVED", "Band verified");
        }

        public async Task<Result<BandVerificationDto>> Reject(int id, BandVerificationRejectInputDto input)
        {
            _logger.SetOperation("RejectBandVerification");

            var reason = ProfileRules.NormalizeOptionalText(input.Reason) ?? string.Empty;

            if (reason.Length is < BandVerificationRules.RejectionReasonMinLength or > BandVerificationRules.RejectionReasonMaxLength)
                return Fail(new Error("INVALID_VERIFICATION_REJECTION_REASON", $"Reason must have between {BandVerificationRules.RejectionReasonMinLength} and {BandVerificationRules.RejectionReasonMaxLength} characters", ErrorType.Validation));

            var request = await FindAsync(id);

            if (request == null)
                return Fail(NotFound());

            var me = _currentUser.UserId;

            if (!await _requestRepository.TryReviewAsync(id, EBandVerificationStatus.Rejected, me, reason, DateTime.UtcNow))
                return Fail(NotPending());

            _logger.Info("BAND_VERIFICATION_REJECTED", "Band verification rejected", userId: me, extra: new { RequestId = id, request.BandId });

            await NotifyOwnersAsync(request.BandId, ENotificationKind.BandVerificationRejected, me);

            return Result<BandVerificationDto>.Ok(await MapAsync((await FindAsync(id))!), "BAND_VERIFICATION_REJECTED", "Verification request rejected");
        }

        public async Task<Result<BandProfileDto>> Unverify(int bandId)
        {
            _logger.SetOperation("UnverifyBand");

            var band = await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band> { Predicate = b => b.Id == bandId });

            if (band == null)
                return Result<BandProfileDto>.Fail(BandNotFound());

            if (!band.IsVerified)
                return Result<BandProfileDto>.Fail(new Error("BAND_NOT_VERIFIED", "This band is not verified", ErrorType.Conflict));

            band.IsVerified = false;
            await _bandRepository.UpdateAsync(band);

            var me = _currentUser.UserId;

            _logger.Info("BAND_VERIFICATION_REVOKED", "Band verification revoked", userId: me, extra: new { BandId = bandId });

            await NotifyOwnersAsync(bandId, ENotificationKind.BandVerificationRevoked, me);

            return await _bandAppService.GetBySlug(band.Slug) is { IsSuccess: true } profile
                ? Result<BandProfileDto>.Ok(profile.Data!, "BAND_VERIFICATION_REVOKED", "Verification removed")
                : Result<BandProfileDto>.Fail(BandNotFound());
        }

        public async Task<BandVerificationStateDto?> StateForBand(int bandId)
        {
            var latest = await _requestRepository.FirstOrDefaultAsync(new QueryOptions<BandVerificationRequest>
            {
                Predicate = r => r.BandId == bandId,
                OrderBy = q => q.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id),
                AsNoTracking = true,
            });

            return latest == null
                ? null
                : new BandVerificationStateDto
                {
                    Status = latest.Status,
                    RequestedAt = latest.CreatedAt,
                    ReviewedAt = latest.ReviewedAt,
                    RejectionReason = latest.Status == EBandVerificationStatus.Rejected ? latest.RejectionReason : null,
                };
        }

        public async Task SendRequestedEmail(int requestId)
        {
            _logger.SetOperation("SendBandVerificationEmail");

            var request = await FindAsync(requestId);

            if (request == null || request.Status != EBandVerificationStatus.Pending)
                return;

            var sent = await _emailService.SendBandVerificationRequestedAsync(new BandVerificationEmailDto
            {
                BandName = request.Band!.Name,
                BandSlug = request.Band.Slug,
                RequestedByName = request.RequestedBy!.Name,
                RequestedByNickname = request.RequestedBy.Nickname,
                Message = request.Message,
                ContactPhone = request.ContactPhone,
            });

            if (!sent)
            {
                _logger.Warning("BAND_VERIFICATION_EMAIL_FAILED", "Band verification email was not accepted by the transport", extra: new { RequestId = requestId });
                throw new InvalidOperationException($"E-mail do pedido de verificação {requestId} não foi aceito pelo transporte.");
            }

            _logger.Info("BAND_VERIFICATION_EMAIL_SENT", "Band verification email sent", extra: new { RequestId = requestId });
        }

        private static List<string> MissingRequirements(Band band, int memberCount)
        {
            var missing = new List<string>();

            if (band.AvatarKey == null)
                missing.Add("avatar");

            if (memberCount < BandVerificationRules.MinMembers)
                missing.Add($"at least {BandVerificationRules.MinMembers} members");

            if (band.InstagramUrl == null && band.YoutubeUrl == null && band.SpotifyUrl == null)
                missing.Add("a social link");

            return missing;
        }

        private async Task NotifyReviewersAsync(int bandId, int actorUserId)
        {
            List<int> reviewers;

            try
            {
                reviewers = await _permissionChecker.UsersGrantedAsync(Permissions.Bands.VerificationsView);
            }
            catch (Exception ex)
            {
                _logger.Error("NOTIFICATION_FAILED", "Band verification reviewers could not be resolved", exception: ex, userId: actorUserId, extra: new { BandId = bandId });
                return;
            }

            await _notificationAppService.NotifyMany(reviewers.Where(id => id != actorUserId).ToList(), new NotificationInput(ENotificationKind.BandVerificationRequested, actorUserId, BandId: bandId));
        }

        private async Task NotifyOwnersAsync(int bandId, ENotificationKind kind, int actorUserId)
        {
            var owners = await _bandAppService.MemberUserIds(bandId, EBandMemberRole.Owner);
            await _notificationAppService.NotifyMany(owners, new NotificationInput(kind, actorUserId, BandId: bandId));
        }

        private void EnqueueRequestedEmail(int requestId)
        {
            try
            {
                _jobDispatcher.EnqueueBandVerificationEmail(requestId);
            }
            catch (Exception ex)
            {
                _logger.Error("BAND_VERIFICATION_EMAIL_ENQUEUE_FAILED", "Band verification email could not be enqueued", exception: ex, extra: new { RequestId = requestId });
            }
        }

        private Task<BandVerificationRequest?> FindAsync(int id) =>
            _requestRepository.FirstOrDefaultAsync(new QueryOptions<BandVerificationRequest>
            {
                Predicate = r => r.Id == id,
                Includes = [r => r.Band!, r => r.Band!.Genres, r => r.RequestedBy!, r => r.ReviewedBy!],
                AsNoTracking = true,
            });

        private async Task<BandVerificationDto> MapAsync(BandVerificationRequest request)
        {
            var band = request.Band!;

            return new BandVerificationDto
            {
                Id = request.Id,
                Band = new BandSummaryDto
                {
                    Id = band.Id,
                    Name = band.Name,
                    Slug = band.Slug,
                    AvatarUrl = _fileStorage.PublicUrl(band.AvatarKey),
                    IsVerified = band.IsVerified,
                    Genres = band.Genres.Select(g => g.Genre).OrderBy(g => g).ToList(),
                },
                BandSocialLinks = new SocialLinksDto
                {
                    Instagram = band.InstagramUrl,
                    Youtube = band.YoutubeUrl,
                    Spotify = band.SpotifyUrl,
                },
                BandMemberCount = await _memberRepository.CountAsync(new QueryOptions<BandMember> { Predicate = m => m.BandId == band.Id && m.User!.IsActive }),
                RequestedBy = Summary(request.RequestedBy!),
                Message = request.Message,
                ContactPhone = request.ContactPhone,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                ReviewedBy = request.ReviewedBy == null ? null : Summary(request.ReviewedBy),
                ReviewedAt = request.ReviewedAt,
                RejectionReason = request.RejectionReason,
            };
        }

        private ProfileSummaryDto Summary(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Nickname = user.Nickname,
            AvatarUrl = _fileStorage.PublicUrl(user.AvatarKey),
        };

        private static Result<BandVerificationDto> Fail(Error error) => Result<BandVerificationDto>.Fail(error);

        private static Error BandNotFound() => new("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound);

        private static Error NotFound() => new("BAND_VERIFICATION_NOT_FOUND", "Verification request not found", ErrorType.NotFound);

        private static Error NotPending() => new("BAND_VERIFICATION_NOT_PENDING", "This request was already reviewed", ErrorType.Conflict);
    }
}
