using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NoiseVault.Application.Common.Time;
using NoiseVault.Application.Interfaces;

namespace NoiseVault.Infra.Data.News
{
    public partial class WhiplashNewsClient : INewsSourceClient
    {
        private const string SitemapUrl = "https://whiplash.net/sitemap_ultimas.xml";
        private const string ChallengeMarker = "Just a moment...";

        private readonly HttpClient _http;
        private readonly WhiplashNewsSettings _settings;

        public WhiplashNewsClient(HttpClient http, WhiplashNewsSettings settings)
        {
            _http = http;
            _settings = settings;
        }

        public bool IsEnabled => _settings.Enabled;

        public async Task<IReadOnlyList<NewsLink>> ListRecentAsync(CancellationToken cancellationToken = default)
        {
            var body = await GetStringAsync(SitemapUrl, cancellationToken);

            XDocument document;
            try
            {
                document = XDocument.Parse(body);
            }
            catch (Exception ex)
            {
                throw new NewsSourceException(ENewsSourceFailure.Unavailable, "Sitemap do Whiplash.Net ilegível.", ex);
            }

            var links = new List<NewsLink>();
            var seen = new HashSet<long>();

            foreach (var loc in document.Descendants().Where(e => e.Name.LocalName == "loc"))
            {
                var url = loc.Value.Trim();
                var match = ArticleUrlRegex().Match(url);

                if (match.Success && long.TryParse(match.Groups[1].Value, out var externalId) && seen.Add(externalId))
                    links.Add(new NewsLink(externalId, url));
            }

            return links;
        }

        public async Task<NewsArticleData?> GetArticleAsync(string url, CancellationToken cancellationToken = default)
        {
            var html = await GetStringAsync(url, cancellationToken);

            var fromJsonLd = ReadJsonLd(html);
            var title = Clean(fromJsonLd.Title) ?? Clean(Meta(html, "og:title"));

            if (title == null)
                return null;

            var image = Clean(fromJsonLd.Image) ?? Clean(Meta(html, "og:image"));
            var author = Clean(fromJsonLd.Author);
            var publishedAt = ParseLocalDate(fromJsonLd.PublishedAt) ?? DateTime.UtcNow;

            return new NewsArticleData(title, IsHttpUrl(image) ? image : null, author, publishedAt);
        }

        private async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", _settings.UserAgent);
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9");

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new NewsSourceException(ENewsSourceFailure.Unavailable, $"Falha ao baixar {url}.", ex);
            }

            using (response)
            {
                if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
                    throw new NewsSourceException(ENewsSourceFailure.Blocked, $"Whiplash.Net respondeu {(int)response.StatusCode}.");

                if (!response.IsSuccessStatusCode)
                    throw new NewsSourceException(ENewsSourceFailure.Unavailable, $"Whiplash.Net respondeu {(int)response.StatusCode}.");

                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (body.Contains(ChallengeMarker, StringComparison.Ordinal))
                    throw new NewsSourceException(ENewsSourceFailure.Blocked, "Whiplash.Net respondeu com o desafio do Cloudflare.");

                return body;
            }
        }

        private static (string? Title, string? Image, string? Author, string? PublishedAt) ReadJsonLd(string html)
        {
            foreach (Match block in JsonLdRegex().Matches(html))
            {
                try
                {
                    using var document = JsonDocument.Parse(block.Groups[1].Value, new JsonDocumentOptions { AllowTrailingCommas = true });
                    var root = document.RootElement;

                    if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("headline", out var headline))
                        continue;

                    string? image = null;
                    if (root.TryGetProperty("image", out var images))
                    {
                        if (images.ValueKind == JsonValueKind.Array && images.GetArrayLength() > 0 && images[0].ValueKind == JsonValueKind.String)
                            image = images[0].GetString();
                        else if (images.ValueKind == JsonValueKind.String)
                            image = images.GetString();
                    }

                    string? author = null;
                    if (root.TryGetProperty("author", out var authorElement) && authorElement.ValueKind == JsonValueKind.Object
                        && authorElement.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                        author = name.GetString();

                    var published = root.TryGetProperty("datePublished", out var date) && date.ValueKind == JsonValueKind.String ? date.GetString() : null;

                    return (headline.ValueKind == JsonValueKind.String ? headline.GetString() : null, image, author, published);
                }
                catch (JsonException)
                {
                }
            }

            var fallbackDate = DatePublishedRegex().Match(html);
            return (null, null, null, fallbackDate.Success ? fallbackDate.Groups[1].Value : null);
        }

        private static string? Meta(string html, string property)
        {
            var match = Regex.Match(html, $"<meta\\s+property=\"{Regex.Escape(property)}\"\\s+content=\"([^\"]*)\"", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static DateTime? ParseLocalDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 19)
                return null;

            if (!DateTime.TryParseExact(value[..19], "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
                return null;

            return AppTime.ToUtc(local);
        }

        private static string? Clean(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var decoded = WebUtility.HtmlDecode(WebUtility.HtmlDecode(value));
            var collapsed = WhitespaceRegex().Replace(decoded, " ").Trim();

            return collapsed.Length == 0 ? null : collapsed;
        }

        private static bool IsHttpUrl(string? value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

        [GeneratedRegex(@"^https://whiplash\.net/materias/news_\d+/(\d+)-[A-Za-z0-9_-]+\.html$")]
        private static partial Regex ArticleUrlRegex();

        [GeneratedRegex(@"<script type=""application/ld\+json"">([\s\S]*?)</script>", RegexOptions.IgnoreCase)]
        private static partial Regex JsonLdRegex();

        [GeneratedRegex(@"""datePublished""\s*:\s*""([^""]+)""")]
        private static partial Regex DatePublishedRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();
    }
}
