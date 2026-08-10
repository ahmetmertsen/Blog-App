using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Report.Commands.CreateCommentReport;
using buduns_server.Application.Features.Report.Commands.CreatePostReport;
using buduns_server.Application.Features.Report.Commands.CreateUserReport;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Reports;

/// <summary>
/// Sikayet olusturma tarafi kullaniciya, inceleme tarafi Admin/Moderator'a
/// acik. Bu testler hem kotu niyetli kullanimin (kendini sikayet, tekrar
/// sikayet, gunluk limit) engellendigini hem de listeleme/detay ekranlarinin
/// ayni hedefe ait sikayetleri dogru grupladigini dogrular.
/// </summary>
public sealed class ReportTests : IntegrationTestBase
{
    public ReportTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_post_report_should_store_snapshot_fields()
    {
        var author = await CreateUserAsync("report-post-author");
        var reporter = await CreateUserAsync("report-post-reporter");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "sikayet edilen icerik"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam, Description = "tekrar eden icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync());
        stored.TargetType.Should().Be(ReportTargetType.Post);
        stored.TargetPostId.Should().Be(post.Id);
        stored.TargetOwnerUserId.Should().Be(author.Id);
        stored.TargetOwnerUserNameSnapshot.Should().Be("report-post-author");
        stored.TargetContentSnapshot.Should().Be("sikayet edilen icerik");
        stored.Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public async Task Reporting_your_own_content_should_be_rejected()
    {
        var author = await CreateUserAsync("self-report-author");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, author.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        (await authentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await authentication.Client.PostAsJsonAsync("/api/Report/createCommentReport", new CreateCommentReportCommand { CommentId = comment.Id, Reason = ReportReason.Spam })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await authentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = author.Id, Reason = ReportReason.Spam })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Duplicate_pending_report_for_the_same_target_should_be_rejected()
    {
        var author = await CreateUserAsync("duplicate-report-author");
        var reporter = await CreateUserAsync("duplicate-report-reporter");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id);
        var command = new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam };

        (await authentication.Client.PostAsJsonAsync("/api/Report/createPostReport", command)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await authentication.Client.PostAsJsonAsync("/api/Report/createPostReport", command)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// PostRepository.GetByIdAsync yalnizca gorunur paylasimlari donduruyor;
    /// bu yuzden gizlenmis/silinmis bir paylasim handler'daki "sikayet edilmeye
    /// uygun degil" dalina hic ulasmadan 404 uretir. Beklenen sozlesme budur.
    /// </summary>
    [Fact]
    public async Task Report_on_a_deleted_post_should_be_rejected()
    {
        var author = await CreateUserAsync("deleted-report-author");
        var reporter = await CreateUserAsync("deleted-report-reporter");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            var tracked = await context.Posts.SingleAsync(item => item.Id == post.Id);
            tracked.isDeleted = true;
            tracked.isPublished = false;
            tracked.Status = PostStatus.DeletedByOwner;
            await context.SaveChangesAsync();
        });
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Daily_report_limit_should_be_enforced()
    {
        var reporter = await CreateUserAsync("limit-reporter");
        await GrantEndpointPermissionsAsync();
        var authors = new List<int>();
        for (var index = 0; index < 11; index++)
        {
            var author = await CreateUserAsync($"limit-author-{index}");
            authors.Add(author.Id);
        }

        using var authentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id);

