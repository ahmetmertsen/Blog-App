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
    public class UtilityRepository : Repository<Utility>, IUtilityRepository
    {
        private readonly BlogAppDbContext _context;

        public UtilityRepository(BlogAppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Utility?> GetByNameAsync(string name)
        {
            return await _context.Utilities.FirstOrDefaultAsync(u => u.Name == name);
        }
    }
}
