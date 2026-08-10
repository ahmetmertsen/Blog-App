using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Comments.Commands.Create;
using buduns_server.Application.Features.Comments.Commands.Delete;
using buduns_server.Application.Features.Comments.Commands.Update;
using buduns_server.Application.Features.Comments.Queries.GetById;
using buduns_server.Application.Features.Comments.Queries.GetByPostId;
using buduns_server.Application.Features.Comments.Queries.GetByUserId;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

public class CommentHandlerTests
{
    [Fact]
    public async Task CreateComment_ShouldTrimContentPersistAndNotifyPostOwner()
    {
        var unitOfWork = CreateUnitOfWorkForCreate(postOwnerId: 3);
        var notificationService = Substitute.For<INotificationService>();
        Comment? persisted = null;
        await unitOfWork.CommentRepository.AddAsync(Arg.Do<Comment>(comment => persisted = comment));
        unitOfWork.CommentRepository.GetDtoByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new CommentDto { Content = "yorum" });
        var handler = new CreateCommentsCommandHandler(unitOfWork, notificationService);

        var response = await handler.Handle(new CreateCommentsCommand { PostId = 7, UserId = 9, Content = "  yorum  " }, CancellationToken.None);

        Assert.Equal("yorum", persisted!.Content);
        Assert.Equal(7, persisted.PostId);
        Assert.Equal(9, persisted.UserId);
        Assert.Equal(CommentStatus.Published, persisted.Status);
        await notificationService.Received(1).AddAsync(Arg.Is<NotificationCreateModel>(model =>
            model.Type == NotificationType.POST_COMMENTED && model.UserId == 3 && model.ActorUserId == 9 && model.PostId == 7), CancellationToken.None);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CreateComment_MissingOrInvisiblePost_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetVisibleOwnerIdAsync(7, Arg.Any<CancellationToken>()).Returns((int?)null);
        var handler = new CreateCommentsCommandHandler(unitOfWork, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateCommentsCommand { PostId = 7, UserId = 9, Content = "yorum" }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_AtPerMinuteLimit_ShouldThrowTooManyRequests()
    {
        var unitOfWork = CreateUnitOfWorkForCreate(postOwnerId: 3);
        unitOfWork.CommentRepository.CountRecentByUserAsync(9, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(10);
        var handler = new CreateCommentsCommandHandler(unitOfWork, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<TooManyRequestsException>(() => handler.Handle(new CreateCommentsCommand { PostId = 7, UserId = 9, Content = "yorum" }, CancellationToken.None));

        await unitOfWork.CommentRepository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Fact]
    public async Task CreateComment_JustBelowPerMinuteLimit_ShouldSucceed()
    {
        var unitOfWork = CreateUnitOfWorkForCreate(postOwnerId: 3);
        unitOfWork.CommentRepository.CountRecentByUserAsync(9, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(9);
        unitOfWork.CommentRepository.GetDtoByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new CommentDto { Content = "yorum" });
        var handler = new CreateCommentsCommandHandler(unitOfWork, Substitute.For<INotificationService>());

        var response = await handler.Handle(new CreateCommentsCommand { PostId = 7, UserId = 9, Content = "yorum" }, CancellationToken.None);

    }

    [Fact]
    public async Task CreateComment_RecentDuplicate_ShouldThrowBadRequest()
    {
        var unitOfWork = CreateUnitOfWorkForCreate(postOwnerId: 3);
        unitOfWork.CommentRepository.HasRecentDuplicateAsync(9, 7, "yorum", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateCommentsCommandHandler(unitOfWork, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateCommentsCommand { PostId = 7, UserId = 9, Content = "  yorum  " }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateComment_MissingDtoAfterSave_ShouldThrowNotFound()
    {
        var unitOfWork = CreateUnitOfWorkForCreate(postOwnerId: 3);
        unitOfWork.CommentRepository.GetDtoByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((CommentDto?)null);
        var handler = new CreateCommentsCommandHandler(unitOfWork, Substitute.For<INotificationService>());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateCommentsCommand { PostId = 7, UserId = 9, Content = "yorum" }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_ShouldTrimContentAndStampUpdateDate()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        unitOfWork.CommentRepository.GetDtoByIdAsync(3, Arg.Any<CancellationToken>()).Returns(new CommentDto { Content = "guncel" });
        var handler = new UpdateCommentsCommandHandler(unitOfWork);

        var response = await handler.Handle(new UpdateCommentsCommand { Id = 3, UserId = 9, Content = "  guncel  " }, CancellationToken.None);

        Assert.Equal("guncel", comment.Content);
        Assert.NotEqual(default, comment.UpdateAt);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task UpdateComment_MissingComment_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns((Comment?)null);
        var handler = new UpdateCommentsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateCommentsCommand { Id = 3, UserId = 9, Content = "guncel" }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_ForeignComment_ShouldThrowForbidden()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        comment.UserId = 11;
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new UpdateCommentsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UpdateCommentsCommand { Id = 3, UserId = 9, Content = "guncel" }, CancellationToken.None));
    }

    [Theory]
    [InlineData(CommentStatus.DeletedByOwner)]
    [InlineData(CommentStatus.HiddenByModerator)]
    [InlineData(CommentStatus.DeletedByModerator)]
    public async Task UpdateComment_NonPublishedComment_ShouldThrowBadRequest(CommentStatus status)
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        comment.Status = status;
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new UpdateCommentsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new UpdateCommentsCommand { Id = 3, UserId = 9, Content = "guncel" }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateComment_OnHiddenPost_ShouldThrowBadRequest()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        comment.Post.Status = PostStatus.HiddenByModerator;
        comment.Post.isPublished = false;
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new UpdateCommentsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new UpdateCommentsCommand { Id = 3, UserId = 9, Content = "guncel" }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_ShouldSoftDeleteAsOwnerDeletion()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new DeleteCommentsCommandHandler(unitOfWork, NullLogger<DeleteCommentsCommandHandler>.Instance);

        var response = await handler.Handle(new DeleteCommentsCommand { Id = 3, UserId = 9 }, CancellationToken.None);

        Assert.Equal(CommentStatus.DeletedByOwner, comment.Status);
        Assert.False(comment.isActive);
        Assert.True(comment.isDeleted);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DeleteComment_AlreadyDeletedByOwner_ShouldBeIdempotent()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        comment.Status = CommentStatus.DeletedByOwner;
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new DeleteCommentsCommandHandler(unitOfWork, NullLogger<DeleteCommentsCommandHandler>.Instance);

        var response = await handler.Handle(new DeleteCommentsCommand { Id = 3, UserId = 9 }, CancellationToken.None);

        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData(CommentStatus.HiddenByModerator)]
    [InlineData(CommentStatus.DeletedByModerator)]
    public async Task DeleteComment_ModeratedComment_ShouldThrowBadRequest(CommentStatus status)
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        comment.Status = status;
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new DeleteCommentsCommandHandler(unitOfWork, NullLogger<DeleteCommentsCommandHandler>.Instance);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new DeleteCommentsCommand { Id = 3, UserId = 9 }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteComment_ForeignComment_ShouldThrowForbidden()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var comment = CreateVisibleComment();
        comment.UserId = 11;
        unitOfWork.CommentRepository.GetForMutationAsync(3, Arg.Any<CancellationToken>()).Returns(comment);
        var handler = new DeleteCommentsCommandHandler(unitOfWork, NullLogger<DeleteCommentsCommandHandler>.Instance);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new DeleteCommentsCommand { Id = 3, UserId = 9 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetCommentById_MissingComment_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.CommentRepository.GetDtoByIdAsync(3, Arg.Any<CancellationToken>()).Returns((CommentDto?)null);
        var handler = new GetCommentByIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetCommentByIdQuery(3), CancellationToken.None));
    }

    [Fact]
    public async Task GetCommentsByPostId_InvisiblePost_ShouldThrowNotFoundBeforeQueryingComments()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.ExistsVisibleAsync(7, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCommentsByPostIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetCommentsByPostIdQuery { PostId = 7 }, CancellationToken.None));

