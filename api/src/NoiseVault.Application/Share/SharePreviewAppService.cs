using System.Globalization;
using System.Text.RegularExpressions;
using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Auth;
using NoiseVault.Application.Bands;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Interfaces;
using NoiseVault.Application.Media.Interfaces;
using NoiseVault.Application.Mentions.Interfaces;
using NoiseVault.Application.Posts.Interfaces;
using NoiseVault.Application.Share.Dtos;
using NoiseVault.Application.Share.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Events;
using NoiseVault.Domain.Enums.Media;
using static NoiseVault.Application.Share.PreviewText;

namespace NoiseVault.Application.Share
{
    public class SharePreviewAppService : ISharePreviewAppService
    {
        private const int AnonymousViewer = 0;
        private const int DescriptionMaxLength = 200;

        private readonly IArchiveRepository _archiveRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IPostRepository _postRepository;
        private readonly IRepository<Band> _bandRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<MediaUpload> _mediaRepository;
        private readonly IRepository<PostMedia> _postMediaRepository;
        private readonly IMediaUploadAppService _mediaAppService;
        private readonly IFileStorage _fileStorage;
        private readonly IMentionAppService _mentionAppService;

        public SharePreviewAppService(
            IArchiveRepository archiveRepository,
            IEventRepository eventRepository,
            IPostRepository postRepository,
            IRepository<Band> bandRepository,
            IRepository<User> userRepository,
            IRepository<MediaUpload> mediaRepository,
            IRepository<PostMedia> postMediaRepository,
            IMediaUploadAppService mediaAppService,
            IFileStorage fileStorage,
            IMentionAppService mentionAppService)
        {
            _archiveRepository = archiveRepository;
            _eventRepository = eventRepository;
            _postRepository = postRepository;
            _bandRepository = bandRepository;
            _userRepository = userRepository;
            _mediaRepository = mediaRepository;
            _postMediaRepository = postMediaRepository;
            _mediaAppService = mediaAppService;
            _fileStorage = fileStorage;
            _mentionAppService = mentionAppService;
        }

        public Task<SharePreviewDto?> Build(string section, string key) => section switch
        {
            "mural" when int.TryParse(key, out var id) => ArchiveAsync(id),
            "eventos" when int.TryParse(key, out var id) => EventAsync(id),
            "feed" when int.TryParse(key, out var id) => PostAsync(id),
            "bandas" => BandAsync(key),
            "perfil" => ProfileAsync(key),
            _ => Task.FromResult<SharePreviewDto?>(null),
        };

        private async Task<SharePreviewDto?> ArchiveAsync(int id)
        {
            var entry = await _archiveRepository.FindAsync(id, AnonymousViewer);

            if (entry == null)
                return null;

            var where = string.Join(" · ", new[] { entry.Venue, entry.City?.Name ?? entry.CityName }.Where(p => !string.IsNullOrWhiteSpace(p)));
            var bands = await BandNamesAsync(entry.Bands.Select(b => (b.BandId, b.Name)));
            var lead = Line(
                ArchiveDate(entry.Year, entry.Month, entry.Day),
                where,
                bands.Count > 0 ? string.Join(", ", bands) : null,
                Count(entry.PhotoCount, "foto", "fotos"),
                Count(entry.VideoCount, "vídeo", "vídeos"),
                Count(entry.AttendanceCount, "pessoa estava lá", "pessoas estavam lá"));

            return new SharePreviewDto
            {
                Title = entry.Title,
                Description = Join(lead, entry.Description, "Mural das antigas do Noise Vault"),
                ImageUrl = await MediaImageAsync(entry.CoverMediaUploadId),
                Path = $"/mural/{id}",
                Type = "article",
            };
        }

        private async Task<SharePreviewDto?> EventAsync(int id)
        {
            var row = await _eventRepository.FindAsync(id, AnonymousViewer);

            if (row == null)
                return null;

            var when = EventWhen(row.StartsAt);
            var where = string.Join(" · ", new[] { row.Venue, row.City?.Name ?? row.CityName }.Where(p => !string.IsNullOrWhiteSpace(p)));
            var cancelled = row.Status == EEventStatus.Cancelled;
            var title = cancelled ? $"[Cancelado] {row.Title}" : row.Title;
            var lineup = await BandNamesAsync(row.Lineup.OrderBy(l => l.Order).Select(l => (l.BandId, l.Name)));
            var lead = Line(
                when,
                where,
                lineup.Count > 0 ? $"com {string.Join(", ", lineup)}" : null,
                cancelled ? null : Count(row.InterestedCount, "pessoa vai", "pessoas vão"));

            return new SharePreviewDto
            {
                Title = title,
                Description = Join(lead, row.Description, "Agenda do Noise Vault"),
                ImageUrl = _fileStorage.PublicUrl(row.BannerKey),
                Path = $"/eventos/{id}",
                Type = "article",
            };
        }

