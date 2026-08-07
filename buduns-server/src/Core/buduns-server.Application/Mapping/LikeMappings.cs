using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;

namespace buduns_server.Application.Mapping
{
    public static class LikeMappings
    {
        // User navigasyonu yuklenmemis olabilir; yuklenmemisse kullanici
        // alanlari bos birakilir.
        public static LikeDto ToDto(this Like like) => new()
        {
            Id = like.Id,
            UserId = like.UserId,
            UserName = like.User?.UserName ?? string.Empty,
            FullName = like.User?.FullName,
            ImageUrl = like.User?.ImageUrl,
            LikedAt = like.CreatedAt
        };
    }
}
