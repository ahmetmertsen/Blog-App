using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;

namespace buduns_server.Application.Mapping
{
    public static class ReportMappings
    {
        public static ReportListDto ToListDto(this Report report) => new()
        {
            Id = report.Id,
            ReporterUserId = report.ReporterUserId,
            ReporterUserName = report.ReporterUser?.UserName,
            ReporterFullName = report.ReporterUser?.FullName,
            TargetType = report.TargetType,
            Priority = ReportPriorityHelper.GetPriority(report.Reason),
            TargetPostId = report.TargetPostId,
            TargetUserId = report.TargetUserId,
            TargetUserName = ResolveTargetUserName(report),
            TargetUserFullName = ResolveTargetUserFullName(report),
            TargetCommentId = report.TargetCommentId,
            TargetOwnerUserId = ResolveTargetOwnerUserId(report),
            TargetOwnerUserName = report.TargetOwnerUserNameSnapshot,
            TargetOwnerFullName = report.TargetOwnerFullNameSnapshot,
            Reason = report.Reason,
            Status = report.Status,
            CreatedAt = report.CreatedAt
        };

        public static ReportDetailDto ToDetailDto(this Report report) => new()
        {
            Id = report.Id,
            ReporterUserId = report.ReporterUserId,
            ReporterUserName = report.ReporterUser?.UserName,
            ReporterFullName = report.ReporterUser?.FullName,
            ReporterEmail = report.ReporterUser?.Email,
            TargetType = report.TargetType,
            Priority = ReportPriorityHelper.GetPriority(report.Reason),
            TargetPostId = report.TargetPostId,
            TargetPostContent = report.TargetPost != null ? report.TargetPost.Content : report.TargetContentSnapshot,
            TargetUserId = report.TargetUserId,
            TargetUserName = ResolveTargetUserName(report),
            TargetUserFullName = ResolveTargetUserFullName(report),
            TargetUserEmail = report.TargetUser?.Email,
            TargetCommentId = report.TargetCommentId,
            TargetCommentContent = report.TargetComment != null ? report.TargetComment.Content : report.TargetContentSnapshot,
            TargetCommentUserId = report.TargetComment != null
                ? report.TargetComment.UserId
                : report.TargetType == ReportTargetType.Comment ? report.TargetOwnerUserId : null,
            TargetCommentUserName = report.TargetComment?.User != null
                ? report.TargetComment.User.UserName
                : report.TargetType == ReportTargetType.Comment ? report.TargetOwnerUserNameSnapshot : null,
            TargetOwnerUserId = ResolveTargetOwnerUserId(report),
            TargetOwnerUserName = report.TargetOwnerUserNameSnapshot,
            TargetOwnerFullName = report.TargetOwnerFullNameSnapshot,
            TargetContentSnapshot = report.TargetContentSnapshot,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedByUserName = report.ReviewedByUser?.UserName,
            CreatedDate = report.CreatedAt,
            ReviewedDate = report.ReviewedDate,
            ReviewNote = report.ReviewNote
        };

        public static RelatedReportDto ToRelatedDto(this Report report) => new()
        {
            Id = report.Id,
            ReporterUserId = report.ReporterUserId,
            ReporterUserName = report.ReporterUser?.UserName,
            ReporterFullName = report.ReporterUser?.FullName,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedByUserName = report.ReviewedByUser?.UserName,
            ReviewNote = report.ReviewNote,
            CreatedAt = report.CreatedAt,
            ReviewedDate = report.ReviewedDate
        };

        public static ModerationActionDto ToDto(this ModerationAction action) => new()
        {
            Id = action.Id,
            ActionType = action.ActionType,
            ModeratorUserId = action.ModeratorUserId,
            ModeratorUserName = action.ModeratorUser?.UserName,
            Note = action.Note,
            ExpiresAt = action.ExpiresAt,
            CreatedAt = action.CreatedAt
        };

        public static List<RelatedReportDto> ToRelatedDtoList(this IEnumerable<Report> reports) => reports.Select(ToRelatedDto).ToList();

        public static List<ModerationActionDto> ToDtoList(this IEnumerable<ModerationAction> actions) => actions.Select(ToDto).ToList();

        // Hedef kullanici yuklenmemisse, sikayet aninda alinan snapshot'a dusulur.
        private static string? ResolveTargetUserName(Report report) => report.TargetUser != null
            ? report.TargetUser.UserName
            : report.TargetType == ReportTargetType.User ? report.TargetOwnerUserNameSnapshot : null;

        private static string? ResolveTargetUserFullName(Report report) => report.TargetUser != null
            ? report.TargetUser.FullName
            : report.TargetType == ReportTargetType.User ? report.TargetOwnerFullNameSnapshot : null;

        private static int? ResolveTargetOwnerUserId(Report report) => report.TargetOwnerUserId ?? report.TargetType switch
        {
            ReportTargetType.User => report.TargetUserId,
            ReportTargetType.Post => report.TargetPost?.UserId,
            ReportTargetType.Comment => report.TargetComment?.UserId,
            _ => null
        };
    }
}
