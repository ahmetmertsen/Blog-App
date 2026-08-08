using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using buduns_server.Persistence.Services;
using NSubstitute;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Bildirim uretimi iki sessiz kurala dayaniyor: kullanici kendi eylemi icin
/// bildirim almaz ve ayni bildirim cooldown suresi icinde tekrarlanmaz. Ikisi
/// de yalnizca servis icinde yasiyor.
/// </summary>
public class NotificationServiceTests
{
    [Fact]
    public async Task BuildAsync_SelfAction_ShouldReturnNull()
    {
        var repository = Substitute.For<INotificationRepository>();
        var service = new NotificationService(repository);

        var notification = await service.BuildAsync(new NotificationCreateModel { Type = NotificationType.POST_LIKED, UserId = 5, ActorUserId = 5, PostId = 1 }, CancellationToken.None);

        Assert.Null(notification);
        await repository.DidNotReceiveWithAnyArgs().ExistsRecentAsync(default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task BuildAsync_WithoutCooldown_ShouldNotQueryRecentNotifications()
    {
        var repository = Substitute.For<INotificationRepository>();
        var service = new NotificationService(repository);

        var notification = await service.BuildAsync(new NotificationCreateModel { Type = NotificationType.POST_COMMENTED, UserId = 5, ActorUserId = 6, PostId = 1 }, CancellationToken.None);

        Assert.NotNull(notification);
        await repository.DidNotReceiveWithAnyArgs().ExistsRecentAsync(default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task BuildAsync_WithinCooldown_ShouldReturnNull()
    {
        var repository = Substitute.For<INotificationRepository>();
        repository.ExistsRecentAsync(NotificationType.POST_LIKED, 5, 6, 1, null, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(true);
        var service = new NotificationService(repository);

        var notification = await service.BuildAsync(new NotificationCreateModel { Type = NotificationType.POST_LIKED, UserId = 5, ActorUserId = 6, PostId = 1, Cooldown = TimeSpan.FromHours(1) }, CancellationToken.None);

        Assert.Null(notification);
    }

    [Fact]
    public async Task BuildAsync_OutsideCooldown_ShouldCreateNotification()
    {
        var repository = Substitute.For<INotificationRepository>();
        repository.ExistsRecentAsync(Arg.Any<NotificationType>(), Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(false);
        var service = new NotificationService(repository);

        var notification = await service.BuildAsync(new NotificationCreateModel { Type = NotificationType.NEW_FOLLOWER, UserId = 5, ActorUserId = 6, Cooldown = TimeSpan.FromHours(24) }, CancellationToken.None);

        Assert.NotNull(notification);
        Assert.Equal(NotificationType.NEW_FOLLOWER, notification!.Type);
        Assert.Equal(5, notification.UserId);
        Assert.Equal(6, notification.ActorUserId);
        Assert.False(notification.IsRead);
        Assert.True(notification.isActive);
        Assert.False(notification.isDeleted);
    }

    [Fact]
    public async Task BuildAsync_ShouldUseExplicitMessageWhenProvided()
    {
        var service = new NotificationService(Substitute.For<INotificationRepository>());

        var notification = await service.BuildAsync(new NotificationCreateModel { Type = NotificationType.MODERATION_WARNING, UserId = 5, Message = "Ozel mesaj" }, CancellationToken.None);

        Assert.Equal("Ozel mesaj", notification!.Message);
    }

    [Theory]
    [InlineData(NotificationType.NEW_FOLLOWER)]
    [InlineData(NotificationType.POST_LIKED)]
    [InlineData(NotificationType.POST_COMMENTED)]
    [InlineData(NotificationType.REPORT_RESOLVED)]
    [InlineData(NotificationType.MODERATION_WARNING)]
    [InlineData(NotificationType.ACCOUNT_SUSPENDED)]
    [InlineData(NotificationType.ACCOUNT_BANNED)]
    [InlineData(NotificationType.POST_HIDDEN)]
    [InlineData(NotificationType.POST_REMOVED)]
    [InlineData(NotificationType.COMMENT_HIDDEN)]
    [InlineData(NotificationType.COMMENT_REMOVED)]
    public async Task BuildAsync_EveryTypeShouldHaveNonEmptyDefaultMessage(NotificationType type)
    {
        var service = new NotificationService(Substitute.For<INotificationRepository>());

        var notification = await service.BuildAsync(new NotificationCreateModel { Type = type, UserId = 5 }, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(notification!.Message));
    }

    [Fact]
    public async Task BuildAsync_ShouldCarryCommentReferenceForUnsavedComment()
    {
        var comment = new Comment { Content = "yorum", PostId = 1, UserId = 6 };
        var service = new NotificationService(Substitute.For<INotificationRepository>());

        var notification = await service.BuildAsync(new NotificationCreateModel { Type = NotificationType.POST_COMMENTED, UserId = 5, ActorUserId = 6, PostId = 1, Comment = comment }, CancellationToken.None);

        // Yorum henuz kaydedilmedigi icin Id yerine navigasyon tasiniyor.
        Assert.Same(comment, notification!.Comment);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistBuiltNotification()
    {
        var repository = Substitute.For<INotificationRepository>();
        var service = new NotificationService(repository);

        var notification = await service.AddAsync(new NotificationCreateModel { Type = NotificationType.POST_COMMENTED, UserId = 5, ActorUserId = 6, PostId = 1 }, CancellationToken.None);

        Assert.NotNull(notification);
        await repository.Received(1).AddAsync(notification!);
    }

    [Fact]
    public async Task AddAsync_SuppressedNotification_ShouldNotTouchRepository()
    {
        var repository = Substitute.For<INotificationRepository>();
        var service = new NotificationService(repository);

        var notification = await service.AddAsync(new NotificationCreateModel { Type = NotificationType.POST_LIKED, UserId = 5, ActorUserId = 5, PostId = 1 }, CancellationToken.None);

        Assert.Null(notification);
        await repository.DidNotReceiveWithAnyArgs().AddAsync(default!);
    }
}
