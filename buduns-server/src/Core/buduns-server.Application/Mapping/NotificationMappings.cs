using System.Linq.Expressions;
using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;

namespace buduns_server.Application.Mapping
{
    public static class NotificationMappings
    {
        // AutoMapper'in ProjectTo'sunun yerini alir: sorguya gomulup SQL'e
        // cevrilebilmesi icin Expression olarak duruyor, derlenmis delege degil.
        public static readonly Expression<Func<Notification, NotificationDto>> ToDto = notification => new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Message = notification.Message,
            UserId = notification.UserId,
            ActorUserId = notification.ActorUserId,
            ActorUserName = notification.ActorUser != null ? notification.ActorUser.UserName : null,
            PostId = notification.PostId,
            CommentId = notification.CommentId,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            CreatedAt = notification.CreatedAt
        };
    }
}
