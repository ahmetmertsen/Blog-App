using blogapp_server.Application.Dtos;
using blogapp_server.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Abstractions.Services
{
    public interface IUserService
    {
        Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto request);
        Task UpdateRefreshToken(string refreshToken, User user, DateTime accessTokenDate);
    }
}
