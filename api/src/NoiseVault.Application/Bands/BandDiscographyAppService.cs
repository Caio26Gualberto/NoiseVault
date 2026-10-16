using NoiseVault.Application.Abstractions.Authentication;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Abstractions.Repository.UnitOfWork;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Profiles;
using NoiseVault.Application.Response;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Bands;

namespace NoiseVault.Application.Bands
{
    public class BandDiscographyAppService : IBandDiscographyAppService
    {
        private static readonly TimeSpan RefreshPause = TimeSpan.FromSeconds(1);

        private readonly IRepository<Band> _bandRepository;
        private readonly IRepository<BandRelease> _releaseRepository;
        private readonly IRepository<BandTrack> _trackRepository;
        private readonly IBandAppService _bandAppService;
        private readonly ISpotifyCatalog _spotify;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;
        private readonly ImageReplacement _images;
        private readonly IApplicationLogger _logger;

        public BandDiscographyAppService(
            IRepository<Band> bandRepository,
            IRepository<BandRelease> releaseRepository,
            IRepository<BandTrack> trackRepository,
            IBandAppService bandAppService,
            ISpotifyCatalog spotify,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork,
            IFileStorage fileStorage,
            IImageProcessor imageProcessor,
            IApplicationLoggerFactory loggerFactory)
        {
            _bandRepository = bandRepository;
            _releaseRepository = releaseRepository;
            _trackRepository = trackRepository;
            _bandAppService = bandAppService;
            _spotify = spotify;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _images = new ImageReplacement(fileStorage, imageProcessor);
            _logger = loggerFactory.Create(LogContext.Bands);
        }

        public async Task<Result<BandDiscographyDto>> Get(string slug)
        {
            var (bandId, role) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return Fail(BandNotFound());

            return Ok(await MapAsync(bandId.Value, role == EBandMemberRole.Owner), "DISCOGRAPHY_FOUND", "Discography found");
        }

        public async Task<Result<BandDiscographyDto>> Import(string slug)
        {
            _logger.SetOperation("ImportDiscography");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            if (!_spotify.IsConfigured)
                return Fail(new Error("SPOTIFY_NOT_CONFIGURED", "Spotify import is not configured on this server", ErrorType.Unexpected));

            var artistId = DiscographyRules.ParseSpotifyArtistId(band!.SpotifyUrl);

            if (artistId == null)
                return Fail(new Error("SPOTIFY_ARTIST_LINK_REQUIRED", "Set the band's Spotify link to the artist page (open.spotify.com/artist/...) before importing", ErrorType.Validation));

            try
            {
                await ImportAsync(band, artistId);
            }
            catch (SpotifyCatalogException ex)
            {
                return Fail(SpotifyError(ex));
            }

            return Ok(await MapAsync(band.Id, true), "DISCOGRAPHY_IMPORTED", "Discography imported from Spotify");
        }

        public async Task<int> RefreshStale()
        {
            _logger.SetOperation("RefreshDiscographies");

            if (!_spotify.IsConfigured)
                return 0;

            var threshold = DateTime.UtcNow.Subtract(DiscographyRules.RefreshInterval);
            var bands = await _bandRepository.ListAsync(new QueryOptions<Band>
            {
                Predicate = b => b.SpotifyArtistId != null && (b.DiscographySyncedAt == null || b.DiscographySyncedAt < threshold),
                OrderBy = q => q.OrderBy(b => b.DiscographySyncedAt).ThenBy(b => b.Id),
            });

            var refreshed = 0;

            foreach (var band in bands)
            {
                try
                {
                    await ImportAsync(band, band.SpotifyArtistId!);
                    refreshed++;
                }
                catch (SpotifyCatalogException ex) when (ex.Failure == ESpotifyFailure.RateLimited)
                {
                    _logger.Warning("DISCOGRAPHY_REFRESH_RATE_LIMITED", "Spotify rate limited the daily refresh; the rest waits for tomorrow", extra: new { Refreshed = refreshed, Pending = bands.Count - refreshed });
                    break;
                }
                catch (SpotifyCatalogException ex)
                {
                    _logger.Warning("DISCOGRAPHY_REFRESH_FAILED", "Discography refresh failed for one band", extra: new { BandId = band.Id, ex.Failure, ex.Message });
                }

                await Task.Delay(RefreshPause);
            }

            if (bands.Count > 0)
                _logger.Info("DISCOGRAPHIES_REFRESHED", "Discographies refreshed from Spotify", extra: new { Refreshed = refreshed, Candidates = bands.Count });

            return refreshed;
        }

