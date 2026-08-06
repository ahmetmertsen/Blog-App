using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories.Common;
using buduns_server.Domain.Entities.Common;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace buduns_server.Persistence.Repositories.Common
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly BudunsDbContext _context;

        public Repository(BudunsDbContext context)
        {
            _context = context;
        }

        public DbSet<T> Table => _context.Set<T>();

        public virtual async Task<List<T>> GetAllAsync() => await Table.ToListAsync();

        public virtual async Task<T?> GetByIdAsync(int id) => await Table.FindAsync(id);

        public async Task AddAsync(T entity) => await Table.AddAsync(entity);

        public void Update(T entity) => Table.Update(entity);

        public async Task DeleteAsync(int id)
        {
            var entity = await Table.FindAsync(id);
            if (entity == null)
            {
                throw new NotFoundException("Entity bulunamadı!");
            }

            Table.Remove(entity);
        }
    }
}
