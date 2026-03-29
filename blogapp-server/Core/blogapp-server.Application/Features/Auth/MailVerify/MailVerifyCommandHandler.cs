using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.Auth;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.MailVerify
{
    public class MailVerifyCommandHandler : IRequestHandler<MailVerifyCommand, MailVerifyCommandResponse>
    {
        private readonly IAuthService _authService;

        public MailVerifyCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<MailVerifyCommandResponse> Handle(MailVerifyCommand commandRequest, CancellationToken cancellationToken)
        {
            MailVerifyRequest request = new()
            {
                Email = commandRequest.Email,
            };

            var response = await _authService.MailVerifyAsync(request);

            MailVerifyCommandResponse commandResponse = new(Succeeded:response.Succeeded, Message:response.Message);
            return commandResponse;
        }
    }
}
