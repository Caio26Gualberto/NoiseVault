namespace NoiseVault.Infra.Data.Cache
{
    public sealed record CacheEntryOptions(
        TimeSpan Ttl,
        IReadOnlyList<string>? Tags = null);

    public interface ICacheClient
    {
        Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions options,
            CancellationToken ct = default);

        Task SetAsync<T>(
            string key,
            T value,
            CacheEntryOptions options,
            CancellationToken ct = default);

        Task RemoveAsync(string key, CancellationToken ct = default);

        Task RemoveByTagAsync(string tag, CancellationToken ct = default);
    }
}
