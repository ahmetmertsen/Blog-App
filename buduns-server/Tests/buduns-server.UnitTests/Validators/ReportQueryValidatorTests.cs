using buduns_server.Application.Features.Report.Commands.CreateCommentReport;
using buduns_server.Application.Features.Report.Commands.CreatePostReport;
using buduns_server.Application.Features.Report.Commands.CreateUserReport;
using buduns_server.Application.Features.Report.Commands.ReviewReport;
using buduns_server.Application.Features.Report.Queries.GetReports;
using buduns_server.Domain.Enums;

namespace buduns_server.UnitTests.Validators;

/// <summary>
/// ReportValidatorTests post sikayeti ve inceleme kararinin bir kismini
/// kapsiyordu. Buradaki testler yorum/kullanici sikayetlerini, sikayet
/// listeleme filtrelerini ve inceleme kararinin kalan dallarini kapsar.
/// </summary>
public class ReportQueryValidatorTests
{
    [Fact]
    public async Task CreateCommentReport_ValidRequest_ShouldSucceed()
    {
        var result = await new CreateCommentReportCommandValidator().ValidateAsync(new CreateCommentReportCommand
        {
            CommentId = 4,
            Reason = ReportReason.Harassment,
            Description = "Hakaret iceriyor"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateCommentReport_NullDescriptionForNonOtherReason_ShouldSucceed()
    {
        var result = await new CreateCommentReportCommandValidator().ValidateAsync(new CreateCommentReportCommand
        {
            CommentId = 4,
            Reason = ReportReason.Spam,
            Description = null
        });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCommentReport_OtherReasonWithoutDescription_ShouldFail(string? description)
    {
        var result = await new CreateCommentReportCommandValidator().ValidateAsync(new CreateCommentReportCommand
        {
            CommentId = 4,
            Reason = ReportReason.Other,
            Description = description
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCommentReportCommand.Description));
    }

    [Fact]
    public async Task CreateCommentReport_InvalidReasonAndCommentId_ShouldFailBothProperties()
    {
        var result = await new CreateCommentReportCommandValidator().ValidateAsync(new CreateCommentReportCommand
        {
            CommentId = 0,
            Reason = (ReportReason)9999
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCommentReportCommand.CommentId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCommentReportCommand.Reason));
    }

    [Fact]
    public async Task CreateCommentReport_DescriptionLongerThanLimit_ShouldFail()
    {
        var result = await new CreateCommentReportCommandValidator().ValidateAsync(new CreateCommentReportCommand
        {
            CommentId = 4,
            Reason = ReportReason.Spam,
            Description = new string('a', 1001)
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateCommentReportCommand.Description));
    }

    [Fact]
    public async Task CreateUserReport_ValidRequest_ShouldSucceed()
    {
        var result = await new CreateUserReportCommandValidator().ValidateAsync(new CreateUserReportCommand
        {
            TargetUserId = 9,
            Reason = ReportReason.Impersonation,
            Description = "Baskasini taklit ediyor"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateUserReport_InvalidTargetAndOtherWithoutDescription_ShouldFail()
    {
        var result = await new CreateUserReportCommandValidator().ValidateAsync(new CreateUserReportCommand
        {
            TargetUserId = 0,
            Reason = ReportReason.Other,
            Description = string.Empty
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateUserReportCommand.TargetUserId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateUserReportCommand.Description));
    }

    [Fact]
    public async Task CreatePostReport_DescriptionAtLimit_ShouldSucceed()
    {
        var result = await new CreatePostReportCommandValidator().ValidateAsync(new CreatePostReportCommand
        {
            PostId = 1,
            Reason = ReportReason.Spam,
            Description = new string('a', 1000)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ReviewReport_PendingStatus_ShouldFail()
    {
        // Sonuclandirilmis bir sikayet tekrar beklemeye alinamaz.
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = ReportStatus.Pending,
            ActionType = ModerationActionType.None,
            ReviewNote = "not"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ReviewReportCommand.Status));
    }

    [Fact]
    public async Task ReviewReport_InvalidStatusEnum_ShouldFail()
    {
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = (ReportStatus)999,
            ActionType = ModerationActionType.None,
            ReviewNote = "not"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ReviewReportCommand.Status));
    }

    [Fact]
    public async Task ReviewReport_NoViolationWithAction_ShouldFail()
    {
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = ReportStatus.ResolvedNoViolation,
            ActionType = ModerationActionType.HidePost,
            ReviewNote = "ihlal yok"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ReviewReportCommand.ActionType));
    }

    [Fact]
    public async Task ReviewReport_InReviewWithAction_ShouldFail()
    {
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = ReportStatus.InReview,
            ActionType = ModerationActionType.BanUser,
            ReviewNote = "inceleniyor"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ReviewReportCommand.ActionType));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public async Task ReviewReport_SuspendUserWithValidDuration_ShouldSucceed(int suspensionDays)
    {
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.SuspendUser,
            SuspensionDays = suspensionDays,
            ReviewNote = "gecici uzaklastirma"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ReviewReport_NonSuspendActionIgnoresSuspensionDays_ShouldSucceed()
    {
        // SuspensionDays kurali yalnizca SuspendUser aksiyonunda islemeli.
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.BanUser,
            SuspensionDays = null,
            ReviewNote = "kalici yasak"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ReviewReport_InvalidActionEnum_ShouldFail()
    {
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = (ModerationActionType)999,
            ReviewNote = "not"
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ReviewReportCommand.ActionType));
    }

    [Fact]
    public async Task ReviewReport_ReviewNoteLongerThanLimit_ShouldFail()
    {
        var result = await new ReviewReportCommandValidator().ValidateAsync(new ReviewReportCommand
        {
            ReportId = 1,
            Status = ReportStatus.InReview,
            ActionType = ModerationActionType.None,
            ReviewNote = new string('a', 1001)
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ReviewReportCommand.ReviewNote));
    }

    [Fact]
    public async Task GetReports_NoFilters_ShouldSucceed()
    {
        var result = await new GetReportsQueryValidator().ValidateAsync(new GetReportsQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetReports_AllFiltersValid_ShouldSucceed()
    {
        var result = await new GetReportsQueryValidator().ValidateAsync(new GetReportsQuery
        {
            Status = ReportStatus.Pending,
            TargetType = ReportTargetType.Comment,
            Reason = ReportReason.Spam,
            FromDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetReports_SameFromAndToDate_ShouldSucceed()
    {
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await new GetReportsQueryValidator().ValidateAsync(new GetReportsQuery { FromDate = date, ToDate = date });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetReports_ToDateBeforeFromDate_ShouldFail()
    {
        var result = await new GetReportsQueryValidator().ValidateAsync(new GetReportsQuery
        {
            FromDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetReportsQuery.ToDate));
    }

    [Fact]
    public async Task GetReports_OnlyOneDateProvided_ShouldSucceed()
    {
        // Tarih karsilastirmasi yalnizca iki tarih birden verildiginde calismali.
        var onlyFrom = await new GetReportsQueryValidator().ValidateAsync(new GetReportsQuery { FromDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) });
        var onlyTo = await new GetReportsQueryValidator().ValidateAsync(new GetReportsQuery { ToDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        Assert.True(onlyFrom.IsValid);
        Assert.True(onlyTo.IsValid);
    }

    [Fact]
    public async Task GetReports_InvalidEnumFilters_ShouldFail()
    {
        var result = await new GetReportsQueryValidator().ValidateAsync(new GetReportsQuery
        {
            Status = (ReportStatus)999,
            TargetType = (ReportTargetType)999,
            Reason = (ReportReason)999
        });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetReportsQuery.Status));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetReportsQuery.TargetType));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetReportsQuery.Reason));
    }
}
