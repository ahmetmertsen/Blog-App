using AutoMapper;
using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Exceptions;
using blogapp_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        
        public UserService(UserManager<User> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
            {
                throw new RegisterFailedException("Email kayıtlar arasında mevcut!");
            }
            else
            {
                user = await _userManager.FindByNameAsync(request.UserName);
                if (user != null)
                {
                    throw new RegisterFailedException("Username kayıtlar arasında mevcut!");
                }
            }
            user = _mapper.Map<User>(request);

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                return new RegisterUserResponseDto
                {
                    Succeeded = true,
                    Message = "Başarıyla kayıt olunmuştur."
                };
            }
            else
            {
                throw new RegisterFailedException("Kayıt sırasında hata oluştu");
            }
        }

        public async Task UpdateRefreshToken(string refreshToken, User user, DateTime accessTokenDate)
        {
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenEndDate = DateTime.UtcNow.AddDays(30);
                await _userManager.UpdateAsync(user);
            }
            else
            {
                throw new NotFoundException("Kullanıcı bulunamadı!");
            }
        }
    }
}
