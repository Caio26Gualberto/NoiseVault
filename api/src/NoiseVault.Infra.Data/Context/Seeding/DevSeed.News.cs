using Microsoft.EntityFrameworkCore;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.News;

namespace NoiseVault.Infra.Data.Context.Seeding
{
    public partial class DevSeed
    {
        private sealed record SeedNews(long ExternalId, string Slug, string Title, string Author, string Image, int HoursAgo);

        private static readonly SeedNews[] News =
        [
            new(384062, "dio", "O lado mais egoísta da humanidade inspirou a história de um dos maiores clássicos de Dio", "Bruce William", "dio_holy_diver_capa_corte.jpg", 1),
            new(384061, "afterforever", "\"O After Forever atual é uma banda cover\", diz Floor Jansen", "João Renato Alves", "after_forever_2025_nuclear_blast_official_press.jpg", 2),
            new(384060, "cazuza", "Cazuza revela seu maior ídolo", "Helton Grunge", "cazuza_capa_preciso_dizer_que_te_amo.jpg", 3),
            new(384059, "rem", "As 10 melhores músicas do R.E.M., segundo os leitores da Rolling Stone", "Mateus Ribeiro", "rem_losing_my_religion.jpg", 26),
            new(384042, "rogerwaters", "Roger Waters diz o que mudaria em \"Dark Side\" se o Pink Floyd tivesse a tecnologia atual", "Bruce William", "darksideofthemoon.jpg", 28),
            new(384041, "offspring", "Noodles se afasta dos shows do Offspring por problema de saúde; leia comunicado", "Gustavo Maiato", "offspring_por_gustavo_maiato.jpg", 30),
        ];

        private async Task<int> EnsureNewsAsync(CancellationToken cancellationToken)
        {
            if (await _context.NewsArticles.AnyAsync(cancellationToken))
                return 0;

            var now = DateTime.UtcNow;

            _context.NewsArticles.AddRange(News.Select(n => new NewsArticle
            {
                Source = ENewsSource.Whiplash,
                ExternalId = n.ExternalId,
                Title = n.Title,
                Url = $"https://whiplash.net/materias/news_666/{n.ExternalId}-{n.Slug}.html",
                ImageUrl = $"https://whiplash.net/imagens_promo_22/{n.Image}",
                Author = n.Author,
                PublishedAt = now.AddHours(-n.HoursAgo),
            }));

            await _context.SaveChangesAsync(cancellationToken);

            return News.Length;
        }
    }
}
