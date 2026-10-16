using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Geo;
using NoiseVault.Domain.Constants;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Media;
using NoiseVault.Domain.Enums.Profiles;
using NoiseVault.Infra.Data.Identity;

namespace NoiseVault.Infra.Data.Context.Seeding
{
    public partial class DevSeed
    {
        public const string EmailDomain = "noisevault.dev";
        private const string Password = "12341234";
        private const string MarkerSlug = "ferrugem";

        private const int SantoAndre = 3547809;
        private const int SaoBernardo = 3548708;
        private const int SaoCaetano = 3548807;
        private const int Diadema = 3513801;
        private const int Maua = 3529401;
        private const int RibeiraoPires = 3543303;
        private const int RioGrandeDaSerra = 3544103;

        private static readonly string[] BandSlugs = ["ferrugem", "pogo-mortal", "vulto", "cinzas"];

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DevSeedMedia _media;
        private readonly IApplicationLogger _logger;

        public DevSeed(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            DevSeedMedia media,
            IApplicationLoggerFactory loggerFactory)
        {
            _context = context;
            _userManager = userManager;
            _media = media;
            _logger = loggerFactory.Create(LogContext.Api);
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            _logger.SetOperation("DevSeed");

            try
            {
                var admin = await _context.Users.Include(u => u.Genres).FirstOrDefaultAsync(u => u.Email == SeedData.AdminEmail, cancellationToken);

                if (admin == null)
                {
                    _logger.Warning("DEV_SEED_SKIPPED", "Dev seed skipped because the admin account does not exist");
                    return;
                }

                var people = await EnsurePeopleAsync(admin, cancellationToken);
                var created = false;

                if (!await _context.Bands.AnyAsync(b => b.Slug == MarkerSlug, cancellationToken))
                {
                    await CreateContentAsync(people, cancellationToken);
                    created = true;
                }

                var restored = await EnsureStoredMediaAsync(people.Values.Select(u => u.Id).ToList(), cancellationToken);

                _logger.Info("DEV_SEED_APPLIED", "Dev seed applied", extra: new { ContentCreated = created, MediaRestored = restored });
            }
            catch (Exception ex)
            {
                _logger.Error("DEV_SEED_FAILED", "Dev seed failed; the API keeps booting without it", exception: ex);
            }
        }

        private sealed record Person(
            string Nickname,
            string Name,
            string Bio,
            int CityId,
            string? Instrument,
            EMusicGenre[] Genres,
            string? Instagram = null,
            string? Youtube = null);

        private static readonly Person[] People =
        [
            new("lucia.ferraz", "Lúcia Ferraz", "Vocal da Ferrugem desde 98. A fita K7 do primeiro ensaio ainda roda aqui em casa.", SantoAndre, "Vocal", [EMusicGenre.Thrash, EMusicGenre.Heavy, EMusicGenre.Speed], "https://instagram.com/lucia.ferraz"),
            new("rafa.tavares", "Rafael Tavares", "Guitarra da Cinzas. Gravo as demos no quarto dos fundos, com o ar-condicionado desligado.", SaoCaetano, "Guitarra", [EMusicGenre.Death, EMusicGenre.MelodicDeath, EMusicGenre.Black], null, "https://youtube.com/@rafatavares"),
            new("marina.k", "Marina Kobayashi", "Baqueta e berro na Pogo Mortal. Organizo o Pogo na Praça todo ano.", SaoBernardo, "Bateria", [EMusicGenre.Punk, EMusicGenre.Hardcore, EMusicGenre.Crossover], "https://instagram.com/marina.k"),
            new("denis.moura", "Dênis Moura", "Grave lento e pesado. Vulto. Colecionador de pedal que não cabe mais na mochila.", Diadema, "Baixo", [EMusicGenre.Doom, EMusicGenre.Sludge, EMusicGenre.Stoner]),
            new("paty.lins", "Patrícia Lins", "Fotógrafa de pista. Se o show rolou no ABC, eu tava lá com a câmera e o tampão de ouvido.", Maua, null, [EMusicGenre.Hardcore, EMusicGenre.Punk, EMusicGenre.Grunge], "https://instagram.com/paty.lins"),
            new("jorge.okamoto", "Jorge Okamoto", "Guitarrista procurando banda. Vinte anos de palco, do bar da esquina ao festival.", RibeiraoPires, "Guitarra", [EMusicGenre.Heavy, EMusicGenre.ClassicRock, EMusicGenre.HardRock]),
            new("bia.santana", "Bia Santana", "Teclado e voz. Entrei na Vulto em 2024 e trouxe o lado mais sombrio do som.", SantoAndre, "Teclado", [EMusicGenre.Gothic, EMusicGenre.Symphonic, EMusicGenre.Doom], null, "https://youtube.com/@biasantana"),
            new("tiago.nunes", "Tiago Nunes", "Blast beat é meditação. Bateria na Pogo Mortal e na Cinzas.", RioGrandeDaSerra, "Bateria", [EMusicGenre.Grindcore, EMusicGenre.Deathcore, EMusicGenre.Hardcore]),
        ];

