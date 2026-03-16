using AutoMapper;
using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.Register
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserCommandResponse>
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public RegisterUserCommandHandler(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<RegisterUserCommandResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            RegisterUserRequestDto userDto = _mapper.Map<RegisterUserRequestDto>(request);
            RegisterUserResponseDto response = await _userService.RegisterAsync(userDto);
            if (response.Succeeded)
            {
                return new RegisterUserCommandResponse(Succeeded: true, Message: "Kullanıcı başarıyla kayıt oldu.");
            }
            else
            {
                return new RegisterUserCommandResponse(Succeeded: response.Succeeded, Message: response.Message);
            }
        }
    }
}
