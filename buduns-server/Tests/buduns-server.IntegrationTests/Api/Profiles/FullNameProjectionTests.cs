using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Bookmarks.Commands.Create;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;

namespace buduns_server.IntegrationTests.Api.Profiles;

public sealed class FullNameProjectionTests : IntegrationTestBase
{
    public FullNameProjectionTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Full_name_should_be_returned_by_user_post_follower_like_and_bookmark_apis()
    {
        var author = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "projection-author", "Projection Author", RoleConstants.User));
        var viewer = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "projection-viewer", "Projection Viewer", RoleConstants.Admin));
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(viewer.Id);
        (await authentication.Client.PostAsync($"/api/Like/{post.Id}", null)).EnsureSuccessStatusCode();
        (await authentication.Client.PostAsJsonAsync("/api/Bookmark", new CreateBookmarksCommand { PostId = post.Id })).EnsureSuccessStatusCode();
        (await authentication.Client.PostAsync($"/api/Follower/{author.Id}", null)).EnsureSuccessStatusCode();

        var user = await authentication.Client.GetDataAsync<UserDto>($"/api/User/getUserById/{author.Id}");
        var posts = await authentication.Client.GetDataAsync<PagedResponse<PostDto>>("/api/Post/getAll?page=1&size=20");
        var followers = await authentication.Client.GetDataAsync<PagedResponse<FollowerDto>>($"/api/Follower/{author.Id}/followers?page=1&size=20");
        var likes = await authentication.Client.GetDataAsync<PagedResponse<LikeDto>>($"/api/Like/post/{post.Id}?page=1&size=20");
        var bookmarks = await authentication.Client.GetDataAsync<PagedResponse<BookmarkDto>>("/api/Bookmark?page=1&size=20");

        user!.FullName.Should().Be("Projection Author");
        posts!.Items.Should().ContainSingle(item => item.Id == post.Id && item.UserFullName == "Projection Author");
        followers!.Items.Should().ContainSingle(item => item.UserId == viewer.Id && item.FullName == "Projection Viewer");
        likes!.Items.Should().ContainSingle(item => item.UserId == viewer.Id && item.FullName == "Projection Viewer");
        bookmarks!.Items.Should().ContainSingle(item => item.PostId == post.Id && item.Post.UserFullName == "Projection Author");
    }
}
