using buduns_server.Domain.Entities.Identity;

namespace buduns_server.Application.Repositories
{
    /// <summary>
    /// Kullanici okuma ve durum guncellemesi icin dar kapi. IRepository&lt;T&gt;
    /// turetilmiyor: User bir BaseEntity degil, IdentityUser&lt;int&gt;. Ekleme ve
    /// silme bilerek yok; hesap yaratmak parola hash'lemesi gerektiriyor ve
    /// yalnizca Identity uzerinden yapilmali.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        void Update(User user);
    }
}
