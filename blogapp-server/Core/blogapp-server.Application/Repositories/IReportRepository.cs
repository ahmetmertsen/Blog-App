using blogapp_server.Application.Repositories.Common;
using blogapp_server.Domain.Entities;
using blogapp_server.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Repositories
{
    public interface IReportRepository : IRepository<Report>
    {
        Task<List<Report>> GetAllWithDetailsAsync();

        Task<Report?> GetByIdWithDetailsAsync(int id);

        Task<List<Report>> GetAllByStatusAsync(ReportStatus status);

        Task<List<Report>> GetAllByTargetTypeAsync(ReportTargetType targetType);

        Task<List<Report>> GetAllByReporterUserIdAsync(int reporterUserId);

        Task<List<Report>> GetAllByTargetPostIdAsync(int postId);

        Task<List<Report>> GetAllByTargetUserIdAsync(int userId);

        Task<bool> HasPendingPostReportAsync(int reporterUserId, int postId);

        Task<bool> HasPendingUserReportAsync(int reporterUserId, int targetUserId);

        Task<List<Report>> GetFilteredReportsAsync(ReportStatus? status, ReportTargetType? targetType, int page, int size);
    }
}
