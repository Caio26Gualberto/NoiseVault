using Microsoft.Extensions.Caching.Hybrid;

namespace Headbanger.Infra.Data.Cache
{
    public class CacheClient : ICacheClient
    {
        private readonly HybridCache _cache;
        public CacheClient(HybridCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions options, CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync<T>(key, async token => await factory(token), Map(options), options.Tags, ct);
        }

        public async Task RemoveAsync(string key, CancellationToken ct = default) => await _cache.RemoveAsync(key, ct);

        public async Task RemoveByTagAsync(string tag, CancellationToken ct = default) => await _cache.RemoveByTagAsync(tag, ct);

        public async Task SetAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken ct = default)
        {
            await _cache.SetAsync(key, value, Map(options), options.Tags, ct);
        }

        private static HybridCacheEntryOptions Map(CacheEntryOptions options) => new()
        {
            Expiration = options.Ttl,
            LocalCacheExpiration = options.Ttl
        };
    }
}
