using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using NoiseVault.Application.Share.Dtos;

namespace NoiseVault.Api.Share
{
    public static class SharePage
    {
        public static string Render(SharePreviewDto? preview, string shareUrl, string frontendBaseUrl)
        {
            var title = preview?.Title ?? "Noise Vault";
            var description = preview?.Description ?? "A cena do ABC num lugar só: feed, mural das antigas, shows e bandas.";
            var image = preview?.ImageUrl ?? $"{frontendBaseUrl}/icons/icon-512.png";
            var target = $"{frontendBaseUrl}{preview?.Path ?? "/feed"}";
            var type = preview?.Type ?? "website";

            var html = new StringBuilder();
            html.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
            html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            html.Append("<meta name=\"theme-color\" content=\"#1c1515\">");
            html.Append("<meta name=\"robots\" content=\"noindex\">");
            html.Append($"<title>{Encode(title)} · Noise Vault</title>");
            html.Append($"<meta name=\"description\" content=\"{Encode(description)}\">");
            Meta(html, "og:site_name", "Noise Vault");
            Meta(html, "og:locale", "pt_BR");
            Meta(html, "og:type", type);
            Meta(html, "og:title", title);
            Meta(html, "og:description", description);
            Meta(html, "og:image", image);
            Meta(html, "og:image:alt", title);
            Meta(html, "og:url", shareUrl);
            html.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">");
            html.Append($"<meta name=\"twitter:title\" content=\"{Encode(title)}\">");
            html.Append($"<meta name=\"twitter:description\" content=\"{Encode(description)}\">");
            html.Append($"<meta name=\"twitter:image\" content=\"{Encode(image)}\">");
            html.Append($"<link rel=\"canonical\" href=\"{Encode(target)}\">");
            html.Append($"<meta http-equiv=\"refresh\" content=\"0; url={Encode(target)}\">");
            html.Append("</head><body style=\"background:#1c1515;color:#f3eae6;font-family:system-ui,sans-serif\">");
            html.Append($"<p><a href=\"{Encode(target)}\" style=\"color:#e53935\">{Encode(title)}</a></p>");
            html.Append($"<script>location.replace({System.Text.Json.JsonSerializer.Serialize(target)})</script>");
            html.Append("</body></html>");

            return html.ToString();
        }

        private static void Meta(StringBuilder html, string property, string content) =>
            html.Append($"<meta property=\"{property}\" content=\"{Encode(content)}\">");

        private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

        private static string Encode(string value) => Encoder.Encode(value);
    }
}
