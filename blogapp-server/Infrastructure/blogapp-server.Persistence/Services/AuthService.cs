using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Abstractions.Token;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Dtos.Auth;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.Helpers;
using blogapp_server.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenHandler _tokenHandler;
        private readonly IUserService _userService;
        private readonly IMailService _mailService;

        public AuthService(UserManager<User> userManager, SignInManager<User> signInManager, ITokenHandler tokenHandler,  IUserService userService, IMailService mailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenHandler = tokenHandler;
            _userService = userService;
            _mailService = mailService;
        }

        public async Task<Token> LoginAsync(string usernameOrEmail, string password)
        {
            var user = await _userManager.FindByNameAsync(usernameOrEmail);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(usernameOrEmail);
                if (user == null)
                {
                    throw new UnauthorizedAccesException("Kullanıcı adı veya şifre hatalı!");
                }
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                Token token = _tokenHandler.CreateAccessToken(user, roles);
                await _userService.UpdateRefreshToken(token.RefreshToken, user, token.Expiration);
                return token;
            }
            else
            {
                throw new UnauthorizedAccesException("Kullanıcı adı veya şifre hatalı!");
            }
        }

        public async Task<Token> RefreshTokenLoginAsync(string refreshToken)
        {
            User? user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user != null && user?.RefreshTokenEndDate > DateTime.UtcNow)
            {
                var roles = await _userManager.GetRolesAsync(user);
                Token token = _tokenHandler.CreateAccessToken(user, roles);
                await _userService.UpdateRefreshToken(token.RefreshToken, user, token.Expiration);
                return token;
            }
            else
            {
                throw new NotFoundException("Kullanıcı bulunumadı!");
            }
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordResetAsync(ForgotPasswordRequest request)
        {
            ForgotPasswordResponse response = new()
            {
                Succeeded = true,
                Message = "Mail adresi doğru ise Şifre Sıfırlama bağlantısı gönderildi"
            };

            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            {
                return response;
            }

            User user = await _userManager.FindByEmailAsync(request.EmailOrUsername);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(request.EmailOrUsername);
                if (user == null)
                {
                    return response;
                }
            }

            // Reset token üretiliyor
            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            resetToken = resetToken.UrlEncode();

            await _mailService.SendForgotPasswordMailAsync(user.Email, user.FullName, user.Id, resetToken);
            return response;
        }


        public async Task<MailVerifyResponse> MailVerifyAsync(MailVerifyRequest request)
        {
            MailVerifyResponse response = new()
            {
                Succeeded = true,
                Message = "Mail adresi doğru ise doğrulama bağlantısı gönderildi"
            };

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return response;
            }

            User user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return response;
            }

            if (user.EmailConfirmed == true)
            {
                return new MailVerifyResponse
                {
                    Succeeded = true,
                    Message = "E-posta adresi zaten doğrulanmış."
                };
            }

            string emailConfirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            emailConfirmToken = emailConfirmToken.UrlEncode();

            await _mailService.SendVerifyMailAsync(user.Email, user.FullName, user.Id, emailConfirmToken);
            return response;
        }

        public async Task<ChangeEmailResponse> ChangeEmailAsync(ChangeEmailRequest request)
        {
            ChangeEmailResponse response = new()
            {
                Succeeded = true,
                Message = "Yeni e-posta adresiniz doğrulama için kontrol edilmelidir. Uygunsa doğrulama bağlantısı gönderildi."
            };

            if (string.IsNullOrWhiteSpace(request.NewEmail))
            {
                return response;
            }

            User user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return response;
            }

            string newEmail = request.NewEmail.Trim().ToLower();
            User user2 = await _userManager.FindByEmailAsync(newEmail);
            if (user2 != null)
            {
                return response;
            }

            string emailChangeToken = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            emailChangeToken = emailChangeToken.UrlEncode();

            await _mailService.SendChangeEmailMailAsync(newEmail, user.FullName, user.Id, emailChangeToken);
            return response;
        }
    }
}
