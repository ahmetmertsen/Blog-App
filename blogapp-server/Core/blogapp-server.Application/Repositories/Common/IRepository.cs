using blogapp_server.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Repositories.Common
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<List<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task<EntityEntry<T>> AddAsync(T entity);
        EntityEntry<T> Update(T entity);
        Task<EntityEntry<T>> DeleteAsync(int id);
    }
}