        public async Task<Result<BandDiscographyDto>> CreateRelease(string slug, BandReleaseInputDto input)
        {
            _logger.SetOperation("CreateRelease");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            var (data, invalid) = Validate(input);

            if (invalid != null)
                return Fail(invalid);

            var count = await _releaseRepository.CountAsync(new QueryOptions<BandRelease> { Predicate = r => r.BandId == band!.Id });

            if (count >= DiscographyRules.MaxReleasesPerBand)
                return Fail(new Error("DISCOGRAPHY_LIMIT_REACHED", $"A band can have at most {DiscographyRules.MaxReleasesPerBand} releases", ErrorType.Conflict));

            var maxOrder = (await _releaseRepository.ListAsync(new QueryOptions<BandRelease> { Predicate = r => r.BandId == band!.Id, AsNoTracking = true }))
                .Select(r => r.Order)
                .DefaultIfEmpty(-1)
                .Max();

            var release = new BandRelease
            {
                BandId = band!.Id,
                Source = EReleaseSource.Manual,
                Order = maxOrder + 1,
                Tracks = data!.Tracks.Select((t, index) => new BandTrack
                {
                    Title = t.Title,
                    DiscNumber = 1,
                    Number = index + 1,
                    DurationSeconds = t.DurationSeconds,
                    ExternalUrl = t.ExternalUrl,
                }).ToList(),
            };

            ApplyManual(release, data);
            await _releaseRepository.AddAsync(release);

            _logger.Info("RELEASE_CREATED", "Manual release created", userId: _currentUser.UserId, extra: new { BandId = band.Id, ReleaseId = release.Id, Tracks = release.Tracks.Count });

            return Ok(await MapAsync(band.Id, true), "RELEASE_CREATED", "Release added");
        }

        public async Task<Result<BandDiscographyDto>> UpdateRelease(string slug, int releaseId, BandReleaseInputDto input)
        {
            _logger.SetOperation("UpdateRelease");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            var (data, invalid) = Validate(input);

            if (invalid != null)
                return Fail(invalid);

            var release = await FindReleaseAsync(band!.Id, releaseId, tracked: true);

            if (release == null)
                return Fail(ReleaseNotFound());

            if (release.Source != EReleaseSource.Manual)
                return Fail(NotEditable());

            ApplyManual(release, data!);

            var featuredTitle = release.Tracks.FirstOrDefault(t => t.IsFeatured)?.Title;

            foreach (var track in release.Tracks.ToList())
                await _trackRepository.DeleteAsync(track);

            release.Tracks.Clear();
            release.Tracks.AddRange(data!.Tracks.Select((t, index) => new BandTrack
            {
                BandReleaseId = release.Id,
                Title = t.Title,
                DiscNumber = 1,
                Number = index + 1,
                DurationSeconds = t.DurationSeconds,
                ExternalUrl = t.ExternalUrl,
                IsFeatured = featuredTitle != null && string.Equals(t.Title, featuredTitle, StringComparison.OrdinalIgnoreCase),
            }));

            await _releaseRepository.UpdateAsync(release);

            return Ok(await MapAsync(band.Id, true), "RELEASE_UPDATED", "Release updated");
        }

        public async Task<Result<BandDiscographyDto>> DeleteRelease(string slug, int releaseId)
        {
            _logger.SetOperation("DeleteRelease");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            var release = await FindReleaseAsync(band!.Id, releaseId, tracked: true);

            if (release == null)
                return Fail(ReleaseNotFound());

            if (release.Source != EReleaseSource.Manual)
                return Fail(NotEditable());

            await RemoveReleaseAsync(release);

            _logger.Info("RELEASE_DELETED", "Manual release deleted", userId: _currentUser.UserId, extra: new { BandId = band.Id, ReleaseId = releaseId });

            return Ok(await MapAsync(band.Id, true), "RELEASE_DELETED", "Release removed");
        }

