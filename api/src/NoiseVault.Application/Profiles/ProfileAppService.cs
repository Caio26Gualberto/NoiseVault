using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Auth;
using NoiseVault.Application.Cities;
using NoiseVault.Application.Cities.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Profiles.Dtos;
using NoiseVault.Application.Profiles.Interfaces;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Profiles;
using NoiseVault.Domain.Interfaces.Authenticate;

namespace NoiseVault.Application.Profiles
{
    public class ProfileAppService : IProfileAppService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IAuthenticateService _authService;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly ICityAppService _cityAppService;
        private readonly ImageReplacement _images;
        private readonly IApplicationLogger _logger;

        public ProfileAppService(
            IRepository<User> userRepository,
            IAuthenticateService authService,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            IImageProcessor imageProcessor,
            ICityAppService cityAppService,
            IApplicationLoggerFactory loggerFactory)
        {
            _userRepository = userRepository;
            _authService = authService;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _cityAppService = cityAppService;
            _images = new ImageReplacement(fileStorage, imageProcessor);
            _logger = loggerFactory.Create(LogContext.Profiles);
        }

        public async Task<Result<ProfileDto>> Me()
        {
            var user = await FindCurrentAsync(tracked: false);

            if (user == null)
                return Result<ProfileDto>.Fail(UserNotFound());

            return Result<ProfileDto>.Ok(await MapAsync(user), "USER_FOUND", "User found successfully");
        }

        public async Task<Result<ProfileDto>> Update(UpdateProfileInputDto input)
        {
            _logger.SetOperation("UpdateProfile");

            var name = UserNameRules.Normalize(input.Name);

            if (!UserNameRules.IsValid(name))
                return Fail("INVALID_NAME", $"Name must be between {UserNameRules.MinLength} and {UserNameRules.MaxLength} characters");

            var bio = ProfileRules.NormalizeOptionalText(input.Bio);

            if (bio is { Length: > ProfileRules.BioMaxLength })
                return Fail("INVALID_BIO", $"Bio must have at most {ProfileRules.BioMaxLength} characters");

            var city = await _cityAppService.Resolve(input.CityId, input.CityName, required: false);

            if (city.Error != null)
                return Result<ProfileDto>.Fail(city.Error);

            var instrument = ProfileRules.NormalizeOptionalText(input.MainInstrument);

            if (instrument is { Length: > ProfileRules.InstrumentMaxLength })
                return Fail("INVALID_INSTRUMENT", $"Instrument must have at most {ProfileRules.InstrumentMaxLength} characters");

            var genres = input.Genres ?? [];

            if (!ProfileRules.IsValidGenres(genres))
                return Fail("INVALID_GENRE", $"Choose up to {ProfileRules.MaxGenres} distinct genres from the supported list");

            var instagram = ProfileRules.NormalizeOptionalText(input.SocialLinks?.Instagram);
            var youtube = ProfileRules.NormalizeOptionalText(input.SocialLinks?.Youtube);
            var spotify = ProfileRules.NormalizeOptionalText(input.SocialLinks?.Spotify);

            if (!ProfileRules.IsValidSocialLink(ESocialNetwork.Instagram, instagram)
                || !ProfileRules.IsValidSocialLink(ESocialNetwork.Youtube, youtube)
                || !ProfileRules.IsValidSocialLink(ESocialNetwork.Spotify, spotify))
                return Fail("INVALID_SOCIAL_LINK", "Each social link must be an http(s) address of its own service");

            var user = await FindCurrentAsync(tracked: true);

            if (user == null)
                return Result<ProfileDto>.Fail(UserNotFound());

            user.Name = name;
            user.Bio = bio;
            user.CityId = city.CityId;
            user.CityName = city.CityName;
            user.MainInstrument = instrument;
            user.InstagramUrl = instagram;
            user.YoutubeUrl = youtube;
            user.SpotifyUrl = spotify;

            user.Genres.RemoveAll(g => !genres.Contains(g.Genre));
            foreach (var genre in genres.Where(g => user.Genres.All(existing => existing.Genre != g)))
                user.Genres.Add(new UserGenre { UserId = user.Id, Genre = genre });

            await _userRepository.UpdateAsync(user);

            return Result<ProfileDto>.Ok(await MapAsync((await FindCurrentAsync(tracked: false))!), "PROFILE_UPDATED", "Profile updated successfully");
        }

        public Task<Result<ProfileDto>> SetAvatar(ImageUploadInput? file, CancellationToken cancellationToken = default) =>
            ReplaceImageAsync(
                file,
                EImagePreset.Avatar,
                folder: "avatar",
                currentKey: user => user.AvatarKey,
                apply: (user, key) => user.AvatarKey = key,
                code: "AVATAR_UPDATED",
                message: "Avatar updated successfully",
                cancellationToken);

        public async Task<Result<ProfileDto>> SetBanner(ImageUploadInput? file, int? position, CancellationToken cancellationToken = default)
        {
            var bannerPosition = position ?? User.DefaultBannerPosition;

            if (!ProfileRules.IsValidBannerPosition(bannerPosition))
                return InvalidBannerPosition();

            return await ReplaceImageAsync(
                file,
                EImagePreset.Banner,
                folder: "banner",
                currentKey: user => user.BannerKey,
                apply: (user, key) =>
                {
                    user.BannerKey = key;
                    user.BannerPosition = bannerPosition;
                },
                code: "BANNER_UPDATED",
                message: "Banner updated successfully",
                cancellationToken);
        }

