using buduns_server.Application.Repositories;
using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Bookmarks.Commands.Create;
using buduns_server.Application.Features.Bookmarks.Commands.Delete;
using buduns_server.Application.Features.Bookmarks.Queries.GetBookmarks;
using buduns_server.Application.Features.Bookmarks.Queries.GetStatus;
using buduns_server.Application.Features.Followers.Commands.Create;
using buduns_server.Application.Features.Followers.Commands.Delete;
using buduns_server.Application.Features.Followers.Queries.GetAllByUserId;
using buduns_server.Application.Features.Followers.Queries.GetById;
using buduns_server.Application.Features.Followers.Queries.GetStatus;
using buduns_server.Application.Features.Likes.Commands.Create;
using buduns_server.Application.Features.Likes.Commands.Delete;
using buduns_server.Application.Features.Likes.Queries.GetById;
using buduns_server.Application.Features.Likes.Queries.GetByPostId;
using buduns_server.Application.Features.Likes.Queries.GetMyLikes;
using buduns_server.Application.Features.Likes.Queries.GetStatus;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

/// <summary>
/// Begeni, yer isareti ve takip uclari "zaten var" durumunda hata degil
/// bilgilendirici cevap donuyor. Bu davranis idempotent istemci tarafinin
/// dayandigi sozlesme.
/// </summary>
public class InteractionHandlerTests
{
    [Fact]
    public async Task CreateLike_NewLike_ShouldReportCreatedAndBuildCooldownNotification()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.GetVisibleOwnerIdAsync(7, Arg.Any<CancellationToken>()).Returns(3);
        var like = new Like { Id = 15, PostId = 7, UserId = 9 };
        likeRepository.CreateIfNotExistsAsync(Arg.Any<Like>(), Arg.Any<Notification?>(), Arg.Any<CancellationToken>()).Returns((like, true));
        var notificationService = Substitute.For<INotificationService>();
        var handler = new CreateLikesCommandHandler(likeRepository, postRepository, notificationService);

        var response = await handler.Handle(new CreateLikesCommand { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.False(response.AlreadyLiked);
        Assert.Equal(15, response.LikeId);
        await notificationService.Received(1).BuildAsync(Arg.Is<NotificationCreateModel>(model =>
            model.Type == NotificationType.POST_LIKED && model.UserId == 3 && model.ActorUserId == 9 && model.PostId == 7 && model.Cooldown == TimeSpan.FromHours(1)), CancellationToken.None);
    }

    [Fact]
    public async Task CreateLike_ExistingLike_ShouldReportAlreadyLikedWithoutFailing()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.GetVisibleOwnerIdAsync(7, Arg.Any<CancellationToken>()).Returns(3);
        likeRepository.CreateIfNotExistsAsync(Arg.Any<Like>(), Arg.Any<Notification?>(), Arg.Any<CancellationToken>()).Returns((new Like { Id = 15 }, false));
        var handler = new CreateLikesCommandHandler(likeRepository, postRepository, Substitute.For<INotificationService>());

        var response = await handler.Handle(new CreateLikesCommand { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.True(response.AlreadyLiked);
    }

    [Fact]
    public async Task CreateLike_InvisiblePost_ShouldThrowNotFound()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.GetVisibleOwnerIdAsync(7, Arg.Any<CancellationToken>()).Returns((int?)null);
        var handler = new CreateLikesCommandHandler(likeRepository, postRepository, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateLikesCommand { PostId = 7, UserId = 9 }, CancellationToken.None));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteLike_ShouldSucceedRegardlessOfExistingLike(bool deleted)
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        likeRepository.DeleteByUserAndPostAsync(9, 7, Arg.Any<CancellationToken>()).Returns(deleted);
        var handler = new DeleteLikesCommandHandler(likeRepository);

        var response = await handler.Handle(new DeleteLikesCommand { PostId = 7, UserId = 9 }, CancellationToken.None);

    }

