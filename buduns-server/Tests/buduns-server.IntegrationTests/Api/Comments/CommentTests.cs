using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Comments.Commands.Create;
using buduns_server.Application.Features.Comments.Commands.Update;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using buduns_server.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Comments;

public sealed class CommentTests : IntegrationTestBase
{
    public CommentTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_comment_should_return_dto_and_notify_the_post_owner()
    {
        var owner = await CreateUserAsync("comment-post-owner");
        var commenter = await CreateUserAsync("commenter-user");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(commenter.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Comment", new CreateCommentsCommand { PostId = post.Id, Content = "  ilk yorum  " });
        var body = await response.ReadDataAsync<CreateCommentsCommandResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Message.Should().NotBeNullOrWhiteSpace();
        body.Comment.Content.Should().Be("ilk yorum");
        body.Comment.UserId.Should().Be(commenter.Id);

        var notification = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().SingleAsync(item => item.UserId == owner.Id));
        notification.Type.Should().Be(NotificationType.POST_COMMENTED);
        notification.ActorUserId.Should().Be(commenter.Id);
        notification.PostId.Should().Be(post.Id);
    }

    [Fact]
    public async Task Commenting_on_your_own_post_should_not_create_a_notification()
    {
        var owner = await CreateUserAsync("self-commenter");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        (await authentication.Client.PostAsJsonAsync("/api/Comment", new CreateCommentsCommand { PostId = post.Id, Content = "kendi yorumum" })).EnsureSuccessStatusCode();

        var notificationCount = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.CountAsync());
        notificationCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_comment_on_missing_post_should_return_not_found()
    {
        var commenter = await CreateUserAsync("missing-post-commenter");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(commenter.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Comment", new CreateCommentsCommand { PostId = 999999, Content = "yorum" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Duplicate_comment_within_a_minute_should_be_rejected()
    {
        var owner = await CreateUserAsync("duplicate-owner");
        var commenter = await CreateUserAsync("duplicate-commenter");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(commenter.Id);
        var command = new CreateCommentsCommand { PostId = post.Id, Content = "ayni yorum" };

        (await authentication.Client.PostAsJsonAsync("/api/Comment", command)).StatusCode.Should().Be(HttpStatusCode.OK);
        var duplicate = await authentication.Client.PostAsJsonAsync("/api/Comment", command);

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await duplicate.ReadErrorAsync()).Code.Should().Be("BAD_REQUEST");
    }

    [Fact]
    public async Task More_than_ten_comments_per_minute_should_be_rejected()
    {
        var owner = await CreateUserAsync("flood-owner");
        var commenter = await CreateUserAsync("flood-commenter");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(commenter.Id);

        for (var index = 0; index < 10; index++)
        {
            (await authentication.Client.PostAsJsonAsync("/api/Comment", new CreateCommentsCommand { PostId = post.Id, Content = $"yorum {index}" }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var throttled = await authentication.Client.PostAsJsonAsync("/api/Comment", new CreateCommentsCommand { PostId = post.Id, Content = "onbirinci yorum" });

        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Update_comment_should_only_be_allowed_for_the_owner()
    {
        var owner = await CreateUserAsync("comment-owner");
        var attacker = await CreateUserAsync("comment-attacker");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, owner.Id, "orijinal"));
        using var ownerAuthentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);
        using var attackerAuthentication = await Factory.CreateAuthenticatedClientAsync(attacker.Id);

        var forbidden = await attackerAuthentication.Client.PutAsJsonAsync($"/api/Comment/{comment.Id}", new UpdateCommentsCommand { Content = "ele gecirildi" });
        var allowed = await ownerAuthentication.Client.PutAsJsonAsync($"/api/Comment/{comment.Id}", new UpdateCommentsCommand { Content = "guncellendi" });

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Comments.AsNoTracking().SingleAsync(item => item.Id == comment.Id));
        stored.Content.Should().Be("guncellendi");
        stored.UpdateAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Delete_comment_should_soft_delete_and_hide_it_from_listings()
    {
        var owner = await CreateUserAsync("delete-comment-owner");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        var response = await authentication.Client.DeleteAsync($"/api/Comment/{comment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Comments.AsNoTracking().SingleAsync(item => item.Id == comment.Id));
        stored.Status.Should().Be(CommentStatus.DeletedByOwner);
        stored.isDeleted.Should().BeTrue();

        using var reader = Factory.CreateHttpsClient();
        var listing = await reader.GetDataAsync<PagedResponse<CommentDto>>($"/api/Comment/post/{post.Id}?page=1&size=20");
        listing!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_comment_twice_should_stay_successful()
    {
        var owner = await CreateUserAsync("idempotent-delete-owner");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        (await authentication.Client.DeleteAsync($"/api/Comment/{comment.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await authentication.Client.DeleteAsync($"/api/Comment/{comment.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_comment_of_another_user_should_be_forbidden()
    {
        var owner = await CreateUserAsync("comment-delete-victim");
        var attacker = await CreateUserAsync("comment-delete-attacker");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(attacker.Id);

        var response = await authentication.Client.DeleteAsync($"/api/Comment/{comment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Comment_listings_should_be_public_and_paged()
    {
        var owner = await CreateUserAsync("listing-owner");
        var commenter = await CreateUserAsync("listing-commenter");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, commenter.Id, "birinci"));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, commenter.Id, "ikinci"));
        using var client = Factory.CreateHttpsClient();

        var byPost = await client.GetDataAsync<PagedResponse<CommentDto>>($"/api/Comment/post/{post.Id}?page=1&size=1");
        var byUser = await client.GetDataAsync<PagedResponse<CommentDto>>($"/api/Comment/user/{commenter.Id}?page=1&size=20");

        byPost!.Items.Should().ContainSingle();
        byPost.TotalCount.Should().Be(2);
        byPost.TotalPages.Should().Be(2);
        byUser!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Comment_listing_of_a_missing_post_should_return_not_found()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/Comment/post/999999?page=1&size=20");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_comment_by_id_should_return_it_publicly()
    {
        var owner = await CreateUserAsync("single-comment-owner");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        var comment = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateCommentAsync(services, post.Id, owner.Id, "tekil yorum"));
        using var client = Factory.CreateHttpsClient();

        var dto = await client.GetDataAsync<CommentDto>($"/api/Comment/{comment.Id}");

        dto!.Content.Should().Be("tekil yorum");
        dto.PostId.Should().Be(post.Id);
        dto.UserName.Should().Be("single-comment-owner");
    }

    [Fact]
    public async Task Get_missing_comment_should_return_not_found()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.GetAsync("/api/Comment/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Comment_write_endpoints_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.PostAsJsonAsync("/api/Comment", new CreateCommentsCommand { PostId = 1, Content = "yorum" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/api/Comment/1", new UpdateCommentsCommand { Content = "yorum" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/api/Comment/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Comment_validation_errors_should_follow_the_global_contract()
    {
        var commenter = await CreateUserAsync("validation-commenter");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(commenter.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Comment", new CreateCommentsCommand { PostId = 0, Content = string.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.ReadErrorAsync();
        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("PostId").And.ContainKey("Content");
    }
}
