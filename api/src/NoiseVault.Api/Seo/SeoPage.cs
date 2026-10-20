using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using NoiseVault.Application.Seo.Dtos;

namespace NoiseVault.Api.Seo
{
    public static class SeoPage
    {
        private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

        public static string Render(SeoPageDto page, string siteUrl)
        {
            var url = $"{siteUrl}{page.Path}";
            var image = page.ImageUrl ?? $"{siteUrl}/icons/icon-512.png";

            var html = new StringBuilder();
            Head(html, $"{page.Title} · Noise Vault", page.Description, url);
            Meta(html, "og:site_name", "Noise Vault");
            Meta(html, "og:locale", "pt_BR");
            Meta(html, "og:type", page.Type);
            Meta(html, "og:title", page.Title);
            Meta(html, "og:description", page.Description);
            Meta(html, "og:image", image);
            Meta(html, "og:url", url);
            html.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">");

            if (page.StructuredData != null)
            {
                var data = Clean(page.StructuredData);

                if (data is Dictionary<string, object?> map)
                    map["url"] = url;

                html.Append("<script type=\"application/ld+json\">");
                html.Append(JsonSerializer.Serialize(data));
                html.Append("</script>");
            }

            html.Append("</head><body><header>");
            html.Append($"<a href=\"{Encode(siteUrl)}/\">Noise Vault</a> · ");
            html.Append($"<a href=\"{Encode(siteUrl)}/eventos\">Agenda</a> · ");
            html.Append($"<a href=\"{Encode(siteUrl)}/mural\">Mural das antigas</a>");
            html.Append("</header><main><article>");
            html.Append($"<h1>{Encode(page.Heading)}</h1>");

            if (!string.IsNullOrWhiteSpace(page.Lead))
                html.Append($"<p><strong>{Encode(page.Lead)}</strong></p>");

            if (page.Facts.Count > 0)
            {
                html.Append("<dl>");
                foreach (var fact in page.Facts)
                {
                    var value = fact.Url != null ? $"<a href=\"{Encode(fact.Url)}\" rel=\"nofollow\">{Encode(fact.Value)}</a>" : Encode(fact.Value);
                    html.Append($"<dt>{Encode(fact.Label)}</dt><dd>{value}</dd>");
                }
                html.Append("</dl>");
            }

            foreach (var paragraph in page.Paragraphs)
                html.Append($"<p>{Encode(paragraph).Replace("\n", "<br>")}</p>");

            foreach (var img in page.Images)
            {
                html.Append($"<figure><img src=\"{Encode(img.Url)}\" alt=\"{Encode(img.Caption ?? page.Heading)}\" loading=\"lazy\">");
                if (!string.IsNullOrWhiteSpace(img.Caption))
                    html.Append($"<figcaption>{Encode(img.Caption)}</figcaption>");
                html.Append("</figure>");
            }

            foreach (var section in page.Sections.Where(s => s.Items.Count > 0))
            {
                html.Append($"<section><h2>{Encode(section.Title)}</h2><ul>");
                foreach (var item in section.Items)
                {
                    var text = item.Path != null ? $"<a href=\"{Encode(siteUrl + item.Path)}\">{Encode(item.Text)}</a>" : Encode(item.Text);
                    var detail = string.IsNullOrWhiteSpace(item.Detail) ? string.Empty : $" — {Encode(item.Detail)}";
                    html.Append($"<li>{text}{detail}</li>");
                }
                html.Append("</ul></section>");
            }

            html.Append("</article></main><footer><p>Noise Vault — a cena do ABC num lugar só. ");
            html.Append($"<a href=\"{Encode(siteUrl)}/criar-conta\">Crie sua conta</a> para curtir, comentar e seguir.</p></footer>");
            html.Append("</body></html>");

            return html.ToString();
        }

        public static string NotFound(string siteUrl)
        {
            var html = new StringBuilder();
            Head(html, "Página não encontrada · Noise Vault", "Esta página não existe ou saiu do ar.", null);
            html.Append("<meta name=\"robots\" content=\"noindex\">");
            html.Append("</head><body><h1>Página não encontrada</h1>");
            html.Append($"<p><a href=\"{Encode(siteUrl)}/eventos\">Ver a agenda de shows</a></p></body></html>");
            return html.ToString();
        }

        public static string Sitemap(IEnumerable<SeoSitemapEntryDto> entries, string siteUrl)
        {
            var xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            foreach (var entry in entries)
            {
                xml.Append("<url><loc>").Append(System.Security.SecurityElement.Escape(siteUrl + entry.Path)).Append("</loc>");
                xml.Append("<lastmod>").Append(entry.LastModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("</lastmod></url>");
            }

            return xml.Append("</urlset>").ToString();
        }

        private static void Head(StringBuilder html, string title, string description, string? canonical)
        {
            html.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
            html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            html.Append($"<title>{Encode(title)}</title>");
            html.Append($"<meta name=\"description\" content=\"{Encode(description)}\">");

            if (canonical != null)
                html.Append($"<link rel=\"canonical\" href=\"{Encode(canonical)}\">");
        }

        private static void Meta(StringBuilder html, string property, string content) =>
            html.Append($"<meta property=\"{property}\" content=\"{Encode(content)}\">");

        private static object? Clean(object? value) => value switch
        {
            Dictionary<string, object?> map => map
                .Select(pair => (pair.Key, Value: Clean(pair.Value)))
                .Where(pair => pair.Value != null)
                .ToDictionary(pair => pair.Key, pair => pair.Value),
            string text => text,
            IEnumerable list => list.Cast<object?>().Select(Clean).Where(item => item != null).ToList(),
            _ => value,
        };

        private static string Encode(string value) => Encoder.Encode(value);
    }
}
