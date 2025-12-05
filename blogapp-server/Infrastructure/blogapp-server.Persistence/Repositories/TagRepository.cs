using blogapp_server.Application.Repositories;
using blogapp_server.Domain.Entities;
using blogapp_server.Persistence.Context;
using blogapp_server.Persistence.Repositories.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Repositories
{
    public class TagRepository : Repository<Tag> , ITagRepository
    {
        private readonly BlogAppDbContext _context;

        public TagRepository(BlogAppDbContext context) : base(context) { _context = context; }
    }
}
