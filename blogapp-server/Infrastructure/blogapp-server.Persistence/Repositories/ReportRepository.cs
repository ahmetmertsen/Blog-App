using blogapp_server.Application.Repositories;
using blogapp_server.Domain.Entities;
using blogapp_server.Domain.Enums;
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
    public class ReportRepository : Repository<Report>, IReportRepository
    {
        private readonly BlogAppDbContext _context;
        public ReportRepository(BlogAppDbContext context) : base(context) { _context = context; }

        public override async Task<List<Report>> GetAllAsync() => await _context.Reports.ToListAsync();

        public override async Task<Report?> GetByIdAsync(int id) => await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<List<Report>> GetAllWithDetailsAsync() => await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetPost)
                .Include(r => r.TargetUser)
                .Include(r => r.ReviewedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<Report?> GetByIdWithDetailsAsync(int id) => await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetPost)
                .Include(r => r.TargetUser)
                .Include(r => r.ReviewedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<List<Report>> GetAllByStatusAsync(ReportStatus status) => await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetPost)
                .Include(r => r.TargetUser)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<List<Report>> GetAllByTargetTypeAsync(ReportTargetType targetType) => await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetPost)
                .Include(r => r.TargetUser)
                .Where(r => r.TargetType == targetType)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<List<Report>> GetAllByReporterUserIdAsync(int reporterUserId) => await _context.Reports
                .Include(r => r.TargetPost)
                .Include(r => r.TargetUser)
                .Where(r => r.ReporterUserId == reporterUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<List<Report>> GetAllByTargetPostIdAsync(int postId) => await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetPost)
                .Where(r => r.TargetType == ReportTargetType.Post && r.TargetPostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<List<Report>> GetAllByTargetUserIdAsync(int userId) => await _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetUser)
                .Where(r => r.TargetType == ReportTargetType.User && r.TargetUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();


        public async Task<bool> HasPendingPostReportAsync(int reporterUserId, int postId) => await _context.Reports.AnyAsync(r =>
                r.ReporterUserId == reporterUserId &&
                r.TargetType == ReportTargetType.Post &&
                r.TargetPostId == postId &&
                r.Status == ReportStatus.Pending);

        public async Task<bool> HasPendingUserReportAsync(int reporterUserId, int targetUserId) => await _context.Reports.AnyAsync(r =>
                r.ReporterUserId == reporterUserId &&
                r.TargetType == ReportTargetType.User &&
                r.TargetUserId == targetUserId &&
                r.Status == ReportStatus.Pending);


        public async Task<List<Report>> GetFilteredReportsAsync(ReportStatus? status, ReportTargetType? targetType, int page, int size)
        {
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? 20 : size;

            IQueryable<Report> query = _context.Reports
                .Include(r => r.ReporterUser)
                .Include(r => r.TargetPost)
                .Include(r => r.TargetUser)
                .Include(r => r.ReviewedByUser)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (targetType.HasValue)
            {
                query = query.Where(r => r.TargetType == targetType.Value);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }
    }
}
