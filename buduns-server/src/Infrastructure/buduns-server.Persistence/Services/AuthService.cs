using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Abstractions.Token;
using buduns_server.Application.Dtos;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Dtos.Auth;
using buduns_server.Application.Exceptions;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace buduns_server.Persistence.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenHandler _tokenHandler;
        private readonly IAuthSessionService _authSessionService;
        private readonly IMailService _mailService;
        private readonly IVerificationChallengeService _verificationChallengeService;
        private readonly JwtTokenOptions _tokenOptions;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<User> userManager, SignInManager<User> signInManager, ITokenHandler tokenHandler, IAuthSessionService authSessionService, IMailService mailService, IVerificationChallengeService verificationChallengeService, IOptions<JwtTokenOptions> tokenOptions, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenHandler = tokenHandler;
            _authSessionService = authSessionService;
            _mailService = mailService;
            _verificationChallengeService = verificationChallengeService;
            _tokenOptions = tokenOptions.Value;
            _logger = logger;
        }

        public async Task<Token> LoginAsync(string usernameOrEmail, string password, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(usernameOrEmail) ?? await _userManager.FindByEmailAsync(usernameOrEmail);

            if (user == null)
            {
                _logger.LogWarning("Login failed. Reason: {Reason}", "UserNotFound");
                throw new UnauthorizedAccesException("Kullanıcı adı veya şifre hatalı!");
            }

            await EnsureModerationStatusAsync(user);

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login blocked. Reason: {Reason}, UserId: {UserId}", "IdentityLockout", user.Id);
                throw new UnauthorizedAccesException("Çok fazla başarısız giriş denemesi yapıldı. Lütfen daha sonra tekrar deneyin.");
            }

            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed. Reason: {Reason}, UserId: {UserId}, UserName: {UserName}", "InvalidPassword", user.Id, user.UserName);
                throw new UnauthorizedAccesException("Kullanıcı adı veya şifre hatalı!");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var sessionId = Guid.NewGuid();
            var refreshToken = _tokenHandler.CreateRefreshToken();
            var token = _tokenHandler.CreateAccessToken(user, roles, sessionId, refreshToken);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());

            await _authSessionService.CreateSessionAsync(user.Id, sessionId, Guid.NewGuid(), refreshToken, refreshTokenExpiresAt, cancellationToken);

            _logger.LogInformation("Login succeeded. UserId: {UserId}, UserName: {UserName}, RolesCount: {RolesCount}, EmailConfirmed: {EmailConfirmed}, SessionId: {SessionId}", user.Id, user.UserName, roles.Count, user.EmailConfirmed, sessionId);

            return token;
        }

        public async Task<Token> RefreshTokenLoginAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var replacementSessionId = Guid.NewGuid();
            var replacementRefreshToken = _tokenHandler.CreateRefreshToken();
            var replacementExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpirationDays());

            var rotation = await _authSessionService.RotateSessionAsync(refreshToken, replacementSessionId, replacementRefreshToken, replacementExpiresAt, cancellationToken);

            var user = await _userManager.FindByIdAsync(rotation.UserId.ToString());
            if (user == null)
            {
                await _authSessionService.RevokeAllSessionsAsync(rotation.UserId, "User not found during refresh", cancellationToken);
                throw new InvalidRefreshTokenException("Refresh token geçersiz veya süresi dolmuş.");
            }

            try
            {
                await EnsureModerationStatusAsync(user);
            }
            catch
            {
                await _authSessionService.RevokeAllSessionsAsync(user.Id, "Account is not allowed to refresh", cancellationToken);
                throw;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenHandler.CreateAccessToken(user, roles, replacementSessionId, replacementRefreshToken);

            _logger.LogInformation("Refresh token login succeeded. UserId: {UserId}, UserName: {UserName}, RolesCount: {RolesCount}, SessionId: {SessionId}", user.Id, user.UserName, roles.Count, replacementSessionId);

            return token;
        }

        public async Task<ForgotPasswordResponse> ForgotPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var response = new ForgotPasswordResponse
            {
                Message = "Mail adresi doğru ise şifre sıfırlama kodu gönderildi."
            };

            if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            {
                return response;
            }

            var user = await _userManager.FindByEmailAsync(request.EmailOrUsername) ?? await _userManager.FindByNameAsync(request.EmailOrUsername);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return response;
            }

            var verificationCode = await _verificationChallengeService.CreateCodeAsync(user.Id, VerificationPurpose.PasswordReset, user.Email, cancellationToken);
            await _mailService.SendForgotPasswordMailAsync(user.Email, user.FullName, verificationCode);

            return response;
        }

        public async Task<MailVerifyResponse> MailVerifyAsync(MailVerifyRequest request, CancellationToken cancellationToken)
        {
            var response = new MailVerifyResponse
            {
                Message = "Doğrulama kodu e-posta adresinize gönderildi."
            };

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return response;
            }

            if (user.EmailConfirmed)
            {
                response.Message = "E-posta adresi zaten doğrulanmış.";
                return response;
            }

            if (user.EmailVerificationSentAt.HasValue && user.EmailVerificationSentAt.Value > DateTime.UtcNow.AddMinutes(-1))
            {
                response.Message = "Doğrulama e-postası kısa süre önce gönderildi. Lütfen tekrar denemeden önce bekleyin.";
                return response;
            }

            var verificationCode = await _verificationChallengeService.CreateCodeAsync(user.Id, VerificationPurpose.EmailVerification, user.Email, cancellationToken);
            await _mailService.SendVerifyMailAsync(user.Email, user.FullName, verificationCode);

            user.EmailVerificationSentAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Verification mail requested. UserId: {UserId}", user.Id);
            return response;
        }

        public async Task<ChangeEmailResponse> ChangeEmailAsync(ChangeEmailRequest request, CancellationToken cancellationToken)
        {
            var response = new ChangeEmailResponse
            {
                Message = "Yeni e-posta adresiniz uygunsa mevcut ve yeni e-posta adreslerinize doğrulama kodları gönderildi."
            };

            if (string.IsNullOrWhiteSpace(request.NewEmail))
            {
                return response;
            }

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                return response;
            }

            var newEmail = request.NewEmail.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(newEmail);
            if (existingUser != null)
            {
                return response;
            }

            var oldEmailVerificationCode = await _verificationChallengeService.CreateCodeAsync(user.Id, VerificationPurpose.EmailChangeOld, newEmail, cancellationToken);
            var newEmailVerificationCode = await _verificationChallengeService.CreateCodeAsync(user.Id, VerificationPurpose.EmailChangeNew, newEmail, cancellationToken);
            await _mailService.SendChangeEmailOldMailAsync(user.Email, user.FullName, newEmail, oldEmailVerificationCode);
            await _mailService.SendChangeEmailMailAsync(newEmail, user.FullName, newEmailVerificationCode);

            return response;
        }

        private async Task EnsureModerationStatusAsync(User user)
        {
            if (user.Status == UserStatus.Banned)
            {
                _logger.LogWarning("Login blocked. Reason: {Reason}, UserId: {UserId}", "UserBanned", user.Id);
                throw new UnauthorizedAccesException("Bu hesap platformdan yasaklanmıştır.");
            }

            if (user.Status != UserStatus.Suspended)
            {
                return;
            }

            if (!user.SuspendedUntil.HasValue || user.SuspendedUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login blocked. Reason: {Reason}, UserId: {UserId}", "UserSuspended", user.Id);
                throw new UnauthorizedAccesException("Bu hesap geçici olarak askıya alınmıştır.");
            }

            user.Status = UserStatus.Active;
            user.SuspendedUntil = null;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new UnauthorizedAccesException("Kullanıcı hesabı güncellenemedi.");
            }
        }

        private int GetRefreshTokenExpirationDays() =>
            _tokenOptions.RefreshTokenExpirationDays > 0 ? _tokenOptions.RefreshTokenExpirationDays : 30;
    }
}
