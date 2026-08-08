namespace buduns_server.Application.Abstractions.Services
{
    /// <summary>
    /// Dagitik onbellek soyutlamasi. Uygulama katmani hangi saglayicinin
    /// (Redis, bellek, hicbiri) kullanildigini bilmez.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Anahtar onbellekte varsa onbellekten, yoksa <paramref name="factory"/>
        /// uzerinden uretip onbelleklendikten sonra doner. Onbellege erisilemezse
        /// hata firlatmaz; dogrudan <paramref name="factory"/> sonucunu doner.
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, TimeSpan timeToLive, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default) where T : class;

        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
