using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Posts.Commands.Create;
using buduns_server.Application.Features.Posts.Commands.Delete;
using buduns_server.Application.Features.Posts.Commands.Update;
using buduns_server.Application.Features.Posts.Queries.GetAll;
using buduns_server.Application.Features.Posts.Queries.GetAllByTagId;
using buduns_server.Application.Features.Posts.Queries.GetById;
using buduns_server.Application.Features.Posts.Queries.GetDailyTopPosts;
using buduns_server.Application.Features.Posts.Queries.GetFollowingPosts;
using buduns_server.Application.Features.Posts.Queries.GetMyPosts;
using buduns_server.Application.Features.Posts.Queries.GetPostsByUserId;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

public class PostHandlerTests
{
    [Fact]
    public async Task CreatePost_ShouldPersistPublishedPostOwnedByCurrentUser()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.TagRepository.GetByIdsAsync(Arg.Any<List<int>>(), Arg.Any<CancellationToken>()).Returns(new List<Tag>());
        Post? persisted = null;
        await unitOfWork.PostRepository.AddAsync(Arg.Do<Post>(post => persisted = post));
        var handler = new CreatePostsCommandHandler(unitOfWork, NullLogger<CreatePostsCommandHandler>.Instance);

        var response = await handler.Handle(new CreatePostsCommand { UserId = 9, Content = "merhaba" }, CancellationToken.None);

        Assert.NotNull(persisted);
        Assert.Equal(9, persisted!.UserId);
        Assert.Equal("merhaba", persisted.Content);
        Assert.True(persisted.isPublished);
        Assert.True(persisted.isActive);
        Assert.False(persisted.isDeleted);
        Assert.Equal(PostStatus.Published, persisted.Status);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CreatePost_ShouldAttachResolvedTagsAndDeduplicateRequestedIds()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var tags = new List<Tag> { new() { Id = 1, Name = "dotnet", NormalizedName = "DOTNET" } };
        List<int>? requestedTagIds = null;
        unitOfWork.TagRepository.GetByIdsAsync(Arg.Do<List<int>>(ids => requestedTagIds = ids), Arg.Any<CancellationToken>()).Returns(tags);
        Post? persisted = null;
        await unitOfWork.PostRepository.AddAsync(Arg.Do<Post>(post => persisted = post));
        var handler = new CreatePostsCommandHandler(unitOfWork, NullLogger<CreatePostsCommandHandler>.Instance);

        await handler.Handle(new CreatePostsCommand { UserId = 9, Content = "merhaba", TagIds = new List<int> { 1, 1, 1 } }, CancellationToken.None);

