using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;

namespace buduns_server.Application.Mapping
{
    public static class FollowerMappings
    {
        // UserName/FullName/Bio/ImageUrl bilerek doldurulmaz; bu kayit yalnizca
        // takip iliskisini tasir, kullanici bilgileri ayrica sorgulanir.
        public static FollowerDto ToDto(this Follower follower) => new()
        {
            Id = follower.Id,
            UserId = follower.FollowingId,
            FollowedAt = follower.CreatedAt
        };
    }
}
