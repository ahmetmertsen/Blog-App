using blogapp_server.Application.Repositories;
using blogapp_server.Domain.Entities;
using blogapp_server.Persistence.Context;
using blogapp_server.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Repositories
{
    public class LikeRepository : Repository<Like>, ILikeRepository
    {
        private readonly BlogAppDbContext _context;

        public LikeRepository(BlogAppDbContext context) : base(context) { _context = context; }

        public async Task<List<Like>> GetLikesByUserIdAsync(int userId) => await _context.Likes
            .Where(l => l.UserId == userId)
            .ToListAsync();

        public async Task<List<Like>> GetLikesByPostIdAsync(int postId) => await _context.Likes
            .Where(l => l.PostId == postId)
            .ToListAsync();
    }
}
