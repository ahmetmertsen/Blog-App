using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.Auth;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordCommandResponse>
    {
        private readonly IAuthService _authService;

        public ForgotPasswordCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<ForgotPasswordCommandResponse> Handle(ForgotPasswordCommand commandRequest, CancellationToken cancellationToken)
        {
            ForgotPasswordRequest request = new()
            {
                EmailOrUsername = commandRequest.EmailOrUsername
            };

            var response = await _authService.ForgotPasswordResetAsync(request);

            ForgotPasswordCommandResponse forgotResponse = new(Succeeded: response.Succeeded, Message: response.Message);
            return forgotResponse;
        }
    }
}
