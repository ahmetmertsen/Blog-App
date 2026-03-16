using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Abstractions.Services
{
    public interface IAuthService
    {
        Task<Dtos.Token> LoginAsync(string usernameOrEmail, string password);
        Task<Dtos.Token> RefreshTokenLoginAsync(string refreshToken);
    }
}
