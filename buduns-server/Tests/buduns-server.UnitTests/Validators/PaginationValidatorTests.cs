using buduns_server.Application.Features.Bookmarks.Queries.GetBookmarks;
using buduns_server.Application.Features.Comments.Queries.GetByPostId;
using buduns_server.Application.Features.Comments.Queries.GetByUserId;
using buduns_server.Application.Features.Followers.Queries.GetAllByUserId;
using buduns_server.Application.Features.Likes.Queries.GetByPostId;
using buduns_server.Application.Features.Likes.Queries.GetMyLikes;
using buduns_server.Application.Features.Notifications.Queries.GetAllByUserId;
using buduns_server.Application.Features.Posts.Queries.GetAll;
using buduns_server.Application.Features.Posts.Queries.GetAllByTagId;
using buduns_server.Application.Features.Posts.Queries.GetFollowingPosts;
using buduns_server.Application.Features.Posts.Queries.GetMyPosts;
using buduns_server.Application.Features.Posts.Queries.GetPostsByUserId;
using buduns_server.Application.Features.Report.Queries.GetReports;
using buduns_server.Application.Features.Tags.Queries.GetAll;
using buduns_server.Application.Features.Users.Queries.GetAll;
using FluentValidation;
using FluentValidation.Results;

namespace buduns_server.UnitTests.Validators;

/// <summary>
/// Ayni sayfalama kurali on altı ayri sorguda elle tekrarlaniyor. Birinde Size
/// ust siniri unutulursa sessizce sinirsiz sayfa boyutu kabul edilir. Buradaki
/// testler tum sorgulari ayni sinir degerleriyle tek yerden kilitler.
/// </summary>
public class PaginationValidatorTests
{
    /// <summary>Sayfalama alanlarini ayarlayip dogrulama sonucunu donduren kutu.</summary>
    private sealed record PagedCase(string Name, Func<int, int, Task<ValidationResult>> Validate);

    private static readonly PagedCase[] Cases =
    {
        Paged(nameof(GetBookmarksQuery), (page, size) => new GetBookmarksQuery { UserId = 1, Page = page, Size = size }, new GetBookmarksQueryValidator()),
        Paged(nameof(GetCommentsByPostIdQuery), (page, size) => new GetCommentsByPostIdQuery { PostId = 1, Page = page, Size = size }, new GetCommentsByPostIdQueryValidator()),
        Paged(nameof(GetCommentsByUserIdQuery), (page, size) => new GetCommentsByUserIdQuery { UserId = 1, Page = page, Size = size }, new GetCommentsByUserIdQueryValidator()),
        Paged(nameof(GetAllFollowersByUserIdQuery), (page, size) => new GetAllFollowersByUserIdQuery { UserId = 1, Page = page, Size = size }, new GetAllFollowersByUserIdQueryValidator()),
        Paged(nameof(GetAllFollowingsByUserIdQuery), (page, size) => new GetAllFollowingsByUserIdQuery { UserId = 1, Page = page, Size = size }, new GetAllFollowingsByUserIdQueryValidator()),
        Paged(nameof(GetLikesByPostIdQuery), (page, size) => new GetLikesByPostIdQuery { PostId = 1, Page = page, Size = size }, new GetLikesByPostIdQueryValidator()),
        Paged(nameof(GetMyLikedPostsQuery), (page, size) => new GetMyLikedPostsQuery { UserId = 1, Page = page, Size = size }, new GetMyLikedPostsQueryValidator()),
        Paged(nameof(GetAllNotificationsByUserIdQuery), (page, size) => new GetAllNotificationsByUserIdQuery { UserId = 1, Page = page, Size = size }, new GetAllNotificationsByUserIdQueryValidator()),
        Paged(nameof(GetAllPostsQuery), (page, size) => new GetAllPostsQuery { Page = page, Size = size }, new GetAllPostsQueryValidator()),
        Paged(nameof(GetAllPostsByTagIdQuery), (page, size) => new GetAllPostsByTagIdQuery { TagId = 1, Page = page, Size = size }, new GetAllPostsByTagIdQueryValidator()),
        Paged(nameof(GetFollowingPostsQuery), (page, size) => new GetFollowingPostsQuery { UserId = 1, Page = page, Size = size }, new GetFollowingPostsQueryValidator()),
        Paged(nameof(GetMyPostsQuery), (page, size) => new GetMyPostsQuery { UserId = 1, Page = page, Size = size }, new GetMyPostsQueryValidator()),
        Paged(nameof(GetPostsByUserIdQuery), (page, size) => new GetPostsByUserIdQuery { UserId = 1, Page = page, Size = size }, new GetPostsByUserIdQueryValidator()),
        Paged(nameof(GetReportsQuery), (page, size) => new GetReportsQuery { Page = page, Size = size }, new GetReportsQueryValidator()),
        Paged(nameof(GetAllTagsQuery), (page, size) => new GetAllTagsQuery { Page = page, Size = size }, new GetAllTagsQueryValidator()),
        Paged(nameof(GetAllUsersQuery), (page, size) => new GetAllUsersQuery { Page = page, Size = size }, new GetAllUsersQueryValidator())
    };

