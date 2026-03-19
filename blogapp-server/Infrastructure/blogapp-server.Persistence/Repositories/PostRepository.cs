using blogapp_server.Application.Repositories;
using blogapp_server.Application.Repositories.Common;
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
    public class PostRepository : Repository<Post> , IPostRepository
    {
        private readonly BlogAppDbContext _context;

        public PostRepository(BlogAppDbContext context) : base(context) { _context = context; }

        
        public override async Task<List<Post>> GetAllAsync() =>
            await _context.Posts
                .Include(p => p.Tags)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.Bookmarks)
                .ToListAsync();
        public override async Task<Post?> GetByIdAsync(int id) =>
            await _context.Posts
                .Include(p => p.Tags)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.Bookmarks)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<List<Post?>> GetAllByTagIdAsync(int TagId) => await _context.Posts
            .Include(p => p.Tags)
            .Where(p => p.Tags.Any(t => t.Id == TagId))
            .ToListAsync();

        public async Task<Post?> GetByIdWithTagsAsync(int id) =>
            await _context.Posts
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);
    }
}
