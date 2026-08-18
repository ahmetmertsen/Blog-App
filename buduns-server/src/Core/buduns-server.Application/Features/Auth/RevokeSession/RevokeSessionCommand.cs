using buduns_server.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace buduns_server.Application.Features.Auth.RevokeSession
{
    public class RevokeSessionCommand : IRequest<RevokeSessionCommandResponse>, ITransactionalRequest, ICurrentUserRequest, IAllowUnverifiedEmail
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public Guid SessionId { get; set; }
    }
}