        await unitOfWork.CommentRepository.DidNotReceiveWithAnyArgs().GetPagedByPostIdAsync(default, default, default, default);
    }

    [Fact]
    public async Task GetCommentsByPostId_ShouldBuildPagedResponse()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.ExistsVisibleAsync(7, Arg.Any<CancellationToken>()).Returns(true);
        var items = new List<CommentDto> { new() { Content = "yorum" } };
        unitOfWork.CommentRepository.GetPagedByPostIdAsync(7, 2, 10, Arg.Any<CancellationToken>()).Returns((items, 15));
        var handler = new GetCommentsByPostIdQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetCommentsByPostIdQuery { PostId = 7, Page = 2, Size = 10 }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetCommentsByUserId_ShouldBuildPagedResponse()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var items = new List<CommentDto> { new() { Content = "yorum" } };
        unitOfWork.CommentRepository.GetPagedByUserIdAsync(9, 1, 20, Arg.Any<CancellationToken>()).Returns((items, 1));
        var handler = new GetCommentsByUserIdQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetCommentsByUserIdQuery { UserId = 9, Page = 1, Size = 20 }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    private static IUnitOfWork CreateUnitOfWorkForCreate(int postOwnerId)
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.PostRepository.GetVisibleOwnerIdAsync(7, Arg.Any<CancellationToken>()).Returns(postOwnerId);
        unitOfWork.CommentRepository.CountRecentByUserAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(0);
        unitOfWork.CommentRepository.HasRecentDuplicateAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(false);
        unitOfWork.CommentRepository.GetDtoByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new CommentDto { Content = "yorum" });
        return unitOfWork;
    }

    private static Comment CreateVisibleComment() => new()
    {
        Id = 3,
        UserId = 9,
        PostId = 7,
        Content = "eski",
        Status = CommentStatus.Published,
        isActive = true,
        isDeleted = false,
        Post = new Post { Id = 7, UserId = 3, Status = PostStatus.Published, isPublished = true, isActive = true, isDeleted = false }
    };
}
