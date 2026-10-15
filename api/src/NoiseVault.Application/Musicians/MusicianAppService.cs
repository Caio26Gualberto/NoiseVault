using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Auth;
using NoiseVault.Application.Cities;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Musicians.Dtos;
using NoiseVault.Application.Musicians.Interfaces;
using NoiseVault.Application.Pagination;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.PublicProfiles.Dtos;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Ensaio;
using NoiseVault.Domain.Enums.Profiles;

namespace NoiseVault.Application.Musicians
{
    public class MusicianAppService : IMusicianAppService
    {
        private readonly IRepository<MusicianProfile> _profileRepository;
        private readonly IRepository<User> _userRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IFileStorage _fileStorage;
        private readonly ImageReplacement _images;
        private readonly IApplicationLogger _logger;

        public MusicianAppService(
            IRepository<MusicianProfile> profileRepository,
            IRepository<User> userRepository,
            ICurrentUser currentUser,
            IFileStorage fileStorage,
            IImageProcessor imageProcessor,
            IApplicationLoggerFactory loggerFactory)
        {
            _profileRepository = profileRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _images = new ImageReplacement(fileStorage, imageProcessor);
            _logger = loggerFactory.Create(LogContext.Ensaio);
        }

        public async Task<Result<MusicianProfileDto>> GetMine()
        {
            var me = _currentUser.UserId;
            var profile = await FindAsync(p => p.UserId == me, tracked: false);

            if (profile != null)
                return Ok(Map(profile), "MUSICIAN_PROFILE_FOUND", "Musician profile found");

            var user = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User>
            {
                Predicate = u => u.Id == me,
                Includes = [u => u.Genres, u => u.City!],
                AsNoTracking = true,
            });

            if (user == null)
                return Fail(new Error("USER_NOT_FOUND", "User not found", ErrorType.NotFound));

            return Ok(new MusicianProfileDto
            {
                Exists = false,
                IsMine = true,
                Person = Summary(user),
                City = user.City == null ? null : CityAppService.Map(user.City),
                CityName = user.CityName,
                Goal = EMusicianGoal.Either,
                Commitment = EMusicianCommitment.PlayLive,
                Reach = EReach.Abc,
                Genres = user.Genres.Select(g => g.Genre).OrderBy(g => g).ToList(),
            }, "MUSICIAN_PROFILE_FOUND", "Musician profile not created yet");
        }

        public async Task<Result<MusicianProfileDto>> SaveMine(MusicianInputDto input)
        {
            _logger.SetOperation("SaveMusicianProfile");

            var error = Validate(input, out var influences, out var instruments, out var links);

            if (error != null)
                return Fail(error);

            var me = _currentUser.UserId;
            var genres = input.Genres ?? [];
            var availability = (input.Availability ?? []).Select(a => (a.Day, a.Period)).ToList();
            var now = DateTime.UtcNow;

            var profile = await FindAsync(p => p.UserId == me, tracked: true);
            var creating = profile == null;
            profile ??= new MusicianProfile { UserId = me };

            ApplyLooking(profile, input.IsLookingForBand, now);
            profile.Goal = input.Goal;
            profile.Commitment = input.Commitment;
            profile.Reach = input.Reach;
            profile.HasGear = input.HasGear;
            profile.HasTransport = input.HasTransport;
            profile.HasRehearsalSpace = input.HasRehearsalSpace;
            profile.Influences = influences;

            profile.Instruments.Clear();
            profile.Instruments.AddRange(instruments.Select((i, index) => new MusicianInstrument
            {
                Instrument = i.Instrument,
                OtherName = i.OtherName,
                Level = i.Level,
                Position = index,
            }));

            profile.SampleLinks.Clear();
            profile.SampleLinks.AddRange(links.Select((url, index) => new MusicianSampleLink { Url = url, Position = index }));

            profile.Genres.RemoveAll(g => !genres.Contains(g.Genre));
            foreach (var genre in genres.Where(g => profile.Genres.All(existing => existing.Genre != g)))
                profile.Genres.Add(new MusicianGenre { MusicianProfileId = profile.Id, Genre = genre });

            profile.Availability.RemoveAll(a => !availability.Contains((a.Day, a.Period)));
            foreach (var (day, period) in availability.Where(slot => profile.Availability.All(a => a.Day != slot.Day || a.Period != slot.Period)))
                profile.Availability.Add(new MusicianAvailability { MusicianProfileId = profile.Id, Day = day, Period = period });

            if (creating)
                await _profileRepository.AddAsync(profile);
            else
                await _profileRepository.UpdateAsync(profile);

            if (creating)
                _logger.Info("MUSICIAN_PROFILE_CREATED", "Musician profile created", userId: me, extra: new { ProfileId = profile.Id, profile.IsLookingForBand });

            return Ok(Map((await FindAsync(p => p.UserId == me, tracked: false))!), "MUSICIAN_PROFILE_SAVED", "Musician profile saved");
        }

