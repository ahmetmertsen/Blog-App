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
    public class CommentRepository : Repository<Comment>, ICommentRepository
    {
        private readonly BlogAppDbContext _context;

        public CommentRepository(BlogAppDbContext context) : base(context) { _context = context; }

        public async Task<List<Comment>> GetAllCommentsByUsernameAsync(string userName) => await _context.Comments
            .Where(c => c.User.UserName == userName)
            .ToListAsync();
    }
}
