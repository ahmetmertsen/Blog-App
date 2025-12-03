using blogapp_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Context
{
    public class BlogAppDbContext : IdentityDbContext<User,Role, int>
    {
        public BlogAppDbContext(DbContextOptions options) : base(options) { }

    }
}
