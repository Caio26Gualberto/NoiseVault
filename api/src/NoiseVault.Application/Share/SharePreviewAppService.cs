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

namespace NoiseVault.Application.Share
{
    public class SharePreviewAppService : ISharePreviewAppService
    {
        private const int AnonymousViewer = 0;
        private const int DescriptionMaxLength = 200;

        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

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
            var lead = string.Join(" · ", new[] { ArchiveDate(entry.Year, entry.Month, entry.Day), where }.Where(p => p.Length > 0));

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

            var when = AppTime.ToLocal(row.StartsAt).ToString("dddd, d 'de' MMMM 'às' HH'h'mm", PtBr);
            var where = string.Join(" · ", new[] { row.Venue, row.City?.Name ?? row.CityName }.Where(p => !string.IsNullOrWhiteSpace(p)));
            var title = row.Status == EEventStatus.Cancelled ? $"[Cancelado] {row.Title}" : row.Title;

            return new SharePreviewDto
            {
                Title = title,
                Description = Join($"{Capitalize(when)} · {where}", row.Description, "Agenda do Noise Vault"),
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

            var first = media.Select(pm => pm.MediaUpload!).FirstOrDefault(m => m.Status == EMediaStatus.Ready);

            return new SharePreviewDto
            {
                Title = $"{post.AuthorName} no Noise Vault",
                Description = Join(null, await WithNamesAsync(post.Content), "Publicação no feed do Noise Vault"),
                ImageUrl = first != null ? ImageOf(first) : _fileStorage.PublicUrl(post.AuthorAvatarKey),
                Path = $"/feed/{id}",
                Type = "article",
            };
        }

        private async Task<SharePreviewDto?> BandAsync(string slug)
        {
            var normalized = BandRules.NormalizeSlug(slug);
            var band = await _bandRepository.FirstOrDefaultAsync(new QueryOptions<Band> { Predicate = b => b.Slug == normalized, AsNoTracking = true });

            if (band == null)
                return null;

            return new SharePreviewDto
            {
                Title = band.Name,
                Description = Join(null, band.Description, "Banda no Noise Vault"),
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
                Description = Join(null, user.Bio, "Perfil no Noise Vault"),
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

        private static string ArchiveDate(int year, int? month, int? day)
        {
            if (month == null)
                return year.ToString(CultureInfo.InvariantCulture);

            var date = new DateTime(year, month.Value, day ?? 1);
            return day == null ? date.ToString("MMMM 'de' yyyy", PtBr) : date.ToString("d 'de' MMMM 'de' yyyy", PtBr);
        }

        private static string Join(string? lead, string? text, string fallback)
        {
            var body = Excerpt(text);
            var parts = new[] { lead, body }.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            return parts.Count == 0 ? fallback : string.Join(" — ", parts);
        }

        private static string? Excerpt(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var flat = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return flat.Length <= DescriptionMaxLength ? flat : flat[..(DescriptionMaxLength - 1)].TrimEnd() + "…";
        }

        private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpper(text[0], PtBr) + text[1..];
    }
}