        public async Task<Result<ProfileDto>> SetBannerPosition(BannerPositionInputDto input)
        {
            if (!ProfileRules.IsValidBannerPosition(input.Position))
                return InvalidBannerPosition();

            var user = await FindCurrentAsync(tracked: true);

            if (user == null)
                return Result<ProfileDto>.Fail(UserNotFound());

            if (user.BannerKey == null)
                return Result<ProfileDto>.Fail(new Error("BANNER_NOT_FOUND", "There is no banner to reposition", ErrorType.NotFound));

            user.BannerPosition = input.Position;
            await _userRepository.UpdateAsync(user);

            return Result<ProfileDto>.Ok(await MapAsync(user), "BANNER_POSITION_UPDATED", "Banner position updated");
        }

        public async Task<Result<ProfileDto>> UpdateNotificationSettings(NotificationSettingsInputDto input)
        {
            var user = await FindCurrentAsync(tracked: true);

            if (user == null)
                return Result<ProfileDto>.Fail(UserNotFound());

            user.EmailOnBandInvite = input.EmailOnBandInvite;
            await _userRepository.UpdateAsync(user);

            return Result<ProfileDto>.Ok(await MapAsync(user), "NOTIFICATION_SETTINGS_UPDATED", "Notification settings updated");
        }

        public Task<Result<ProfileDto>> RemoveAvatar() =>
            RemoveImageAsync(user => user.AvatarKey, user => user.AvatarKey = null, "AVATAR_REMOVED", "Avatar removed");

        public Task<Result<ProfileDto>> RemoveBanner() =>
            RemoveImageAsync(
                user => user.BannerKey,
                user =>
                {
                    user.BannerKey = null;
                    user.BannerPosition = User.DefaultBannerPosition;
                },
                "BANNER_REMOVED",
                "Banner removed");

        private async Task<Result<ProfileDto>> ReplaceImageAsync(
            ImageUploadInput? file,
            EImagePreset preset,
            string folder,
            Func<User, string?> currentKey,
            Action<User, string> apply,
            string code,
            string message,
            CancellationToken cancellationToken)
        {
            _logger.SetOperation(code);

            var upload = await _images.UploadAsync(file, preset, $"users/{_currentUser.UserId}/{folder}", cancellationToken);

            if (!upload.Succeeded)
                return Result<ProfileDto>.Fail(upload.Error!);

            var newKey = upload.Key!;
            var user = await FindCurrentAsync(tracked: true);

            if (user == null)
            {
                await _images.DeleteAsync(newKey);
                return Result<ProfileDto>.Fail(UserNotFound());
            }

            var oldKey = currentKey(user);
            apply(user, newKey);

            await _images.SaveAsync(newKey, oldKey, () => _userRepository.UpdateAsync(user));

            _logger.Info(code, message, userId: user.Id, extra: new { Key = newKey });

            return Result<ProfileDto>.Ok(await MapAsync(user), code, message);
        }

        private async Task<Result<ProfileDto>> RemoveImageAsync(Func<User, string?> currentKey, Action<User> clear, string code, string message)
        {
            var user = await FindCurrentAsync(tracked: true);

            if (user == null)
                return Result<ProfileDto>.Fail(UserNotFound());

            var oldKey = currentKey(user);

            if (oldKey != null)
            {
                clear(user);
                await _userRepository.UpdateAsync(user);
                await _fileStorage.DeleteAsync(oldKey);
                _logger.Info(code, message, userId: user.Id, extra: new { Key = oldKey });
            }

            return Result<ProfileDto>.Ok(await MapAsync(user), code, message);
        }

        private Task<User?> FindCurrentAsync(bool tracked) =>
            _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == _currentUser.UserId,
                Includes = [u => u.Genres, u => u.City!],
                AsNoTracking = !tracked,
            });

        private static Result<ProfileDto> Fail(string code, string message) =>
            Result<ProfileDto>.Fail(new Error(code, message, ErrorType.Validation));

        private static Result<ProfileDto> InvalidBannerPosition() =>
            Fail("INVALID_BANNER_POSITION", $"Banner position must be between {ProfileRules.MinBannerPosition} and {ProfileRules.MaxBannerPosition}");

        private static Error UserNotFound() => new("USER_NOT_FOUND", "User not found", ErrorType.NotFound);

        private async Task<ProfileDto> MapAsync(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Nickname = user.Nickname,
            Email = user.Email,
            Role = _currentUser.Role,
            IsNeededChangePassword = await _authService.IsPasswordChangeRequired(user.Email),
            AvatarUrl = _fileStorage.PublicUrl(user.AvatarKey),
            BannerUrl = _fileStorage.PublicUrl(user.BannerKey),
            BannerPosition = user.BannerPosition,
            EmailOnBandInvite = user.EmailOnBandInvite,
            Bio = user.Bio,
            City = user.City == null ? null : CityAppService.Map(user.City),
            CityName = user.CityName,
            MainInstrument = user.MainInstrument,
            Genres = user.Genres.Select(g => g.Genre).OrderBy(g => g).ToList(),
            SocialLinks = new SocialLinksDto
            {
                Instagram = user.InstagramUrl,
                Youtube = user.YoutubeUrl,
                Spotify = user.SpotifyUrl,
            },
            CreatedAt = user.CreatedAt,
        };
    }
}
