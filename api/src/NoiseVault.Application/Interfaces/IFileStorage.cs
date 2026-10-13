namespace NoiseVault.Application.Interfaces
{
    public interface IFileStorage
    {
        Task<bool> PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
        string? PublicUrl(string? key);
    }
}
