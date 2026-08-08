using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Posts;

/// <summary>
/// daily-top50 tam tablo taramasi yapan en pahali sorgu. Redis onbellegi
/// devreye alindiktan sonra hem siralamanin bozulmadigini hem de ikinci
/// istegin veritabanina inmedigini dogrular.
/// </summary>
public sealed class DailyTopPostsTests : IntegrationTestBase
{
    private const string Endpoint = "/api/Post/daily-top50";

    public DailyTopPostsTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Daily_top_should_rank_posts_by_daily_score()
    {
        var author = await CreateUserAsync("top-author", "Top Author");
        var liker = await CreateUserAsync("top-liker", "Top Liker");
        var commenter = await CreateUserAsync("top-commenter", "Top Commenter");

        // Yorum agirligi (0.6) begeni agirligindan (0.4) yuksek: tek yorumlu
        // paylasim tek begenili paylasimin ustunde olmali.
        var likedPost = await CreatePostAsync(author.Id, "begenilen paylasim");
        var commentedPost = await CreatePostAsync(author.Id, "yorumlanan paylasim");
        await CreatePostAsync(author.Id, "hareketsiz paylasim");

        await AddLikeAsync(likedPost.Id, liker.Id);
        await AddCommentAsync(commentedPost.Id, commenter.Id);

        var response = await GetDailyTopAsync();

        response.Should().HaveCount(2);
        response[0].PostId.Should().Be(commentedPost.Id);
        response[0].Rank.Should().Be(1);
        response[0].DailyCommentCount.Should().Be(1);
        response[0].Score.Should().BeApproximately(0.6, 0.0001);
        response[1].PostId.Should().Be(likedPost.Id);
        response[1].Rank.Should().Be(2);
        response[1].DailyLikeCount.Should().Be(1);
        response[1].UserName.Should().Be("top-author");
    }

    [Fact]
    public async Task Daily_top_should_serve_repeated_requests_from_cache()
    {
        var author = await CreateUserAsync("cache-author", "Cache Author");
        var firstLiker = await CreateUserAsync("cache-liker-1", "Cache Liker One");
        var secondLiker = await CreateUserAsync("cache-liker-2", "Cache Liker Two");
        var post = await CreatePostAsync(author.Id, "onbelleklenecek paylasim");
        await AddLikeAsync(post.Id, firstLiker.Id);

        var first = await GetDailyTopAsync();
        first.Should().ContainSingle();
        first[0].DailyLikeCount.Should().Be(1);

        // Veritabani degisti ama onbellek hala gecerli: ikinci istek eski
        // sonucu dondurmeli, yani sorgu tekrar calismamis olmali.
        await AddLikeAsync(post.Id, secondLiker.Id);

        var cached = await GetDailyTopAsync();
        cached[0].DailyLikeCount.Should().Be(1);

        await Factory.ClearCacheAsync();

        var afterInvalidation = await GetDailyTopAsync();
        afterInvalidation[0].DailyLikeCount.Should().Be(2);
    }

    private async Task<List<TopPostDto>> GetDailyTopAsync()
    {
        using var client = Factory.CreateHttpsClient();
        var response = await client.GetAsync(Endpoint);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<List<TopPostDto>>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private Task<User> CreateUserAsync(string userName, string fullName) =>
        Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, userName, fullName, RoleConstants.User));

    private Task<Post> CreatePostAsync(int userId, string content) =>
        Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, userId, content));

    private Task AddLikeAsync(int postId, int userId) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        context.Likes.Add(new Like { PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
        await context.SaveChangesAsync();
    });

    private Task AddCommentAsync(int postId, int userId) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        context.Comments.Add(new Comment { PostId = postId, UserId = userId, Content = "yorum", Status = CommentStatus.Published, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
        await context.SaveChangesAsync();
    });
}
