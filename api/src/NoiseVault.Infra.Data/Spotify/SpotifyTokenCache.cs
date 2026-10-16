namespace NoiseVault.Infra.Data.Spotify
{
    public class SpotifyTokenCache
    {
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string? _token;
        private DateTime _expiresAt;

        public async Task<string> GetOrRefreshAsync(Func<CancellationToken, Task<(string Token, int ExpiresInSeconds)>> refresh, CancellationToken cancellationToken)
        {
            if (_token != null && DateTime.UtcNow < _expiresAt)
                return _token;

            await _gate.WaitAsync(cancellationToken);

            try
            {
                if (_token != null && DateTime.UtcNow < _expiresAt)
                    return _token;

                var (token, expiresIn) = await refresh(cancellationToken);
                _token = token;
                _expiresAt = DateTime.UtcNow.AddSeconds(Math.Max(expiresIn - 60, 30));
                return _token;
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Invalidate()
        {
            _token = null;
        }
    }
}
