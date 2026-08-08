using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Bookmarks.Commands.Create;
using buduns_server.Application.Features.Bookmarks.Queries.GetStatus;
using buduns_server.Application.Features.Followers.Commands.Create;
using buduns_server.Application.Features.Followers.Queries.GetStatus;
using buduns_server.Application.Features.Likes.Commands.Create;
using buduns_server.Application.Features.Likes.Queries.GetStatus;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Interactions;

/// <summary>
/// Begeni/kaydetme/takip uclari idempotent: ikinci cagri hata degil
/// "zaten var" bilgisi doner. Bu davranis istemcinin yeniden deneme
/// mantiginin dayandigi sozlesme.
/// </summary>
public sealed class LikeBookmarkFollowTests : IntegrationTestBase
{
    public LikeBookmarkFollowTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Like_lifecycle_should_create_report_status_and_remove()
    {
        var owner = await CreateUserAsync("like-post-owner");
        var liker = await CreateUserAsync("liker-user");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(liker.Id);

        var created = await authentication.Client.PostAsync($"/api/Like/{post.Id}", null);
        var createdBody = await created.Content.ReadFromJsonAsync<CreateLikesCommandResponse>();
        var duplicate = await authentication.Client.PostAsync($"/api/Like/{post.Id}", null);
        var duplicateBody = await duplicate.Content.ReadFromJsonAsync<CreateLikesCommandResponse>();
        var status = await authentication.Client.GetFromJsonAsync<GetLikeStatusQueryResponse>($"/api/Like/status/{post.Id}");
        var removed = await authentication.Client.DeleteAsync($"/api/Like/{post.Id}");
        var statusAfterRemoval = await authentication.Client.GetFromJsonAsync<GetLikeStatusQueryResponse>($"/api/Like/status/{post.Id}");

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        createdBody!.AlreadyLiked.Should().BeFalse();
        duplicateBody!.AlreadyLiked.Should().BeTrue();
        duplicateBody.LikeId.Should().Be(createdBody.LikeId);
        status!.IsLiked.Should().BeTrue();
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        statusAfterRemoval!.IsLiked.Should().BeFalse();
    }

