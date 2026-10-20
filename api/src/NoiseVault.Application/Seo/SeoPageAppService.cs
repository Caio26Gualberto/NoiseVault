using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Archive.Dtos;
using NoiseVault.Application.Archive.Interfaces;
using NoiseVault.Application.Bands.Dtos;
using NoiseVault.Application.Bands.Interfaces;
using NoiseVault.Application.Events.Dtos;
using NoiseVault.Application.Events.Interfaces;
using NoiseVault.Application.Seo.Dtos;
using NoiseVault.Application.Seo.Interfaces;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Archive;
using NoiseVault.Domain.Enums.Bands;
using NoiseVault.Domain.Enums.Events;
using NoiseVault.Domain.Enums.Media;
using static NoiseVault.Application.Share.PreviewText;

namespace NoiseVault.Application.Seo
{
    public class SeoPageAppService : ISeoPageAppService
    {
        private const int DescriptionMaxLength = 160;
        private const int ListSize = 50;
        private const int BandListSize = 20;
        private const int SitemapLimit = 5000;
        private const string SiteName = "Noise Vault";
        private const string Tagline = "A cena do ABC num lugar só: agenda de shows, mural das antigas e bandas de metal, punk, hardcore e afins.";

        private readonly IEventAppService _eventAppService;
        private readonly IArchiveAppService _archiveAppService;
        private readonly IBandAppService _bandAppService;
        private readonly IBandDiscographyAppService _discographyAppService;
        private readonly IRepository<Band> _bandRepository;
        private readonly IRepository<Event> _eventRepository;
        private readonly IRepository<ArchiveEntry> _archiveRepository;

        public SeoPageAppService(
            IEventAppService eventAppService,
            IArchiveAppService archiveAppService,
            IBandAppService bandAppService,
            IBandDiscographyAppService discographyAppService,
            IRepository<Band> bandRepository,
            IRepository<Event> eventRepository,
            IRepository<ArchiveEntry> archiveRepository)
        {
            _eventAppService = eventAppService;
            _archiveAppService = archiveAppService;
            _bandAppService = bandAppService;
            _discographyAppService = discographyAppService;
            _bandRepository = bandRepository;
            _eventRepository = eventRepository;
            _archiveRepository = archiveRepository;
        }

        public Task<SeoPageDto?> Build(string section, string? key) => section switch
        {
            "home" => AgendaAsync("/"),
            "eventos" when key == null => AgendaAsync("/eventos"),
            "eventos" when int.TryParse(key, out var id) => EventAsync(id),
            "mural" when key == null => ArchiveListAsync(),
            "mural" when int.TryParse(key, out var id) => ArchiveAsync(id),
            "bandas" when !string.IsNullOrWhiteSpace(key) => BandAsync(key),
            _ => Task.FromResult<SeoPageDto?>(null),
        };

        public async Task<List<SeoSitemapEntryDto>> Sitemap()
        {
            var entries = new List<SeoSitemapEntryDto>
            {
                new() { Path = "/eventos", LastModified = DateTime.UtcNow.Date },
                new() { Path = "/mural", LastModified = DateTime.UtcNow.Date },
            };

            var bands = await _bandRepository.ListAsync(new QueryOptions<Band>
            {
                OrderBy = q => q.OrderByDescending(b => b.Id),
                Take = SitemapLimit,
                AsNoTracking = true,
            });
            entries.AddRange(bands.Select(b => new SeoSitemapEntryDto { Path = $"/bandas/{b.Slug}", LastModified = b.UpdatedAt ?? b.CreatedAt }));

            var events = await _eventRepository.ListAsync(new QueryOptions<Event>
            {
                OrderBy = q => q.OrderByDescending(e => e.Id),
                Take = SitemapLimit,
                AsNoTracking = true,
            });
            entries.AddRange(events.Select(e => new SeoSitemapEntryDto { Path = $"/eventos/{e.Id}", LastModified = e.UpdatedAt ?? e.CreatedAt }));

            var archive = await _archiveRepository.ListAsync(new QueryOptions<ArchiveEntry>
            {
                Predicate = a => a.Status == EArchiveEntryStatus.Published,
                OrderBy = q => q.OrderByDescending(a => a.Id),
                Take = SitemapLimit,
                AsNoTracking = true,
            });
            entries.AddRange(archive.Select(a => new SeoSitemapEntryDto { Path = $"/mural/{a.Id}", LastModified = a.UpdatedAt ?? a.CreatedAt }));

            return entries;
        }

