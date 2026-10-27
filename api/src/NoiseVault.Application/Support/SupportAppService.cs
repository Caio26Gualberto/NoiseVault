using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Authorization;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Media.Dtos;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Application.Support.Dtos;
using NoiseVault.Application.Support.Interfaces;
using NoiseVault.Application.Support.Models;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support
{
    public class SupportAppService : ISupportAppService
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly IRepository<SupportTicketMedia> _ticketMediaRepository;
        private readonly IRepository<SupportTicketMessage> _messageRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IMediaUploadAppService _mediaAppService;
        private readonly INotificationAppService _notificationAppService;
        private readonly IFileStorage _fileStorage;
        private readonly IApplicationLogger _logger;

        public SupportAppService(
            ISupportTicketRepository ticketRepository,
            IRepository<SupportTicketMedia> ticketMediaRepository,
            IRepository<SupportTicketMessage> messageRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IPermissionChecker permissionChecker,
            IMediaUploadAppService mediaAppService,
            INotificationAppService notificationAppService,
            IFileStorage fileStorage,
            IApplicationLoggerFactory loggerFactory)
        {
            _ticketRepository = ticketRepository;
            _ticketMediaRepository = ticketMediaRepository;
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _permissionChecker = permissionChecker;
            _mediaAppService = mediaAppService;
            _notificationAppService = notificationAppService;
            _fileStorage = fileStorage;
            _logger = loggerFactory.Create(LogContext.Support);
        }

        public async Task<Result<SupportTicketDto>> Create(SupportTicketInputDto input)
        {
            _logger.SetOperation("CreateSupportTicket");

            var me = _currentUser.UserId;

            var title = Normalize(input.Title);

            if (title.Length is < SupportRules.TitleMinLength or > SupportRules.TitleMaxLength)
                return Fail<SupportTicketDto>("INVALID_SUPPORT_TITLE", $"Summary must have between {SupportRules.TitleMinLength} and {SupportRules.TitleMaxLength} characters");

            var description = Normalize(input.Description);

            if (description.Length is < SupportRules.DescriptionMinLength or > SupportRules.DescriptionMaxLength)
                return Fail<SupportTicketDto>("INVALID_SUPPORT_DESCRIPTION", $"Description must have between {SupportRules.DescriptionMinLength} and {SupportRules.DescriptionMaxLength} characters");

            var tags = (input.Tags ?? []).Distinct().ToList();

            if (tags.Count == 0)
                return Fail<SupportTicketDto>("SUPPORT_TAG_REQUIRED", "Pick at least one tag");

            if (tags.Any(t => !Enum.IsDefined(t)))
                return Fail<SupportTicketDto>("INVALID_SUPPORT_TAG", "Some tag is not in the supported list");

            var areas = (input.Areas ?? []).Distinct().ToList();

            if (areas.Any(a => !Enum.IsDefined(a)))
                return Fail<SupportTicketDto>("INVALID_SUPPORT_AREA", "Some page is not in the supported list");

            var mediaIds = (input.MediaIds ?? []).ToList();

            if (mediaIds.Count > SupportRules.MaxMedia)
                return Fail<SupportTicketDto>("TOO_MANY_SUPPORT_MEDIA", $"A report can have at most {SupportRules.MaxMedia} photos and videos");

            if (mediaIds.Distinct().Count() != mediaIds.Count)
                return Result<SupportTicketDto>.Fail(new Error("MEDIA_NOT_ATTACHABLE", "The same media appears twice", ErrorType.Validation));

            if (mediaIds.Count > 0)
            {
                var attachable = await _mediaAppService.ListAttachable(mediaIds, me, EMediaProfile.Support);

                if (attachable.Count != mediaIds.Count)
                    return Result<SupportTicketDto>.Fail(new Error("MEDIA_NOT_ATTACHABLE", "Some media is not available anymore; upload it again", ErrorType.Conflict));

                if (attachable.Count(m => m.Kind == EMediaKind.Video) > SupportRules.MaxVideos)
                    return Fail<SupportTicketDto>("TOO_MANY_SUPPORT_VIDEOS", $"A report can have at most {SupportRules.MaxVideos} video");
            }

            if (await _ticketRepository.CountOpenAsync(me) >= SupportRules.MaxOpenTicketsPerUser)
                return Result<SupportTicketDto>.Fail(new Error("SUPPORT_TICKET_LIMIT_REACHED", $"You already have {SupportRules.MaxOpenTicketsPerUser} open reports; wait for an answer", ErrorType.Conflict));

            var now = DateTime.UtcNow;
            var ticket = new SupportTicket
            {
                ReporterUserId = me,
                Title = title,
                Description = description,
                OriginPath = SupportRules.NormalizeOriginPath(input.OriginPath),
                UserAgent = SupportRules.Truncate(input.UserAgent, SupportRules.UserAgentMaxLength),
                ViewportWidth = SupportRules.NormalizeViewport(input.ViewportWidth),
                ViewportHeight = SupportRules.NormalizeViewport(input.ViewportHeight),
                LastActivityAt = now,
                Tags = tags.Select(t => new SupportTicketTag { Tag = t }).ToList(),
                Areas = areas.Select(a => new SupportTicketArea { Area = a }).ToList(),
            };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _ticketRepository.AddAsync(ticket);

                if (mediaIds.Count > 0)
                    await _ticketMediaRepository.AddRangeAsync(mediaIds
                        .Select((mediaId, index) => new SupportTicketMedia { SupportTicketId = ticket.Id, MediaUploadId = mediaId, Order = index })
                        .ToList());

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("SUPPORT_TICKET_CREATED", "Support ticket created", userId: me,
                extra: new { TicketId = ticket.Id, Tags = string.Join(",", tags), Areas = string.Join(",", areas), Media = mediaIds.Count });

            await NotifyStaffAsync(ENotificationKind.SupportTicketCreated, ticket.Id, me);

            return Result<SupportTicketDto>.Ok(await DetailAsync<SupportTicketDto>((await _ticketRepository.FindAsync(ticket.Id, me))!), "SUPPORT_TICKET_CREATED", "Report sent");
        }

        public async Task<Result<PagedResultDto<SupportTicketDto>>> ListMine(PagedRequestDto query)
        {
            query.Normalize();

            var (rows, totalCount) = await _ticketRepository.PageAsync(new SupportTicketFilter(ReporterUserId: _currentUser.UserId), query.Skip, query.PageSize);

            return Result<PagedResultDto<SupportTicketDto>>.Ok(
                PagedResultDto<SupportTicketDto>.Create(rows.Select(r => Fill(new SupportTicketDto(), r)).ToList(), query.Page, query.PageSize, totalCount),
                "SUPPORT_TICKETS_FOUND",
                "Support tickets found");
        }

        public async Task<Result<SupportTicketDto>> GetMine(int id)
        {
            var row = await _ticketRepository.FindAsync(id, _currentUser.UserId);

            return row == null
                ? Result<SupportTicketDto>.Fail(NotFound())
                : Result<SupportTicketDto>.Ok(await DetailAsync<SupportTicketDto>(row), "SUPPORT_TICKET_FOUND", "Support ticket found");
        }

        public async Task<Result<PagedResultDto<SupportMessageDto>>> MyMessages(int id, PagedRequestDto query)
        {
            if (await _ticketRepository.FindAsync(id, _currentUser.UserId) == null)
                return Result<PagedResultDto<SupportMessageDto>>.Fail(NotFound());

            return await PageMessagesAsync(id, query);
        }

        public async Task<Result<SupportMessageDto>> ReplyAsReporter(int id, SupportMessageInputDto input)
        {
            _logger.SetOperation("ReplySupportTicket");

            var me = _currentUser.UserId;

            var ticket = await _ticketRepository.FirstOrDefaultAsync(new QueryOptions<SupportTicket>
            {
                Predicate = t => t.Id == id && t.ReporterUserId == me,
            });

            if (ticket == null)
                return Result<SupportMessageDto>.Fail(NotFound());

            if (ticket.Status == ESupportTicketStatus.Dismissed)
                return Result<SupportMessageDto>.Fail(new Error("SUPPORT_TICKET_CLOSED", "This report was closed; open a new one if it still happens", ErrorType.Conflict));

            var text = Normalize(input.Text);

            if (text.Length is 0 or > SupportRules.MessageMaxLength)
                return Fail<SupportMessageDto>("INVALID_SUPPORT_MESSAGE", $"Message must have between 1 and {SupportRules.MessageMaxLength} characters");

            var reopened = ticket.Status == ESupportTicketStatus.Resolved;
            var message = await AddMessageAsync(ticket, text, fromStaff: false, reopen: reopened);

            if (reopened)
                _logger.Info("SUPPORT_TICKET_REOPENED", "Support ticket reopened by the reporter", userId: me, extra: new { TicketId = id });

            await NotifyStaffAsync(ENotificationKind.SupportTicketReplied, id, me);

            return Result<SupportMessageDto>.Ok(MapMessage(message, _currentUser.UserId), "SUPPORT_MESSAGE_CREATED", "Message sent");
        }

        public Task<Result<MediaDto>> UploadPhoto(ImageUploadInput? file, CancellationToken cancellationToken = default) =>
            _mediaAppService.UploadPhoto(file, EMediaProfile.Support, cancellationToken);

        public Task<Result<VideoUploadDto>> StartVideo(StartVideoUploadInputDto input) =>
            _mediaAppService.StartVideo(input, EMediaProfile.Support);

        public async Task<Result<PagedResultDto<SupportTicketAdminDto>>> List(SupportTicketListQueryDto query)
        {
            query.Normalize();

            var sort = query.Sort?.ToLowerInvariant() switch
            {
                "newest" => ESupportTicketSort.Newest,
                "priority" => ESupportTicketSort.Priority,
                _ => ESupportTicketSort.Activity,
            };

            var filter = new SupportTicketFilter(
                Status: query.Status,
                Tag: query.Tag,
                Area: query.Area,
                Priority: query.Priority,
                Search: OptionalText(query.Search),
                ProtocolId: SupportRules.ParseProtocol(query.Search),
                Sort: sort);

            var (rows, totalCount) = await _ticketRepository.PageAsync(filter, query.Skip, query.PageSize);
            var (canEdit, canReply, canDelete) = await StaffPermissionsAsync();

            return Result<PagedResultDto<SupportTicketAdminDto>>.Ok(
                PagedResultDto<SupportTicketAdminDto>.Create(rows.Select(r => FillAdmin(new SupportTicketAdminDto(), r, canEdit, canReply, canDelete)).ToList(), query.Page, query.PageSize, totalCount),
                "SUPPORT_TICKETS_FOUND",
                "Support tickets found");
        }

        public async Task<Result<SupportSummaryDto>> Summary()
        {
            var counts = await _ticketRepository.CountByStatusAsync();

            return Result<SupportSummaryDto>.Ok(new SupportSummaryDto
            {
                Open = counts.GetValueOrDefault(ESupportTicketStatus.Open),
                InProgress = counts.GetValueOrDefault(ESupportTicketStatus.InProgress),
                Resolved = counts.GetValueOrDefault(ESupportTicketStatus.Resolved),
                Dismissed = counts.GetValueOrDefault(ESupportTicketStatus.Dismissed),
            }, "SUPPORT_SUMMARY_FOUND", "Support summary found");
        }

        public async Task<Result<SupportTicketAdminDto>> Get(int id)
        {
            var row = await _ticketRepository.FindAsync(id, null);

            return row == null
                ? Result<SupportTicketAdminDto>.Fail(NotFound())
                : Result<SupportTicketAdminDto>.Ok(await AdminDetailAsync(row), "SUPPORT_TICKET_FOUND", "Support ticket found");
        }

        public async Task<Result<PagedResultDto<SupportMessageDto>>> Messages(int id, PagedRequestDto query)
        {
            if (await _ticketRepository.FindAsync(id, null) == null)
                return Result<PagedResultDto<SupportMessageDto>>.Fail(NotFound());

            return await PageMessagesAsync(id, query);
        }

        public async Task<Result<SupportMessageDto>> ReplyAsStaff(int id, SupportMessageInputDto input)
        {
            _logger.SetOperation("ReplySupportTicketAsStaff");

            var ticket = await _ticketRepository.FirstOrDefaultAsync(new QueryOptions<SupportTicket> { Predicate = t => t.Id == id });

            if (ticket == null)
                return Result<SupportMessageDto>.Fail(NotFound());

            var text = Normalize(input.Text);

            if (text.Length is 0 or > SupportRules.MessageMaxLength)
                return Fail<SupportMessageDto>("INVALID_SUPPORT_MESSAGE", $"Message must have between 1 and {SupportRules.MessageMaxLength} characters");

            var me = _currentUser.UserId;
            var message = await AddMessageAsync(ticket, text, fromStaff: true, reopen: false);

            await _notificationAppService.Notify(ticket.ReporterUserId, new NotificationInput(ENotificationKind.SupportTicketReplied, me, SupportTicketId: id));

            return Result<SupportMessageDto>.Ok(MapMessage(message, me), "SUPPORT_MESSAGE_CREATED", "Message sent");
        }

        public async Task<Result<SupportTicketAdminDto>> Update(int id, SupportTicketUpdateInputDto input)
        {
            _logger.SetOperation("UpdateSupportTicket");

            if (!Enum.IsDefined(input.Status))
                return Fail<SupportTicketAdminDto>("INVALID_SUPPORT_STATUS", "Status is not in the supported list");

            if (!Enum.IsDefined(input.Priority))
                return Fail<SupportTicketAdminDto>("INVALID_SUPPORT_PRIORITY", "Priority is not in the supported list");

            var note = input.InternalNote?.Trim();

            if (note is { Length: > SupportRules.NoteMaxLength })
                return Fail<SupportTicketAdminDto>("INVALID_SUPPORT_NOTE", $"Internal note must have at most {SupportRules.NoteMaxLength} characters");

            var ticket = await _ticketRepository.FirstOrDefaultAsync(new QueryOptions<SupportTicket> { Predicate = t => t.Id == id });

            if (ticket == null)
                return Result<SupportTicketAdminDto>.Fail(NotFound());

            var previous = ticket.Status;
            var statusChanged = previous != input.Status;

            ticket.Priority = input.Priority;
            ticket.InternalNote = string.IsNullOrEmpty(note) ? null : note;

            if (statusChanged)
                ApplyStatus(ticket, input.Status, DateTime.UtcNow);

            await _ticketRepository.UpdateAsync(ticket);

            var me = _currentUser.UserId;

            _logger.Info("SUPPORT_TICKET_UPDATED", "Support ticket updated", userId: me,
                extra: new { TicketId = id, From = previous, To = ticket.Status, ticket.Priority });

            if (statusChanged)
                await _notificationAppService.Notify(ticket.ReporterUserId, new NotificationInput(ENotificationKind.SupportTicketStatusChanged, me, SupportTicketId: id));

            return Result<SupportTicketAdminDto>.Ok(await AdminDetailAsync((await _ticketRepository.FindAsync(id, null))!), "SUPPORT_TICKET_UPDATED", "Support ticket updated");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteSupportTicket");

            if (await _ticketRepository.FindAsync(id, null) == null)
                return Result.Fail(NotFound());

            List<int> mediaIds;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                mediaIds = await _ticketRepository.PurgeAsync(id);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            await _mediaAppService.Discard(mediaIds);

            _logger.Info("SUPPORT_TICKET_DELETED", "Support ticket deleted", userId: _currentUser.UserId,
                extra: new { TicketId = id, Media = mediaIds.Count });

            return Result.Ok("SUPPORT_TICKET_DELETED", "Support ticket deleted");
        }

        private async Task<SupportTicketMessage> AddMessageAsync(SupportTicket ticket, string text, bool fromStaff, bool reopen)
        {
            var now = DateTime.UtcNow;
            var message = new SupportTicketMessage
            {
                SupportTicketId = ticket.Id,
                AuthorUserId = _currentUser.UserId,
                Text = text,
                FromStaff = fromStaff,
            };

            if (reopen)
                ApplyStatus(ticket, ESupportTicketStatus.Open, now);
            else if (fromStaff && ticket.Status == ESupportTicketStatus.Open)
                ApplyStatus(ticket, ESupportTicketStatus.InProgress, now);

            ticket.LastActivityAt = now;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _messageRepository.AddAsync(message);
                await _ticketRepository.UpdateAsync(ticket);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            return (await LoadMessageAsync(message.Id)) ?? message;
        }

        private async Task<SupportTicketMessage?> LoadMessageAsync(int messageId) =>
            await _messageRepository.FirstOrDefaultAsync(new QueryOptions<SupportTicketMessage>
            {
                Predicate = m => m.Id == messageId,
                Includes = [m => m.Author!],
                AsNoTracking = true,
            });

        private static void ApplyStatus(SupportTicket ticket, ESupportTicketStatus status, DateTime now)
        {
            ticket.Status = status;
            ticket.LastActivityAt = now;
            ticket.ClosedAt = SupportRules.IsClosed(status) ? now : null;
        }

        private async Task<Result<PagedResultDto<SupportMessageDto>>> PageMessagesAsync(int ticketId, PagedRequestDto query)
        {
            query.Normalize();

            var (rows, totalCount) = await _ticketRepository.PageMessagesAsync(ticketId, query.Skip, query.PageSize);
            var me = _currentUser.UserId;

            return Result<PagedResultDto<SupportMessageDto>>.Ok(
                PagedResultDto<SupportMessageDto>.Create(rows.Select(m => MapMessage(m, me)).ToList(), query.Page, query.PageSize, totalCount),
                "SUPPORT_MESSAGES_FOUND",
                "Messages found");
        }

        private async Task NotifyStaffAsync(ENotificationKind kind, int ticketId, int actorUserId)
        {
            List<int> staff;

            try
            {
                staff = await _permissionChecker.UsersGrantedAsync(Permissions.Support.View);
            }
            catch (Exception ex)
            {
                _logger.Error("NOTIFICATION_FAILED", "Support staff could not be resolved", exception: ex, userId: actorUserId, extra: new { Kind = kind, TicketId = ticketId });
                return;
            }

            await _notificationAppService.NotifyMany(staff, new NotificationInput(kind, actorUserId, SupportTicketId: ticketId));
        }

        private async Task<(bool CanEdit, bool CanReply, bool CanDelete)> StaffPermissionsAsync()
        {
            var me = _currentUser.UserId;
            return (await _permissionChecker.IsGrantedAsync(me, Permissions.Support.Edit),
                await _permissionChecker.IsGrantedAsync(me, Permissions.Support.Reply),
                await _permissionChecker.IsGrantedAsync(me, Permissions.Support.Delete));
        }

        private async Task<T> DetailAsync<T>(SupportTicketReadModel row) where T : SupportTicketDto, new()
        {
            var dto = Fill(new T(), row);
            dto.Media = await MediaAsync(row.Id);
            return dto;
        }

        private async Task<SupportTicketAdminDto> AdminDetailAsync(SupportTicketReadModel row)
        {
            var (canEdit, canReply, canDelete) = await StaffPermissionsAsync();
            var dto = FillAdmin(new SupportTicketAdminDto(), row, canEdit, canReply, canDelete);
            dto.Media = await MediaAsync(row.Id);
            return dto;
        }

        private async Task<List<MediaDto>> MediaAsync(int ticketId)
        {
            var media = await _ticketMediaRepository.ListAsync(new QueryOptions<SupportTicketMedia>
            {
                Predicate = m => m.SupportTicketId == ticketId,
                Includes = [m => m.MediaUpload!],
                OrderBy = q => q.OrderBy(m => m.Order).ThenBy(m => m.Id),
                AsNoTracking = true,
            });

            return media.Select(m => _mediaAppService.Describe(m.MediaUpload!)).ToList();
        }

        private static T Fill<T>(T dto, SupportTicketReadModel r) where T : SupportTicketDto
        {
            dto.Id = r.Id;
            dto.Protocol = SupportRules.Protocol(r.Id);
            dto.Title = r.Title;
            dto.Description = r.Description;
            dto.Status = r.Status;
            dto.Priority = r.Priority;
            dto.Tags = r.Tags.OrderBy(t => t).ToList();
            dto.Areas = r.Areas.OrderBy(a => a).ToList();
            dto.PhotoCount = r.PhotoCount;
            dto.VideoCount = r.VideoCount;
            dto.MessageCount = r.MessageCount;
            dto.AwaitingStaff = SupportRules.IsOpen(r.Status)
                && (r.LastMessageFromStaff == false || (r.LastMessageFromStaff == null && r.Status == ESupportTicketStatus.Open));
            dto.CreatedAt = r.CreatedAt;
            dto.LastActivityAt = r.LastActivityAt;
            dto.ClosedAt = r.ClosedAt;
            return dto;
        }

        private SupportTicketAdminDto FillAdmin(SupportTicketAdminDto dto, SupportTicketReadModel r, bool canEdit, bool canReply, bool canDelete)
        {
            Fill(dto, r);
            dto.Reporter = new ProfileSummaryDto
            {
                Id = r.ReporterUserId,
                Name = r.ReporterName,
                Nickname = r.ReporterNickname,
                AvatarUrl = _fileStorage.PublicUrl(r.ReporterAvatarKey),
            };
            dto.OriginPath = r.OriginPath;
            dto.UserAgent = r.UserAgent;
            dto.ViewportWidth = r.ViewportWidth;
            dto.ViewportHeight = r.ViewportHeight;
            dto.InternalNote = r.InternalNote;
            dto.CanEdit = canEdit;
            dto.CanReply = canReply;
            dto.CanDelete = canDelete;
            return dto;
        }

        private SupportMessageDto MapMessage(SupportTicketMessage message, int viewerUserId) => new()
        {
            Id = message.Id,
            Text = message.Text,
            FromStaff = message.FromStaff,
            Mine = message.AuthorUserId == viewerUserId,
            Author = new ProfileSummaryDto
            {
                Id = message.AuthorUserId,
                Name = message.Author?.Name ?? string.Empty,
                Nickname = message.Author?.Nickname ?? string.Empty,
                AvatarUrl = _fileStorage.PublicUrl(message.Author?.AvatarKey),
            },
            CreatedAt = message.CreatedAt,
        };

        private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

        private static string? OptionalText(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        private static Result<T> Fail<T>(string code, string message) =>
            Result<T>.Fail(new Error(code, message, ErrorType.Validation));

        private static Error NotFound() => new("SUPPORT_TICKET_NOT_FOUND", "Support ticket not found", ErrorType.NotFound);
    }
}
