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
    public class BookmarkRepository : Repository<Bookmark> , IBookmarkRepository
    {
        private readonly BlogAppDbContext _context;

        public BookmarkRepository(BlogAppDbContext context) : base(context) { _context = context; }

        public async Task<List<Bookmark>> GetBookmarksByUserIdAsync(int userId) => await _context.Bookmarks
            .Where(b => b.UserId == userId)
            .ToListAsync();

    }
}
