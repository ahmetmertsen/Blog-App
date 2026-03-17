using blogapp_server.Application.Repositories.Common;
using blogapp_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Repositories
{
    public interface ILikeRepository : IRepository<Like>
    {
        Task<List<Like>> GetAllLikesByUsernameAsync(string userName);
    }
}
