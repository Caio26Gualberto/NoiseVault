using NoiseVault.Application.Seo.Dtos;

namespace NoiseVault.Application.Seo.Interfaces
{
    public interface ISeoPageAppService
    {
        Task<SeoPageDto?> Build(string section, string? key);
        Task<List<SeoSitemapEntryDto>> Sitemap();
    }
}
