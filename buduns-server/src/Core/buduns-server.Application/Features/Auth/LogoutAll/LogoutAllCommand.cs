using buduns_server.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace buduns_server.Application.Features.Auth.LogoutAll
{
    public class LogoutAllCommand : IRequest<LogoutAllCommandResponse>, ITransactionalRequest, ICurrentUserRequest, IAllowUnverifiedEmail
    {
        [JsonIgnore]
        public int UserId { get; set; }
    }
}
