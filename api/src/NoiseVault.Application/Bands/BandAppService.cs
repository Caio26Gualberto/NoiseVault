using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Notifications;
using NoiseVault.Application.Notifications.Interfaces;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Cities.Interfaces;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Notifications;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Bands
{
    public class BandAppService : IBandAppService
    {
        private readonly IRepository<Band> _bandRepository;
        private readonly IRepository<BandMember> _memberRepository;
        private readonly IRepository<BandVerificationRequest> _verificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly IPostRepository _postRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ImageReplacement _images;
        private readonly INotificationAppService _notificationAppService;
        private readonly ICityAppService _cityAppService;
        private readonly IApplicationLogger _logger;

        public BandAppService(
            IRepository<Band> bandRepository,
            IRepository<BandMember> memberRepository,
            IRepository<BandVerificationRequest> verificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            IImageProcessor imageProcessor,
            IPostRepository postRepository,
            IEventRepository eventRepository,
            INotificationAppService notificationAppService,
            ICityAppService cityAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _eventRepository = eventRepository;
            _postRepository = postRepository;
            _bandRepository = bandRepository;
            _memberRepository = memberRepository;
            _verificationRepository = verificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _images = new ImageReplacement(fileStorage, imageProcessor);
            _notificationAppService = notificationAppService;
            _cityAppService = cityAppService;
            _logger = loggerFactory.Create(LogContext.Bands);
        }

        public async Task<Result<BandProfileDto>> Create(CreateBandInputDto input)
        {
            _logger.SetOperation("CreateBand");

            var (data, error) = await ValidateAsync(input);

            if (error != null)
                return Result<BandProfileDto>.Fail(error);

            var instrument = ProfileRules.NormalizeOptionalText(input.Instrument);

            if (instrument is { Length: > ProfileRules.InstrumentMaxLength })
                return Fail("INVALID_INSTRUMENT", $"Instrument must have at most {ProfileRules.InstrumentMaxLength} characters");

            if (await VerifiedNameTakenAsync(data!.Name, null))
                return Result<BandProfileDto>.Fail(VerifiedNameTaken());

            var me = _currentUser.UserId;

            if (await CountMemberships(me) >= BandRules.MaxBandsPerUser)
                return Result<BandProfileDto>.Fail(new Error("BAND_LIMIT_REACHED", $"You can be part of at most {BandRules.MaxBandsPerUser} bands", ErrorType.Conflict));

            var band = new Band { Slug = await UniqueSlugAsync(BandRules.BaseSlug(data!.Name)) };
            Apply(band, data);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _bandRepository.AddAsync(band);
                await _memberRepository.AddAsync(new BandMember
                {
                    BandId = band.Id,
                    UserId = me,
                    Instrument = instrument,
                    Role = EBandMemberRole.Owner,
                    JoinedAt = DateTime.UtcNow,
                });
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("BAND_CREATED", "Band created", userId: me, extra: new { BandId = band.Id, band.Slug });

            return Result<BandProfileDto>.Ok(await MapAsync(band), "BAND_CREATED", "Band created successfully");
        }

        public async Task<Result<BandProfileDto>> GetBySlug(string slug)
        {
            var normalized = BandRules.NormalizeSlug(slug);

            var band = await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Slug == normalized,
                Includes = [b => b.Genres],
                AsNoTracking = true,
            });

            if (band == null)
                return Result<BandProfileDto>.Fail(BandNotFound());

            return Result<BandProfileDto>.Ok(await MapAsync(band), "BAND_FOUND", "Band found");
        }

        public async Task<Result<BandProfileDto>> Update(string slug, BandInputDto input)
        {
            _logger.SetOperation("UpdateBand");

            var (data, error) = await ValidateAsync(input);

            if (error != null)
                return Result<BandProfileDto>.Fail(error);

            var band = await FindOwnedAsync(slug, tracked: true);

            if (band == null)
                return Result<BandProfileDto>.Fail(await DeniedAsync(slug));

            if (!band.IsVerified && await VerifiedNameTakenAsync(data!.Name, band.Id))
                return Result<BandProfileDto>.Fail(VerifiedNameTaken());

            Apply(band, data!);
            await _bandRepository.UpdateAsync(band);

            return Result<BandProfileDto>.Ok(await MapAsync(band), "BAND_UPDATED", "Band updated successfully");
        }

        public async Task<Result<List<BandSummaryDto>>> Suggested()
        {
            var me = _currentUser.UserId;

            var bands = await _bandRepository.ListAsync(new QueryOptions<Band>
            {
                Predicate = b => !b.Members.Any(m => m.UserId == me),
                Includes = [b => b.Genres],
                OrderBy = q => q.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id),
                Take = BandRules.SuggestedCount,
                AsNoTracking = true,
            });

            return Result<List<BandSummaryDto>>.Ok(bands.Select(MapSummary).ToList(), "BANDS_FOUND", "Bands found");
        }

        public async Task<Result<PagedResultDto<BandSummaryDto>>> Search(BandSearchQueryDto query)
        {
            query.Normalize();

            var term = (query.Search ?? string.Empty).Trim().ToLowerInvariant();

            if (term.Length < BandRules.MinSearchLength)
                return Result<PagedResultDto<BandSummaryDto>>.Ok(
                    PagedResultDto<BandSummaryDto>.Create([], query.Page, query.PageSize, 0),
                    "BANDS_FOUND",
                    "Bands found");

            var options = new QueryOptions<Band>
            {
                Predicate = b => b.Name.ToLower().Contains(term) || b.Slug.Contains(term),
                Includes = [b => b.Genres],
                OrderBy = q => q.OrderBy(b => b.Name).ThenBy(b => b.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _bandRepository.CountAsync(options);
            var bands = await _bandRepository.ListAsync(options);

            return Result<PagedResultDto<BandSummaryDto>>.Ok(
                PagedResultDto<BandSummaryDto>.Create(bands.Select(MapSummary).ToList(), query.Page, query.PageSize, totalCount),
                "BANDS_FOUND",
                "Bands found");
        }

        public async Task<Result<List<BandSummaryDto>>> Mine() =>
            Result<List<BandSummaryDto>>.Ok(await ListSummariesForUser(_currentUser.UserId), "BANDS_FOUND", "Bands found");

        public async Task<Result<List<BandSummaryDto>>> Owned()
        {
            var me = _currentUser.UserId;

            var bands = await _bandRepository.ListAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner),
                Includes = [b => b.Genres],
                OrderBy = q => q.OrderBy(b => b.Name).ThenBy(b => b.Id),
                AsNoTracking = true,
            });

            return Result<List<BandSummaryDto>>.Ok(bands.Select(MapSummary).ToList(), "BANDS_FOUND", "Bands found");
        }

        public Task<Result<BandProfileDto>> SetAvatar(string slug, ImageUploadInput? file, CancellationToken cancellationToken = default) =>
            ReplaceImageAsync(
                slug,
                file,
                EImagePreset.Avatar,
                folder: "avatar",
                currentKey: band => band.AvatarKey,
                apply: (band, key) => band.AvatarKey = key,
                code: "AVATAR_UPDATED",
                message: "Avatar updated successfully",
                cancellationToken);

        public async Task<Result<BandProfileDto>> SetBanner(string slug, ImageUploadInput? file, int? position, CancellationToken cancellationToken = default)
        {
            var bannerPosition = position ?? Band.DefaultBannerPosition;

            if (!ProfileRules.IsValidBannerPosition(bannerPosition))
                return InvalidBannerPosition();

            return await ReplaceImageAsync(
                slug,
                file,
                EImagePreset.Banner,
                folder: "banner",
                currentKey: band => band.BannerKey,
                apply: (band, key) =>
                {
                    band.BannerKey = key;
                    band.BannerPosition = bannerPosition;
                },
                code: "BANNER_UPDATED",
                message: "Banner updated successfully",
                cancellationToken);
        }

        public async Task<Result<BandProfileDto>> SetBannerPosition(string slug, BannerPositionInputDto input)
        {
            if (!ProfileRules.IsValidBannerPosition(input.Position))
                return InvalidBannerPosition();

            var band = await FindOwnedAsync(slug, tracked: true);

            if (band == null)
                return Result<BandProfileDto>.Fail(await DeniedAsync(slug));

            if (band.BannerKey == null)
                return Result<BandProfileDto>.Fail(new Error("BANNER_NOT_FOUND", "There is no banner to reposition", ErrorType.NotFound));

            band.BannerPosition = input.Position;
            await _bandRepository.UpdateAsync(band);

            return Result<BandProfileDto>.Ok(await MapAsync(band), "BANNER_POSITION_UPDATED", "Banner position updated");
        }

        public Task<Result<BandProfileDto>> RemoveAvatar(string slug) =>
            RemoveImageAsync(slug, band => band.AvatarKey, band => band.AvatarKey = null, "AVATAR_REMOVED", "Avatar removed");

        public Task<Result<BandProfileDto>> RemoveBanner(string slug) =>
            RemoveImageAsync(
                slug,
                band => band.BannerKey,
                band =>
                {
                    band.BannerKey = null;
                    band.BannerPosition = Band.DefaultBannerPosition;
                },
                "BANNER_REMOVED",
                "Banner removed");

        public async Task<Result<BandProfileDto>> RemoveMember(string slug, int userId)
        {
            _logger.SetOperation("RemoveBandMember");

            var band = await FindOwnedAsync(slug, tracked: false);

            if (band == null)
                return Result<BandProfileDto>.Fail(await DeniedAsync(slug));

            var member = await FindMemberAsync(band.Id, userId);

            if (member == null)
                return Result<BandProfileDto>.Fail(MemberNotFound());

            if (member.Role == EBandMemberRole.Owner)
                return Result<BandProfileDto>.Fail(new Error("BAND_OWNER_NOT_REMOVABLE", "Band owners leave on their own; they cannot be removed", ErrorType.Conflict));

            await _memberRepository.DeleteAsync(member);

            _logger.Info("BAND_MEMBER_REMOVED", "Band member removed", userId: _currentUser.UserId, extra: new { BandId = band.Id, RemovedUserId = userId });

            await _notificationAppService.Notify(userId, new NotificationInput(ENotificationKind.BandRemoved, _currentUser.UserId, BandId: band.Id));

            return Result<BandProfileDto>.Ok(await MapAsync(band), "BAND_MEMBER_REMOVED", "Member removed from the band");
        }

        public async Task<Result<BandProfileDto>> PromoteMember(string slug, int userId)
        {
            _logger.SetOperation("PromoteBandMember");

            var band = await FindOwnedAsync(slug, tracked: false);

            if (band == null)
                return Result<BandProfileDto>.Fail(await DeniedAsync(slug));

            var member = await FindMemberAsync(band.Id, userId);

            if (member == null)
                return Result<BandProfileDto>.Fail(MemberNotFound());

            if (member.Role != EBandMemberRole.Owner)
            {
                member.Role = EBandMemberRole.Owner;
                await _memberRepository.UpdateAsync(member);

                _logger.Info("BAND_MEMBER_PROMOTED", "Band member promoted to owner", userId: _currentUser.UserId, extra: new { BandId = band.Id, PromotedUserId = userId });

                await _notificationAppService.Notify(userId, new NotificationInput(ENotificationKind.BandPromoted, _currentUser.UserId, BandId: band.Id));
            }

            return Result<BandProfileDto>.Ok(await MapAsync(band), "BAND_MEMBER_PROMOTED", "Member is now a band owner");
        }

        public async Task<Result> Leave(string slug)
        {
            _logger.SetOperation("LeaveBand");

            var (bandId, role) = await FindAccess(slug);

            if (bandId == null)
                return Result.Fail(BandNotFound());

            if (role == null)
                return Result.Fail(MemberNotFound());

            if (role == EBandMemberRole.Owner)
            {
                var owners = await _memberRepository.CountAsync(new QueryOptions<BandMember>
                {
                    Predicate = m => m.BandId == bandId && m.Role == EBandMemberRole.Owner,
                });

                if (owners <= 1)
                    return Result.Fail(new Error("BAND_LAST_OWNER", "Make another member an owner before leaving the band", ErrorType.Conflict));
            }

            var member = await FindMemberAsync(bandId.Value, _currentUser.UserId);

            if (member == null)
                return Result.Fail(MemberNotFound());

            await _memberRepository.DeleteAsync(member);

            _logger.Info("BAND_MEMBER_LEFT", "Band member left", userId: _currentUser.UserId, extra: new { BandId = bandId });

            await _notificationAppService.NotifyMany(
                await MemberUserIds(bandId.Value, EBandMemberRole.Owner),
                new NotificationInput(ENotificationKind.BandMemberLeft, _currentUser.UserId, BandId: bandId.Value));

            return Result.Ok("BAND_LEFT", "You left the band");
        }

        public async Task<List<BandSummaryDto>> ListSummariesForUser(int userId)
        {
            var bands = await _bandRepository.ListAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Members.Any(m => m.UserId == userId),
                Includes = [b => b.Genres],
                OrderBy = q => q.OrderBy(b => b.Name).ThenBy(b => b.Id),
                AsNoTracking = true,
            });

            return bands.Select(MapSummary).ToList();
        }

        public async Task<Dictionary<int, BandSummaryDto>> SummariesByIds(IReadOnlyCollection<int> bandIds)
        {
            if (bandIds.Count == 0)
                return [];

            var bands = await _bandRepository.ListAsync(new QueryOptions<Band>
            {
                Predicate = b => bandIds.Contains(b.Id),
                Includes = [b => b.Genres],
                AsNoTracking = true,
            });

            return bands.ToDictionary(b => b.Id, MapSummary);
        }

        public async Task<Dictionary<string, BandSummaryDto>> SummariesBySlugs(IReadOnlyCollection<string> slugs)
        {
            if (slugs.Count == 0)
                return [];

            var bands = await _bandRepository.ListAsync(new QueryOptions<Band>
            {
                Predicate = b => slugs.Contains(b.Slug),
                Includes = [b => b.Genres],
                AsNoTracking = true,
            });

            return bands.ToDictionary(b => b.Slug, MapSummary);
        }

        public async Task<(int? BandId, EBandMemberRole? Role)> FindAccess(string slug)
        {
            var normalized = BandRules.NormalizeSlug(slug);

            var band = await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Slug == normalized,
                AsNoTracking = true,
            });

            if (band == null)
                return (null, null);

            var member = await FindMemberAsync(band.Id, _currentUser.UserId);

            return (band.Id, member?.Role);
        }

        public async Task<List<int>> MemberUserIds(int bandId, EBandMemberRole? role = null)
        {
            var members = await _memberRepository.ListAsync(new QueryOptions<BandMember>
            {
                Predicate = m => m.BandId == bandId && (role == null || m.Role == role),
                AsNoTracking = true,
            });

            return members.Select(m => m.UserId).ToList();
        }

        public async Task<bool> IsMember(int bandId, int userId) =>
            await _memberRepository.CountAsync(new QueryOptions<BandMember>
            {
                Predicate = m => m.BandId == bandId && m.UserId == userId,
            }) > 0;

        public Task<int> CountMemberships(int userId) =>
            _memberRepository.CountAsync(new QueryOptions<BandMember> { Predicate = m => m.UserId == userId });

        public Task AddMember(int bandId, int userId, string? instrument) =>
            _memberRepository.AddAsync(new BandMember
            {
                BandId = bandId,
                UserId = userId,
                Instrument = instrument,
                Role = EBandMemberRole.Member,
                JoinedAt = DateTime.UtcNow,
            });

        private async Task<Result<BandProfileDto>> ReplaceImageAsync(
            string slug,
            ImageUploadInput? file,
            EImagePreset preset,
            string folder,
            Func<Band, string?> currentKey,
            Action<Band, string> apply,
            string code,
            string message,
            CancellationToken cancellationToken)
        {
            _logger.SetOperation(code);

            var band = await FindOwnedAsync(slug, tracked: true);

            if (band == null)
                return Result<BandProfileDto>.Fail(await DeniedAsync(slug));

            var upload = await _images.UploadAsync(file, preset, $"bands/{band.Id}/{folder}", cancellationToken);

            if (!upload.Succeeded)
                return Result<BandProfileDto>.Fail(upload.Error!);

            var newKey = upload.Key!;
            var oldKey = currentKey(band);
            apply(band, newKey);

            await _images.SaveAsync(newKey, oldKey, () => _bandRepository.UpdateAsync(band));

            _logger.Info(code, message, userId: _currentUser.UserId, extra: new { BandId = band.Id, Key = newKey });

            return Result<BandProfileDto>.Ok(await MapAsync(band), code, message);
        }

        private async Task<Result<BandProfileDto>> RemoveImageAsync(string slug, Func<Band, string?> currentKey, Action<Band> clear, string code, string message)
        {
            var band = await FindOwnedAsync(slug, tracked: true);

            if (band == null)
                return Result<BandProfileDto>.Fail(await DeniedAsync(slug));

            var oldKey = currentKey(band);

            if (oldKey != null)
            {
                clear(band);
                await _bandRepository.UpdateAsync(band);
                await _images.DeleteAsync(oldKey);
                _logger.Info(code, message, userId: _currentUser.UserId, extra: new { BandId = band.Id, Key = oldKey });
            }

            return Result<BandProfileDto>.Ok(await MapAsync(band), code, message);
        }

        private Task<Band?> FindOwnedAsync(string slug, bool tracked)
        {
            var normalized = BandRules.NormalizeSlug(slug);
            var me = _currentUser.UserId;

            return _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Slug == normalized && b.Members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner),
                Includes = [b => b.Genres],
                AsNoTracking = !tracked,
            });
        }

        private async Task<Error> DeniedAsync(string slug)
        {
            var (bandId, _) = await FindAccess(slug);

            return bandId == null
                ? BandNotFound()
                : new Error("BAND_FORBIDDEN", "Only band owners can do this", ErrorType.Forbidden);
        }

        private Task<BandMember?> FindMemberAsync(int bandId, int userId) =>
            _memberRepository.FirstOrDefaultAsync(new QueryOptions<BandMember>
            {
                Predicate = m => m.BandId == bandId && m.UserId == userId,
            });

        private async Task<string> UniqueSlugAsync(string baseSlug)
        {
            var slug = baseSlug;

            for (var suffix = 2; await SlugTakenAsync(slug); suffix++)
                slug = BandRules.WithSuffix(baseSlug, suffix);

            return slug;
        }

        private async Task<bool> SlugTakenAsync(string slug) =>
            await _bandRepository.CountAsync(new QueryOptions<Band> { Predicate = b => b.Slug == slug, IncludeDeleted = true }) > 0;

        private async Task<bool> VerifiedNameTakenAsync(string name, int? exceptBandId)
        {
            var normalized = name.ToLowerInvariant();

            return await _bandRepository.CountAsync(new QueryOptions<Band>
            {
                Predicate = b => b.IsVerified && b.Id != exceptBandId && b.Name.ToLower() == normalized,
            }) > 0;
        }

        private static Error VerifiedNameTaken() =>
            new("BAND_NAME_RESERVED", "A verified band already uses this name", ErrorType.Conflict);

        private async Task<BandVerificationStateDto?> VerificationStateAsync(int bandId)
        {
            var latest = await _verificationRepository.FirstOrDefaultAsync(new QueryOptions<BandVerificationRequest>
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

        private sealed record ValidBand(
            string Name,
            string? Description,
            int? CityId,
            string? CityName,
            List<EMusicGenre> Genres,
            int? FoundedYear,
            string? Instagram,
            string? Youtube,
            string? Spotify);

        private async Task<(ValidBand? Data, Error? Error)> ValidateAsync(BandInputDto input)
        {
            var name = BandRules.NormalizeName(input.Name);

            if (!BandRules.IsValidName(name))
                return (null, Invalid("INVALID_BAND_NAME", $"Band name must be between {BandRules.NameMinLength} and {BandRules.NameMaxLength} characters"));

            var description = ProfileRules.NormalizeOptionalText(input.Description);

            if (description is { Length: > BandRules.DescriptionMaxLength })
                return (null, Invalid("INVALID_BAND_DESCRIPTION", $"Description must have at most {BandRules.DescriptionMaxLength} characters"));

            var city = await _cityAppService.Resolve(input.CityId, input.CityName, required: false);

            if (city.Error != null)
                return (null, city.Error);

            var genres = input.Genres ?? [];

            if (!ProfileRules.IsValidGenres(genres))
                return (null, Invalid("INVALID_GENRE", $"Choose up to {ProfileRules.MaxGenres} distinct genres from the supported list"));

            if (!BandRules.IsValidFoundedYear(input.FoundedYear))
                return (null, Invalid("INVALID_FOUNDED_YEAR", $"Founded year must be between {BandRules.MinFoundedYear} and the current year"));

            var instagram = ProfileRules.NormalizeOptionalText(input.SocialLinks?.Instagram);
            var youtube = ProfileRules.NormalizeOptionalText(input.SocialLinks?.Youtube);
            var spotify = ProfileRules.NormalizeOptionalText(input.SocialLinks?.Spotify);

            if (!ProfileRules.IsValidSocialLink(ESocialNetwork.Instagram, instagram)
                || !ProfileRules.IsValidSocialLink(ESocialNetwork.Youtube, youtube)
                || !ProfileRules.IsValidSocialLink(ESocialNetwork.Spotify, spotify))
                return (null, Invalid("INVALID_SOCIAL_LINK", "Each social link must be an http(s) address of its own service"));

            return (new ValidBand(name, description, city.CityId, city.CityName, genres, input.FoundedYear, instagram, youtube, spotify), null);
        }

        private static void Apply(Band band, ValidBand data)
        {
            band.Name = data.Name;
            band.Description = data.Description;
            band.CityId = data.CityId;
            band.CityName = data.CityName;
            band.FoundedYear = data.FoundedYear;
            band.InstagramUrl = data.Instagram;
            band.YoutubeUrl = data.Youtube;
            band.SpotifyUrl = data.Spotify;

            band.Genres.RemoveAll(g => !data.Genres.Contains(g.Genre));
            foreach (var genre in data.Genres.Where(g => band.Genres.All(existing => existing.Genre != g)))
                band.Genres.Add(new BandGenre { BandId = band.Id, Genre = genre });
        }

        private async Task<BandProfileDto> MapAsync(Band band)
        {
            var members = await _memberRepository.ListAsync(new QueryOptions<BandMember>
            {
                Predicate = m => m.BandId == band.Id && m.User!.IsActive,
                Includes = [m => m.User!],
                OrderBy = q => q.OrderBy(m => m.Role).ThenBy(m => m.JoinedAt).ThenBy(m => m.Id),
                AsNoTracking = true,
            });

            var me = _currentUser.UserIdOrNull;

            return new BandProfileDto
            {
                Id = band.Id,
                Name = band.Name,
                Slug = band.Slug,
                Description = band.Description,
                City = await _cityAppService.Get(band.CityId),
                CityName = band.CityName,
                Genres = band.Genres.Select(g => g.Genre).OrderBy(g => g).ToList(),
                FoundedYear = band.FoundedYear,
                AvatarUrl = _fileStorage.PublicUrl(band.AvatarKey),
                BannerUrl = _fileStorage.PublicUrl(band.BannerKey),
                BannerPosition = band.BannerPosition,
                IsVerified = band.IsVerified,
                SocialLinks = new SocialLinksDto
                {
                    Instagram = band.InstagramUrl,
                    Youtube = band.YoutubeUrl,
                    Spotify = band.SpotifyUrl,
                },
                Members = members.Select(m => new BandMemberDto
                {
                    User = new ProfileSummaryDto
                    {
                        Id = m.User!.Id,
                        Name = m.User.Name,
                        Nickname = m.User.Nickname,
                        AvatarUrl = _fileStorage.PublicUrl(m.User.AvatarKey),
                    },
                    Instrument = m.Instrument,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                }).ToList(),
                MyRole = members.FirstOrDefault(m => m.UserId == me)?.Role,
                Verification = members.Any(m => m.UserId == me && m.Role == EBandMemberRole.Owner) ? await VerificationStateAsync(band.Id) : null,
                PostCount = await _postRepository.CountPublishedByBandAsync(band.Id),
                UpcomingEventCount = await _eventRepository.CountUpcomingByBandAsync(band.Id, DateTime.UtcNow),
            };
        }

        private BandSummaryDto MapSummary(Band band) => new()
        {
            Id = band.Id,
            Name = band.Name,
            Slug = band.Slug,
            AvatarUrl = _fileStorage.PublicUrl(band.AvatarKey),
            IsVerified = band.IsVerified,
            Genres = band.Genres.Select(g => g.Genre).OrderBy(g => g).ToList(),
        };

        private static Result<BandProfileDto> Fail(string code, string message) =>
            Result<BandProfileDto>.Fail(Invalid(code, message));

        private static Error Invalid(string code, string message) => new(code, message, ErrorType.Validation);

        private static Result<BandProfileDto> InvalidBannerPosition() =>
            Fail("INVALID_BANNER_POSITION", $"Banner position must be between {ProfileRules.MinBannerPosition} and {ProfileRules.MaxBannerPosition}");

        private static Error BandNotFound() => new("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound);

        private static Error MemberNotFound() => new("BAND_MEMBER_NOT_FOUND", "This person is not a member of the band", ErrorType.NotFound);
    }
}
