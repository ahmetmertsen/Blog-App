using buduns_server.Application.Common.Consts;
using buduns_server.Application.Features.Report.Commands.CreateCommentReport;
using buduns_server.Application.Features.Report.Commands.CreatePostReport;
using buduns_server.Application.Features.Report.Commands.CreateUserReport;
using buduns_server.Application.Features.Report.Commands.ReviewReport;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Moderation;

/// <summary>
/// Inceleme karari tek istekte birden fazla tabloyu degistiriyor: sikayet
/// durumu, hedef icerik/kullanici, moderasyon kaydi, bildirimler ve oturumlar.
/// Bu zincirin tamami yalnizca gercek veritabaniyla dogrulanabilir.
/// </summary>
public sealed class ModerationActionTests : IntegrationTestBase
{
    public ModerationActionTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Claiming_a_report_should_move_every_open_report_of_the_target_to_in_review()
    {
        var scenario = await CreatePostReportScenarioAsync("claim", extraReporters: 1);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(scenario.ModeratorId);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = scenario.ReportId,
            Status = ReportStatus.InReview,
            ActionType = ModerationActionType.None,
            ReviewNote = "inceleme basladi"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().ToListAsync());
        reports.Should().HaveCount(2);
        reports.Should().OnlyContain(report => report.Status == ReportStatus.InReview && report.ReviewedByUserId == scenario.ModeratorId);
    }

    [Fact]
    public async Task Resolving_without_violation_should_close_reports_without_touching_the_content()
    {
        var scenario = await CreatePostReportScenarioAsync("no-violation");
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(scenario.ModeratorId);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = scenario.ReportId,
            Status = ReportStatus.ResolvedNoViolation,
            ActionType = ModerationActionType.None,
            ReviewNote = "ihlal yok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().SingleAsync(item => item.Id == scenario.PostId));
        post.Status.Should().Be(PostStatus.Published);

        var report = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync(item => item.Id == scenario.ReportId));
        report.Status.Should().Be(ReportStatus.ResolvedNoViolation);
        report.ReviewedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task Hide_post_action_should_hide_the_post_and_notify_its_author()
    {
        var scenario = await CreatePostReportScenarioAsync("hide-post");
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(scenario.ModeratorId);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = scenario.ReportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.HidePost,
            ReviewNote = "kural ihlali"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().SingleAsync(item => item.Id == scenario.PostId));
        post.Status.Should().Be(PostStatus.HiddenByModerator);
        post.isPublished.Should().BeFalse();

        var notifications = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().ToListAsync());
        notifications.Should().Contain(item => item.UserId == scenario.AuthorId && item.Type == NotificationType.POST_HIDDEN);
        notifications.Should().Contain(item => item.UserId == scenario.ReporterId && item.Type == NotificationType.REPORT_RESOLVED);

        var moderationAction = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().ModerationActions.AsNoTracking().SingleAsync());
        moderationAction.ActionType.Should().Be(ModerationActionType.HidePost);
        moderationAction.ModeratorUserId.Should().Be(scenario.ModeratorId);
        moderationAction.TargetPostId.Should().Be(scenario.PostId);
    }

    [Fact]
    public async Task Delete_post_action_should_soft_delete_the_post()
    {
        var scenario = await CreatePostReportScenarioAsync("delete-post");
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(scenario.ModeratorId);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = scenario.ReportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.DeletePost,
            ReviewNote = "kaldirildi"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().SingleAsync(item => item.Id == scenario.PostId));
        post.Status.Should().Be(PostStatus.DeletedByModerator);
        post.isDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Post_action_on_a_user_report_should_be_rejected()
    {
        var target = await CreateUserAsync("wrong-action-target");
        var reporter = await CreateUserAsync("wrong-action-reporter");
        var moderator = await CreateUserAsync("wrong-action-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = target.Id, Reason = ReportReason.Harassment })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services => (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync()).Id);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = reportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.HidePost,
            ReviewNote = "yanlis aksiyon"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Suspend_user_action_should_suspend_the_account_and_revoke_its_sessions()
    {
        var target = await CreateUserAsync("suspend-target");
        var reporter = await CreateUserAsync("suspend-reporter");
        var moderator = await CreateUserAsync("suspend-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using var targetAuthentication = await Factory.CreateAuthenticatedClientAsync(target.Id);
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = target.Id, Reason = ReportReason.Harassment })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services => (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync()).Id);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = reportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.SuspendUser,
            SuspensionDays = 7,
            ReviewNote = "gecici uzaklastirma"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var suspended = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == target.Id));
        suspended.Status.Should().Be(UserStatus.Suspended);
        suspended.SuspendedUntil.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));

        (await targetAuthentication.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var moderationAction = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().ModerationActions.AsNoTracking().SingleAsync());
        moderationAction.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Ban_user_action_should_ban_the_account_and_notify_it()
    {
        var target = await CreateUserAsync("ban-target");
        var reporter = await CreateUserAsync("ban-reporter");
        var moderator = await CreateUserAsync("ban-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = target.Id, Reason = ReportReason.Threat })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services => (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync()).Id);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = reportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.BanUser,
            ReviewNote = "kalici yasak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var banned = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == target.Id));
        banned.Status.Should().Be(UserStatus.Banned);
        banned.SuspendedUntil.Should().BeNull();

        var notifications = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().ToListAsync());
        notifications.Should().Contain(item => item.UserId == target.Id && item.Type == NotificationType.ACCOUNT_BANNED);
    }

    [Fact]
    public async Task Warn_user_action_should_only_create_a_notification()
    {
        var target = await CreateUserAsync("warn-target");
        var reporter = await CreateUserAsync("warn-reporter");
        var moderator = await CreateUserAsync("warn-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = target.Id, Reason = ReportReason.Spam })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services => (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync()).Id);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = reportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.WarnUser,
            ReviewNote = "uyari"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var warned = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == target.Id));
        warned.Status.Should().Be(UserStatus.Active);

        var notifications = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().ToListAsync());
        notifications.Should().Contain(item => item.UserId == target.Id && item.Type == NotificationType.MODERATION_WARNING);
    }

    [Fact]
    public async Task Hide_comment_action_should_hide_the_comment_and_notify_its_author()
    {
        var author = await CreateUserAsync("comment-mod-author");
        var reporter = await CreateUserAsync("comment-mod-reporter");
        var moderator = await CreateUserAsync("comment-mod-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, author.Id, "kotu yorum"));
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createCommentReport", new CreateCommentReportCommand { CommentId = comment.Id, Reason = ReportReason.HateSpeech })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services => (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync()).Id);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = reportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.HideComment,
            ReviewNote = "gizlendi"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Comments.AsNoTracking().SingleAsync(item => item.Id == comment.Id));
        stored.Status.Should().Be(CommentStatus.HiddenByModerator);
        stored.isActive.Should().BeFalse();

        var notifications = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().ToListAsync());
        notifications.Should().Contain(item => item.UserId == author.Id && item.Type == NotificationType.COMMENT_HIDDEN);
    }

    [Fact]
    public async Task Already_resolved_report_should_not_be_reviewed_again()
    {
        var scenario = await CreatePostReportScenarioAsync("resolved-twice");
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(scenario.ModeratorId);
        var command = new ReviewReportCommand
        {
            ReportId = scenario.ReportId,
            Status = ReportStatus.ResolvedNoViolation,
            ActionType = ModerationActionType.None,
            ReviewNote = "ihlal yok"
        };

        (await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", command)).StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", command);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Moderator_should_not_apply_a_user_action_to_their_own_account()
    {
        var moderator = await CreateUserAsync("self-moderation-moderator", RoleConstants.Moderator);
        var reporter = await CreateUserAsync("self-moderation-reporter");
        await GrantEndpointPermissionsAsync();
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = moderator.Id, Reason = ReportReason.Spam })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services => (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync()).Id);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await moderatorAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = reportId,
            Status = ReportStatus.ResolvedActionTaken,
            ActionType = ModerationActionType.BanUser,
            ReviewNote = "kendini yasaklama"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Review_endpoint_should_be_closed_to_regular_users()
    {
        var scenario = await CreatePostReportScenarioAsync("closed-review");
        using var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(scenario.ReporterId);

        var response = await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = scenario.ReportId,
            Status = ReportStatus.ResolvedNoViolation,
            ActionType = ModerationActionType.None,
            ReviewNote = "yetkisiz"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Review_of_a_missing_report_should_return_not_found()
    {
        var moderator = await CreateUserAsync("missing-report-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Report/review", new ReviewReportCommand
        {
            ReportId = 999999,
            Status = ReportStatus.InReview,
            ActionType = ModerationActionType.None,
            ReviewNote = "not"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record ReportScenario(int AuthorId, int ReporterId, int ModeratorId, int PostId, int ReportId);

    private async Task<ReportScenario> CreatePostReportScenarioAsync(string prefix, int extraReporters = 0)
    {
        var author = await CreateUserAsync($"{prefix}-author");
        var reporter = await CreateUserAsync($"{prefix}-reporter");
        var moderator = await CreateUserAsync($"{prefix}-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, $"{prefix} icerigi"));

        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam })).EnsureSuccessStatusCode();
        }

        for (var index = 0; index < extraReporters; index++)
        {
            var extra = await CreateUserAsync($"{prefix}-extra-reporter-{index}");
            using var extraAuthentication = await Factory.CreateAuthenticatedClientAsync(extra.Id);
            (await extraAuthentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Harassment })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services =>
            (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().OrderBy(item => item.Id).FirstAsync()).Id);

        return new ReportScenario(author.Id, reporter.Id, moderator.Id, post.Id, reportId);
    }
}