        Assert.Equal(new[] { 1 }, requestedTagIds);
        Assert.Same(tags, persisted!.Tags);
    }

    [Fact]
    public async Task CreatePost_UnknownTagId_ShouldThrowBadRequestAndNotPersist()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.TagRepository.GetByIdsAsync(Arg.Any<List<int>>(), Arg.Any<CancellationToken>()).Returns(new List<Tag> { new() { Id = 1, Name = "dotnet", NormalizedName = "DOTNET" } });
        var handler = new CreatePostsCommandHandler(unitOfWork, NullLogger<CreatePostsCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new CreatePostsCommand { UserId = 9, Content = "merhaba", TagIds = new List<int> { 1, 42 } }, CancellationToken.None));

        Assert.Contains("42", exception.Message);
        await unitOfWork.PostRepository.DidNotReceiveWithAnyArgs().AddAsync(default!);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdatePost_ShouldReplaceContentAndTags()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var post = new Post { Id = 5, UserId = 9, Content = "eski", isPublished = true, Status = PostStatus.Published };
        post.Tags.Add(new Tag { Id = 1, Name = "eski-tag", NormalizedName = "ESKI-TAG" });
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns(post);
        var newTag = new Tag { Id = 2, Name = "yeni-tag", NormalizedName = "YENI-TAG" };
        unitOfWork.TagRepository.GetByIdsAsync(Arg.Any<List<int>>(), Arg.Any<CancellationToken>()).Returns(new List<Tag> { newTag });
        var handler = new UpdatePostsCommandHandler(unitOfWork);

        var response = await handler.Handle(new UpdatePostsCommand { Id = 5, UserId = 9, Content = "yeni", TagIds = new List<int> { 2 } }, CancellationToken.None);

        Assert.Equal("yeni", post.Content);
        Assert.Equal(new[] { newTag }, post.Tags);
        Assert.True(post.isPublished);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task UpdatePost_EmptyTagList_ShouldClearExistingTags()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var post = new Post { Id = 5, UserId = 9, Content = "eski" };
        post.Tags.Add(new Tag { Id = 1, Name = "eski-tag", NormalizedName = "ESKI-TAG" });
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns(post);
        unitOfWork.TagRepository.GetByIdsAsync(Arg.Any<List<int>>(), Arg.Any<CancellationToken>()).Returns(new List<Tag>());
        var handler = new UpdatePostsCommandHandler(unitOfWork);

        await handler.Handle(new UpdatePostsCommand { Id = 5, UserId = 9, Content = "yeni", TagIds = new List<int>() }, CancellationToken.None);

        Assert.Empty(post.Tags);
    }

    [Fact]
    public async Task UpdatePost_MissingPost_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns((Post?)null);
        var handler = new UpdatePostsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdatePostsCommand { Id = 5, UserId = 9, Content = "yeni" }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePost_ForeignPost_ShouldThrowUnauthorizedAndNotChangeContent()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var post = new Post { Id = 5, UserId = 11, Content = "eski" };
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns(post);
        var handler = new UpdatePostsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<UnauthorizedAccesException>(() => handler.Handle(new UpdatePostsCommand { Id = 5, UserId = 9, Content = "yeni" }, CancellationToken.None));

        Assert.Equal("eski", post.Content);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdatePost_UnknownTagId_ShouldThrowBadRequestBeforeTouchingPost()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var post = new Post { Id = 5, UserId = 9, Content = "eski" };
        post.Tags.Add(new Tag { Id = 1, Name = "eski-tag", NormalizedName = "ESKI-TAG" });
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns(post);
        unitOfWork.TagRepository.GetByIdsAsync(Arg.Any<List<int>>(), Arg.Any<CancellationToken>()).Returns(new List<Tag>());
        var handler = new UpdatePostsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new UpdatePostsCommand { Id = 5, UserId = 9, Content = "yeni", TagIds = new List<int> { 7 } }, CancellationToken.None));

        Assert.Equal("eski", post.Content);
        Assert.Single(post.Tags);
    }

    [Fact]
    public async Task DeletePost_ShouldSoftDeleteAsOwnerDeletion()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var post = new Post { Id = 5, UserId = 9, isPublished = true, isActive = true, Status = PostStatus.Published };
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns(post);
        var handler = new DeletePostsCommandHandler(unitOfWork, NullLogger<DeletePostsCommandHandler>.Instance);

        var response = await handler.Handle(new DeletePostsCommand { Id = 5, UserId = 9 }, CancellationToken.None);

        Assert.Equal(PostStatus.DeletedByOwner, post.Status);
        Assert.False(post.isPublished);
        Assert.False(post.isActive);
        Assert.True(post.isDeleted);
        unitOfWork.PostRepository.Received(1).Update(post);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DeletePost_MissingPost_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns((Post?)null);
        var handler = new DeletePostsCommandHandler(unitOfWork, NullLogger<DeletePostsCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeletePostsCommand { Id = 5, UserId = 9 }, CancellationToken.None));
    }

    [Fact]
    public async Task DeletePost_ForeignPost_ShouldThrowUnauthorized()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var post = new Post { Id = 5, UserId = 11, isPublished = true, Status = PostStatus.Published };
        unitOfWork.PostRepository.GetByIdWithTagsAsync(5).Returns(post);
        var handler = new DeletePostsCommandHandler(unitOfWork, NullLogger<DeletePostsCommandHandler>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccesException>(() => handler.Handle(new DeletePostsCommand { Id = 5, UserId = 9 }, CancellationToken.None));

        Assert.Equal(PostStatus.Published, post.Status);
    }

    [Fact]
    public async Task GetPostById_ShouldPassAuthenticatedViewerToRepository()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var dto = new PostDto { Id = 5 };
        unitOfWork.PostRepository.GetDtoByIdAsync(5, 42, Arg.Any<CancellationToken>()).Returns(dto);
        var handler = new GetPostByIdQueryHandler(unitOfWork, HandlerTestContext.CreateHttpContextAccessor(42));

        var result = await handler.Handle(new GetPostByIdQuery(5), CancellationToken.None);

        Assert.Same(dto, result);
    }

    [Fact]
    public async Task GetPostById_AnonymousViewer_ShouldPassNullViewer()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetDtoByIdAsync(5, null, Arg.Any<CancellationToken>()).Returns(new PostDto { Id = 5 });
        var handler = new GetPostByIdQueryHandler(unitOfWork, HandlerTestContext.CreateHttpContextAccessor(null));

        var result = await handler.Handle(new GetPostByIdQuery(5), CancellationToken.None);

        Assert.Equal(5, result.Id);
        await unitOfWork.PostRepository.Received(1).GetDtoByIdAsync(5, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPostById_MissingPost_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetDtoByIdAsync(Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns((PostDto?)null);
        var handler = new GetPostByIdQueryHandler(unitOfWork, HandlerTestContext.CreateHttpContextAccessor(null));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetPostByIdQuery(5), CancellationToken.None));
    }

    [Fact]
    public async Task GetAllPosts_ShouldForwardEveryFilterAndBuildPagedResponse()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var items = new List<PostDto> { new() { Id = 1 } };
        unitOfWork.PostRepository.GetPagedAsync(2, 25, 3, 4, "dotnet", "popular", 42, Arg.Any<CancellationToken>()).Returns((items, 51));
        var handler = new GetAllPostsQueryHandler(unitOfWork, HandlerTestContext.CreateHttpContextAccessor(42));

        var result = await handler.Handle(new GetAllPostsQuery { Page = 2, Size = 25, TagId = 3, UserId = 4, Search = "dotnet", SortBy = "popular" }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.Size);
        Assert.Equal(51, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetPostsByTagId_ShouldForwardViewerAndPaging()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var items = new List<PostDto> { new() { Id = 1 } };
        unitOfWork.PostRepository.GetPagedByTagIdAsync(3, 1, 20, 42, Arg.Any<CancellationToken>()).Returns((items, 1));
        var handler = new GetAllPostsByTagIdQueryHandler(unitOfWork, HandlerTestContext.CreateHttpContextAccessor(42));

        var result = await handler.Handle(new GetAllPostsByTagIdQuery { TagId = 3, Page = 1, Size = 20 }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPostsByUserId_ShouldForwardViewerSeparatelyFromTargetUser()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetPagedByUserIdAsync(7, 1, 20, 42, Arg.Any<CancellationToken>()).Returns((new List<PostDto>(), 0));
        var handler = new GetPostsByUserIdQueryHandler(unitOfWork, HandlerTestContext.CreateHttpContextAccessor(42));

        await handler.Handle(new GetPostsByUserIdQuery { UserId = 7, Page = 1, Size = 20 }, CancellationToken.None);

        await unitOfWork.PostRepository.Received(1).GetPagedByUserIdAsync(7, 1, 20, 42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyPosts_ShouldUseCurrentUserAsBothTargetAndViewer()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetPagedByUserIdAsync(9, 1, 20, 9, Arg.Any<CancellationToken>()).Returns((new List<PostDto>(), 0));
        var handler = new GetMyPostsQueryHandler(unitOfWork);

        await handler.Handle(new GetMyPostsQuery { UserId = 9, Page = 1, Size = 20 }, CancellationToken.None);

        await unitOfWork.PostRepository.Received(1).GetPagedByUserIdAsync(9, 1, 20, 9, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFollowingPosts_ShouldQueryCurrentUserFeed()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var items = new List<PostDto> { new() { Id = 3 } };
        unitOfWork.PostRepository.GetPagedFollowingAsync(9, 2, 10, Arg.Any<CancellationToken>()).Returns((items, 12));
        var handler = new GetFollowingPostsQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetFollowingPostsQuery { UserId = 9, Page = 2, Size = 10 }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetDailyTopPosts_ShouldAssignSequentialRanks()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetDailyTopPostsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 50, Arg.Any<CancellationToken>())
            .Returns(new List<TopPostDto> { new() { PostId = 1 }, new() { PostId = 2 }, new() { PostId = 3 } });
        var handler = new GetDailyTopPostsQueryHandler(unitOfWork, new PassThroughCacheService(), Options.Create(new CacheOptions()));

        var result = await handler.Handle(new GetDailyTopPostsQuery(), CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 3 }, result.Select(item => item.Rank));
    }

    [Fact]
    public async Task GetDailyTopPosts_ShouldUseDayScopedCacheKeyAndConfiguredTtl()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetDailyTopPostsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), 50, Arg.Any<CancellationToken>()).Returns(new List<TopPostDto>());
        var cache = new PassThroughCacheService();
        var handler = new GetDailyTopPostsQueryHandler(unitOfWork, cache, Options.Create(new CacheOptions { DailyTopPostsTtlSeconds = 42 }));

        await handler.Handle(new GetDailyTopPostsQuery(), CancellationToken.None);

        Assert.StartsWith("posts:daily-top:", cache.LastKey);
        Assert.EndsWith(":50", cache.LastKey);
        Assert.Equal(TimeSpan.FromSeconds(42), cache.LastTimeToLive);
    }

    [Fact]
    public async Task GetDailyTopPosts_CachedResult_ShouldNotHitDatabase()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var cached = new List<TopPostDto> { new() { PostId = 9, Rank = 1 } };
        var handler = new GetDailyTopPostsQueryHandler(unitOfWork, new StubCacheService(cached), Options.Create(new CacheOptions()));

        var result = await handler.Handle(new GetDailyTopPostsQuery(), CancellationToken.None);

        Assert.Same(cached, result);
        await unitOfWork.PostRepository.DidNotReceiveWithAnyArgs().GetDailyTopPostsAsync(default, default, default, default);
    }

    [Fact]
    public async Task GetDailyTopPosts_ShouldQueryExactlyOneDayWindow()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        DateTime start = default;
        DateTime end = default;
        unitOfWork.PostRepository
            .GetDailyTopPostsAsync(Arg.Do<DateTime>(value => start = value), Arg.Do<DateTime>(value => end = value), 50, Arg.Any<CancellationToken>())
            .Returns(new List<TopPostDto>());
        var handler = new GetDailyTopPostsQueryHandler(unitOfWork, new PassThroughCacheService(), Options.Create(new CacheOptions()));

        await handler.Handle(new GetDailyTopPostsQuery(), CancellationToken.None);

        Assert.Equal(TimeSpan.FromDays(1), end - start);
        Assert.True(start <= DateTime.UtcNow && DateTime.UtcNow < end, "Pencere su ani icermeliydi.");
    }

    /// <summary>Onbellegi devre disi birakip fabrikayi her seferinde calistirir.</summary>
    private sealed class PassThroughCacheService : ICacheService
    {
        public string LastKey { get; private set; } = string.Empty;
        public TimeSpan LastTimeToLive { get; private set; }

        public Task<T> GetOrSetAsync<T>(string key, TimeSpan timeToLive, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default) where T : class
        {
            LastKey = key;
            LastTimeToLive = timeToLive;
            return factory(cancellationToken);
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>Her zaman onbellekten servis eder; fabrika hic calismaz.</summary>
    private sealed class StubCacheService : ICacheService
    {
        private readonly object _value;

        public StubCacheService(object value) => _value = value;

        public Task<T> GetOrSetAsync<T>(string key, TimeSpan timeToLive, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default) where T : class =>
            Task.FromResult((T)_value);

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
