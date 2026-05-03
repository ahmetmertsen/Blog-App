using blogapp_server.Application.Dtos.Auth;
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
        Task<ForgotPasswordResponse> ForgotPasswordResetAsync(ForgotPasswordRequest request);
        Task<MailVerifyResponse> MailVerifyAsync(MailVerifyRequest request);
        Task<ChangeEmailResponse> ChangeEmailAsync(ChangeEmailRequest request);
        Task<ChangePhoneNumberResponse> ChangePhoneNumberAsync(ChangePhoneNumberRequest request);
    }
}
