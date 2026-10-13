using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Auth;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.PublicProfiles.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.PublicProfiles
{
    public class PublicProfileAppService : IPublicProfileAppService
    {
        public const int MinSearchLength = 2;

        private readonly IRepository<User> _userRepository;
        private readonly IFileStorage _fileStorage;

        public PublicProfileAppService(IRepository<User> userRepository, IFileStorage fileStorage)
        {
            _userRepository = userRepository;
            _fileStorage = fileStorage;
        }

        public async Task<Result<UserProfileDto>> GetByNickname(string nickname)
        {
            var normalized = NicknameRules.Normalize(nickname);

            var user = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Nickname == normalized && u.IsActive,
                Includes = [u => u.Genres],
                AsNoTracking = true,
            });

            if (user == null)
                return Result<UserProfileDto>.Fail(new Error("PROFILE_NOT_FOUND", "Profile not found", ErrorType.NotFound));

            return Result<UserProfileDto>.Ok(new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Nickname = user.Nickname,
                AvatarUrl = _fileStorage.PublicUrl(user.AvatarKey),
                BannerUrl = _fileStorage.PublicUrl(user.BannerKey),
                BannerPosition = user.BannerPosition,
                Bio = user.Bio,
                City = user.City,
                MainInstrument = user.MainInstrument,
                Genres = user.Genres.Select(g => g.Genre).OrderBy(g => g).ToList(),
                SocialLinks = new SocialLinksDto
                {
                    Instagram = user.InstagramUrl,
                    Youtube = user.YoutubeUrl,
                    Spotify = user.SpotifyUrl,
                },
                MemberSince = user.CreatedAt,
            }, "USER_PROFILE_FOUND", "Profile found");
        }

        public async Task<Result<PagedResultDto<ProfileSummaryDto>>> Search(UserSearchQueryDto query)
        {
            query.Normalize();

            var term = (query.Search ?? string.Empty).Trim().ToLowerInvariant();

            if (term.Length < MinSearchLength)
                return Result<PagedResultDto<ProfileSummaryDto>>.Ok(
                    PagedResultDto<ProfileSummaryDto>.Create([], query.Page, query.PageSize, 0),
                    "USERS_FOUND",
                    "Users found");

            var options = new QueryOptions<User>
            {
                Predicate = u => u.IsActive && (u.Name.ToLower().Contains(term) || u.Nickname.Contains(term)),
                OrderBy = q => q.OrderBy(u => u.Nickname).ThenBy(u => u.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var totalCount = await _userRepository.CountAsync(options);
            var users = await _userRepository.ListAsync(options);

            var items = users
                .Select(u => new ProfileSummaryDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Nickname = u.Nickname,
                    AvatarUrl = _fileStorage.PublicUrl(u.AvatarKey),
                })
                .ToList();

            return Result<PagedResultDto<ProfileSummaryDto>>.Ok(
                PagedResultDto<ProfileSummaryDto>.Create(items, query.Page, query.PageSize, totalCount),
                "USERS_FOUND",
                "Users found");
        }
    }
}
