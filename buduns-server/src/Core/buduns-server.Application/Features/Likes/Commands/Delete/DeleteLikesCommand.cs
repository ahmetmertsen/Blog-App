using buduns_server.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace buduns_server.Application.Features.Likes.Commands.Delete
{
    public class DeleteLikesCommand : IRequest<DeleteLikesCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public int PostId { get; set; }
    }
}