    [Fact]
    public async Task GetLikeStatus_InvisiblePost_ShouldThrowNotFound()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.ExistsVisibleAsync(7, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetLikeStatusQueryHandler(likeRepository, postRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetLikeStatusQuery { PostId = 7, UserId = 9 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetLikeStatus_ExistingLike_ShouldReturnLikeId()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.ExistsVisibleAsync(7, Arg.Any<CancellationToken>()).Returns(true);
        likeRepository.GetByUserAndPostAsync(9, 7, Arg.Any<CancellationToken>()).Returns(new Like { Id = 15 });
        var handler = new GetLikeStatusQueryHandler(likeRepository, postRepository);

        var response = await handler.Handle(new GetLikeStatusQuery { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.True(response.IsLiked);
        Assert.Equal(15, response.LikeId);
    }

    [Fact]
    public async Task GetLikeStatus_MissingLike_ShouldReturnNegativeStatus()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.ExistsVisibleAsync(7, Arg.Any<CancellationToken>()).Returns(true);
        likeRepository.GetByUserAndPostAsync(9, 7, Arg.Any<CancellationToken>()).Returns((Like?)null);
        var handler = new GetLikeStatusQueryHandler(likeRepository, postRepository);

        var response = await handler.Handle(new GetLikeStatusQuery { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.False(response.IsLiked);
        Assert.Null(response.LikeId);
    }

    [Fact]
    public async Task GetLikesByPostId_InvisiblePost_ShouldThrowNotFound()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.ExistsVisibleAsync(7, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetLikesByPostIdQueryHandler(likeRepository, postRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetLikesByPostIdQuery { PostId = 7 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetMyLikedPosts_ShouldBuildPagedResponse()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        var items = new List<LikedPostDto> { new() { LikeId = 1 } };
        likeRepository.GetPagedByUserIdAsync(9, 2, 10, Arg.Any<CancellationToken>()).Returns((items, 11));
        var handler = new GetMyLikedPostsQueryHandler(likeRepository);

        var result = await handler.Handle(new GetMyLikedPostsQuery { UserId = 9, Page = 2, Size = 10 }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetLikeById_MissingLike_ShouldThrowNotFound()
    {
        var likeRepository = Substitute.For<ILikeRepository>();
        likeRepository.GetByIdAsync(15).Returns((Like?)null);
        var handler = new GetLikeByIdQueryHandler(likeRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetLikeByIdQuery(15), CancellationToken.None));
    }

    [Fact]
    public async Task CreateBookmark_NewBookmark_ShouldReportCreated()
    {
        var bookmarkRepository = Substitute.For<IBookmarkRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.GetByIdAsync(7).Returns(new Post { Id = 7 });
        bookmarkRepository.CreateIfNotExistsAsync(Arg.Any<Bookmark>(), Arg.Any<CancellationToken>()).Returns((new Bookmark { Id = 22 }, true));
        var handler = new CreateBookmarksCommandHandler(bookmarkRepository, postRepository);

        var response = await handler.Handle(new CreateBookmarksCommand { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.False(response.AlreadyBookmarked);
        Assert.Equal(22, response.BookmarkId);
    }

    [Fact]
    public async Task CreateBookmark_ExistingBookmark_ShouldReportAlreadyBookmarked()
    {
        var bookmarkRepository = Substitute.For<IBookmarkRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.GetByIdAsync(7).Returns(new Post { Id = 7 });
        bookmarkRepository.CreateIfNotExistsAsync(Arg.Any<Bookmark>(), Arg.Any<CancellationToken>()).Returns((new Bookmark { Id = 22 }, false));
        var handler = new CreateBookmarksCommandHandler(bookmarkRepository, postRepository);

        var response = await handler.Handle(new CreateBookmarksCommand { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.True(response.AlreadyBookmarked);
    }

    [Fact]
    public async Task CreateBookmark_MissingPost_ShouldThrowNotFound()
    {
        var bookmarkRepository = Substitute.For<IBookmarkRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.GetByIdAsync(7).Returns((Post?)null);
        var handler = new CreateBookmarksCommandHandler(bookmarkRepository, postRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateBookmarksCommand { PostId = 7, UserId = 9 }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateBookmark_ShouldStampOwnershipAndActiveFlags()
    {
        var bookmarkRepository = Substitute.For<IBookmarkRepository>();
        var postRepository = Substitute.For<IPostRepository>();
        postRepository.GetByIdAsync(7).Returns(new Post { Id = 7 });
        Bookmark? candidate = null;
        bookmarkRepository.CreateIfNotExistsAsync(Arg.Do<Bookmark>(bookmark => candidate = bookmark), Arg.Any<CancellationToken>()).Returns((new Bookmark { Id = 22 }, true));
        var handler = new CreateBookmarksCommandHandler(bookmarkRepository, postRepository);

        await handler.Handle(new CreateBookmarksCommand { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.Equal(7, candidate!.PostId);
        Assert.Equal(9, candidate.UserId);
        Assert.True(candidate.isActive);
        Assert.False(candidate.isDeleted);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteBookmark_ShouldSucceedRegardlessOfExistingBookmark(bool deleted)
    {
        var bookmarkRepository = Substitute.For<IBookmarkRepository>();
        bookmarkRepository.DeleteByUserAndPostAsync(9, 7, Arg.Any<CancellationToken>()).Returns(deleted);
        var handler = new DeleteBookmarksCommandHandler(bookmarkRepository);

        var response = await handler.Handle(new DeleteBookmarksCommand { PostId = 7, UserId = 9 }, CancellationToken.None);

    }

    [Fact]
    public async Task GetBookmarkStatus_ShouldReflectRepositoryResult()
    {
        var bookmarkRepository = Substitute.For<IBookmarkRepository>();
        bookmarkRepository.GetByUserAndPostAsync(9, 7, Arg.Any<CancellationToken>()).Returns(new Bookmark { Id = 22 });
        var handler = new GetBookmarkStatusQueryHandler(bookmarkRepository);

        var response = await handler.Handle(new GetBookmarkStatusQuery { PostId = 7, UserId = 9 }, CancellationToken.None);

        Assert.True(response.IsBookmarked);
        Assert.Equal(22, response.BookmarkId);
    }

    [Fact]
    public async Task GetBookmarks_ShouldBuildPagedResponse()
    {
        var bookmarkRepository = Substitute.For<IBookmarkRepository>();
        var items = new List<BookmarkDto> { new() { Id = 1, PostId = 7, Post = new PostDto() } };
        bookmarkRepository.GetPagedByUserIdAsync(9, 1, 20, Arg.Any<CancellationToken>()).Returns((items, 1));
        var handler = new GetBookmarksQueryHandler(bookmarkRepository);

        var result = await handler.Handle(new GetBookmarksQuery { UserId = 9, Page = 1, Size = 20 }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task CreateFollower_SelfFollow_ShouldThrowBadRequestBeforeLookup()
    {
        var followerRepository = Substitute.For<IFollowerRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var handler = new CreateFollowersCommandHandler(followerRepository, userRepository, NullLogger<CreateFollowersCommandHandler>.Instance, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateFollowersCommand { UserId = 9, FollowingId = 9 }, CancellationToken.None));

        await userRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task CreateFollower_MissingTargetUser_ShouldThrowNotFound()
    {
        var followerRepository = Substitute.For<IFollowerRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new CreateFollowersCommandHandler(followerRepository, userRepository, NullLogger<CreateFollowersCommandHandler>.Instance, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateFollowersCommand { UserId = 9, FollowingId = 3 }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFollower_BannedTargetUser_ShouldThrowBadRequest()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var followerRepository = Substitute.For<IFollowerRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        HandlerTestContext.RegisterUsers(userRepository, HandlerTestContext.CreateUser(3, "yasakli", UserStatus.Banned));
        var handler = new CreateFollowersCommandHandler(followerRepository, userRepository, NullLogger<CreateFollowersCommandHandler>.Instance, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateFollowersCommand { UserId = 9, FollowingId = 3 }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFollower_NewFollow_ShouldBuildDailyCooldownNotification()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var userRepository = Substitute.For<IUserRepository>();
        var followerRepository = Substitute.For<IFollowerRepository>();
        followerRepository.CreateIfNotExistsAsync(Arg.Any<Follower>(), Arg.Any<Notification?>(), Arg.Any<CancellationToken>()).Returns((new Follower { Id = 33 }, true));
        HandlerTestContext.RegisterUsers(userRepository, HandlerTestContext.CreateUser(3));
        var notificationService = Substitute.For<INotificationService>();
        var handler = new CreateFollowersCommandHandler(followerRepository, userRepository, NullLogger<CreateFollowersCommandHandler>.Instance, notificationService);

        var response = await handler.Handle(new CreateFollowersCommand { UserId = 9, FollowingId = 3 }, CancellationToken.None);

        Assert.False(response.AlreadyFollowing);
        Assert.Equal(33, response.FollowId);
        await notificationService.Received(1).BuildAsync(Arg.Is<NotificationCreateModel>(model =>
            model.Type == NotificationType.NEW_FOLLOWER && model.UserId == 3 && model.ActorUserId == 9 && model.Cooldown == TimeSpan.FromHours(24)), CancellationToken.None);
    }

    [Fact]
    public async Task CreateFollower_ExistingFollow_ShouldReportAlreadyFollowing()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var userRepository = Substitute.For<IUserRepository>();
        var followerRepository = Substitute.For<IFollowerRepository>();
        followerRepository.CreateIfNotExistsAsync(Arg.Any<Follower>(), Arg.Any<Notification?>(), Arg.Any<CancellationToken>()).Returns((new Follower { Id = 33 }, false));
        HandlerTestContext.RegisterUsers(userRepository, HandlerTestContext.CreateUser(3));
        var handler = new CreateFollowersCommandHandler(followerRepository, userRepository, NullLogger<CreateFollowersCommandHandler>.Instance, Substitute.For<INotificationService>());

        var response = await handler.Handle(new CreateFollowersCommand { UserId = 9, FollowingId = 3 }, CancellationToken.None);

        Assert.True(response.AlreadyFollowing);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteFollower_ShouldSucceedRegardlessOfExistingFollow(bool deleted)
    {
        var followerRepository = Substitute.For<IFollowerRepository>();
        followerRepository.DeleteByUsersAsync(9, 3, Arg.Any<CancellationToken>()).Returns(deleted);
        var handler = new DeleteFollowersCommandHandler(followerRepository, NullLogger<DeleteFollowersCommandHandler>.Instance);

        var response = await handler.Handle(new DeleteFollowersCommand { UserId = 9, FollowingId = 3 }, CancellationToken.None);

    }

    [Fact]
    public async Task GetFollowerStatus_BannedTargetUser_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var followerRepository = Substitute.For<IFollowerRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        HandlerTestContext.RegisterUsers(userRepository, HandlerTestContext.CreateUser(3, "yasakli", UserStatus.Banned));
        var handler = new GetFollowerStatusQueryHandler(followerRepository, userRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetFollowerStatusQuery { UserId = 9, FollowingId = 3 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetFollowerStatus_ExistingFollow_ShouldReturnFollowId()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var userRepository = Substitute.For<IUserRepository>();
        var followerRepository = Substitute.For<IFollowerRepository>();
        followerRepository.GetByUsersAsync(9, 3, Arg.Any<CancellationToken>()).Returns(new Follower { Id = 33 });
        HandlerTestContext.RegisterUsers(userRepository, HandlerTestContext.CreateUser(3));
        var handler = new GetFollowerStatusQueryHandler(followerRepository, userRepository);

        var response = await handler.Handle(new GetFollowerStatusQuery { UserId = 9, FollowingId = 3 }, CancellationToken.None);

        Assert.True(response.IsFollowing);
        Assert.Equal(33, response.FollowId);
    }

    [Fact]
    public async Task GetFollowersByUserId_BannedUser_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var followerRepository = Substitute.For<IFollowerRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        HandlerTestContext.RegisterUsers(userRepository, HandlerTestContext.CreateUser(3, "yasakli", UserStatus.Banned));
        var handler = new GetAllFollowersByUserIdQueryHandler(followerRepository, userRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetAllFollowersByUserIdQuery { UserId = 3 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetFollowersByUserId_ShouldBuildPagedResponse()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var userRepository = Substitute.For<IUserRepository>();
        var followerRepository = Substitute.For<IFollowerRepository>();
        var items = new List<FollowerDto> { new() { Id = 1, UserId = 9 } };
        followerRepository.GetPagedFollowersByUserIdAsync(3, 1, 20, Arg.Any<CancellationToken>()).Returns((items, 1));
        HandlerTestContext.RegisterUsers(userRepository, HandlerTestContext.CreateUser(3));
        var handler = new GetAllFollowersByUserIdQueryHandler(followerRepository, userRepository);

        var result = await handler.Handle(new GetAllFollowersByUserIdQuery { UserId = 3, Page = 1, Size = 20 }, CancellationToken.None);

        Assert.Same(items, result.Items);
    }

    [Fact]
    public async Task GetFollowingsByUserId_MissingUser_ShouldThrowNotFound()
    {
        var followerRepository = Substitute.For<IFollowerRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new GetAllFollowingsByUserIdQueryHandler(followerRepository, userRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetAllFollowingsByUserIdQuery { UserId = 3 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetFollowerById_MissingFollow_ShouldThrowNotFound()
    {
        var followerRepository = Substitute.For<IFollowerRepository>();
        followerRepository.GetByIdAsync(33).Returns((Follower?)null);
        var handler = new GetFollowerByIdQueryHandler(followerRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetFollowerByIdQuery(33), CancellationToken.None));
    }

    [Fact]
    public async Task GetFollowerById_ShouldMapFollowingUserAsSubject()
    {
        var followerRepository = Substitute.For<IFollowerRepository>();
        followerRepository.GetByIdAsync(33).Returns(new Follower { Id = 33, FollowerId = 9, FollowingId = 3 });
        var handler = new GetFollowerByIdQueryHandler(followerRepository);

        var dto = await handler.Handle(new GetFollowerByIdQuery(33), CancellationToken.None);

        Assert.Equal(3, dto.UserId);
    }
}