        private async Task<SharePreviewDto?> PostAsync(int id)
        {
            var post = await _postRepository.FindVisibleAsync(id, AnonymousViewer);

            if (post == null || post.PublishedAt == null)
                return null;

            if (post.ArchiveEntryId != null)
                return await ArchiveAsync(post.ArchiveEntryId.Value);

            if (post.EventId != null)
                return await EventAsync(post.EventId.Value);

            var media = await _postMediaRepository.ListAsync(new QueryOptions<PostMedia>
            {
                Predicate = pm => pm.PostId == id,
                Includes = [pm => pm.MediaUpload!],
                OrderBy = q => q.OrderBy(pm => pm.Order),
                AsNoTracking = true,
            });

            var ready = media.Select(pm => pm.MediaUpload!).Where(m => m.Status == EMediaStatus.Ready).ToList();
            var first = ready.FirstOrDefault();
            var lead = Line(
                Count(ready.Count(m => m.Kind == EMediaKind.Photo), "foto", "fotos"),
                Count(ready.Count(m => m.Kind == EMediaKind.Video), "vídeo", "vídeos"),
                Count(post.LikeCount, "curtida", "curtidas"),
                Count(post.CommentCount, "comentário", "comentários"),
                AppTime.ToLocal(post.PublishedAt.Value).ToString("d 'de' MMMM 'de' yyyy", PtBr));
            var text = Excerpt(await WithNamesAsync(post.Content));

            return new SharePreviewDto
            {
                Title = $"{post.AuthorName} (@{post.AuthorNickname}) no Noise Vault",
                Description = text != null ? $"{lead}: “{text}”" : $"{lead}. Veja a publicação no Noise Vault, a cena do ABC num lugar só.",
                ImageUrl = first != null ? ImageOf(first) : _fileStorage.PublicUrl(post.AuthorAvatarKey),
                Path = $"/feed/{id}",
                Type = "article",
            };
        }

        private async Task<SharePreviewDto?> BandAsync(string slug)
        {
            var normalized = BandRules.NormalizeSlug(slug);
            var band = await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band>
            {
                Predicate = b => b.Slug == normalized,
                Includes = [b => b.City!],
                AsNoTracking = true,
            });

            if (band == null)
                return null;

            var lead = Line("Banda", band.City?.Name ?? band.CityName, band.FoundedYear != null ? $"desde {band.FoundedYear}" : null);

            return new SharePreviewDto
            {
                Title = band.Name,
                Description = Join(lead, band.Description ?? "Conheça a banda no Noise Vault, a cena do ABC num lugar só.", lead),
                ImageUrl = _fileStorage.PublicUrl(band.BannerKey) ?? _fileStorage.PublicUrl(band.AvatarKey),
                Path = $"/bandas/{band.Slug}",
                Type = "profile",
            };
        }

        private async Task<SharePreviewDto?> ProfileAsync(string nickname)
        {
            var normalized = NicknameRules.Normalize(nickname);
            var user = await _userRepository.FirstOrDefaultAsync(new QueryOptions<User> { Predicate = u => u.Nickname == normalized && u.IsActive, AsNoTracking = true });

            if (user == null)
                return null;

            return new SharePreviewDto
            {
                Title = $"{user.Name} (@{user.Nickname})",
                Description = Join(null, user.Bio, $"Perfil de {user.Name} no Noise Vault, a cena do ABC num lugar só."),
                ImageUrl = _fileStorage.PublicUrl(user.AvatarKey),
                Path = $"/perfil/{user.Nickname}",
                Type = "profile",
            };
        }

        private async Task<string> WithNamesAsync(string text)
        {
            var mentions = await _mentionAppService.Resolve([text]);

            foreach (var mention in mentions.Values)
                text = Regex.Replace(text, $"(?<![a-z0-9._@-])@{Regex.Escape(mention.Handle)}(?![a-z0-9._-])", mention.Name, RegexOptions.IgnoreCase);

            return text;
        }

        private async Task<List<string>> BandNamesAsync(IEnumerable<(int? BandId, string? Name)> rows)
        {
            var list = rows.ToList();
            var ids = list.Where(r => r.BandId != null).Select(r => r.BandId!.Value).Distinct().ToList();
            var names = ids.Count == 0
                ? []
                : (await _bandRepository.ListAsync(new QueryOptions<Band> { Predicate = b => ids.Contains(b.Id), AsNoTracking = true })).ToDictionary(b => b.Id, b => b.Name);

            return list
                .Select(r => r.BandId != null && names.TryGetValue(r.BandId.Value, out var name) ? name : r.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .ToList();
        }

        private async Task<string?> MediaImageAsync(int? mediaId)
        {
            if (mediaId == null)
                return null;

            var media = await _mediaRepository.FirstOrDefaultAsync(new QueryOptions<MediaUpload> { Predicate = m => m.Id == mediaId, AsNoTracking = true });
            return media == null ? null : ImageOf(media);
        }

        private string? ImageOf(MediaUpload media)
        {
            var described = _mediaAppService.Describe(media);
            return media.Kind == EMediaKind.Video ? described.PosterUrl : described.Url;
        }

        private static string Join(string? lead, string? text, string fallback)
        {
            var body = Excerpt(text);
            var parts = new[] { lead, body }.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            return parts.Count == 0 ? fallback : string.Join(" — ", parts);
        }

        private static string? Excerpt(string? text) => PreviewText.Excerpt(text, DescriptionMaxLength);
    }
}