    [Fact]
    public async Task Like_should_notify_the_post_owner_only_once_within_the_cooldown()
    {
        var owner = await CreateUserAsync("like-notify-owner");
        var liker = await CreateUserAsync("like-notify-liker");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(liker.Id);

        (await authentication.Client.PostAsync($"/api/Like/{post.Id}", null)).EnsureSuccessStatusCode();
        (await authentication.Client.DeleteAsync($"/api/Like/{post.Id}")).EnsureSuccessStatusCode();
        (await authentication.Client.PostAsync($"/api/Like/{post.Id}", null)).EnsureSuccessStatusCode();

        var notifications = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().Where(item => item.UserId == owner.Id).ToListAsync());
        notifications.Should().ContainSingle();
        notifications[0].Type.Should().Be(NotificationType.POST_LIKED);
    }

    [Fact]
    public async Task Liking_your_own_post_should_not_create_a_notification()
    {
        var owner = await CreateUserAsync("self-liker");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        (await authentication.Client.PostAsync($"/api/Like/{post.Id}", null)).EnsureSuccessStatusCode();

        var notificationCount = await Factory.ExecuteScopeAsync(async services => await services.GetRequiredService<BudunsDbContext>().Notifications.CountAsync());
        notificationCount.Should().Be(0);
    }

    [Fact]
    public async Task Like_on_missing_or_deleted_post_should_return_not_found()
    {
        var owner = await CreateUserAsync("deleted-post-owner");
        var liker = await CreateUserAsync("deleted-post-liker");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            var tracked = await context.Posts.SingleAsync(item => item.Id == post.Id);
            tracked.isDeleted = true;
            tracked.isActive = false;
            tracked.Status = PostStatus.DeletedByOwner;
            await context.SaveChangesAsync();
        });
        using var authentication = await Factory.CreateAuthenticatedClientAsync(liker.Id);

        (await authentication.Client.PostAsync($"/api/Like/{post.Id}", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await authentication.Client.PostAsync("/api/Like/999999", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task My_liked_posts_should_list_liked_posts_with_their_content()
    {
        var owner = await CreateUserAsync("liked-list-owner");
        var liker = await CreateUserAsync("liked-list-liker");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id, "begenilen icerik"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(liker.Id);
        (await authentication.Client.PostAsync($"/api/Like/{post.Id}", null)).EnsureSuccessStatusCode();

        var response = await authentication.Client.GetFromJsonAsync<PagedResponse<LikedPostDto>>("/api/Like/me?page=1&size=20");

        response!.Items.Should().ContainSingle();
        response.Items[0].Post.Content.Should().Be("begenilen icerik");
    }

    [Fact]
    public async Task Like_listing_of_a_post_should_include_liker_profiles()
    {
        var owner = await CreateUserAsync("like-listing-owner");
        var liker = await CreateUserAsync("like-listing-liker");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(liker.Id);
        (await authentication.Client.PostAsync($"/api/Like/{post.Id}", null)).EnsureSuccessStatusCode();

        var response = await authentication.Client.GetFromJsonAsync<PagedResponse<LikeDto>>($"/api/Like/post/{post.Id}?page=1&size=20");

        response!.Items.Should().ContainSingle().Which.UserName.Should().Be("like-listing-liker");
    }

    [Fact]
    public async Task Bookmark_lifecycle_should_create_report_status_and_remove()
    {
        var owner = await CreateUserAsync("bookmark-post-owner");
        var reader = await CreateUserAsync("bookmark-reader");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reader.Id);

        var created = await authentication.Client.PostAsJsonAsync("/api/Bookmark", new CreateBookmarksCommand { PostId = post.Id });
        var createdBody = await created.Content.ReadFromJsonAsync<CreateBookmarksCommandResponse>();
        var duplicate = await authentication.Client.PostAsJsonAsync("/api/Bookmark", new CreateBookmarksCommand { PostId = post.Id });
        var duplicateBody = await duplicate.Content.ReadFromJsonAsync<CreateBookmarksCommandResponse>();
        var listing = await authentication.Client.GetFromJsonAsync<PagedResponse<BookmarkDto>>("/api/Bookmark?page=1&size=20");
        var status = await authentication.Client.GetFromJsonAsync<GetBookmarkStatusQueryResponse>($"/api/Bookmark/status/{post.Id}");
        var removed = await authentication.Client.DeleteAsync($"/api/Bookmark/{post.Id}");
        var statusAfterRemoval = await authentication.Client.GetFromJsonAsync<GetBookmarkStatusQueryResponse>($"/api/Bookmark/status/{post.Id}");

        createdBody!.AlreadyBookmarked.Should().BeFalse();
        duplicateBody!.AlreadyBookmarked.Should().BeTrue();
        listing!.Items.Should().ContainSingle().Which.PostId.Should().Be(post.Id);
        status!.IsBookmarked.Should().BeTrue();
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        statusAfterRemoval!.IsBookmarked.Should().BeFalse();
    }

    [Fact]
    public async Task Bookmark_should_not_notify_the_post_owner()
    {
        var owner = await CreateUserAsync("silent-bookmark-owner");
        var reader = await CreateUserAsync("silent-bookmark-reader");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reader.Id);

        (await authentication.Client.PostAsJsonAsync("/api/Bookmark", new CreateBookmarksCommand { PostId = post.Id })).EnsureSuccessStatusCode();

        var notificationCount = await Factory.ExecuteScopeAsync(async services => await services.GetRequiredService<BudunsDbContext>().Notifications.CountAsync());
        notificationCount.Should().Be(0);
    }

    [Fact]
    public async Task Bookmark_of_a_missing_post_should_return_not_found()
    {
        var reader = await CreateUserAsync("missing-bookmark-reader");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reader.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Bookmark", new CreateBookmarksCommand { PostId = 999999 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Removing_a_bookmark_that_does_not_exist_should_still_succeed()
    {
        var owner = await CreateUserAsync("noop-bookmark-owner");
        var reader = await CreateUserAsync("noop-bookmark-reader");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reader.Id);

        var response = await authentication.Client.DeleteAsync($"/api/Bookmark/{post.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Follow_lifecycle_should_create_report_status_and_remove()
    {
        var follower = await CreateUserAsync("follow-source");
        var target = await CreateUserAsync("follow-target-user");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(follower.Id);

        var created = await authentication.Client.PostAsync($"/api/Follower/{target.Id}", null);
        var createdBody = await created.Content.ReadFromJsonAsync<CreateFollowersCommandResponse>();
        var duplicate = await authentication.Client.PostAsync($"/api/Follower/{target.Id}", null);
        var duplicateBody = await duplicate.Content.ReadFromJsonAsync<CreateFollowersCommandResponse>();
        var status = await authentication.Client.GetFromJsonAsync<GetFollowerStatusQueryResponse>($"/api/Follower/status/{target.Id}");
        var removed = await authentication.Client.DeleteAsync($"/api/Follower/{target.Id}");
        var statusAfterRemoval = await authentication.Client.GetFromJsonAsync<GetFollowerStatusQueryResponse>($"/api/Follower/status/{target.Id}");

        createdBody!.AlreadyFollowing.Should().BeFalse();
        duplicateBody!.AlreadyFollowing.Should().BeTrue();
        status!.IsFollowing.Should().BeTrue();
        removed.StatusCode.Should().Be(HttpStatusCode.OK);
        statusAfterRemoval!.IsFollowing.Should().BeFalse();
    }

    [Fact]
    public async Task Follow_should_notify_the_target_user()
    {
        var follower = await CreateUserAsync("notify-follower");
        var target = await CreateUserAsync("notify-followed");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(follower.Id);

        (await authentication.Client.PostAsync($"/api/Follower/{target.Id}", null)).EnsureSuccessStatusCode();

        var notification = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().SingleAsync(item => item.UserId == target.Id));
        notification.Type.Should().Be(NotificationType.NEW_FOLLOWER);
        notification.ActorUserId.Should().Be(follower.Id);
    }

    [Fact]
    public async Task Self_follow_should_be_rejected()
    {
        var user = await CreateUserAsync("self-follower");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsync($"/api/Follower/{user.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Following_a_banned_user_should_be_rejected()
    {
        var follower = await CreateUserAsync("banned-follow-source");
        var banned = await CreateUserAsync("banned-follow-target");
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, banned.Id, UserStatus.Banned));
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(follower.Id);

        var response = await authentication.Client.PostAsync($"/api/Follower/{banned.Id}", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Following_a_missing_user_should_return_not_found()
    {
        var follower = await CreateUserAsync("missing-follow-source");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(follower.Id);

        var response = await authentication.Client.PostAsync("/api/Follower/999999", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Follower_and_following_listings_should_be_public_and_symmetric()
    {
        var follower = await CreateUserAsync("listing-follower");
        var target = await CreateUserAsync("listing-followed");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(follower.Id);
        (await authentication.Client.PostAsync($"/api/Follower/{target.Id}", null)).EnsureSuccessStatusCode();
        using var client = Factory.CreateHttpsClient();

        var followers = await client.GetFromJsonAsync<PagedResponse<FollowerDto>>($"/api/Follower/{target.Id}/followers?page=1&size=20");
        var followings = await client.GetFromJsonAsync<PagedResponse<FollowerDto>>($"/api/Follower/{follower.Id}/followings?page=1&size=20");

        followers!.Items.Should().ContainSingle().Which.UserId.Should().Be(follower.Id);
        followings!.Items.Should().ContainSingle().Which.UserId.Should().Be(target.Id);
    }

    [Fact]
    public async Task Follower_listing_of_a_banned_user_should_return_not_found()
    {
        var banned = await CreateUserAsync("banned-listing-user");
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, banned.Id, UserStatus.Banned));
        using var client = Factory.CreateHttpsClient();

        (await client.GetAsync($"/api/Follower/{banned.Id}/followers?page=1&size=20")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/Follower/{banned.Id}/followings?page=1&size=20")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task User_profile_should_report_follower_and_following_counts()
    {
        var follower = await CreateUserAsync("count-follower");
        var target = await CreateUserAsync("count-followed");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(follower.Id);
        (await authentication.Client.PostAsync($"/api/Follower/{target.Id}", null)).EnsureSuccessStatusCode();
        using var client = Factory.CreateHttpsClient();

        var targetProfile = await client.GetFromJsonAsync<UserDto>($"/api/User/getUserById/{target.Id}");
        var followerProfile = await client.GetFromJsonAsync<UserDto>($"/api/User/getUserByUsername/count-follower");

        targetProfile!.FollowerCount.Should().Be(1);
        targetProfile.FollowingCount.Should().Be(0);
        followerProfile!.FollowingCount.Should().Be(1);
    }

    [Fact]
    public async Task Interaction_endpoints_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.PostAsync("/api/Like/1", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/Bookmark", new CreateBookmarksCommand { PostId = 1 })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/api/Follower/1", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/Follower/status/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
