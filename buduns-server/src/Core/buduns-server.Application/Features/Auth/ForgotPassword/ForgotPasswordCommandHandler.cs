using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos.Auth;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Auth.ForgotPassword
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

            var response = await _authService.ForgotPasswordResetAsync(request, cancellationToken);

            ForgotPasswordCommandResponse forgotResponse = new(Message: response.Message);
            return forgotResponse;
        }
    }
}
