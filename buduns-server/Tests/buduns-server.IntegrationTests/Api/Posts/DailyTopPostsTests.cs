using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Sorgu Faz 3'te paylasimdan degil gunun olaylarindan baslayacak sekilde
    /// yeniden yazildi. Gorunurluk filtreleri artik olay tarafinda uygulaniyor;
    /// bu testler her bir filtrenin tasinma sirasinda dusmedigini dogrular.
    /// </summary>
    [Fact]
    public async Task Daily_top_should_exclude_invisible_and_self_generated_activity()
    {
        var author = await CreateUserAsync("edge-author", "Edge Author");
        var bannedAuthor = await CreateUserAsync("edge-banned-author", "Edge Banned Author");
        var liker = await CreateUserAsync("edge-liker", "Edge Liker");
        var bannedLiker = await CreateUserAsync("edge-banned-liker", "Edge Banned Liker");

        var visiblePost = await CreatePostAsync(author.Id, "gorunur paylasim");
        var selfLikedPost = await CreatePostAsync(author.Id, "kendi begendigi paylasim");
        var deletedPost = await CreatePostAsync(author.Id, "silinmis paylasim");
        var bannedAuthorPost = await CreatePostAsync(bannedAuthor.Id, "yasakli yazarin paylasimi");
        var bannedLikerPost = await CreatePostAsync(author.Id, "yasakli kullanicinin begendigi");
        var staleLikePost = await CreatePostAsync(author.Id, "dun begenilen paylasim");

        await AddLikeAsync(visiblePost.Id, liker.Id);
        await AddLikeAsync(selfLikedPost.Id, author.Id);
        await AddLikeAsync(deletedPost.Id, liker.Id);
        await AddLikeAsync(bannedAuthorPost.Id, liker.Id);
        await AddLikeAsync(bannedLikerPost.Id, bannedLiker.Id);
        await AddLikeAsync(staleLikePost.Id, liker.Id, DateTime.UtcNow.AddDays(-2));

        await SoftDeletePostAsync(deletedPost.Id);
        await BanUserAsync(bannedAuthor.Id);
        await BanUserAsync(bannedLiker.Id);

        var response = await GetDailyTopAsync();

        // Listeye yalnizca gercekten gecerli aktivite alan paylasim girmeli.
        response.Should().ContainSingle();
        response[0].PostId.Should().Be(visiblePost.Id);
        response[0].DailyLikeCount.Should().Be(1);
    }

    [Fact]
    public async Task Daily_top_should_cap_at_fifty_posts_ordered_by_score()
    {
        var author = await CreateUserAsync("cap-author", "Cap Author");
        var liker = await CreateUserAsync("cap-liker", "Cap Liker");
        var commenter = await CreateUserAsync("cap-commenter", "Cap Commenter");

        // 60 paylasimin hepsi bir begeni alir; ilk ikisi ayrica yorum alir.
        var posts = new List<int>();
        for (var index = 0; index < 60; index++)
        {
            var post = await CreatePostAsync(author.Id, $"cap post {index}");
            posts.Add(post.Id);
            await AddLikeAsync(post.Id, liker.Id);
        }

        await AddCommentAsync(posts[0], commenter.Id);
        await AddCommentAsync(posts[1], commenter.Id);

        var response = await GetDailyTopAsync();

        response.Should().HaveCount(50);
        response.Select(item => item.Rank).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
        response[0].Rank.Should().Be(1);
        response[49].Rank.Should().Be(50);

        // Yorum + begeni alan ikisi (skor 1.0) tepede olmali.
        response.Take(2).Select(item => item.PostId).Should().BeEquivalentTo(new[] { posts[0], posts[1] });
        response[0].Score.Should().BeApproximately(1.0, 0.0001);
        response[2].Score.Should().BeApproximately(0.4, 0.0001);
        response.Select(item => item.Score).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Daily_top_should_return_empty_when_there_is_no_activity_today()
    {
        var author = await CreateUserAsync("quiet-author", "Quiet Author");
        var liker = await CreateUserAsync("quiet-liker", "Quiet Liker");
        var post = await CreatePostAsync(author.Id, "dun begenilen");
        await AddLikeAsync(post.Id, liker.Id, DateTime.UtcNow.AddDays(-3));

        var response = await GetDailyTopAsync();

        response.Should().BeEmpty();
    }

    [Fact]
    public async Task Daily_top_total_counts_should_include_activity_from_other_days()
    {
        var author = await CreateUserAsync("total-author", "Total Author");
        var todayLiker = await CreateUserAsync("total-liker-today", "Total Liker Today");
        var oldLiker = await CreateUserAsync("total-liker-old", "Total Liker Old");
        var bookmarker = await CreateUserAsync("total-bookmarker", "Total Bookmarker");
        var post = await CreatePostAsync(author.Id, "toplam sayim paylasimi");

        await AddLikeAsync(post.Id, todayLiker.Id);
        await AddLikeAsync(post.Id, oldLiker.Id, DateTime.UtcNow.AddDays(-5));
        await AddBookmarkAsync(post.Id, bookmarker.Id);

        var response = await GetDailyTopAsync();

        response.Should().ContainSingle();
        // Gunluk sayim yalnizca bugunu, toplam sayim tum zamani kapsar.
        response[0].DailyLikeCount.Should().Be(1);
        response[0].LikeCount.Should().Be(2);
        response[0].BookmarkCount.Should().Be(1);
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

    private Task AddLikeAsync(int postId, int userId, DateTime? createdAt = null) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        context.Likes.Add(new Like { PostId = postId, UserId = userId, CreatedAt = createdAt ?? DateTime.UtcNow, isActive = true, isDeleted = false });
        await context.SaveChangesAsync();
    });

    private Task AddBookmarkAsync(int postId, int userId) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        context.Bookmarks.Add(new Bookmark { PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
        await context.SaveChangesAsync();
    });

    private Task SoftDeletePostAsync(int postId) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var post = await context.Posts.SingleAsync(item => item.Id == postId);
        post.isDeleted = true;
        post.isActive = false;
        await context.SaveChangesAsync();
    });

    private Task BanUserAsync(int userId) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var user = await context.Users.SingleAsync(item => item.Id == userId);
        user.Status = UserStatus.Banned;
        await context.SaveChangesAsync();
    });

    private Task AddCommentAsync(int postId, int userId) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        context.Comments.Add(new Comment { PostId = postId, UserId = userId, Content = "yorum", Status = CommentStatus.Published, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
        await context.SaveChangesAsync();
    });
}