        public async Task<Result<BandDiscographyDto>> SetCover(string slug, int releaseId, ImageUploadInput? file, CancellationToken cancellationToken = default)
        {
            _logger.SetOperation("SetReleaseCover");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            var release = await FindReleaseAsync(band!.Id, releaseId, tracked: true);

            if (release == null)
                return Fail(ReleaseNotFound());

            if (release.Source != EReleaseSource.Manual)
                return Fail(NotEditable());

            var upload = await _images.UploadAsync(file, EImagePreset.Avatar, $"bands/{band.Id}/releases", cancellationToken);

            if (!upload.Succeeded)
                return Fail(upload.Error!);

            var oldKey = release.CoverKey;
            release.CoverKey = upload.Key;

            await _images.SaveAsync(upload.Key!, oldKey, () => _releaseRepository.UpdateAsync(release));

            return Ok(await MapAsync(band.Id, true), "RELEASE_COVER_UPDATED", "Cover updated");
        }

        public async Task<Result<BandDiscographyDto>> Reorder(string slug, DiscographyOrderInputDto input)
        {
            _logger.SetOperation("ReorderDiscography");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            var releases = await _releaseRepository.ListAsync(new QueryOptions<BandRelease> { Predicate = r => r.BandId == band!.Id });
            var ids = input.ReleaseIds ?? [];

            if (ids.Count != releases.Count || ids.Distinct().Count() != ids.Count || releases.Any(r => !ids.Contains(r.Id)))
                return Fail(new Error("INVALID_RELEASE_ORDER", "Send every release of the band exactly once", ErrorType.Validation));

            foreach (var release in releases)
            {
                release.Order = ids.IndexOf(release.Id);
                await _releaseRepository.UpdateAsync(release);
            }

            return Ok(await MapAsync(band!.Id, true), "DISCOGRAPHY_REORDERED", "Order saved");
        }

        public async Task<Result<BandDiscographyDto>> SetHidden(string slug, int releaseId, ReleaseVisibilityInputDto input)
        {
            _logger.SetOperation("SetReleaseVisibility");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            var release = await FindReleaseAsync(band!.Id, releaseId, tracked: true);

            if (release == null)
                return Fail(ReleaseNotFound());

            release.IsHidden = input.Hidden;
            await _releaseRepository.UpdateAsync(release);

            return Ok(await MapAsync(band.Id, true), "RELEASE_VISIBILITY_UPDATED", input.Hidden ? "Release hidden" : "Release visible");
        }

        public async Task<Result<BandDiscographyDto>> SetFeatured(string slug, FeaturedTrackInputDto input)
        {
            _logger.SetOperation("SetFeaturedTrack");

            var (band, error) = await OwnedBandAsync(slug);

            if (error != null)
                return Fail(error);

            var tracks = await _trackRepository.ListAsync(new QueryOptions<BandTrack>
            {
                Predicate = t => t.Release!.BandId == band!.Id && !t.Release.IsDeleted,
            });

            if (input.TrackId != null && tracks.All(t => t.Id != input.TrackId))
                return Fail(new Error("TRACK_NOT_FOUND", "Track not found in this band", ErrorType.NotFound));

            foreach (var track in tracks.Where(t => t.IsFeatured != (t.Id == input.TrackId)))
            {
                track.IsFeatured = track.Id == input.TrackId;
                await _trackRepository.UpdateAsync(track);
            }

            return Ok(await MapAsync(band!.Id, true), "FEATURED_TRACK_UPDATED", input.TrackId == null ? "Featured track cleared" : "Featured track set");
        }

