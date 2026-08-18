using buduns_server.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace buduns_server.Persistence.Context
{
    internal static class DbContextSaveExtensions
    {
        /// <summary>
        /// EF Core'a ozgu eszamanlilik istisnasini uygulama seviyesine cevirir.
        /// Veritabanina yazan her yol buradan gecmeli: dogrudan
        /// <c>SaveChangesAsync</c> cagiran bir kod, ceviriyi atladigi icin
        /// 409 yerine 500 uretir.
        /// </summary>
        public static async Task<int> SaveTranslatedAsync(this BudunsDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                return await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException();
            }
        }
    }
}