        public async Task<Result<MusicianProfileDto>> SetLooking(MusicianLookingInputDto input)
        {
            var me = _currentUser.UserId;
            var profile = await FindAsync(p => p.UserId == me, tracked: true);

            if (profile == null)
                return Fail(ProfileRequired());

            ApplyLooking(profile, input.IsLookingForBand, DateTime.UtcNow);
            await _profileRepository.UpdateAsync(profile);

            return Ok(Map((await FindAsync(p => p.UserId == me, tracked: false))!), "MUSICIAN_LOOKING_CHANGED", "Looking for band changed");
        }

        public async Task<Result<MusicianProfileDto>> SetPhoto(ImageUploadInput? file, CancellationToken cancellationToken = default)
        {
            _logger.SetOperation("SetMusicianPhoto");

            var me = _currentUser.UserId;
            var profile = await FindAsync(p => p.UserId == me, tracked: true);

            if (profile == null)
                return Fail(ProfileRequired());

            var upload = await _images.UploadAsync(file, EImagePreset.Flyer, $"users/{me}/musician", cancellationToken);

            if (!upload.Succeeded)
                return Fail(upload.Error!);

            var oldKey = profile.PhotoKey;
            profile.PhotoKey = upload.Key;

            await _images.SaveAsync(upload.Key!, oldKey, () => _profileRepository.UpdateAsync(profile));

            _logger.Info("MUSICIAN_PHOTO_UPDATED", "Musician photo updated", userId: me, extra: new { ProfileId = profile.Id, upload.Key });

            return Ok(Map((await FindAsync(p => p.UserId == me, tracked: false))!), "MUSICIAN_PHOTO_UPDATED", "Photo updated");
        }

        public async Task<Result<MusicianProfileDto>> RemovePhoto()
        {
            var me = _currentUser.UserId;
            var profile = await FindAsync(p => p.UserId == me, tracked: true);

            if (profile == null)
                return Fail(ProfileRequired());

            var oldKey = profile.PhotoKey;

            if (oldKey != null)
            {
                profile.PhotoKey = null;
                await _profileRepository.UpdateAsync(profile);
                await _images.DeleteAsync(oldKey);
            }

            return Ok(Map((await FindAsync(p => p.UserId == me, tracked: false))!), "MUSICIAN_PHOTO_REMOVED", "Photo removed");
        }

        public async Task<Result<PagedResultDto<MusicianCardDto>>> Search(MusicianSearchQueryDto query)
        {
            query.Normalize();

            var instrument = query.Instrument;
            var cityId = query.CityId;
            var abc = query.Abc == true;
            var abcIds = CityRules.AbcCityIds;
            var genre = query.Genre;
            var withSamples = query.WithSamples == true;

            var options = new QueryOptions<MusicianProfile>
            {
                Predicate = p => p.IsLookingForBand
                    && p.User!.IsActive
                    && (instrument == null || p.Instruments.Any(i => i.Instrument == instrument))
                    && (cityId == null || p.User.CityId == cityId)
                    && (!abc || (p.User.CityId != null && abcIds.Contains(p.User.CityId.Value)))
                    && (genre == null || p.Genres.Any(g => g.Genre == genre))
                    && (!withSamples || p.SampleLinks.Any()),
                Includes = [p => p.User!, p => p.User!.City!, p => p.Instruments, p => p.Genres, p => p.SampleLinks],
                OrderBy = q => q.OrderByDescending(p => p.LookingSince).ThenByDescending(p => p.Id),
                Skip = query.Skip,
                Take = query.PageSize,
                AsNoTracking = true,
            };

            var total = await _profileRepository.CountAsync(options);
            var rows = await _profileRepository.ListAsync(options);

            return Result<PagedResultDto<MusicianCardDto>>.Ok(
                PagedResultDto<MusicianCardDto>.Create(rows.Select(Card).ToList(), query.Page, query.PageSize, total),
                "MUSICIANS_FOUND",
                "Musicians found");
        }