        private async Task ImportAsync(Band band, string artistId)
        {
            var albums = await _spotify.GetArtistAlbumsAsync(artistId);
            var deduped = albums
                .Select(a => (Album: a, Year: ParseYear(a.ReleaseDate)))
                .GroupBy(x => DiscographyRules.DedupeKey(x.Album.Name, x.Year))
                .Select(g => g.OrderByDescending(x => x.Album.Tracks.Count).ThenBy(x => x.Album.Id, StringComparer.Ordinal).First())
                .OrderByDescending(x => x.Album.ReleaseDate, StringComparer.Ordinal)
                .ThenBy(x => x.Album.Name, StringComparer.Ordinal)
                .ToList();

            var existing = await _releaseRepository.ListAsync(new QueryOptions<BandRelease>
            {
                Predicate = r => r.BandId == band.Id,
                Includes = [r => r.Tracks],
            });

            var bySpotifyId = existing.Where(r => r.SpotifyAlbumId != null).ToDictionary(r => r.SpotifyAlbumId!);
            var nextOrder = existing.Select(r => r.Order).DefaultIfEmpty(-1).Max() + 1;
            var added = 0;
            var updated = 0;
            var removed = 0;
            var now = DateTime.UtcNow;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var seen = new HashSet<string>();
                var manualCount = existing.Count(r => r.SpotifyAlbumId == null);

                foreach (var (album, year) in deduped)
                {
                    if (!bySpotifyId.ContainsKey(album.Id) && manualCount + seen.Count >= DiscographyRules.MaxReleasesPerBand)
                        break;

                    seen.Add(album.Id);
                    var tracks = album.Tracks.Take(DiscographyRules.MaxTracksPerRelease).ToList();

                    if (bySpotifyId.TryGetValue(album.Id, out var release))
                    {
                        var featuredId = release.Tracks.FirstOrDefault(t => t.IsFeatured)?.SpotifyTrackId;
                        ApplySpotify(release, album, year);

                        foreach (var track in release.Tracks.ToList())
                            await _trackRepository.DeleteAsync(track);

                        release.Tracks.Clear();
                        release.Tracks.AddRange(tracks.Select(t => ToTrack(t, release.Id, featuredId)));
                        await _releaseRepository.UpdateAsync(release);
                        updated++;
                        continue;
                    }

                    var created = new BandRelease
                    {
                        BandId = band.Id,
                        Source = EReleaseSource.Spotify,
                        SpotifyAlbumId = album.Id,
                        Order = nextOrder++,
                        Tracks = tracks.Select(t => ToTrack(t, 0, null)).ToList(),
                    };

                    ApplySpotify(created, album, year);
                    await _releaseRepository.AddAsync(created);
                    added++;
                }

                foreach (var gone in existing.Where(r => r.SpotifyAlbumId != null && !seen.Contains(r.SpotifyAlbumId)))
                {
                    await RemoveReleaseAsync(gone);
                    removed++;
                }

                band.SpotifyArtistId = artistId;
                band.DiscographySyncedAt = now;
                await _bandRepository.UpdateAsync(band);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info("DISCOGRAPHY_IMPORTED", "Discography imported from Spotify", userId: _currentUser.UserIdOrNull, extra: new { BandId = band.Id, Added = added, Updated = updated, Removed = removed });
        }

        private async Task RemoveReleaseAsync(BandRelease release)
        {
            foreach (var track in release.Tracks.ToList())
                await _trackRepository.DeleteAsync(track);

            await _releaseRepository.SoftDelete(release);

            if (release.CoverKey != null)
                await _images.DeleteAsync(release.CoverKey);
        }

        private static void ApplySpotify(BandRelease release, SpotifyAlbum album, int year)
        {
            release.Title = DiscographyRules.NormalizeTitle(album.Name);
            release.Kind = album.AlbumType switch
            {
                "single" => album.TotalTracks >= 4 ? EReleaseKind.Ep : EReleaseKind.Single,
                _ => EReleaseKind.Album,
            };
            release.Year = year;
            release.ReleaseDate = ParseDate(album.ReleaseDate, album.ReleaseDatePrecision);
            release.CoverUrl = album.CoverUrl;
            release.ExternalUrl = album.Url;
        }

        private static BandTrack ToTrack(SpotifyTrack track, int releaseId, string? featuredSpotifyId) => new()
        {
            BandReleaseId = releaseId,
            Title = DiscographyRules.NormalizeTitle(track.Name),
            DiscNumber = track.DiscNumber,
            Number = track.Number,
            DurationSeconds = track.DurationMs > 0 ? (int)Math.Round(track.DurationMs / 1000d) : null,
            SpotifyTrackId = track.Id,
            ExternalUrl = track.Url,
            IsFeatured = featuredSpotifyId != null && featuredSpotifyId == track.Id,
        };

