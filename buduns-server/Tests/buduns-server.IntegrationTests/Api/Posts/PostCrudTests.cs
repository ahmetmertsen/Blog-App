using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Posts.Commands.Create;
using buduns_server.Application.Features.Posts.Commands.Delete;
using buduns_server.Application.Features.Posts.Commands.Update;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using buduns_server.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Posts;

public sealed class PostCrudTests : IntegrationTestBase
{
    public PostCrudTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_post_should_publish_it_and_attach_tags()
    {
        var author = await CreateUserAsync("post-author");
        await GrantEndpointPermissionsAsync();
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "dotnet"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "yeni paylasim", TagIds = new List<int> { tag.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().Include(post => post.Tags).SingleAsync(post => post.UserId == author.Id));
        stored.Content.Should().Be("yeni paylasim");
        stored.Status.Should().Be(PostStatus.Published);
        stored.isPublished.Should().BeTrue();
        stored.Tags.Should().ContainSingle().Which.Id.Should().Be(tag.Id);
    }

    [Fact]
    public async Task Create_post_with_unknown_tag_should_return_bad_request()
    {
        var author = await CreateUserAsync("bad-tag-author");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik", TagIds = new List<int> { 999999 } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ApiResponse>())!.Error!.Message.Should().Contain("999999");
    }

    [Fact]
    public async Task Create_post_should_ignore_client_supplied_user_id()
    {
        var author = await CreateUserAsync("ownership-author");
        var victim = await CreateUserAsync("ownership-victim");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        // UserId [JsonIgnore]; govdeden gelen deger CurrentUserBehavior tarafindan eziliyor.
        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new { content = "sahiplik testi", userId = victim.Id, tagIds = Array.Empty<int>() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().SingleAsync(post => post.Content == "sahiplik testi"));
        stored.UserId.Should().Be(author.Id);
    }

