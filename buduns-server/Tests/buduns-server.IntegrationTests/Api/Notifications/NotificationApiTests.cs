using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Notifications.Commands.MarkAllAsRead;
using buduns_server.Application.Features.Notifications.Queries.GetUnreadCount;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Notifications;

/// <summary>
/// Bildirim uclari yalnizca istegi yapan kullanicinin kayitlarina dokunmali.
/// Sahiplik kontrolu repository sorgusunun icinde oldugu icin, yanlis bir
/// filtre baska kullanicinin bildirimini okutabilir ya da sildirebilir.
/// </summary>
public sealed class NotificationApiTests : IntegrationTestBase
{
    public NotificationApiTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Notification_listing_should_support_unread_filter_and_paging()
    {
        var owner = await CreateUserAsync("notification-owner");
        await SeedNotificationsAsync(owner.Id, unread: 2, read: 1);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        var all = await authentication.Client.GetDataAsync<PagedResponse<NotificationDto>>("/api/Notification?page=1&size=20");
        var unread = await authentication.Client.GetDataAsync<PagedResponse<NotificationDto>>("/api/Notification?page=1&size=20&onlyUnread=true");
        var paged = await authentication.Client.GetDataAsync<PagedResponse<NotificationDto>>("/api/Notification?page=1&size=1");

        all!.TotalCount.Should().Be(3);
        unread!.TotalCount.Should().Be(2);
        unread.Items.Should().OnlyContain(item => !item.IsRead);
        paged!.Items.Should().ContainSingle();
        paged.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Notification_listing_should_not_leak_other_users_notifications()
    {
        var owner = await CreateUserAsync("notification-isolated-owner");
        var stranger = await CreateUserAsync("notification-stranger");
        await SeedNotificationsAsync(owner.Id, unread: 1, read: 0);
        await SeedNotificationsAsync(stranger.Id, unread: 3, read: 0);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        var response = await authentication.Client.GetDataAsync<PagedResponse<NotificationDto>>("/api/Notification?page=1&size=20");

        response!.TotalCount.Should().Be(1);
        response.Items.Should().OnlyContain(item => item.UserId == owner.Id);
    }

    [Fact]
    public async Task Unread_count_should_reflect_only_unread_notifications_of_the_current_user()
    {
        var owner = await CreateUserAsync("unread-count-owner");
        var stranger = await CreateUserAsync("unread-count-stranger");
        await SeedNotificationsAsync(owner.Id, unread: 2, read: 3);
        await SeedNotificationsAsync(stranger.Id, unread: 5, read: 0);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        var response = await authentication.Client.GetDataAsync<GetUnreadNotificationCountQueryResponse>("/api/Notification/unread-count");

        response!.UnreadCount.Should().Be(2);
    }

    [Fact]
    public async Task Mark_as_read_should_stamp_read_date_and_lower_the_unread_count()
    {
        var owner = await CreateUserAsync("mark-read-owner");
        var notifications = await SeedNotificationsAsync(owner.Id, unread: 2, read: 0);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        var response = await authentication.Client.PatchAsync($"/api/Notification/{notifications[0].Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().SingleAsync(item => item.Id == notifications[0].Id));
        stored.IsRead.Should().BeTrue();
        stored.ReadAt.Should().NotBeNull();

        var unreadCount = await authentication.Client.GetDataAsync<GetUnreadNotificationCountQueryResponse>("/api/Notification/unread-count");
        unreadCount!.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task Mark_as_read_of_another_users_notification_should_return_not_found()
    {
        var owner = await CreateUserAsync("read-victim");
        var attacker = await CreateUserAsync("read-attacker");
        var notifications = await SeedNotificationsAsync(owner.Id, unread: 1, read: 0);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(attacker.Id);

        var response = await authentication.Client.PatchAsync($"/api/Notification/{notifications[0].Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().SingleAsync(item => item.Id == notifications[0].Id));
        stored.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task Mark_all_as_read_should_return_the_updated_count_and_be_idempotent()
    {
        var owner = await CreateUserAsync("mark-all-owner");
        var stranger = await CreateUserAsync("mark-all-stranger");
        await SeedNotificationsAsync(owner.Id, unread: 3, read: 1);
        await SeedNotificationsAsync(stranger.Id, unread: 2, read: 0);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        var first = await authentication.Client.PatchAsync("/api/Notification/read-all", null);
        var firstBody = await first.ReadDataAsync<MarkAllNotificationsAsReadCommandResponse>();
        var second = await authentication.Client.PatchAsync("/api/Notification/read-all", null);
        var secondBody = await second.ReadDataAsync<MarkAllNotificationsAsReadCommandResponse>();

        firstBody!.UpdatedCount.Should().Be(3);
        secondBody!.UpdatedCount.Should().Be(0);

        // Baska kullanicinin bildirimleri okunmamis kalmali.
        var strangerUnread = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.CountAsync(item => item.UserId == stranger.Id && !item.IsRead));
        strangerUnread.Should().Be(2);
    }

    [Fact]
    public async Task Delete_notification_should_soft_delete_and_hide_it_from_listings()
    {
        var owner = await CreateUserAsync("notification-deleter");
        var notifications = await SeedNotificationsAsync(owner.Id, unread: 2, read: 0);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);

        var response = await authentication.Client.DeleteAsync($"/api/Notification/{notifications[0].Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Notifications.AsNoTracking().SingleAsync(item => item.Id == notifications[0].Id));
        stored.isDeleted.Should().BeTrue();

        var listing = await authentication.Client.GetDataAsync<PagedResponse<NotificationDto>>("/api/Notification?page=1&size=20");
        listing!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Delete_notification_of_another_user_should_return_not_found()
    {
        var owner = await CreateUserAsync("delete-notification-victim");
        var attacker = await CreateUserAsync("delete-notification-attacker");
        var notifications = await SeedNotificationsAsync(owner.Id, unread: 1, read: 0);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(attacker.Id);

        var response = await authentication.Client.DeleteAsync($"/api/Notification/{notifications[0].Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Notification_endpoints_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.GetAsync("/api/Notification")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/Notification/unread-count")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PatchAsync("/api/Notification/1/read", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PatchAsync("/api/Notification/read-all", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync("/api/Notification/1")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private Task<List<Notification>> SeedNotificationsAsync(int userId, int unread, int read) => Factory.ExecuteScopeAsync(async services =>
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var created = new List<Notification>();

        for (var index = 0; index < unread + read; index++)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.MODERATION_WARNING,
                Message = $"bildirim {index}",
                IsRead = index >= unread,
                ReadAt = index >= unread ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow.AddMinutes(-index),
                isActive = true,
                isDeleted = false
            };
            context.Notifications.Add(notification);
            created.Add(notification);
        }

        await context.SaveChangesAsync();
        return created;
    });
}
