using blogapp_server.Application.Repositories.Common;
using blogapp_server.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Repositories
{
    public interface IFollowerRepository : IRepository<Follower>
    {
        Task<bool> IsFollowExistsAsync(int followerId, int followingId);
        Task<Follower?> GetFollowAsync(int followerId, int followingId);
        EntityEntry<Follower> Delete(Follower entity);
        Task<List<Follower>> GetAllFollowersByUserIdAsync(int userId);
        Task<List<Follower>> GetAllFollowingsByUserIdAsync(int userId);
    }
}
