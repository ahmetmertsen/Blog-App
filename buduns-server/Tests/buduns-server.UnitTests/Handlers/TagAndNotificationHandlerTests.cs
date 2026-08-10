using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Notifications.Commands.Delete;
using buduns_server.Application.Features.Notifications.Commands.MarkAllAsRead;
using buduns_server.Application.Features.Notifications.Commands.MarkAsRead;
using buduns_server.Application.Features.Notifications.Queries.GetAllByUserId;
using buduns_server.Application.Features.Notifications.Queries.GetUnreadCount;
using buduns_server.Application.Features.Tags.Commands.Create;
using buduns_server.Application.Features.Tags.Commands.Delete;
using buduns_server.Application.Features.Tags.Commands.Update;
using buduns_server.Application.Features.Tags.Queries.GetAll;
using buduns_server.Application.Features.Tags.Queries.GetById;
using buduns_server.Domain.Entities;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

public class TagAndNotificationHandlerTests
{
    [Fact]
    public async Task CreateTag_ShouldNormalizeDisplayNameAndKey()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.TagRepository.ExistsByNormalizedNameAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);
        Tag? persisted = null;
        await unitOfWork.TagRepository.AddAsync(Arg.Do<Tag>(tag => persisted = tag));
        var handler = new CreateTagsCommandHandler(unitOfWork);

        var response = await handler.Handle(new CreateTagsCommand("  dotnet   core  "), CancellationToken.None);

        Assert.Equal("dotnet core", persisted!.Name);
        Assert.Equal("DOTNET CORE", persisted.NormalizedName);
        Assert.True(persisted.isActive);
        Assert.False(persisted.isDeleted);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CreateTag_ExistingNormalizedName_ShouldThrowBadRequest()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.TagRepository.ExistsByNormalizedNameAsync("DOTNET", Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateTagsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateTagsCommand("DotNet"), CancellationToken.None));

        await unitOfWork.TagRepository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }

    [Fact]
    public async Task UpdateTag_ShouldExcludeItselfFromDuplicateCheck()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var tag = new Tag { Id = 4, Name = "eski", NormalizedName = "ESKI", isActive = true };
        unitOfWork.TagRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns(tag);
        unitOfWork.TagRepository.ExistsByNormalizedNameAsync("YENI", 4, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateTagsCommandHandler(unitOfWork);

        var response = await handler.Handle(new UpdateTagsCommand(4, " yeni "), CancellationToken.None);

        Assert.Equal("yeni", tag.Name);
        Assert.Equal("YENI", tag.NormalizedName);
        await unitOfWork.TagRepository.Received(1).ExistsByNormalizedNameAsync("YENI", 4, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTag_MissingTag_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.TagRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns((Tag?)null);
        var handler = new UpdateTagsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateTagsCommand(4, "yeni"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTag_DuplicateName_ShouldThrowBadRequestAndKeepOldName()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var tag = new Tag { Id = 4, Name = "eski", NormalizedName = "ESKI" };
        unitOfWork.TagRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns(tag);
        unitOfWork.TagRepository.ExistsByNormalizedNameAsync("YENI", 4, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateTagsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new UpdateTagsCommand(4, "yeni"), CancellationToken.None));

        Assert.Equal("eski", tag.Name);
    }

    [Fact]
    public async Task DeleteTag_ShouldSoftDelete()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var tag = new Tag { Id = 4, Name = "dotnet", NormalizedName = "DOTNET", isActive = true, isDeleted = false };
        unitOfWork.TagRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns(tag);
        var handler = new DeleteTagsCommandHandler(unitOfWork);

        var response = await handler.Handle(new DeleteTagsCommand(4), CancellationToken.None);

        Assert.False(tag.isActive);
        Assert.True(tag.isDeleted);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DeleteTag_MissingTag_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.TagRepository.GetVisibleByIdAsync(4, Arg.Any<CancellationToken>()).Returns((Tag?)null);
        var handler = new DeleteTagsCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteTagsCommand(4), CancellationToken.None));
    }

    [Fact]
    public async Task GetTagById_MissingTag_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.TagRepository.GetDtoByIdAsync(4, Arg.Any<CancellationToken>()).Returns((TagDto?)null);
        var handler = new GetTagByIdQueryHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetTagByIdQuery(4), CancellationToken.None));
    }

    [Fact]
    public async Task GetAllTags_ShouldForwardSearchAndBuildPagedResponse()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var items = new List<TagDto> { new() { Id = 1, Name = "dotnet" } };
        unitOfWork.TagRepository.GetPagedAsync(1, 50, "dot", Arg.Any<CancellationToken>()).Returns((items, 1));
        var handler = new GetAllTagsQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetAllTagsQuery { Page = 1, Size = 50, Search = "dot" }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task MarkNotificationAsRead_ShouldSaveWhenFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.NotificationRepository.MarkAsReadAsync(5, 9, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new MarkNotificationAsReadCommandHandler(unitOfWork);

        var response = await handler.Handle(new MarkNotificationAsReadCommand { Id = 5, UserId = 9 }, CancellationToken.None);

        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task MarkNotificationAsRead_ForeignOrMissingNotification_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.NotificationRepository.MarkAsReadAsync(5, 9, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new MarkNotificationAsReadCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new MarkNotificationAsReadCommand { Id = 5, UserId = 9 }, CancellationToken.None));

        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_ShouldReturnUpdatedCount()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.NotificationRepository.MarkAllAsReadAsync(9, Arg.Any<CancellationToken>()).Returns(4);
        var handler = new MarkAllNotificationsAsReadCommandHandler(unitOfWork);

        var response = await handler.Handle(new MarkAllNotificationsAsReadCommand { UserId = 9 }, CancellationToken.None);

        Assert.Equal(4, response.UpdatedCount);
        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_NothingToUpdate_ShouldSkipSave()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.NotificationRepository.MarkAllAsReadAsync(9, Arg.Any<CancellationToken>()).Returns(0);
        var handler = new MarkAllNotificationsAsReadCommandHandler(unitOfWork);

        var response = await handler.Handle(new MarkAllNotificationsAsReadCommand { UserId = 9 }, CancellationToken.None);

        Assert.Equal(0, response.UpdatedCount);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task DeleteNotification_ShouldSoftDeleteWhenOwned()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.NotificationRepository.SoftDeleteByIdAndUserAsync(5, 9, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteNotificationCommandHandler(unitOfWork);

        var response = await handler.Handle(new DeleteNotificationCommand { Id = 5, UserId = 9 }, CancellationToken.None);

        await unitOfWork.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DeleteNotification_ForeignNotification_ShouldThrowNotFound()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.NotificationRepository.SoftDeleteByIdAndUserAsync(5, 9, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteNotificationCommandHandler(unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteNotificationCommand { Id = 5, UserId = 9 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetNotifications_ShouldForwardUnreadFilterAndBuildPagedResponse()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        var items = new List<NotificationDto> { new() { Id = 1, Message = "mesaj" } };
        unitOfWork.NotificationRepository.GetPagedByUserIdAsync(9, 2, 10, true, Arg.Any<CancellationToken>()).Returns((items, 11));
        var handler = new GetAllNotificationsByUserIdQueryHandler(unitOfWork);

        var result = await handler.Handle(new GetAllNotificationsByUserIdQuery { UserId = 9, Page = 2, Size = 10, OnlyUnread = true }, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetUnreadNotificationCount_ShouldReturnRepositoryValue()
    {
        var unitOfWork = HandlerTestContext.CreateUnitOfWork();
        unitOfWork.NotificationRepository.GetUnreadCountAsync(9, Arg.Any<CancellationToken>()).Returns(7);
        var handler = new GetUnreadNotificationCountQueryHandler(unitOfWork);

        var response = await handler.Handle(new GetUnreadNotificationCountQuery { UserId = 9 }, CancellationToken.None);

        Assert.Equal(7, response.UnreadCount);
    }
}
