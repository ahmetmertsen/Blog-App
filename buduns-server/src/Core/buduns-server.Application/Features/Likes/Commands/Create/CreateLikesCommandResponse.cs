namespace buduns_server.Application.Features.Likes.Commands.Create
{
    public record CreateLikesCommandResponse(string Message, int LikeId, bool AlreadyLiked);
}
