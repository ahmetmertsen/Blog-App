using buduns_server.Application.Mapping;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;

namespace buduns_server.UnitTests.Mapping;

/// <summary>
/// ReportProfile en karmasik profildi: hedef kullanici/icerik yuklenmemisse
/// sikayet aninda alinan snapshot alanlarina dusen kosullu ifadeler iceriyordu.
/// Bu kurallar hicbir test tarafindan korunmuyordu.
/// </summary>
public class ReportMappingTests
{
    private static Report CreateReport(ReportTargetType targetType) => new()
    {
        Id = 1,
        ReporterUserId = 10,
        TargetType = targetType,
        Reason = ReportReason.Harassment,
        Status = ReportStatus.Pending,
        Description = "aciklama",
        CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
        TargetOwnerUserNameSnapshot = "snapshot-user",
        TargetOwnerFullNameSnapshot = "Snapshot User",
        TargetContentSnapshot = "snapshot-icerik"
    };

    [Fact]
    public void UserReport_ShouldFallBackToSnapshotWhenTargetUserNotLoaded()
    {
        var report = CreateReport(ReportTargetType.User);
        report.TargetUserId = 20;
        report.TargetUser = null;

        var dto = report.ToListDto();

        Assert.Equal("snapshot-user", dto.TargetUserName);
        Assert.Equal("Snapshot User", dto.TargetUserFullName);
        Assert.Equal(20, dto.TargetOwnerUserId);
    }

    [Fact]
    public void UserReport_ShouldPreferLoadedTargetUserOverSnapshot()
    {
        var report = CreateReport(ReportTargetType.User);
        report.TargetUserId = 20;
        report.TargetUser = new User { Id = 20, UserName = "canli-user", FullName = "Canli User" };

        var dto = report.ToListDto();

        Assert.Equal("canli-user", dto.TargetUserName);
        Assert.Equal("Canli User", dto.TargetUserFullName);
    }

    [Fact]
    public void PostReport_ShouldResolveOwnerFromLoadedPost()
    {
        var report = CreateReport(ReportTargetType.Post);
        report.TargetPostId = 55;
        report.TargetPost = new Post { Id = 55, UserId = 77, Content = "post icerigi" };

        Assert.Equal(77, report.ToListDto().TargetOwnerUserId);
        // Post yuklendigi icin snapshot yerine canli icerik kullanilir.
        Assert.Equal("post icerigi", report.ToDetailDto().TargetPostContent);
    }

    [Fact]
    public void PostReport_ShouldFallBackToContentSnapshotWhenPostDeleted()
    {
        var report = CreateReport(ReportTargetType.Post);
        report.TargetPostId = 55;
        report.TargetPost = null;

        var dto = report.ToDetailDto();

        Assert.Equal("snapshot-icerik", dto.TargetPostContent);
        Assert.Null(dto.TargetOwnerUserId);
    }

    [Fact]
    public void ExplicitTargetOwnerUserId_ShouldWinOverDerivedValue()
    {
        var report = CreateReport(ReportTargetType.Post);
        report.TargetOwnerUserId = 99;
        report.TargetPost = new Post { Id = 55, UserId = 77 };

        Assert.Equal(99, report.ToListDto().TargetOwnerUserId);
    }

    [Fact]
    public void CommentReport_ShouldResolveCommentUserFromNavigation()
    {
        var report = CreateReport(ReportTargetType.Comment);
        report.TargetCommentId = 8;
        report.TargetComment = new Comment { Id = 8, UserId = 44, Content = "yorum", User = new User { Id = 44, UserName = "yorumcu" } };

        var dto = report.ToDetailDto();

        Assert.Equal(44, dto.TargetCommentUserId);
        Assert.Equal("yorumcu", dto.TargetCommentUserName);
        Assert.Equal("yorum", dto.TargetCommentContent);
    }

    [Fact]
    public void CommentReport_ShouldFallBackToSnapshotWhenCommentDeleted()
    {
        var report = CreateReport(ReportTargetType.Comment);
        report.TargetCommentId = 8;
        report.TargetOwnerUserId = 44;
        report.TargetComment = null;

        var dto = report.ToDetailDto();

        Assert.Equal(44, dto.TargetCommentUserId);
        Assert.Equal("snapshot-user", dto.TargetCommentUserName);
        Assert.Equal("snapshot-icerik", dto.TargetCommentContent);
    }

    [Fact]
    public void DetailDto_ShouldMapReporterReviewerAndCreatedDate()
    {
        var report = CreateReport(ReportTargetType.User);
        report.ReporterUser = new User { Id = 10, UserName = "sikayetci", FullName = "Sikayetci", Email = "s@test.com" };
        report.ReviewedByUserId = 30;
        report.ReviewedByUser = new User { Id = 30, UserName = "moderator" };
        report.ReviewNote = "not";
        report.ReviewedDate = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc);

        var dto = report.ToDetailDto();

        Assert.Equal("sikayetci", dto.ReporterUserName);
        Assert.Equal("s@test.com", dto.ReporterEmail);
        Assert.Equal("moderator", dto.ReviewedByUserName);
        Assert.Equal("not", dto.ReviewNote);
        // CreatedDate, entity'deki CreatedAt'ten gelir (ad farkli).
        Assert.Equal(report.CreatedAt, dto.CreatedDate);
        Assert.Equal(report.ReviewedDate, dto.ReviewedDate);
    }

    [Fact]
    public void RelatedReportAndModerationActionLists_ShouldMap()
    {
        var report = CreateReport(ReportTargetType.User);
        report.ReporterUser = new User { Id = 10, UserName = "sikayetci", FullName = "Sikayetci" };

        var related = new[] { report }.ToRelatedDtoList();
        Assert.Single(related);
        Assert.Equal("sikayetci", related[0].ReporterUserName);
        Assert.Equal("aciklama", related[0].Description);

        var actions = new[]
        {
            new ModerationAction { Id = 2, ActionType = ModerationActionType.BanUser, ModeratorUserId = 30, Note = "kalici", ModeratorUser = new User { Id = 30, UserName = "moderator" } }
        }.ToDtoList();

        Assert.Single(actions);
        Assert.Equal("moderator", actions[0].ModeratorUserName);
        Assert.Equal(ModerationActionType.BanUser, actions[0].ActionType);
        Assert.Equal("kalici", actions[0].Note);
    }

    [Fact]
    public void ModerationAction_ShouldNotThrowWhenModeratorNotLoaded()
    {
        var dto = new ModerationAction { Id = 2, ModeratorUserId = 30, ModeratorUser = null! }.ToDto();

        Assert.Equal(30, dto.ModeratorUserId);
        Assert.Null(dto.ModeratorUserName);
    }
}