        private static int ParseYear(string releaseDate) =>
            releaseDate.Length >= 4 && int.TryParse(releaseDate[..4], out var year) ? year : DiscographyRules.MinYear;

        private static DateTime? ParseDate(string releaseDate, string precision) =>
            precision == "day" && DateTime.TryParse(releaseDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var date)
                ? date
                : null;

        private sealed record ValidTrack(string Title, string? ExternalUrl, int? DurationSeconds);

        private sealed record ValidRelease(string Title, EReleaseKind Kind, int Year, string? ExternalUrl, List<ValidTrack> Tracks);

        private static (ValidRelease? Data, Error? Error) Validate(BandReleaseInputDto input)
        {
            var title = DiscographyRules.NormalizeTitle(input.Title);

            if (title.Length is < DiscographyRules.TitleMinLength or > DiscographyRules.TitleMaxLength)
                return (null, new Error("INVALID_RELEASE_TITLE", $"Title must have between {DiscographyRules.TitleMinLength} and {DiscographyRules.TitleMaxLength} characters", ErrorType.Validation));

            if (!Enum.IsDefined(input.Kind))
                return (null, new Error("INVALID_RELEASE_KIND", "Unknown release kind", ErrorType.Validation));

            if (input.Year < DiscographyRules.MinYear || input.Year > AppTime.Today.Year)
                return (null, new Error("INVALID_RELEASE_YEAR", $"Year must be between {DiscographyRules.MinYear} and the current year", ErrorType.Validation));

            var url = ProfileRules.NormalizeOptionalText(input.ExternalUrl);

            if (!DiscographyRules.IsValidExternalUrl(url))
                return (null, InvalidUrl());

            var tracks = new List<ValidTrack>();

            foreach (var track in input.Tracks ?? [])
            {
                var trackTitle = DiscographyRules.NormalizeTitle(track.Title);

                if (trackTitle.Length is < DiscographyRules.TitleMinLength or > DiscographyRules.TitleMaxLength)
                    return (null, new Error("INVALID_TRACK_TITLE", $"Each track needs a title with up to {DiscographyRules.TitleMaxLength} characters", ErrorType.Validation));

                var trackUrl = ProfileRules.NormalizeOptionalText(track.ExternalUrl);

                if (!DiscographyRules.IsValidExternalUrl(trackUrl))
                    return (null, InvalidUrl());

                tracks.Add(new ValidTrack(trackTitle, trackUrl, track.DurationSeconds is > 0 ? track.DurationSeconds : null));
            }

            if (tracks.Count > DiscographyRules.MaxTracksPerRelease)
                return (null, new Error("INVALID_TRACK_TITLE", $"A release can have at most {DiscographyRules.MaxTracksPerRelease} tracks", ErrorType.Validation));

            return (new ValidRelease(title, input.Kind, input.Year, url, tracks), null);
        }

        private static void ApplyManual(BandRelease release, ValidRelease data)
        {
            release.Title = data.Title;
            release.Kind = data.Kind;
            release.Year = data.Year;
            release.ExternalUrl = data.ExternalUrl;
        }

        private async Task<(Band? Band, Error? Error)> OwnedBandAsync(string slug)
        {
            var (bandId, role) = await _bandAppService.FindAccess(slug);

            if (bandId == null)
                return (null, BandNotFound());

            if (role != EBandMemberRole.Owner)
                return (null, new Error("BAND_FORBIDDEN", "Only band owners can do this", ErrorType.Forbidden));

            var band = await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band> { Predicate = b => b.Id == bandId });

            return (band, null);
        }

        private Task<BandRelease?> FindReleaseAsync(int bandId, int releaseId, bool tracked) =>
            _releaseRepository.FirstOrDefaultAsync(new QueryOptions<BandRelease>
            {
                Predicate = r => r.Id == releaseId && r.BandId == bandId,
                Includes = [r => r.Tracks],
                AsNoTracking = !tracked,
            });

        private async Task<BandDiscographyDto> MapAsync(int bandId, bool owner)
        {
            var band = (await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band> { Predicate = b => b.Id == bandId, AsNoTracking = true }))!;
            var releases = await _releaseRepository.ListAsync(new QueryOptions<BandRelease>
            {
                Predicate = r => r.BandId == bandId && (owner || !r.IsHidden),
                Includes = [r => r.Tracks],
                OrderBy = q => q.OrderBy(r => r.Order).ThenBy(r => r.Id),
                AsNoTracking = true,
            });

