using buduns_server.Application.Mapping;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;

namespace buduns_server.UnitTests.Mapping;

/// <summary>
/// NotificationMappings.ToDto bir Expression olarak duruyor; SQL'e cevrilebilir
/// kalmasi entegrasyon testinde dogrulaniyor, alan esleme kurallari ise burada.
/// </summary>
public class NotificationMappingTests
{
    private static readonly Func<Notification, Application.Dtos.NotificationDto> Map = NotificationMappings.ToDto.Compile();

    [Fact]
    public void ToDto_ShouldMapAllFields()
    {
        var createdAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var readAt = new DateTime(2026, 6, 1, 11, 0, 0, DateTimeKind.Utc);
        var notification = new Notification
        {
            Id = 12,
            Type = NotificationType.POST_COMMENTED,
            Message = "Paylasiminiz yorumlandi.",
            UserId = 3,
            ActorUserId = 9,
            ActorUser = new User { Id = 9, UserName = "yorumcu", FullName = "Yorumcu" },
            PostId = 44,
            CommentId = 55,
            IsRead = true,
            ReadAt = readAt,
            CreatedAt = createdAt
        };

        var dto = Map(notification);

        Assert.Equal(12, dto.Id);
        Assert.Equal(NotificationType.POST_COMMENTED, dto.Type);
        Assert.Equal("Paylasiminiz yorumlandi.", dto.Message);
        Assert.Equal(3, dto.UserId);
        Assert.Equal(9, dto.ActorUserId);
        Assert.Equal("yorumcu", dto.ActorUserName);
        Assert.Equal(44, dto.PostId);
        Assert.Equal(55, dto.CommentId);
        Assert.True(dto.IsRead);
        Assert.Equal(readAt, dto.ReadAt);
        Assert.Equal(createdAt, dto.CreatedAt);
    }

    [Fact]
    public void ToDto_SystemNotificationWithoutActor_ShouldLeaveActorFieldsNull()
    {
        var dto = Map(new Notification
        {
            Id = 1,
            Type = NotificationType.MODERATION_WARNING,
            Message = "Uyari",
            UserId = 3,
            ActorUserId = null,
            ActorUser = null
        });

        Assert.Null(dto.ActorUserId);
        Assert.Null(dto.ActorUserName);
        Assert.Null(dto.PostId);
        Assert.Null(dto.CommentId);
        Assert.Null(dto.ReadAt);
        Assert.False(dto.IsRead);
    }

    [Fact]
    public void ToDto_ActorNavigationNotLoaded_ShouldNotThrow()
    {
        // Navigasyon yuklenmemis olabilir; ifade null kontrolunu icermeli.
        var dto = Map(new Notification { Id = 1, Type = NotificationType.POST_LIKED, Message = "Begeni", UserId = 3, ActorUserId = 9, ActorUser = null });

        Assert.Equal(9, dto.ActorUserId);
        Assert.Null(dto.ActorUserName);
    }
}
