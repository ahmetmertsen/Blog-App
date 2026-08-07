using buduns_server.Application.Features.Posts.Commands.Create;
using buduns_server.Application.Features.Posts.Commands.Update;
using buduns_server.Domain.Entities;

namespace buduns_server.Application.Mapping
{
    public static class PostMappings
    {
        // UserId, Tags ve durum alanlarini handler kendisi atar.
        public static Post ToEntity(this CreatePostsCommand command) => new()
        {
            Content = command.Content
        };

        // Id atanmaz: post zaten command.Id ile cekildigi icin ayni deger.
        public static void ApplyTo(this UpdatePostsCommand command, Post post)
        {
            post.Content = command.Content;
        }
    }
}
