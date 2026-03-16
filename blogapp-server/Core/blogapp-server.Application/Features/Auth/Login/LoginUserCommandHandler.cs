using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Exceptions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.Login
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginUserCommandResponse>
    {
        private readonly IAuthService _authService;

        public LoginUserCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<LoginUserCommandResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var token = await _authService.LoginAsync(request.UsernameOrEmail, request.Password);
            if (token == null)
            {
                throw new UnauthorizedAccesException("Kullanıcı adı, Email veya şifre hatalı!");
            } else
            {
                return new LoginUserCommandResponse(Succeeded: true, Message: "Giriş başarılı!", Token: token);
            }
        }
    }
}
