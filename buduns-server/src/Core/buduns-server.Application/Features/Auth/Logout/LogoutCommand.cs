using buduns_server.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace buduns_server.Application.Features.Auth.Logout
{
    public class LogoutCommand : IRequest<LogoutCommandResponse>, ITransactionalRequest, ICurrentUserRequest, ICurrentSessionRequest, IAllowUnverifiedEmail
    {
        [JsonIgnore]
        public int UserId { get; set; }

        [JsonIgnore]
        public Guid CurrentSessionId { get; set; }
    }
}
