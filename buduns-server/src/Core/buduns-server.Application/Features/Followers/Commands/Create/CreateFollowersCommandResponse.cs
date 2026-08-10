namespace buduns_server.Application.Features.Followers.Commands.Create
{
    public record CreateFollowersCommandResponse(string Message, int FollowId, bool AlreadyFollowing);
}
