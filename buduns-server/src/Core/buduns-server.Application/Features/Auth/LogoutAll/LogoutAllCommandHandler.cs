using buduns_server.Application.Abstractions.Services;
using MediatR;

namespace buduns_server.Application.Features.Auth.LogoutAll
{
    public class LogoutAllCommandHandler : IRequestHandler<LogoutAllCommand, LogoutAllCommandResponse>
    {
        private readonly IAuthSessionService _authSessionService;

        public LogoutAllCommandHandler(IAuthSessionService authSessionService)
        {
            _authSessionService = authSessionService;
        }

        public async Task<LogoutAllCommandResponse> Handle(LogoutAllCommand request, CancellationToken cancellationToken)
        {
            await _authSessionService.RevokeAllSessionsAsync(request.UserId, "User logout from all sessions", cancellationToken);

            return new LogoutAllCommandResponse("Tüm oturumlar başarıyla kapatıldı.");
        }
    }
}
