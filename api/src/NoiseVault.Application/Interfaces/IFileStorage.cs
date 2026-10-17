namespace NoiseVault.Application.Interfaces
{
    public sealed record MultipartPart(int PartNumber, string ETag);

    public interface IFileStorage
    {
        Task<bool> PutAsync(string key, byte[] content, string contentType, CancellationToken cancellationToken = default);
        Task<bool> PutFileAsync(string key, string path, string contentType, CancellationToken cancellationToken = default);
        Task<bool> DownloadToFileAsync(string key, string path, CancellationToken cancellationToken = default);
        Task<long?> SizeAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
        Task<bool> DeletePrefixAsync(string prefix, CancellationToken cancellationToken = default);
        Task<long> SizeOfPrefixAsync(string prefix, CancellationToken cancellationToken = default);
        string? PublicUrl(string? key);

        Task<string?> StartMultipartAsync(string key, string contentType, CancellationToken cancellationToken = default);
        string PresignPartUrl(string key, string uploadId, int partNumber, TimeSpan lifetime);
        Task<bool> CompleteMultipartAsync(string key, string uploadId, IReadOnlyList<MultipartPart> parts, CancellationToken cancellationToken = default);
        Task AbortMultipartAsync(string key, string uploadId, CancellationToken cancellationToken = default);
    }
}
