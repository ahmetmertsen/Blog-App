using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Exceptions;
using MediatR;

namespace buduns_server.Application.Features.Auth.RevokeSession
{
    public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, RevokeSessionCommandResponse>
    {
        private readonly IAuthSessionService _authSessionService;

        public RevokeSessionCommandHandler(IAuthSessionService authSessionService)
        {
            _authSessionService = authSessionService;
        }

        public async Task<RevokeSessionCommandResponse> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
        {
            var revoked = await _authSessionService.RevokeSessionAsync(request.UserId, request.SessionId, "Revoked by user", cancellationToken);

            if (!revoked)
            {
                throw new NotFoundException("Oturum bulunamadı.");
            }

            return new RevokeSessionCommandResponse("Oturum başarıyla iptal edildi.");
        }
    }
}