        private async Task<Dictionary<string, User>> EnsurePeopleAsync(User admin, CancellationToken cancellationToken)
        {
            var cities = await _context.Cities
                .Where(c => c.Id == SantoAndre || c.Id == SaoBernardo || c.Id == SaoCaetano || c.Id == Diadema || c.Id == Maua || c.Id == RibeiraoPires || c.Id == RioGrandeDaSerra)
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            var people = new Dictionary<string, User> { ["admin"] = admin };

            if (admin.AvatarKey == null)
            {
                admin.Bio ??= "Guitarra da Ferrugem desde 98 e curadoria do mural. Se tiver foto antiga da cena, manda pra cá.";
                admin.CityId ??= SantoAndre;
                admin.MainInstrument ??= "Guitarra";
                admin.AvatarKey = DevSeedMedia.ImageKey($"users/{admin.Id}/avatar", "avatar");
                admin.BannerKey = DevSeedMedia.ImageKey($"users/{admin.Id}/banner", "banner");
                ApplyLocation(admin, cities);

                if (admin.Genres.Count == 0)
                    admin.Genres.AddRange([new UserGenre { Genre = EMusicGenre.Thrash }, new UserGenre { Genre = EMusicGenre.Heavy }, new UserGenre { Genre = EMusicGenre.Hardcore }]);

                await _context.SaveChangesAsync(cancellationToken);
            }

            foreach (var person in People)
            {
                var email = $"{person.Nickname}@{EmailDomain}";
                var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

                if (existing != null)
                {
                    people[person.Nickname] = existing;
                    continue;
                }

                people[person.Nickname] = await CreatePersonAsync(person, email, cities, cancellationToken);
            }

            return people;
        }

        private async Task<User> CreatePersonAsync(Person person, string email, Dictionary<int, City> cities, CancellationToken cancellationToken)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var user = new User
            {
                Email = email,
                Name = person.Name,
                Nickname = person.Nickname,
                Bio = person.Bio,
                CityId = person.CityId,
                MainInstrument = person.Instrument,
                InstagramUrl = person.Instagram,
                YoutubeUrl = person.Youtube,
                Genres = person.Genres.Select(g => new UserGenre { Genre = g }).ToList(),
            };

            ApplyLocation(user, cities);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            user.AvatarKey = DevSeedMedia.ImageKey($"users/{user.Id}/avatar", "avatar");
            user.BannerKey = DevSeedMedia.ImageKey($"users/{user.Id}/banner", "banner");
            await _context.SaveChangesAsync(cancellationToken);

            var applicationUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                UserId = user.Id,
            };

            var result = await _userManager.CreateAsync(applicationUser, Password);

            if (!result.Succeeded)
                throw new InvalidOperationException($"O seed não conseguiu criar a conta '{person.Nickname}': {string.Join("; ", result.Errors.Select(e => e.Description))}");

            await _userManager.AddToRoleAsync(applicationUser, Roles.User);
            await transaction.CommitAsync(cancellationToken);

