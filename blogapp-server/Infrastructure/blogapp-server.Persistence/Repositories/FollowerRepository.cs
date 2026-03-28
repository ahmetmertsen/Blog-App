using blogapp_server.Application.Repositories;
using blogapp_server.Domain.Entities;
using blogapp_server.Persistence.Context;
using blogapp_server.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Repositories
{
    public class FollowerRepository : Repository<Follower>, IFollowerRepository
    {
        private readonly BlogAppDbContext _context;

        public FollowerRepository(BlogAppDbContext context) : base(context) { _context = context; }

        public async Task<bool> IsFollowExistsAsync(int followerId, int followingId) =>
            await _context.Followers.AnyAsync(x => x.FollowerId == followerId && x.FollowingId == followingId);


        public async Task<Follower?> GetFollowAsync(int followerId, int followingId) =>
            await _context.Followers.FirstOrDefaultAsync(x => x.FollowerId == followerId && x.FollowingId == followingId);

        public EntityEntry<Follower> Delete(Follower entity) => Table.Remove(entity);

        public async Task<List<Follower>> GetAllFollowersByUserIdAsync(int userId) =>
            await _context.Followers
                .Include(f => f.FollowerUser)
                .Include(f => f.FollowingUser)
                .Where(f => f.FollowingUser.Id == userId)
                .ToListAsync();

        public async Task<List<Follower>> GetAllFollowingsByUserIdAsync(int userId) =>
            await _context.Followers
                .Include(f => f.FollowerUser)
                .Include(f => f.FollowingUser)
                .Where(f => f.FollowerUser.Id == userId)
                .ToListAsync();

    }
}