        private async Task<SeoPageDto?> AgendaAsync(string path)
        {
            var result = await _eventAppService.List(new EventListQueryDto { PageSize = ListSize });
            var events = result.Data?.Items ?? [];

            return new SeoPageDto
            {
                Title = path == "/" ? $"{SiteName}: a cena do ABC num lugar só" : "Agenda de shows do ABC",
                Description = Tagline,
                Path = path,
                Heading = path == "/" ? SiteName : "Agenda de shows",
                Lead = Tagline,
                Sections =
                [
                    new SeoSectionDto { Title = "Próximos shows", Items = events.Select(EventLink).ToList() },
                    new SeoSectionDto
                    {
                        Title = "Mais no Noise Vault",
                        Items =
                        [
                            new SeoLinkDto { Text = "Mural das antigas", Path = "/mural", Detail = "Fotos e fitas da cena dos anos 80 e 90" },
                        ],
                    },
                ],
                StructuredData = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "WebSite",
                    ["name"] = SiteName,
                    ["description"] = Tagline,
                    ["inLanguage"] = "pt-BR",
                },
            };
        }

        private async Task<SeoPageDto?> EventAsync(int id)
        {
            var result = await _eventAppService.Get(id);

            if (result.Data is not { } e)
                return null;

            var cancelled = e.Status == EEventStatus.Cancelled;
            var city = City(e.City, e.CityName);
            var lineup = e.Lineup.Where(l => l.Name != null).ToList();
            var facts = new List<SeoFactDto>
            {
                new() { Label = "Quando", Value = EventWhen(e.StartsAt) },
                new() { Label = "Onde", Value = Line(e.Venue, city) },
            };

            if (e.Genres.Count > 0)
                facts.Add(new SeoFactDto { Label = "Estilos", Value = string.Join(", ", e.Genres.Select(Genre)) });

            facts.Add(new SeoFactDto { Label = "Entrada", Value = e.Price is { } price ? $"a partir de {price.ToString("C", PtBr)}" : "franca" });

            if (!string.IsNullOrWhiteSpace(e.TicketsUrl))
                facts.Add(new SeoFactDto { Label = "Ingressos", Value = e.TicketsUrl, Url = e.TicketsUrl });

            if (cancelled)
                facts.Insert(0, new SeoFactDto { Label = "Situação", Value = "Cancelado" });

            var title = cancelled ? $"[Cancelado] {e.Title}" : e.Title;
            var lead = Line(EventWhen(e.StartsAt), e.Venue, city, lineup.Count > 0 ? $"com {string.Join(", ", lineup.Select(l => l.Name))}" : null);

            return new SeoPageDto
            {
                Title = $"{title} · {EventDay(e.StartsAt)}",
                Description = Excerpt(Line(lead, e.Description), DescriptionMaxLength) ?? lead,
                Path = $"/eventos/{e.Id}",
                ImageUrl = e.BannerUrl,
                Type = "article",
                Heading = title,
                Lead = lead,
                Facts = facts,
                Paragraphs = Paragraphs(e.Description),
                Images = e.BannerUrl != null ? [new SeoImageDto { Url = e.BannerUrl, Caption = e.Title }] : [],
                Sections =
                [
                    new SeoSectionDto
                    {
                        Title = "Line-up",
                        Items = lineup.Select(l => new SeoLinkDto { Text = l.Name!, Path = l.Band != null ? $"/bandas/{l.Band.Slug}" : null }).ToList(),
                    },
                    new SeoSectionDto { Title = "Mais shows", Items = [new SeoLinkDto { Text = "Agenda de shows do ABC", Path = "/eventos" }] },
                ],
                StructuredData = EventData(e, city, lineup),
            };
        }

        private async Task<SeoPageDto?> ArchiveListAsync()
        {
            var result = await _archiveAppService.List(new ArchiveListQueryDto { PageSize = ListSize });
            var entries = result.Data?.Items ?? [];
            const string description = "O arquivo da cena do ABC: fotos e fitas de shows dos anos 80 e 90, contadas por quem viveu.";

            return new SeoPageDto
            {
                Title = "Mural das antigas: a história da cena do ABC",
                Description = description,
                Path = "/mural",
                Heading = "Mural das antigas",
                Lead = description,
                Sections =
                [
                    new SeoSectionDto { Title = "Registros recentes", Items = entries.Select(ArchiveLink).ToList() },
                    new SeoSectionDto { Title = "Mais no Noise Vault", Items = [new SeoLinkDto { Text = "Agenda de shows", Path = "/eventos" }] },
                ],
            };
        }

