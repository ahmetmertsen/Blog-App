using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.Auth;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.ChangeEmail
{
    public class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand, ChangeEmailCommandResponse>
    {
        private readonly IAuthService _authService;

        public ChangeEmailCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<ChangeEmailCommandResponse> Handle(ChangeEmailCommand commandRequest, CancellationToken cancellationToken)
        {
            ChangeEmailRequest request = new()
            {
                UserId = commandRequest.UserId,
                NewEmail = commandRequest.NewEmail
            };

            var response = await _authService.ChangeEmailAsync(request);

            ChangeEmailCommandResponse changeResponse = new(Succeeded: response.Succeeded, Message: response.Message);
            return changeResponse;
        }
    }
}
