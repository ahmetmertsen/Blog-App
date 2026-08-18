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
    }
}
