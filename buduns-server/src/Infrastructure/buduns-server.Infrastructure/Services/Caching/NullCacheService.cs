using buduns_server.Application.Abstractions.Services;

namespace buduns_server.Infrastructure.Services.Caching
{
    /// <summary>
    /// Redis yapilandirilmadiginda devreye girer. Her cagriyi dogrudan veri
    /// kaynagina gecirir; boylece onbellek olmadan da uygulama calisir.
    /// </summary>
    public sealed class NullCacheService : ICacheService
    {
        public Task<T> GetOrSetAsync<T>(string key, TimeSpan timeToLive, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default) where T : class =>
            factory(cancellationToken);

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
