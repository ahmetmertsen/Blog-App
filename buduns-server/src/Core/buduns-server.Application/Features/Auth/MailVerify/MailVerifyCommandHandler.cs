using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos.Auth;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Auth.MailVerify
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
                UserId = commandRequest.UserId,
            };

            var response = await _authService.MailVerifyAsync(request, cancellationToken);

            MailVerifyCommandResponse commandResponse = new(Message:response.Message);
            return commandResponse;
        }
    }
}
