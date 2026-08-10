using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos.User;
using buduns_server.Application.Mapping;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Auth.Register
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
    {
        private readonly IUserService _userService;

        public RegisterUserCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            RegisterUserRequestDto userDto = request.ToRequestDto();
            RegisterUserResponseDto response = await _userService.RegisterAsync(userDto, cancellationToken);

            return new RegisterUserCommandResponse(Message: response.Message);
        }
    }
}
