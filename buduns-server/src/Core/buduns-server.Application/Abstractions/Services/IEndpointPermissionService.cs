namespace buduns_server.Application.Abstractions.Services
{
    public interface IEndpointPermissionService
    {
        /// <summary>
        /// Kullanicinin bir yetki koduna erisip erisemedigini soyler.
        /// Endpoint'in veritabaninda kaydi varsa karar o kaydin rollerine gore
        /// verilir; kayit yoksa kodda bildirilen varsayilan rollere dusulur.
        /// Kaydi olup hic rolu olmayan bir endpoint herkese kapalidir — yani
        /// "kaydi sil" degil, "rolleri bosalt" kapatmanin dogru yoludur.
        /// </summary>
        Task<bool> HasAccessAsync(int userId, string code, IReadOnlyList<string> defaultRoles, CancellationToken cancellationToken = default);

        /// <summary>Bir yetki kodunun onbellekteki rol kumesini dusurur.</summary>
        Task InvalidateAsync(string code, CancellationToken cancellationToken = default);
    }
}