    [Fact]
    public async Task AllPagedQueries_AtLimitBoundaries_ShouldSucceed()
    {
        foreach (var pagedCase in Cases)
        {
            var lower = await pagedCase.Validate(1, 1);
            var upper = await pagedCase.Validate(int.MaxValue, 100);

            Assert.True(lower.IsValid, $"{pagedCase.Name}: page=1, size=1 gecerli olmaliydi. {Describe(lower)}");
            Assert.True(upper.IsValid, $"{pagedCase.Name}: size=100 gecerli olmaliydi. {Describe(upper)}");
        }
    }

    [Fact]
    public async Task AllPagedQueries_PageBelowOne_ShouldFail()
    {
        foreach (var pagedCase in Cases)
        {
            var zero = await pagedCase.Validate(0, 20);
            var negative = await pagedCase.Validate(-1, 20);

            Assert.Contains(zero.Errors, error => error.PropertyName == "Page");
            Assert.Contains(negative.Errors, error => error.PropertyName == "Page");
        }
    }

    [Fact]
    public async Task AllPagedQueries_SizeOutsideRange_ShouldFail()
    {
        foreach (var pagedCase in Cases)
        {
            var zero = await pagedCase.Validate(1, 0);
            var overflow = await pagedCase.Validate(1, 101);

            Assert.Contains(zero.Errors, error => error.PropertyName == "Size");
            Assert.Contains(overflow.Errors, error => error.PropertyName == "Size");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("recent")]
    [InlineData("oldest")]
    [InlineData("popular")]
    [InlineData("  POPULAR  ")]
    public async Task GetAllPosts_SupportedSort_ShouldSucceed(string? sortBy)
    {
        var result = await new GetAllPostsQueryValidator().ValidateAsync(new GetAllPostsQuery { SortBy = sortBy });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetAllPosts_NonPositiveFilters_ShouldFail()
    {
        var result = await new GetAllPostsQueryValidator().ValidateAsync(new GetAllPostsQuery { TagId = 0, UserId = 0 });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetAllPostsQuery.TagId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetAllPostsQuery.UserId));
    }

    [Fact]
    public async Task GetAllPosts_NullFilters_ShouldSucceed()
    {
        // TagId/UserId kurallari yalnizca deger verildiginde calismali.
        var result = await new GetAllPostsQueryValidator().ValidateAsync(new GetAllPostsQuery { TagId = null, UserId = null, Search = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetAllPosts_SearchLongerThanLimit_ShouldFail()
    {
        var result = await new GetAllPostsQueryValidator().ValidateAsync(new GetAllPostsQuery { Search = new string('a', 101) });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetAllPostsQuery.Search));
    }

    [Fact]
    public async Task GetAllTags_SearchLengthBoundary_ShouldMatchLimit()
    {
        var atLimit = await new GetAllTagsQueryValidator().ValidateAsync(new GetAllTagsQuery { Search = new string('a', 100) });
        var overLimit = await new GetAllTagsQueryValidator().ValidateAsync(new GetAllTagsQuery { Search = new string('a', 101) });

        Assert.True(atLimit.IsValid);
        Assert.Contains(overLimit.Errors, error => error.PropertyName == nameof(GetAllTagsQuery.Search));
    }

    [Fact]
    public async Task PagedQueries_NonPositiveTargetIdentifiers_ShouldFail()
    {
        Assert.Contains((await new GetCommentsByPostIdQueryValidator().ValidateAsync(new GetCommentsByPostIdQuery { PostId = 0 })).Errors, error => error.PropertyName == nameof(GetCommentsByPostIdQuery.PostId));
        Assert.Contains((await new GetCommentsByUserIdQueryValidator().ValidateAsync(new GetCommentsByUserIdQuery { UserId = 0 })).Errors, error => error.PropertyName == nameof(GetCommentsByUserIdQuery.UserId));
        Assert.Contains((await new GetAllFollowersByUserIdQueryValidator().ValidateAsync(new GetAllFollowersByUserIdQuery { UserId = 0 })).Errors, error => error.PropertyName == nameof(GetAllFollowersByUserIdQuery.UserId));
        Assert.Contains((await new GetAllFollowingsByUserIdQueryValidator().ValidateAsync(new GetAllFollowingsByUserIdQuery { UserId = 0 })).Errors, error => error.PropertyName == nameof(GetAllFollowingsByUserIdQuery.UserId));
        Assert.Contains((await new GetLikesByPostIdQueryValidator().ValidateAsync(new GetLikesByPostIdQuery { PostId = 0 })).Errors, error => error.PropertyName == nameof(GetLikesByPostIdQuery.PostId));
        Assert.Contains((await new GetAllPostsByTagIdQueryValidator().ValidateAsync(new GetAllPostsByTagIdQuery { TagId = 0 })).Errors, error => error.PropertyName == nameof(GetAllPostsByTagIdQuery.TagId));
        Assert.Contains((await new GetPostsByUserIdQueryValidator().ValidateAsync(new GetPostsByUserIdQuery { UserId = 0 })).Errors, error => error.PropertyName == nameof(GetPostsByUserIdQuery.UserId));
    }

    private static PagedCase Paged<TQuery>(string name, Func<int, int, TQuery> factory, IValidator<TQuery> validator) =>
        new(name, (page, size) => validator.ValidateAsync(factory(page, size)));

    private static string Describe(ValidationResult result) =>
        string.Join(" | ", result.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"));
}
