using buduns_server.Application.Abstractions.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace buduns_server.Infrastructure.Services.Caching
{
    public sealed class DistributedCacheService : ICacheService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// Anahtar basina tek ucus. TTL doldugu anda gelen es zamanli istekler
        /// tek bir uretime indirgenir; aksi halde pahali sorgu her istek icin
        /// bastan calisir (cache stampede). Anahtarlar kullanicidan degil
        /// koddan geldigi icin sozluk sinirsiz buyumez.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new();

        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheService> _logger;

        public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrSetAsync<T>(string key, TimeSpan timeToLive, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default) where T : class
        {
            var cached = await TryGetAsync<T>(key, cancellationToken);
            if (cached != null)
            {
                return cached;
            }

            var keyLock = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await keyLock.WaitAsync(cancellationToken);
            try
            {
                // Kilidi beklerken baska bir istek onbellegi doldurmus olabilir.
                cached = await TryGetAsync<T>(key, cancellationToken);
                if (cached != null)
                {
                    return cached;
                }

                var value = await factory(cancellationToken);
                await TrySetAsync(key, value, timeToLive, cancellationToken);
                return value;
            }
            finally
            {
                keyLock.Release();
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Onbellek anahtari silinemedi. CacheKey: {CacheKey}", key);
            }
        }

        private async Task<T?> TryGetAsync<T>(string key, CancellationToken cancellationToken) where T : class
        {
            try
            {
                var payload = await _cache.GetAsync(key, cancellationToken);
                if (payload == null || payload.Length == 0)
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                // Redis erisilemiyorsa istek basarisiz olmaz, veri kaynagina duser.
                _logger.LogWarning(exception, "Onbellek okunamadi, veri kaynagina dusuluyor. CacheKey: {CacheKey}", key);
                return null;
            }
        }

        private async Task TrySetAsync<T>(string key, T? value, TimeSpan timeToLive, CancellationToken cancellationToken) where T : class
        {
            if (value == null)
            {
                return;
            }

            try
            {
                var payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
                await _cache.SetAsync(key, payload, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = timeToLive }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Onbellege yazilamadi. CacheKey: {CacheKey}", key);
            }
        }
    }
}
