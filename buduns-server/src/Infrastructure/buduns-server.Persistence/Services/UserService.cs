using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Application.Dtos.User;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Persistence.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthSessionService _authSessionService;
        private readonly IMailService _mailService;
        private readonly IVerificationChallengeService _verificationChallengeService;
        private readonly ILogger<UserService> _logger;
        
        public UserService(UserManager<User> userManager, RoleManager<Role> roleManager, IUnitOfWork unitOfWork, IAuthSessionService authSessionService, IMailService mailService, IVerificationChallengeService verificationChallengeService, ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _authSessionService = authSessionService;
            _mailService = mailService;
            _verificationChallengeService = verificationChallengeService;
            _logger = logger;
        }

        public async Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken)
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

            if (!await _roleManager.RoleExistsAsync(RoleConstants.User))
            {
                throw new RegisterFailedException("Kayıt tamamlanamadı. User rolü sistemde tanımlı değil.");
            }

            user = request.ToEntity();

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, RoleConstants.User);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);

                    throw new RegisterFailedException("Kullanıcı rolü atanamadığı için kayıt tamamlanamadı. User rolünün tanımlı olduğundan emin olun.");

                }

                var message = "Kullanıcı başarıyla kaydedildi. E-posta adresinizi doğrulamanız gerekiyor.";
                try
                {
                    var verificationCode = await _verificationChallengeService.CreateCodeAsync(user.Id, VerificationPurpose.EmailVerification, user.Email!, cancellationToken);
                    await _mailService.SendVerifyMailAsync(user.Email!, user.FullName, verificationCode);

                    user.EmailVerificationSentAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }
                catch (Exception exception)
                {
                    message = "Kullanıcı başarıyla kaydedildi ancak doğrulama e-postası gönderilemedi. Tekrar gönderme işlemini kullanabilirsiniz.";
                    _logger.LogWarning(
                        exception,
                        "Registration verification mail could not be sent. UserId: {UserId}",
                        user.Id);
                }

                return new RegisterUserResponseDto
                {
                    Message = message
                };
            }
            else
            {
                throw new RegisterFailedException("Kayıt sırasında hata oluştu");
            }
        }

        public async Task<UpdateUserPasswordResponse> UpdatePasswordAsync(UpdateUserPasswordRequest request, CancellationToken cancellationToken)
        {
            User? user = await _userManager.FindByEmailAsync(request.EmailOrUsername) ?? await _userManager.FindByNameAsync(request.EmailOrUsername);
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new PasswordChangeFailedException("Kullanıcının e-posta adresi bulunamadı.");
            }
            if (string.IsNullOrWhiteSpace(request.VerificationCode))
            {
                throw new PasswordChangeFailedException("Şifre sıfırlama kodu geçersiz.");
            }
            if (!request.newPassword.Equals(request.newPasswordConfirmed))
            {
                throw new PasswordChangeFailedException("Şifreler uyuşmuyor.");
            }

            await _verificationChallengeService.ValidateCodeAsync(user.Id, VerificationPurpose.PasswordReset, user.Email, request.VerificationCode, cancellationToken);
            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            IdentityResult result = await _userManager.ResetPasswordAsync(user, resetToken, request.newPassword);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                user.LastPasswordChangedDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                await _authSessionService.RevokeAllSessionsAsync(user.Id, "Password changed", cancellationToken);
                
                return new UpdateUserPasswordResponse
                {
                    Message = "Şifre başarılı bir şekilde güncellenmiştir."
                };
            }
            else
            {
                throw new PasswordChangeFailedException("Şifre oluşturulurken hata oluştu...");
            }
        }


        public async Task<UpdateUserMailVerifyResponse> UpdateUserMailVerify(UpdateUserMailVerifyRequest request, CancellationToken cancellationToken)
        {
            User? user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }
            if (user.EmailConfirmed == true)
            {
                return new UpdateUserMailVerifyResponse
                {
                    Message = "E-posta adresi zaten doğrulanmış."
                };
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new BadRequestException("Kullanıcının e-posta adresi bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(request.VerificationCode))
            {
                throw new BadRequestException("Doğrulama kodu geçersiz.");
            }

            await _verificationChallengeService.ValidateCodeAsync(user.Id, VerificationPurpose.EmailVerification, user.Email, request.VerificationCode, cancellationToken);
            string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                user.EmailVerificationSentAt = null;

                return new UpdateUserMailVerifyResponse
                {
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
            User? user = await _userManager.FindByIdAsync(request.UserId.ToString());
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
                    Message = "Profil başarıyla güncellendi."
                };
            }
            else
            {
                throw new BadRequestException($"Profil güncellenirken bir hata oluştu.");
            }
        }

        public async Task<UpdateUserEmailResponse> UpdateUserEmailAsync(UpdateUserEmailRequest request, CancellationToken cancellationToken)
        {
            User? user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new BadRequestException("Kullanıcının mevcut e-posta adresi bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(request.OldEmailVerificationCode) || string.IsNullOrWhiteSpace(request.NewEmailVerificationCode))
            {
                throw new BadRequestException("Doğrulama kodları geçersiz.");
            }

            var newEmail = request.NewEmail.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(newEmail);
            if (existingUser != null)
            {
                throw new ChangeEmailFailedException("Email güncellenirken hata oluştu...");
            }

            var oldEmail = user.Email;
            await _verificationChallengeService.ValidateEmailChangeCodesAsync(user.Id, newEmail, request.OldEmailVerificationCode, request.NewEmailVerificationCode, cancellationToken);

            string token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            IdentityResult result = await _userManager.ChangeEmailAsync(user, newEmail, token);

            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                await _userManager.UpdateAsync(user);
                await _authSessionService.RevokeAllSessionsAsync(user.Id, "Email changed", cancellationToken);
                await _mailService.SendMailAsync(oldEmail, "E-Posta Adresiniz Değiştirildi", "Buduns hesabınızın e-posta adresi değiştirildi. Bu işlemi siz yapmadıysanız hesabınızı güvene alın.");

                return new UpdateUserEmailResponse
                {
                    Message = "Email başarılı bir şekilde güncellenmiştir."
                };
            }
            else
            {
                throw new ChangeEmailFailedException("Email güncellenirken hata oluştu...");
            }
        }

        public async Task<(List<AdminUserDto> Items, int TotalCount)> GetPagedUsersAsync(int page, int size, string? search, UserStatus? status, bool? emailConfirmed, CancellationToken cancellationToken)
        {
            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(user => (user.UserName != null && EF.Functions.ILike(user.UserName, $"%{keyword}%")) || EF.Functions.ILike(user.FullName, $"%{keyword}%") || (user.Email != null && EF.Functions.ILike(user.Email, $"%{keyword}%")));
            }

            if (status.HasValue)
            {
                query = query.Where(user => user.Status == status.Value);
            }

            if (emailConfirmed.HasValue)
            {
                query = query.Where(user => user.EmailConfirmed == emailConfirmed.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query.OrderBy(user => user.UserName).ThenBy(user => user.Id).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
            var items = new List<AdminUserDto>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new AdminUserDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    EmailConfirmed = user.EmailConfirmed,
                    Status = user.Status,
                    SuspendedUntil = user.SuspendedUntil,
                    LockoutEnd = user.LockoutEnd,
                    IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    Roles = roles.OrderBy(role => role).ToArray()
                });
            }

            return (items, totalCount);
        }

        public async Task<UserDto> GetUserById(int userId)
        {
            var user = await ProjectUsers().FirstOrDefaultAsync(item => item.Id == userId);
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            return user;
        }

        public async Task<UserDto> GetUserByUserName(string userName)
        {
            var normalizedUserName = _userManager.NormalizeName(userName);
            var userId = await _userManager.Users.AsNoTracking().Where(item => item.NormalizedUserName == normalizedUserName).Select(item => (int?)item.Id).FirstOrDefaultAsync();
            if (userId == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            return await ProjectUsers().FirstAsync(item => item.Id == userId.Value);
        }

        private IQueryable<UserDto> ProjectUsers() => _userManager.Users.AsNoTracking().Select(user => new UserDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName,
            Bio = user.Bio,
            ImageUrl = user.ImageUrl,
            FollowerCount = user.Followers.Count(follow => follow.isActive && !follow.isDeleted && follow.FollowerUser.Status != Domain.Enums.UserStatus.Banned),
            FollowingCount = user.Followings.Count(follow => follow.isActive && !follow.isDeleted && follow.FollowingUser.Status != Domain.Enums.UserStatus.Banned)
        });

        public async Task AssignRoleToUserAsync(int actorUserId, int targetUserId, string[] roles, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(targetUserId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var requestedRoleNames = (roles ?? Array.Empty<string>()).Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (requestedRoleNames.Length == 0)
            {
                throw new BadRequestException("Kullanıcıya en az bir rol atanmalıdır.");
            }

            var availableRoleNames = await _roleManager.Roles.AsNoTracking().Where(role => role.Name != null).Select(role => role.Name!).ToListAsync(cancellationToken);
            var invalidRoles = requestedRoleNames.Where(role => !availableRoleNames.Contains(role, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (invalidRoles.Length > 0)
            {
                throw new BadRequestException($"Tanımlı olmayan roller gönderildi: {string.Join(", ", invalidRoles)}");
            }

            var resolvedRoles = requestedRoleNames.Select(role => availableRoleNames.First(availableRole => string.Equals(availableRole, role, StringComparison.OrdinalIgnoreCase))).ToArray();
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removesAdminRole = currentRoles.Contains(RoleConstants.Admin, StringComparer.OrdinalIgnoreCase) && !resolvedRoles.Contains(RoleConstants.Admin, StringComparer.OrdinalIgnoreCase);

            if (actorUserId == targetUserId && removesAdminRole)
            {
                throw new BadRequestException("Kendi Admin rolünüzü kaldıramazsınız.");
            }

            if (removesAdminRole)
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync(RoleConstants.Admin);
                if (adminUsers.Count <= 1)
                {
                    throw new BadRequestException("Sistemde en az bir Admin kullanıcısı bulunmalıdır.");
                }
            }

            var rolesToAdd = resolvedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            var rolesToRemove = currentRoles.Except(resolvedRoles, StringComparer.OrdinalIgnoreCase).ToArray();
            if (rolesToAdd.Length == 0 && rolesToRemove.Length == 0)
            {
                return;
            }

            if (rolesToAdd.Length > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    throw new BadRequestException($"Roller atanamadı: {GetIdentityErrors(addResult)}");
                }
            }

            if (rolesToRemove.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    if (rolesToAdd.Length > 0)
                    {
                        await _userManager.RemoveFromRolesAsync(user, rolesToAdd);
                    }

                    throw new BadRequestException($"Mevcut roller kaldırılamadı: {GetIdentityErrors(removeResult)}");
                }
            }

            var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!securityStampResult.Succeeded)
            {
                throw new BadRequestException($"Rol değişikliği güvenlik bilgisine yansıtılamadı: {GetIdentityErrors(securityStampResult)}");
            }

            await _authSessionService.RevokeAllSessionsAsync(user.Id, "Roles changed", cancellationToken);
        }


        public async Task<string[]> GetRolesToUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.ToArray();
        }

        private static string GetIdentityErrors(IdentityResult result) => string.Join(", ", result.Errors.Select(error => error.Description));

    }
}
