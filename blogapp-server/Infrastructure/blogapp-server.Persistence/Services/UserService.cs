using AutoMapper;
using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.User;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.Helpers;
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

        public async Task<UpdateUserPasswordResponse> UpdatePasswordAsync(UpdateUserPasswordRequest request)
        {
            User user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }
            if (string.IsNullOrWhiteSpace(request.ResetToken))
            {
                throw new PasswordChangeFailedException("Şifre sıfırlama bağlantısı geçersiz.");
            }
            if (!request.newPassword.Equals(request.newPasswordConfirmed))
            {
                throw new PasswordChangeFailedException("Şifreler uyuşmuyor.");
            }
            string resetToken = request.ResetToken.UrlDecode();

            IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, request.newPassword);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                user.LastPasswordChangedDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                

                return new UpdateUserPasswordResponse
                {
                    Succeeded = true,
                    Message = "Şifre başarılı bir şekilde güncellenmiştir."
                };
            }
            else
            {
                throw new PasswordChangeFailedException("Şifre oluşturulurken hata oluştu...");
            }
        }


        public async Task<UpdateUserMailVerifyResponse> UpdateUserMailVerify(UpdateUserMailVerifyRequest request)
        {
            User user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }
            if (user.EmailConfirmed == true)
            {
                return new UpdateUserMailVerifyResponse
                {
                    Succeeded = true,
                    Message = "E-posta adresi zaten doğrulanmış."
                };
            }

            if (string.IsNullOrWhiteSpace(request.EmailConfirmToken))
            {
                throw new BadRequestException("Doğrulama bağlantısı geçersiz.");
            }

            string token = request.EmailConfirmToken.UrlDecode();
            IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);

                return new UpdateUserMailVerifyResponse
                {
                    Succeeded = true,
                    Message = "E-posta adresiniz başarıyla doğrulandı."
                };
            }
            else
            {
                throw new MailVerifyFailedException("E-posta doğrulama başarısız. Bağlantı süresi dolmuş olabilir.");
            }
        }

        public async Task<UpdateUserProfileResponse> UpdateUserProfile(UpdateUserProfileRequest request)
        {
            User user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            user.FullName = request.FullName;
            user.Bio = request.Bio;
            user.ImageUrl = request.ImageUrl;

            IdentityResult result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return new UpdateUserProfileResponse
                {
                    Succeeded = true,
                    Message = "Profil başarıyla güncellendi."
                };
            }
            else
            {
                throw new BadRequestException($"Profil güncellenirken bir hata oluştu.");
            }
        }

        public async Task<UpdateUserEmailResponse> UpdateUserEmailAsync(UpdateUserEmailRequest request)
        {
            User user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(request.ChangeEmailToken))
            {
                throw new BadRequestException("Doğrulama bağlantısı geçersiz.");
            }
            string token = request.ChangeEmailToken.UrlDecode();
            IdentityResult result = await _userManager.ChangeEmailAsync(user, request.NewEmail, token);

            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                await _userManager.UpdateAsync(user);

                return new UpdateUserEmailResponse
                {
                    Succeeded = true,
                    Message = "Email başarılı bir şekilde güncellenmiştir."
                };
            }
            else
            {
                throw new ChangeEmailFailedException("Email güncellenirken hata oluştu...");
            }
        }
    }
}
