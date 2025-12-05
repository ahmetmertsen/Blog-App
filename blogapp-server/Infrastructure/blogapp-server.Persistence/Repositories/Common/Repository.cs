using blogapp_server.Application.Repositories.Common;
using blogapp_server.Domain.Entities.Common;
using blogapp_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Repositories.Common
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly BlogAppDbContext _context;

        public Repository(BlogAppDbContext context)
        {
            _context = context;
        }

        public DbSet<T> Table => _context.Set<T>();

        public async Task<List<T>> GetAllAsync() => await Table.ToListAsync();

        public async Task<T> GetByIdAsync(int id) => await Table.FindAsync(id);

        public async Task<EntityEntry<T>> AddAsync(T entity) => await Table.AddAsync(entity);

        public EntityEntry<T> Update(T entity) => Table.Update(entity);

        public async Task<EntityEntry<T>> DeleteAsync(int id)
        {
            var entity = await Table.FindAsync(id);
            if (entity == null)
            {
                //Exception yazılacak.
            }
            var result = Table.Remove(entity);
            return result;
        }
    }
}
