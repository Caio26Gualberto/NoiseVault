using NoiseVault.Application.Share.Dtos;

namespace NoiseVault.Application.Share.Interfaces
{
    public interface ISharePreviewAppService
    {
        Task<SharePreviewDto?> Build(string section, string key);
    }
}
