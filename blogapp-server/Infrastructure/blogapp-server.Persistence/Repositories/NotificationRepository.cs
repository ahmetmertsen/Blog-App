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
    public class NotificationRepository : Repository<Notification> , INotificationRepository
    {
        private readonly BlogAppDbContext _context;

        public NotificationRepository(BlogAppDbContext context) : base(context) { _context = context; }

        public async Task<List<Notification>> GetAllNotificationsByUserIdAsync(int userId) => await _context.Notifications
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }
}
