namespace buduns_server.Application.UnitOfWork
{
    /// <summary>
    /// Yazmalarin ne zaman kalici oldugunu yoneten sinir. Repository'ler
    /// degisiklikleri yalnizca isaretler; kalici hale gelmeleri buradan gecer.
    /// Repository'ler bu arayuzden dagitilmaz, dogrudan enjekte edilir.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Verilen isi tek bir transaction icinde calistirir: is tamamlanirsa
        /// commit, istisna cikarsa rollback. Sinir zaten acikken cagrilirsa
        /// yeni transaction acilmaz, mevcut sinira katilinir.
        /// </summary>
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);
    }
}