        private async Task<SeoPageDto?> ArchiveAsync(int id)
        {
            var result = await _archiveAppService.Get(id);

            if (result.Data is not { Status: EArchiveEntryStatus.Published } a)
                return null;

            var date = ArchiveDate(a.Year, a.Month, a.Day);
            var city = City(a.City, a.CityName);
            var bands = a.Bands.Select(b => b.Name).ToList();
            var lead = Line(date, a.Venue, city, bands.Count > 0 ? string.Join(", ", bands) : null);
            var images = a.Media
                .Select(m => new SeoImageDto { Url = (m.Media.Kind == EMediaKind.Video ? m.Media.PosterUrl : m.Media.Url) ?? string.Empty, Caption = m.Caption })
                .Where(i => i.Url.Length > 0)
                .ToList();
            var cover = a.Cover?.Kind == EMediaKind.Video ? a.Cover.PosterUrl : a.Cover?.Url;
            var facts = new List<SeoFactDto> { new() { Label = "Quando", Value = date } };

            if (Line(a.Venue, city) is { Length: > 0 } where)
                facts.Add(new SeoFactDto { Label = "Onde", Value = where });

            facts.Add(new SeoFactDto { Label = a.IsSubmission ? "Acervo de" : "Publicado por", Value = a.Author.Name });

            if (Line(Count(a.PhotoCount, "foto", "fotos"), Count(a.VideoCount, "vídeo", "vídeos")) is { Length: > 0 } media)
                facts.Add(new SeoFactDto { Label = "Mídias", Value = media });

            return new SeoPageDto
            {
                Title = $"{a.Title} ({date})",
                Description = Excerpt(Line(lead, a.Description), DescriptionMaxLength) ?? lead,
                Path = $"/mural/{a.Id}",
                ImageUrl = cover,
                Type = "article",
                Heading = a.Title,
                Lead = lead,
                Facts = facts,
                Paragraphs = Paragraphs(a.Description),
                Images = images,
                Sections =
                [
                    new SeoSectionDto
                    {
                        Title = "Bandas",
                        Items = a.Bands.Select(b => new SeoLinkDto { Text = b.Name, Path = b.Band != null ? $"/bandas/{b.Band.Slug}" : null }).ToList(),
                    },
                    new SeoSectionDto { Title = "Mais memórias", Items = [new SeoLinkDto { Text = "Mural das antigas", Path = "/mural" }] },
                ],
                StructuredData = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "CreativeWork",
                    ["name"] = a.Title,
                    ["description"] = Excerpt(a.Description, 500),
                    ["dateCreated"] = ArchiveIsoDate(a.Year, a.Month, a.Day),
                    ["image"] = images.Count > 0 ? images.Select(i => i.Url).ToList() : null,
                    ["contentLocation"] = Place(a.Venue, a.City?.Name ?? a.CityName, a.City?.Uf),
                    ["about"] = a.Bands.Count > 0 ? a.Bands.Select(b => new Dictionary<string, object?> { ["@type"] = "MusicGroup", ["name"] = b.Name }).ToList() : null,
                    ["inLanguage"] = "pt-BR",
                },
            };
        }

        private async Task<SeoPageDto?> BandAsync(string slug)
        {
            var result = await _bandAppService.GetBySlug(slug);

            if (result.Data is not { } b)
                return null;

            var city = City(b.City, b.CityName);
            var genres = b.Genres.Select(Genre).ToList();
            var discography = (await _discographyAppService.Get(b.Slug)).Data?.Releases.Where(r => !r.IsHidden).ToList() ?? [];
            var events = (await _eventAppService.ByBand(b.Slug, new EventListQueryDto { PageSize = BandListSize })).Data?.Items ?? [];
            var archive = (await _archiveAppService.ByBand(b.Slug, new ArchiveListQueryDto { PageSize = BandListSize })).Data?.Items ?? [];
            var lead = Line("Banda", city, b.FoundedYear != null ? $"desde {b.FoundedYear}" : null, genres.Count > 0 ? string.Join(", ", genres) : null);
            var facts = new List<SeoFactDto>();

            if (city != null)
                facts.Add(new SeoFactDto { Label = "Cidade", Value = city });

            if (b.FoundedYear != null)
                facts.Add(new SeoFactDto { Label = "Desde", Value = b.FoundedYear.Value.ToString(PtBr) });

            if (genres.Count > 0)
                facts.Add(new SeoFactDto { Label = "Estilos", Value = string.Join(", ", genres) });

            var links = new[] { b.SocialLinks.Instagram, b.SocialLinks.Youtube, b.SocialLinks.Spotify }.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            facts.AddRange(links.Select(l => new SeoFactDto { Label = "Link", Value = l!, Url = l }));

            var image = b.BannerUrl ?? b.AvatarUrl;

            return new SeoPageDto
            {
                Title = city != null ? $"{b.Name} — banda de {city}" : b.Name,
                Description = Excerpt(Line(lead, b.Description), DescriptionMaxLength) ?? lead,
                Path = $"/bandas/{b.Slug}",
                ImageUrl = image,
                Type = "profile",
                Heading = b.Name,
                Lead = lead,
                Facts = facts,
                Paragraphs = Paragraphs(b.Description),
                Images = b.AvatarUrl != null ? [new SeoImageDto { Url = b.AvatarUrl, Caption = b.Name }] : [],
                Sections =
                [
                    new SeoSectionDto
                    {
                        Title = "Integrantes",
                        Items = b.Members.Select(m => new SeoLinkDto
                        {
                            Text = m.User.Name,
                            Detail = m.Instrument ?? (m.Role == EBandMemberRole.Owner ? "Responsável" : null),
                        }).ToList(),
                    },
                    new SeoSectionDto
                    {
                        Title = "Discografia",
                        Items = discography.Select(r => new SeoLinkDto { Text = r.Title, Detail = $"{ReleaseKind(r.Kind)}, {r.Year}" }).ToList(),
                    },
                    new SeoSectionDto { Title = "Shows", Items = events.Select(EventLink).ToList() },
                    new SeoSectionDto { Title = "No mural das antigas", Items = archive.Select(ArchiveLink).ToList() },
                ],
                StructuredData = new Dictionary<string, object?>
                {
                    ["@context"] = "https://schema.org",
                    ["@type"] = "MusicGroup",
                    ["name"] = b.Name,
                    ["description"] = Excerpt(b.Description, 500),
                    ["image"] = image,
                    ["foundingDate"] = b.FoundedYear?.ToString(PtBr),
                    ["foundingLocation"] = city != null ? Place(null, b.City?.Name ?? b.CityName, b.City?.Uf) : null,
                    ["genre"] = genres.Count > 0 ? genres : null,
                    ["sameAs"] = links.Count > 0 ? links : null,
                    ["album"] = discography.Count > 0
                        ? discography.Select(r => new Dictionary<string, object?> { ["@type"] = "MusicAlbum", ["name"] = r.Title, ["datePublished"] = r.Year.ToString(PtBr) }).ToList()
                        : null,
                },
            };
        }

        private static SeoLinkDto EventLink(EventDto e) => new()
        {
            Text = e.Status == EEventStatus.Cancelled ? $"[Cancelado] {e.Title}" : e.Title,
            Path = $"/eventos/{e.Id}",
            Detail = Line(EventWhen(e.StartsAt), e.Venue, City(e.City, e.CityName)),
        };

        private static SeoLinkDto ArchiveLink(ArchiveEntryDto a) => new()
        {
            Text = a.Title,
            Path = $"/mural/{a.Id}",
            Detail = Line(ArchiveDate(a.Year, a.Month, a.Day), a.Venue, City(a.City, a.CityName)),
        };

        private static Dictionary<string, object?> EventData(EventDto e, string? city, List<EventLineupItemDto> lineup) => new()
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "MusicEvent",
            ["name"] = e.Title,
            ["description"] = Excerpt(e.Description, 500),
            ["startDate"] = IsoLocal(e.StartsAt),
            ["endDate"] = e.EndsAt is { } ends ? IsoLocal(ends) : null,
            ["eventStatus"] = e.Status == EEventStatus.Cancelled ? "https://schema.org/EventCancelled" : "https://schema.org/EventScheduled",
            ["eventAttendanceMode"] = "https://schema.org/OfflineEventAttendanceMode",
            ["image"] = e.BannerUrl,
            ["location"] = Place(e.Venue, e.City?.Name ?? e.CityName, e.City?.Uf) ?? new Dictionary<string, object?> { ["@type"] = "Place", ["name"] = city },
            ["performer"] = lineup.Count > 0
                ? lineup.Select(l => new Dictionary<string, object?> { ["@type"] = "MusicGroup", ["name"] = l.Name }).ToList()
                : null,
            ["offers"] = e.TicketsUrl != null || e.Price != null
                ? new Dictionary<string, object?>
                {
                    ["@type"] = "Offer",
                    ["url"] = e.TicketsUrl,
                    ["price"] = (e.Price ?? 0m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    ["priceCurrency"] = "BRL",
                }
                : null,
        };

        private static Dictionary<string, object?>? Place(string? venue, string? locality, string? region)
        {
            if (string.IsNullOrWhiteSpace(venue) && string.IsNullOrWhiteSpace(locality))
                return null;

            return new Dictionary<string, object?>
            {
                ["@type"] = "Place",
                ["name"] = string.IsNullOrWhiteSpace(venue) ? locality : venue,
                ["address"] = string.IsNullOrWhiteSpace(locality)
                    ? null
                    : new Dictionary<string, object?>
                    {
                        ["@type"] = "PostalAddress",
                        ["addressLocality"] = locality,
                        ["addressRegion"] = region,
                        ["addressCountry"] = region != null ? "BR" : null,
                    },
            };
        }
    }
}