    [Fact]
    public async Task Update_post_should_replace_content_and_tags_for_the_owner()
    {
        var author = await CreateUserAsync("update-author");
        await GrantEndpointPermissionsAsync();
        var oldTag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "eski"));
        var newTag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "yeni"));
        var post = await Factory.ExecuteScopeAsync(async services =>
        {
            var created = await DatabaseSeeder.CreatePostAsync(services, author.Id, "eski icerik");
            var context = services.GetRequiredService<BudunsDbContext>();
            var tracked = await context.Posts.Include(item => item.Tags).SingleAsync(item => item.Id == created.Id);
            tracked.Tags.Add(await context.Tags.SingleAsync(item => item.Id == oldTag.Id));
            await context.SaveChangesAsync();
            return created;
        });
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.PutAsJsonAsync("/api/Post/update", new UpdatePostsCommand { Id = post.Id, Content = "yeni icerik", TagIds = new List<int> { newTag.Id } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().Include(item => item.Tags).SingleAsync(item => item.Id == post.Id));
        stored.Content.Should().Be("yeni icerik");
        stored.Tags.Should().ContainSingle().Which.Id.Should().Be(newTag.Id);
    }

    [Fact]
    public async Task Update_post_of_another_user_should_be_rejected()
    {
        var author = await CreateUserAsync("victim-author");
        var attacker = await CreateUserAsync("attacker-user");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "orijinal"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(attacker.Id);

        var response = await authentication.Client.PutAsJsonAsync("/api/Post/update", new UpdatePostsCommand { Id = post.Id, Content = "ele gecirildi" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().SingleAsync(item => item.Id == post.Id));
        stored.Content.Should().Be("orijinal");
    }

    [Fact]
    public async Task Delete_post_should_soft_delete_it_and_hide_it_from_listings()
    {
        var author = await CreateUserAsync("delete-author");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/Post/delete")
        {
            Content = JsonContent.Create(new DeletePostsCommand { Id = post.Id })
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Posts.AsNoTracking().SingleAsync(item => item.Id == post.Id));
        stored.Status.Should().Be(PostStatus.DeletedByOwner);
        stored.isDeleted.Should().BeTrue();

        using var reader = Factory.CreateHttpsClient();
        (await reader.GetAsync($"/api/Post/getById/{post.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var listing = await reader.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/getAll?page=1&size=20");
        listing!.Items.Should().NotContain(item => item.Id == post.Id);
    }

    [Fact]
    public async Task Delete_post_of_another_user_should_be_rejected()
    {
        var author = await CreateUserAsync("delete-victim");
        var attacker = await CreateUserAsync("delete-attacker");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(attacker.Id);

        var response = await authentication.Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/Post/delete")
        {
            Content = JsonContent.Create(new DeletePostsCommand { Id = post.Id })
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_post_by_id_should_be_public_and_report_viewer_specific_flags()
    {
        var author = await CreateUserAsync("flag-author");
        var viewer = await CreateUserAsync("flag-viewer");
        await GrantEndpointPermissionsAsync();
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using var viewerAuthentication = await Factory.CreateAuthenticatedClientAsync(viewer.Id);
        (await viewerAuthentication.Client.PostAsync($"/api/Like/{post.Id}", null)).EnsureSuccessStatusCode();

        using var anonymous = Factory.CreateHttpsClient();
        var anonymousPost = await anonymous.GetFromJsonAsync<PostDto>($"/api/Post/getById/{post.Id}");
        var viewerPost = await viewerAuthentication.Client.GetFromJsonAsync<PostDto>($"/api/Post/getById/{post.Id}");

        anonymousPost!.IsLiked.Should().BeFalse();
        anonymousPost.IsOwner.Should().BeFalse();
        anonymousPost.LikeCount.Should().Be(1);
        viewerPost!.IsLiked.Should().BeTrue();
        viewerPost.IsOwner.Should().BeFalse();
    }

    [Fact]
    public async Task Get_post_by_id_should_flag_ownership_for_the_author()
    {
        var author = await CreateUserAsync("owner-flag-author");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var dto = await authentication.Client.GetFromJsonAsync<PostDto>($"/api/Post/getById/{post.Id}");

        dto!.IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task Get_all_posts_should_apply_paging_search_and_tag_filters()
    {
        var author = await CreateUserAsync("filter-author");
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "filtre"));
        var tagged = await Factory.ExecuteScopeAsync(async services =>
        {
            var created = await DatabaseSeeder.CreatePostAsync(services, author.Id, "aranan kelime iceren paylasim");
            var context = services.GetRequiredService<BudunsDbContext>();
            var tracked = await context.Posts.Include(item => item.Tags).SingleAsync(item => item.Id == created.Id);
            tracked.Tags.Add(await context.Tags.SingleAsync(item => item.Id == tag.Id));
            await context.SaveChangesAsync();
            return created;
        });
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "alakasiz paylasim"));
        using var client = Factory.CreateHttpsClient();

        var searchResult = await client.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/getAll?page=1&size=20&search=aranan");
        var tagResult = await client.GetFromJsonAsync<PagedResponse<PostDto>>($"/api/Post/getAll?page=1&size=20&tagId={tag.Id}");
        var pagedResult = await client.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/getAll?page=1&size=1");

        searchResult!.Items.Should().ContainSingle().Which.Id.Should().Be(tagged.Id);
        tagResult!.Items.Should().ContainSingle().Which.Id.Should().Be(tagged.Id);
        pagedResult!.Items.Should().ContainSingle();
        pagedResult.TotalCount.Should().Be(2);
        pagedResult.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Get_all_posts_should_reject_invalid_paging()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/Post/getAll?page=0&size=500");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        body!.Error!.ValidationErrors.Should().ContainKey("Page").And.ContainKey("Size");
    }

    [Fact]
    public async Task Get_all_posts_sorted_by_oldest_and_recent_should_be_mirrored()
    {
        var author = await CreateUserAsync("sort-author");
        var older = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "eski"));
        await Task.Delay(20);
        var newer = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "yeni"));
        using var client = Factory.CreateHttpsClient();

        var recent = await client.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/getAll?page=1&size=20&sortBy=recent");
        var oldest = await client.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/getAll?page=1&size=20&sortBy=oldest");

        recent!.Items.First().Id.Should().Be(newer.Id);
        oldest!.Items.First().Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task Posts_of_banned_users_should_not_be_listed()
    {
        var banned = await CreateUserAsync("banned-author");
        var visible = await CreateUserAsync("visible-author");
        var hiddenPost = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, banned.Id, "yasakli yazar"));
        var visiblePost = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, visible.Id, "normal yazar"));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, banned.Id, UserStatus.Banned));
        using var client = Factory.CreateHttpsClient();

        var listing = await client.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/getAll?page=1&size=20");

        listing!.Items.Select(item => item.Id).Should().Contain(visiblePost.Id).And.NotContain(hiddenPost.Id);
    }

    [Fact]
    public async Task My_posts_should_only_contain_the_current_users_posts()
    {
        var author = await CreateUserAsync("my-posts-author");
        var other = await CreateUserAsync("other-author");
        await GrantEndpointPermissionsAsync();
        var mine = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "benim"));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, other.Id, "baskasinin"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        var response = await authentication.Client.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/me?page=1&size=20");

        response!.Items.Should().ContainSingle().Which.Id.Should().Be(mine.Id);
    }

    [Fact]
    public async Task Following_feed_should_only_contain_posts_of_followed_users()
    {
        var reader = await CreateUserAsync("feed-reader");
        var followed = await CreateUserAsync("feed-followed");
        var stranger = await CreateUserAsync("feed-stranger");
        await GrantEndpointPermissionsAsync();
        var followedPost = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, followed.Id, "takip edilen"));
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, stranger.Id, "yabanci"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(reader.Id);
        (await authentication.Client.PostAsync($"/api/Follower/{followed.Id}", null)).EnsureSuccessStatusCode();

        var response = await authentication.Client.GetFromJsonAsync<PagedResponse<PostDto>>("/api/Post/following?page=1&size=20");

        response!.Items.Should().ContainSingle().Which.Id.Should().Be(followedPost.Id);
    }

    [Fact]
    public async Task Posts_by_tag_endpoint_should_return_only_tagged_posts()
    {
        var author = await CreateUserAsync("tag-endpoint-author");
        var tag = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateTagAsync(services, "etiket"));
        var tagged = await Factory.ExecuteScopeAsync(async services =>
        {
            var created = await DatabaseSeeder.CreatePostAsync(services, author.Id, "etiketli");
            var context = services.GetRequiredService<BudunsDbContext>();
            var tracked = await context.Posts.Include(item => item.Tags).SingleAsync(item => item.Id == created.Id);
            tracked.Tags.Add(await context.Tags.SingleAsync(item => item.Id == tag.Id));
            await context.SaveChangesAsync();
            return created;
        });
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id, "etiketsiz"));
        using var client = Factory.CreateHttpsClient();

        var response = await client.GetFromJsonAsync<PagedResponse<PostDto>>($"/api/Post/tag/{tag.Id}?page=1&size=20");

        response!.Items.Should().ContainSingle().Which.Id.Should().Be(tagged.Id);
    }

    [Fact]
    public async Task Post_write_endpoints_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/api/Post/update", new UpdatePostsCommand { Id = 1, Content = "icerik" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/Post/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/Post/following")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_write_endpoints_should_reject_users_without_endpoint_permission()
    {
        var author = await CreateUserAsync("permissionless-author");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(author.Id);

        // Endpoint yetkisi seed edilmedi: RolePermissionFilter 403 dondurmeli.
        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