        public async Task<Result<MusicianProfileDto>> GetByNickname(string nickname)
        {
            var normalized = NicknameRules.Normalize(nickname);
            var me = _currentUser.UserId;

            var profile = await FindAsync(p => p.User!.Nickname == normalized && p.User.IsActive, tracked: false);

            if (profile == null || (!profile.IsLookingForBand && profile.UserId != me))
                return Fail(new Error("MUSICIAN_PROFILE_NOT_FOUND", "Musician profile not found", ErrorType.NotFound));

            return Ok(Map(profile), "MUSICIAN_PROFILE_FOUND", "Musician profile found");
        }

        public async Task<bool> IsLooking(int userId) =>
            await _profileRepository.CountAsync(new QueryOptions<MusicianProfile> { Predicate = p => p.UserId == userId && p.IsLookingForBand }) > 0;

        public async Task<bool> HasProfile(int userId) =>
            await _profileRepository.CountAsync(new QueryOptions<MusicianProfile> { Predicate = p => p.UserId == userId }) > 0;

        public async Task<Dictionary<int, MusicianCardDto>> CardsByUserIds(IReadOnlyCollection<int> userIds)
        {
            if (userIds.Count == 0)
                return [];

            var rows = await _profileRepository.ListAsync(new QueryOptions<MusicianProfile>
            {
                Predicate = p => userIds.Contains(p.UserId),
                Includes = [p => p.User!, p => p.User!.City!, p => p.Instruments, p => p.Genres, p => p.SampleLinks],
                AsNoTracking = true,
            });

            return rows.ToDictionary(p => p.UserId, Card);
        }

        private Task<MusicianProfile?> FindAsync(System.Linq.Expressions.Expression<Func<MusicianProfile, bool>> predicate, bool tracked) =>
            _profileRepository.FirstOrDefaultAsync(new QueryOptions<MusicianProfile>
            {
                Predicate = predicate,
                Includes = [p => p.User!, p => p.User!.City!, p => p.Instruments, p => p.Genres, p => p.Availability, p => p.SampleLinks],
                AsNoTracking = !tracked,
            });

        private static void ApplyLooking(MusicianProfile profile, bool looking, DateTime now)
        {
            if (looking && !profile.IsLookingForBand)
                profile.LookingSince = now;
            else if (!looking)
                profile.LookingSince = null;

            profile.IsLookingForBand = looking;
        }

