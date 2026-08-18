using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace buduns_server.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BudunsDbContext _context;

        public UnitOfWork(BudunsDbContext context)
        {
            _context = context;
        }

        // EF Core'a ozgu eszamanlilik istisnasi burada uygulama seviyesine
        // cevriliyor; boylece Application katmani EF Core'a bagli kalmiyor.
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException();
            }
        }

        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            // Sinir zaten acik: ikinci bir transaction acmak EF'te
            // InvalidOperationException uretir. Mevcut sinira katilmak,
            // ic ice cagriyi ayri bir isleme bolmekten de dogru.
            if (_context.Database.CurrentTransaction != null)
            {
                return await operation(cancellationToken);
            }

            // Dispose commit edilmemis transaction'i geri alir; istisna
            // yolunda ayrica RollbackAsync cagirmaya gerek yok.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
    }
}