            var mapped = releases.Select(MapRelease).ToList();
            var featured = releases
                .SelectMany(r => r.Tracks.Where(t => t.IsFeatured).Select(t => (Release: r, Track: t)))
                .FirstOrDefault();

            return new BandDiscographyDto
            {
                SyncedAt = band.DiscographySyncedAt,
                SpotifyArtistId = band.SpotifyArtistId,
                SpotifyArtistUrl = band.SpotifyArtistId == null ? null : $"https://open.spotify.com/artist/{band.SpotifyArtistId}",
                CanEdit = owner,
                CanImport = owner && _spotify.IsConfigured && DiscographyRules.ParseSpotifyArtistId(band.SpotifyUrl) != null,
                FeaturedTrack = featured.Track == null
                    ? null
                    : new FeaturedTrackDto
                    {
                        Track = MapTrack(featured.Track),
                        ReleaseId = featured.Release.Id,
                        ReleaseTitle = featured.Release.Title,
                        CoverUrl = CoverOf(featured.Release),
                    },
                Releases = mapped,
            };
        }

        private BandReleaseDto MapRelease(BandRelease release) => new()
        {
            Id = release.Id,
            Title = release.Title,
            Kind = release.Kind,
            Year = release.Year,
            ReleaseDate = release.ReleaseDate,
            CoverUrl = CoverOf(release),
            Source = release.Source,
            SpotifyAlbumId = release.SpotifyAlbumId,
            ExternalUrl = release.ExternalUrl,
            Order = release.Order,
            IsHidden = release.IsHidden,
            Tracks = release.Tracks.OrderBy(t => t.DiscNumber).ThenBy(t => t.Number).ThenBy(t => t.Id).Select(MapTrack).ToList(),
        };

        private static BandTrackDto MapTrack(BandTrack track) => new()
        {
            Id = track.Id,
            Title = track.Title,
            DiscNumber = track.DiscNumber,
            Number = track.Number,
            DurationSeconds = track.DurationSeconds,
            SpotifyTrackId = track.SpotifyTrackId,
            ExternalUrl = track.ExternalUrl,
            IsFeatured = track.IsFeatured,
        };

        private string? CoverOf(BandRelease release) =>
            release.Source == EReleaseSource.Spotify ? release.CoverUrl : _fileStorage.PublicUrl(release.CoverKey);

        private static Error SpotifyError(SpotifyCatalogException ex) => ex.Failure switch
        {
            ESpotifyFailure.NotConfigured => new Error("SPOTIFY_NOT_CONFIGURED", "Spotify import is not configured on this server", ErrorType.Unexpected),
            ESpotifyFailure.NotFound => new Error("SPOTIFY_ARTIST_NOT_FOUND", "Spotify did not find this artist", ErrorType.NotFound),
            ESpotifyFailure.RateLimited => new Error("SPOTIFY_RATE_LIMITED", "Spotify is limiting requests right now; try again in a few minutes", ErrorType.Conflict),
            _ => new Error("SPOTIFY_UNAVAILABLE", "Spotify did not answer; try again later", ErrorType.Unexpected),
        };

        private static Result<BandDiscographyDto> Ok(BandDiscographyDto data, string code, string message) => Result<BandDiscographyDto>.Ok(data, code, message);

        private static Result<BandDiscographyDto> Fail(Error error) => Result<BandDiscographyDto>.Fail(error);

        private static Error InvalidUrl() => new("INVALID_EXTERNAL_URL", "Links must be http(s) addresses with up to 500 characters", ErrorType.Validation);

        private static Error BandNotFound() => new("BAND_NOT_FOUND", "Band not found", ErrorType.NotFound);

        private static Error ReleaseNotFound() => new("RELEASE_NOT_FOUND", "Release not found", ErrorType.NotFound);

        private static Error NotEditable() => new("RELEASE_NOT_EDITABLE", "Releases imported from Spotify can only be hidden or reordered", ErrorType.Conflict);
    }
}