            return user;
        }

        private static void ApplyLocation(User user, Dictionary<int, City> cities)
        {
            if (user.CityId == null || !cities.TryGetValue(user.CityId.Value, out var city) || city.Latitude == null || city.Longitude == null)
                return;

            var snapped = GeoRules.Snap(city.Latitude.Value, city.Longitude.Value);
            user.ApproxLatitude = snapped.Latitude;
            user.ApproxLongitude = snapped.Longitude;
            user.ApproxLocationUpdatedAt = DateTime.UtcNow;
        }

        private async Task<int> EnsureStoredMediaAsync(List<int> ownerIds, CancellationToken cancellationToken)
        {
            var restored = 0;

            var users = await _context.Users.Where(u => ownerIds.Contains(u.Id)).ToListAsync(cancellationToken);
            foreach (var user in users)
            {
                restored += await RestoreImageAsync(user.AvatarKey, cancellationToken);
                restored += await RestoreImageAsync(user.BannerKey, cancellationToken);
            }

            var bands = await _context.Bands.Where(b => BandSlugs.Contains(b.Slug)).ToListAsync(cancellationToken);
            foreach (var band in bands)
            {
                restored += await RestoreImageAsync(band.AvatarKey, cancellationToken);
                restored += await RestoreImageAsync(band.BannerKey, cancellationToken);
            }

            var events = await _context.Events.Where(e => ownerIds.Contains(e.OrganizerUserId) && e.BannerKey != null).ToListAsync(cancellationToken);
            foreach (var item in events)
                restored += await RestoreImageAsync(item.BannerKey, cancellationToken);

            var bandIds = bands.Select(b => b.Id).ToList();
            var releases = await _context.BandReleases.Where(r => bandIds.Contains(r.BandId) && r.CoverKey != null).ToListAsync(cancellationToken);
            foreach (var release in releases)
                restored += await RestoreImageAsync(release.CoverKey, cancellationToken);

            var musicians = await _context.MusicianProfiles.Where(p => ownerIds.Contains(p.UserId) && p.PhotoKey != null).ToListAsync(cancellationToken);
            foreach (var musician in musicians)
                restored += await RestoreImageAsync(musician.PhotoKey, cancellationToken);

            var uploads = await _context.MediaUploads
                .Where(m => ownerIds.Contains(m.OwnerUserId) && m.Status == EMediaStatus.Ready && m.StorageKey != null && m.StorageKey.Contains("/seed-"))
                .ToListAsync(cancellationToken);

            foreach (var upload in uploads)
            {
                try
                {
                    if (upload.Kind == EMediaKind.Photo)
                    {
                        var size = await _media.EnsureImageAsync(upload.StorageKey!, cancellationToken);

                        if (size == null)
                            continue;

                        upload.Width = size.Value.Width;
                        upload.Height = size.Value.Height;
                        restored++;
                        continue;
                    }

                    var folder = upload.StorageKey![..upload.StorageKey.LastIndexOf('/')];
                    var video = await _media.EnsureVideoAsync(folder, cancellationToken);

                    if (video == null)
                        continue;

                    upload.Width = video.Value.Width;
                    upload.Height = video.Value.Height;
                    upload.DurationSeconds = video.Value.DurationSeconds;
                    upload.SizeBytes = video.Value.SizeBytes;
                    upload.PosterKey = DevSeedMedia.PosterKey(folder);
                    restored++;
                }
                catch (Exception ex)
                {
                    _logger.Error("DEV_SEED_MEDIA_FAILED", "Dev seed could not restore a media file; the rest continues", exception: ex, extra: new { MediaId = upload.Id, upload.Kind, Key = upload.StorageKey });
                }
            }

            if (_context.ChangeTracker.HasChanges())
                await _context.SaveChangesAsync(cancellationToken);

            return restored;
        }

        private async Task<int> RestoreImageAsync(string? key, CancellationToken cancellationToken)
        {
            if (key == null || !key.Contains("/seed-", StringComparison.Ordinal))
                return 0;

            try
            {
                return await _media.EnsureImageAsync(key, cancellationToken) == null ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger.Error("DEV_SEED_MEDIA_FAILED", "Dev seed could not restore a media file; the rest continues", exception: ex, extra: new { Key = key });
                return 0;
            }
        }
    }
}