        // Varsayilan gunluk limit 10.
        for (var index = 0; index < 10; index++)
        {
            var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, authors[index]));
            (await authentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var lastPost = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, authors[10]));
        var throttled = await authentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = lastPost.Id, Reason = ReportReason.Spam });

        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Report_listing_should_group_reports_of_the_same_target()
    {
        var author = await CreateUserAsync("grouped-report-author");
        var firstReporter = await CreateUserAsync("grouped-reporter-one");
        var secondReporter = await CreateUserAsync("grouped-reporter-two");
        var moderator = await CreateUserAsync("grouped-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));

        using (var firstAuthentication = await Factory.CreateAuthenticatedClientAsync(firstReporter.Id))
        {
            (await firstAuthentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam })).EnsureSuccessStatusCode();
        }

        using (var secondAuthentication = await Factory.CreateAuthenticatedClientAsync(secondReporter.Id))
        {
            (await secondAuthentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Threat })).EnsureSuccessStatusCode();
        }

        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);
        var listing = await moderatorAuthentication.Client.GetDataAsync<PagedResponse<ReportListDto>>("/api/Report?page=1&size=20");

        var item = listing!.Items.Should().ContainSingle().Subject;
        item.TargetPostId.Should().Be(post.Id);
        item.ReportCount.Should().Be(2);
        // Threat kritik oncelikli; grup onceligi en yuksegini almali.
        item.Priority.Should().Be(ReportPriority.Critical);
        item.ReasonCounts.Should().ContainKey(ReportReason.Spam).And.ContainKey(ReportReason.Threat);
    }

    [Fact]
    public async Task Report_detail_should_include_related_reports_and_priority()
    {
        var author = await CreateUserAsync("detail-report-author");
        var reporter = await CreateUserAsync("detail-reporter");
        var moderator = await CreateUserAsync("detail-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "detay icerigi"));
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Harassment, Description = "aciklama" })).EnsureSuccessStatusCode();
        }

        var reportId = await Factory.ExecuteScopeAsync(async services =>
            (await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync()).Id);
        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var detail = await moderatorAuthentication.Client.GetDataAsync<ReportDetailDto>($"/api/Report/getById/{reportId}");

        detail!.TargetPostContent.Should().Be("detay icerigi");
        detail.ReporterUserName.Should().Be("detail-reporter");
        detail.Priority.Should().Be(ReportPriority.High);
        detail.ReportCount.Should().Be(1);
        detail.RelatedReports.Should().ContainSingle();
        detail.ModerationActions.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_endpoints_for_moderators_should_be_closed_to_regular_users()
    {
        var user = await CreateUserAsync("report-regular-user");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        (await authentication.Client.GetAsync("/api/Report?page=1&size=20")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await authentication.Client.GetAsync("/api/Report/getById/1")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Report_creation_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = 1, Reason = ReportReason.Spam })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/Report/createCommentReport", new CreateCommentReportCommand { CommentId = 1, Reason = ReportReason.Spam })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = 1, Reason = ReportReason.Spam })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Report_listing_filters_should_be_applied()
    {
        var author = await CreateUserAsync("filter-report-author");
        var reporter = await CreateUserAsync("filter-report-reporter");
        var moderator = await CreateUserAsync("filter-report-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using (var reporterAuthentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id))
        {
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createPostReport", new CreatePostReportCommand { PostId = post.Id, Reason = ReportReason.Spam })).EnsureSuccessStatusCode();
            (await reporterAuthentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = author.Id, Reason = ReportReason.Harassment })).EnsureSuccessStatusCode();
        }

        using var moderatorAuthentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var postReports = await moderatorAuthentication.Client.GetDataAsync<PagedResponse<ReportListDto>>($"/api/Report?page=1&size=20&targetType={ReportTargetType.Post}");
        var spamReports = await moderatorAuthentication.Client.GetDataAsync<PagedResponse<ReportListDto>>($"/api/Report?page=1&size=20&reason={ReportReason.Spam}");
        var resolvedReports = await moderatorAuthentication.Client.GetDataAsync<PagedResponse<ReportListDto>>($"/api/Report?page=1&size=20&status={ReportStatus.ResolvedActionTaken}");

        postReports!.Items.Should().ContainSingle().Which.TargetType.Should().Be(ReportTargetType.Post);
        spamReports!.Items.Should().ContainSingle().Which.Reason.Should().Be(ReportReason.Spam);
        resolvedReports!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_listing_with_invalid_date_range_should_return_validation_error()
    {
        var moderator = await CreateUserAsync("date-range-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await authentication.Client.GetAsync("/api/Report?page=1&size=20&fromDate=2026-02-01&toDate=2026-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.ReadErrorAsync();
        error.ValidationErrors.Should().ContainKey("ToDate");
    }

    [Fact]
    public async Task Comment_report_should_store_comment_snapshot()
    {
        var author = await CreateUserAsync("comment-report-author");
        var reporter = await CreateUserAsync("comment-report-reporter");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, author.Id, "sikayet edilen yorum"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Report/createCommentReport", new CreateCommentReportCommand { CommentId = comment.Id, Reason = ReportReason.HateSpeech });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Reports.AsNoTracking().SingleAsync());
        stored.TargetType.Should().Be(ReportTargetType.Comment);
        stored.TargetCommentId.Should().Be(comment.Id);
        stored.TargetContentSnapshot.Should().Be("sikayet edilen yorum");
    }

    [Fact]
    public async Task User_report_against_a_banned_user_should_be_rejected()
    {
        var target = await CreateUserAsync("banned-report-target");
        var reporter = await CreateUserAsync("banned-report-reporter");
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, target.Id, UserStatus.Banned));
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reporter.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Report/createUserReport", new CreateUserReportCommand { TargetUserId = target.Id, Reason = ReportReason.Spam });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
