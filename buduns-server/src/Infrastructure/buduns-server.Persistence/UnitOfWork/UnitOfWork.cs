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
    }
}
