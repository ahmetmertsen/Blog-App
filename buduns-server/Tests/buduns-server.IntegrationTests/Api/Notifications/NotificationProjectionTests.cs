using buduns_server.Application.Common.Consts;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Notifications;

/// <summary>
/// Bildirim listesi eskiden AutoMapper'in ProjectTo'su ile projekte ediliyordu.
/// Yerine gecen Expression'in EF tarafindan SQL'e cevrilebildigi yalnizca
/// gercek veritabanina karsi dogrulanabilir; birim test bunu gosteremez.
/// </summary>
public sealed class NotificationProjectionTests : IntegrationTestBase
{
    public NotificationProjectionTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Notification_projection_should_translate_to_sql_and_fill_actor_name()
    {
        var owner = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "notify-owner", "Notify Owner", RoleConstants.User));
        var actor = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "notify-actor", "Notify Actor", RoleConstants.User));
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, owner.Id));

        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            context.Notifications.AddRange(
                new Notification { UserId = owner.Id, ActorUserId = actor.Id, PostId = post.Id, Type = NotificationType.POST_LIKED, Message = "Paylasimin begenildi.", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-1), isActive = true, isDeleted = false },
                // ActorUser'i olmayan sistem bildirimi: null kontrolunun SQL'e dogru cevrildigini dogrular.
                new Notification { UserId = owner.Id, ActorUserId = null, Type = NotificationType.MODERATION_WARNING, Message = "Uyari.", IsRead = true, ReadAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow.AddMinutes(-2), isActive = true, isDeleted = false });
            await context.SaveChangesAsync();
        });

        var (items, totalCount) = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<IUnitOfWork>().NotificationRepository.GetPagedByUserIdAsync(owner.Id, 1, 10, false, CancellationToken.None));

        totalCount.Should().Be(2);
        items.Should().HaveCount(2);

        var liked = items.Single(item => item.Type == NotificationType.POST_LIKED);
        liked.ActorUserId.Should().Be(actor.Id);
        liked.ActorUserName.Should().Be("notify-actor");
        liked.PostId.Should().Be(post.Id);
        liked.Message.Should().Be("Paylasimin begenildi.");
        liked.IsRead.Should().BeFalse();

        var warning = items.Single(item => item.Type == NotificationType.MODERATION_WARNING);
        warning.ActorUserId.Should().BeNull();
        warning.ActorUserName.Should().BeNull();
        warning.IsRead.Should().BeTrue();
        warning.ReadAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Notification_projection_should_respect_unread_filter()
    {
        var owner = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "unread-owner", "Unread Owner", RoleConstants.User));

        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            context.Notifications.AddRange(
                new Notification { UserId = owner.Id, Type = NotificationType.MODERATION_WARNING, Message = "okunmamis", IsRead = false, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false },
                new Notification { UserId = owner.Id, Type = NotificationType.MODERATION_WARNING, Message = "okunmus", IsRead = true, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false });
            await context.SaveChangesAsync();
        });

        var (items, _) = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<IUnitOfWork>().NotificationRepository.GetPagedByUserIdAsync(owner.Id, 1, 10, true, CancellationToken.None));

        items.Should().ContainSingle();
        items[0].Message.Should().Be("okunmamis");
    }
}
