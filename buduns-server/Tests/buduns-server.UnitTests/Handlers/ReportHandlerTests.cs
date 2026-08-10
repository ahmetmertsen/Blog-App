using buduns_server.Application.Common.Options;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Report.Commands.CreateCommentReport;
using buduns_server.Application.Features.Report.Commands.CreatePostReport;
using buduns_server.Application.Features.Report.Commands.CreateUserReport;
using buduns_server.Application.Features.Report.Queries.GetById;
using buduns_server.Application.Features.Report.Queries.GetReports;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ReportEntity = buduns_server.Domain.Entities.Report;

namespace buduns_server.UnitTests.Handlers;

public class ReportHandlerTests
{
    [Fact]
    public async Task CreatePostReport_ShouldSnapshotOwnerAndContent()
    {
        var unitOfWork = CreateUnitOfWork();
        var owner = HandlerTestContext.CreateUser(3, "yazar");
        unitOfWork.PostRepository.GetByIdAsync(7).Returns(new Post { Id = 7, UserId = 3, User = owner, Content = "  sikayet edilen icerik  ", Status = PostStatus.Published, isPublished = true, isDeleted = false });
        ReportEntity? persisted = null;
        await unitOfWork.ReportRepository.AddAsync(Arg.Do<ReportEntity>(report => persisted = report));
        var handler = new CreatePostReportCommandHandler(unitOfWork, NullLogger<CreatePostReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        var response = await handler.Handle(new CreatePostReportCommand { PostId = 7, UserId = 9, Reason = ReportReason.Spam, Description = "  aciklama  " }, CancellationToken.None);

        Assert.Equal(ReportTargetType.Post, persisted!.TargetType);
        Assert.Equal(7, persisted.TargetPostId);
        Assert.Equal(3, persisted.TargetOwnerUserId);
        Assert.Equal("yazar", persisted.TargetOwnerUserNameSnapshot);
        Assert.Equal("sikayet edilen icerik", persisted.TargetContentSnapshot);
        Assert.Equal("aciklama", persisted.Description);
        Assert.Equal(ReportStatus.Pending, persisted.Status);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CreatePostReport_MissingPost_ShouldThrowNotFound()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdAsync(7).Returns((Post?)null);
        var handler = new CreatePostReportCommandHandler(unitOfWork, NullLogger<CreatePostReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreatePostReportCommand { PostId = 7, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePostReport_OwnPost_ShouldThrowBadRequest()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdAsync(7).Returns(new Post { Id = 7, UserId = 9, Status = PostStatus.Published, isPublished = true });
        var handler = new CreatePostReportCommandHandler(unitOfWork, NullLogger<CreatePostReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreatePostReportCommand { PostId = 7, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Theory]
    [InlineData(PostStatus.HiddenByModerator, true, false)]
    [InlineData(PostStatus.Published, false, false)]
    [InlineData(PostStatus.Published, true, true)]
    public async Task CreatePostReport_InvisiblePost_ShouldThrowBadRequest(PostStatus status, bool isPublished, bool isDeleted)
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdAsync(7).Returns(new Post { Id = 7, UserId = 3, Status = status, isPublished = isPublished, isDeleted = isDeleted });
        var handler = new CreatePostReportCommandHandler(unitOfWork, NullLogger<CreatePostReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreatePostReportCommand { PostId = 7, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePostReport_AtDailyLimit_ShouldThrowTooManyRequests()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdAsync(7).Returns(new Post { Id = 7, UserId = 3, Status = PostStatus.Published, isPublished = true });
        unitOfWork.ReportRepository.CountRecentReportsByUserAsync(9, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(3);
        var handler = new CreatePostReportCommandHandler(unitOfWork, NullLogger<CreatePostReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions { DailyReportLimit = 3 }));

        await Assert.ThrowsAsync<TooManyRequestsException>(() => handler.Handle(new CreatePostReportCommand { PostId = 7, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreatePostReport_NonPositiveConfiguredLimit_ShouldFallBackToOne()
    {
        // Math.Max(1, ...) kurali: yanlis yapilandirma sikayeti tamamen kapatmamali.
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdAsync(7).Returns(new Post { Id = 7, UserId = 3, Status = PostStatus.Published, isPublished = true });
        unitOfWork.ReportRepository.CountRecentReportsByUserAsync(9, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        var handler = new CreatePostReportCommandHandler(unitOfWork, NullLogger<CreatePostReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions { DailyReportLimit = 0 }));

        var response = await handler.Handle(new CreatePostReportCommand { PostId = 7, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None);

    }

    [Fact]
    public async Task CreatePostReport_ExistingPendingReport_ShouldThrowBadRequest()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdAsync(7).Returns(new Post { Id = 7, UserId = 3, Status = PostStatus.Published, isPublished = true });
        unitOfWork.ReportRepository.HasPendingPostReportAsync(9, 7, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreatePostReportCommandHandler(unitOfWork, NullLogger<CreatePostReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreatePostReportCommand { PostId = 7, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateCommentReport_ShouldSnapshotCommentOwnerAndContent()
    {
        var unitOfWork = CreateUnitOfWork();
        var commentOwner = HandlerTestContext.CreateUser(3, "yorumcu");
        unitOfWork.CommentRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns(new Comment { Id = 4, UserId = 3, User = commentOwner, Content = "yorum icerigi", PostId = 7 });
        ReportEntity? persisted = null;
        await unitOfWork.ReportRepository.AddAsync(Arg.Do<ReportEntity>(report => persisted = report));
        var handler = new CreateCommentReportCommandHandler(unitOfWork, NullLogger<CreateCommentReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        var response = await handler.Handle(new CreateCommentReportCommand { CommentId = 4, UserId = 9, Reason = ReportReason.Harassment }, CancellationToken.None);

        Assert.Equal(ReportTargetType.Comment, persisted!.TargetType);
        Assert.Equal(4, persisted.TargetCommentId);
        Assert.Null(persisted.TargetPostId);
        Assert.Null(persisted.TargetUserId);
        Assert.Equal(3, persisted.TargetOwnerUserId);
        Assert.Equal("yorumcu", persisted.TargetOwnerUserNameSnapshot);
        Assert.Equal("yorum icerigi", persisted.TargetContentSnapshot);
    }

    [Fact]
    public async Task CreateCommentReport_MissingComment_ShouldThrowNotFound()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.CommentRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns((Comment?)null);
        var handler = new CreateCommentReportCommandHandler(unitOfWork, NullLogger<CreateCommentReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateCommentReportCommand { CommentId = 4, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateCommentReport_OwnComment_ShouldThrowBadRequest()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.CommentRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns(new Comment { Id = 4, UserId = 9, Content = "yorum" });
        var handler = new CreateCommentReportCommandHandler(unitOfWork, NullLogger<CreateCommentReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateCommentReportCommand { CommentId = 4, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateCommentReport_ExistingPendingReport_ShouldThrowBadRequest()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.CommentRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns(new Comment { Id = 4, UserId = 3, Content = "yorum" });
        unitOfWork.ReportRepository.HasPendingCommentReportAsync(9, 4, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateCommentReportCommandHandler(unitOfWork, NullLogger<CreateCommentReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateCommentReportCommand { CommentId = 4, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateUserReport_ShouldSnapshotTargetUserAndBio()
    {
        var unitOfWork = CreateUnitOfWork();
        var target = HandlerTestContext.CreateUser(3, "hedef");
        target.Bio = "  profil metni  ";
        var userManager = HandlerTestContext.CreateUserManager(target);
        ReportEntity? persisted = null;
        await unitOfWork.ReportRepository.AddAsync(Arg.Do<ReportEntity>(report => persisted = report));
        var handler = new CreateUserReportCommandHandler(unitOfWork, userManager, NullLogger<CreateUserReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        var response = await handler.Handle(new CreateUserReportCommand { TargetUserId = 3, UserId = 9, Reason = ReportReason.Impersonation }, CancellationToken.None);

        Assert.Equal(ReportTargetType.User, persisted!.TargetType);
        Assert.Equal(3, persisted.TargetUserId);
        Assert.Equal(3, persisted.TargetOwnerUserId);
        Assert.Equal("hedef", persisted.TargetOwnerUserNameSnapshot);
        Assert.Equal("profil metni", persisted.TargetContentSnapshot);
    }

    [Fact]
    public async Task CreateUserReport_SelfReport_ShouldThrowBadRequestBeforeLookup()
    {
        var unitOfWork = CreateUnitOfWork();
        var userManager = HandlerTestContext.CreateUserManager();
        var handler = new CreateUserReportCommandHandler(unitOfWork, userManager, NullLogger<CreateUserReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateUserReportCommand { TargetUserId = 9, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));

        await userManager.DidNotReceiveWithAnyArgs().FindByIdAsync(default!);
    }

    [Fact]
    public async Task CreateUserReport_MissingTargetUser_ShouldThrowNotFound()
    {
        var unitOfWork = CreateUnitOfWork();
        var userManager = HandlerTestContext.CreateUserManager();
        userManager.FindByIdAsync("3").Returns((User?)null);
        var handler = new CreateUserReportCommandHandler(unitOfWork, userManager, NullLogger<CreateUserReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateUserReportCommand { TargetUserId = 3, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateUserReport_AlreadyBannedTarget_ShouldThrowBadRequest()
    {
        var unitOfWork = CreateUnitOfWork();
        var userManager = HandlerTestContext.CreateUserManager(HandlerTestContext.CreateUser(3, "hedef", UserStatus.Banned));
        var handler = new CreateUserReportCommandHandler(unitOfWork, userManager, NullLogger<CreateUserReportCommandHandler>.Instance, Options.Create(new ReportPolicyOptions()));

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateUserReportCommand { TargetUserId = 3, UserId = 9, Reason = ReportReason.Spam }, CancellationToken.None));
    }

    [Fact]
    public async Task GetReportById_MissingReport_ShouldThrowNotFound()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.ReportRepository.GetByIdWithDetailsAsync(1).Returns((ReportEntity?)null);
        var handler = new GetReportByIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetReportByIdQuery { ReportId = 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetReportById_ReportWithoutTarget_ShouldThrowBadRequest()
    {
        var unitOfWork = CreateUnitOfWork();
        unitOfWork.ReportRepository.GetByIdWithDetailsAsync(1).Returns(new ReportEntity { Id = 1, TargetType = ReportTargetType.Post, TargetPostId = null });
        var handler = new GetReportByIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new GetReportByIdQuery { ReportId = 1 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetReportById_ShouldAggregateRelatedReportsPriorityAndActions()
    {
        var unitOfWork = CreateUnitOfWork();
        var report = new ReportEntity { Id = 1, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Spam, ReporterUserId = 9 };
        var relatedReports = new List<ReportEntity>
        {
            report,
            new() { Id = 2, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Threat, ReporterUserId = 10, ModerationActions = { new ModerationAction { Id = 5, ActionType = ModerationActionType.WarnUser, CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) } } },
            new() { Id = 3, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Spam, ReporterUserId = 11, ModerationActions = { new ModerationAction { Id = 6, ActionType = ModerationActionType.BanUser, CreatedAt = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc) } } }
        };
        unitOfWork.ReportRepository.GetByIdWithDetailsAsync(1).Returns(report);
        unitOfWork.ReportRepository.GetReportsForTargetAsync(ReportTargetType.User, 3, Arg.Any<CancellationToken>()).Returns(relatedReports);
        var handler = new GetReportByIdQueryHandler(unitOfWork);

        var dto = await handler.Handle(new GetReportByIdQuery { ReportId = 1 }, CancellationToken.None);

        // Threat, gruptaki en yuksek onceligi belirler.
        Assert.Equal(ReportPriority.Critical, dto.Priority);
        Assert.Equal(3, dto.ReportCount);
        Assert.Equal(3, dto.RelatedReports.Count);
        // Aksiyonlar en yeniden eskiye siralanir.
        Assert.Equal(new[] { 6, 5 }, dto.ModerationActions.Select(action => action.Id));
    }

    [Fact]
    public async Task GetReports_ShouldGroupByTargetAndSummarizeReasons()
    {
        var unitOfWork = CreateUnitOfWork();
        var firstDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastDate = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var reports = new List<ReportEntity>
        {
            new() { Id = 1, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Spam, CreatedAt = firstDate },
            new() { Id = 2, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Spam, CreatedAt = lastDate },
            new() { Id = 3, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Threat, CreatedAt = firstDate }
        };
        unitOfWork.ReportRepository.GetFilteredReportGroupsAsync(null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>()).Returns((reports, 3));
        var handler = new GetReportsQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetReportsQuery(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(3, item.ReportCount);
        Assert.Equal(ReportPriority.Critical, item.Priority);
        Assert.Equal(2, item.ReasonCounts[ReportReason.Spam]);
        Assert.Equal(1, item.ReasonCounts[ReportReason.Threat]);
        Assert.Equal(firstDate, item.FirstReportDate);
        Assert.Equal(lastDate, item.LastReportDate);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetReports_ShouldOrderByPriorityThenLastReportDate()
    {
        var unitOfWork = CreateUnitOfWork();
        var reports = new List<ReportEntity>
        {
            new() { Id = 1, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Spam, CreatedAt = new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, TargetType = ReportTargetType.User, TargetUserId = 4, Reason = ReportReason.Threat, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        };
        unitOfWork.ReportRepository.GetFilteredReportGroupsAsync(null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>()).Returns((reports, 2));
        var handler = new GetReportsQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetReportsQuery(), CancellationToken.None);

        // Kritik oncelikli grup, daha yeni olan dusuk oncelikli grubun ustunde olmali.
        Assert.Equal(new int?[] { 4, 3 }, result.Items.Select(item => item.TargetUserId).ToArray());
    }

    [Fact]
    public async Task GetReports_LongContent_ShouldBeTruncatedIntoPreview()
    {
        var unitOfWork = CreateUnitOfWork();
        var reports = new List<ReportEntity>
        {
            new() { Id = 1, TargetType = ReportTargetType.Post, TargetPostId = 7, Reason = ReportReason.Spam, TargetPost = new Post { Id = 7, Content = new string('x', 200) } }
        };
        unitOfWork.ReportRepository.GetFilteredReportGroupsAsync(null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>()).Returns((reports, 1));
        var handler = new GetReportsQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetReportsQuery(), CancellationToken.None);

        var preview = Assert.Single(result.Items).TargetPostContentPreview;
        Assert.Equal(163, preview!.Length);
        Assert.EndsWith("...", preview);
    }

    [Fact]
    public async Task GetReports_BlankContent_ShouldProduceNullPreview()
    {
        var unitOfWork = CreateUnitOfWork();
        var reports = new List<ReportEntity>
        {
            new() { Id = 1, TargetType = ReportTargetType.User, TargetUserId = 3, Reason = ReportReason.Spam, TargetContentSnapshot = "   " }
        };
        unitOfWork.ReportRepository.GetFilteredReportGroupsAsync(null, null, null, null, null, 1, 20, Arg.Any<CancellationToken>()).Returns((reports, 1));
        var handler = new GetReportsQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetReportsQuery(), CancellationToken.None);

        Assert.Null(Assert.Single(result.Items).TargetPostContentPreview);
    }

    [Fact]
    public async Task GetReports_ShouldForwardEveryFilterToRepository()
    {
        var unitOfWork = CreateUnitOfWork();
        var fromDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        unitOfWork.ReportRepository.GetFilteredReportGroupsAsync(ReportStatus.Pending, ReportTargetType.Comment, ReportReason.Spam, fromDate, toDate, 2, 10, Arg.Any<CancellationToken>())
            .Returns((new List<ReportEntity>(), 0));
        var handler = new GetReportsQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetReportsQuery
        {
            Status = ReportStatus.Pending,
            TargetType = ReportTargetType.Comment,
            Reason = ReportReason.Spam,
            FromDate = fromDate,
            ToDate = toDate,
            Page = 2,
            Size = 10
        }, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.Size);
    }

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.ReportRepository.CountRecentReportsByUserAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        unitOfWork.ReportRepository.HasPendingPostReportAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.ReportRepository.HasPendingCommentReportAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.ReportRepository.HasPendingUserReportAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(false);
        return unitOfWork;
    }
}