        private static Error? Validate(MusicianInputDto input, out string? influences, out List<MusicianInstrumentDto> instruments, out List<string> links)
        {
            influences = ProfileRules.NormalizeOptionalText(input.Influences);
            instruments = (input.Instruments ?? [])
                .Select(i => new MusicianInstrumentDto
                {
                    Instrument = i.Instrument,
                    OtherName = i.Instrument == EInstrument.Other ? ProfileRules.NormalizeOptionalText(i.OtherName) : null,
                    Level = i.Level,
                })
                .ToList();
            links = (input.SampleLinks ?? []).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

            if (instruments.Count is < 1 or > MusicianRules.MaxInstruments
                || instruments.Any(i => !MusicianRules.IsValidInstrument(i.Instrument, i.OtherName) || !Enum.IsDefined(i.Level))
                || instruments.Select(i => (i.Instrument, i.OtherName?.ToLowerInvariant())).Distinct().Count() != instruments.Count)
                return Validation("INVALID_MUSICIAN_INSTRUMENTS", $"Choose from 1 to {MusicianRules.MaxInstruments} distinct instruments, each with a level");

            if (!Enum.IsDefined(input.Goal) || !Enum.IsDefined(input.Commitment) || !Enum.IsDefined(input.Reach))
                return Validation("INVALID_MUSICIAN_PREFERENCES", "Goal, commitment and reach must be supported values");

            if (!ProfileRules.IsValidGenres(input.Genres ?? []))
                return Validation("INVALID_GENRE", $"Choose up to {ProfileRules.MaxGenres} distinct genres from the supported list");

            var availability = input.Availability ?? [];

            if (availability.Any(a => !Enum.IsDefined(a.Day) || !Enum.IsDefined(a.Period))
                || availability.Select(a => (a.Day, a.Period)).Distinct().Count() != availability.Count)
                return Validation("INVALID_AVAILABILITY", "Availability must be distinct days and periods");

            if (links.Count > MusicianRules.MaxSampleLinks || links.Any(l => !MusicianRules.IsValidSampleLink(l)) || links.Distinct().Count() != links.Count)
                return Validation("INVALID_SAMPLE_LINK", $"Up to {MusicianRules.MaxSampleLinks} https links from YouTube, Spotify, Bandcamp or SoundCloud");

            if (influences is { Length: > MusicianRules.InfluencesMaxLength })
                return Validation("INVALID_INFLUENCES", $"Influences must have at most {MusicianRules.InfluencesMaxLength} characters");

            return null;
        }

        private MusicianProfileDto Map(MusicianProfile profile) => new()
        {
            Exists = true,
            IsMine = profile.UserId == _currentUser.UserId,
            Person = Summary(profile.User!),
            PhotoUrl = _fileStorage.PublicUrl(profile.PhotoKey),
            City = profile.User!.City == null ? null : CityAppService.Map(profile.User.City),
            CityName = profile.User.CityName,
            IsLookingForBand = profile.IsLookingForBand,
            LookingSince = profile.LookingSince,
            Goal = profile.Goal,
            Commitment = profile.Commitment,
            Reach = profile.Reach,
            HasGear = profile.HasGear,
            HasTransport = profile.HasTransport,
            HasRehearsalSpace = profile.HasRehearsalSpace,
            Influences = profile.Influences,
            Instruments = profile.Instruments.OrderBy(i => i.Position).Select(MapInstrument).ToList(),
            Genres = profile.Genres.Select(g => g.Genre).OrderBy(g => g).ToList(),
            Availability = profile.Availability
                .OrderBy(a => a.Day).ThenBy(a => a.Period)
                .Select(a => new AvailabilitySlotDto { Day = a.Day, Period = a.Period })
                .ToList(),
            SampleLinks = profile.SampleLinks.OrderBy(l => l.Position).Select(l => l.Url).ToList(),
        };

        private MusicianCardDto Card(MusicianProfile profile) => new()
        {
            Person = Summary(profile.User!),
            PhotoUrl = _fileStorage.PublicUrl(profile.PhotoKey),
            City = profile.User!.City == null ? null : CityAppService.Map(profile.User.City),
            CityName = profile.User.CityName,
            Instrument = profile.Instruments.OrderBy(i => i.Position).Select(MapInstrument).FirstOrDefault(),
            Genres = profile.Genres.Select(g => g.Genre).OrderBy(g => g).Take(MusicianRules.CardGenres).ToList(),
            HasSamples = profile.SampleLinks.Count > 0,
            LookingSince = profile.LookingSince,
        };

        private static MusicianInstrumentDto MapInstrument(MusicianInstrument instrument) => new()
        {
            Instrument = instrument.Instrument,
            OtherName = instrument.OtherName,
            Level = instrument.Level,
        };

        private ProfileSummaryDto Summary(User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Nickname = user.Nickname,
            AvatarUrl = _fileStorage.PublicUrl(user.AvatarKey),
        };

        private static Error ProfileRequired() =>
            new("MUSICIAN_PROFILE_REQUIRED", "Create your musician profile first", ErrorType.Conflict);

        private static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

        private static Result<MusicianProfileDto> Ok(MusicianProfileDto dto, string code, string message) =>
            Result<MusicianProfileDto>.Ok(dto, code, message);

        private static Result<MusicianProfileDto> Fail(Error error) => Result<MusicianProfileDto>.Fail(error);
    }
}
