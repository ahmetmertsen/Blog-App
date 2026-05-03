using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.Auth;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.ChangePhoneNumber
{
    public class ChangePhoneNumberCommandHandler : IRequestHandler<ChangePhoneNumberCommand, ChangePhoneNumberCommandResponse>
    {
        private readonly IAuthService _authService;

        public ChangePhoneNumberCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<ChangePhoneNumberCommandResponse> Handle(ChangePhoneNumberCommand commandRequest, CancellationToken cancellationToken)
        {
            ChangePhoneNumberRequest request = new()
            {
                UserId = commandRequest.UserId,
                NewPhoneNumber = commandRequest.NewPhoneNumber
            };

            var response = await _authService.ChangePhoneNumberAsync(request);

            return new ChangePhoneNumberCommandResponse(Succeeded:response.Succeeded, Message:response.Message);
        }
    }
}
