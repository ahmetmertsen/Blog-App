using buduns_server.Application.Exceptions;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace buduns_server.Application.Common.Behaviors
{
    public class AccountStatusBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;

        public AccountStatusBehavior(IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return await next(cancellationToken);
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                principal.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccesException("Geçerli kullanıcı bilgisi bulunamadı.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new UnauthorizedAccesException("Kullanıcı hesabı bulunamadı.");
            }

            if (user.Status == UserStatus.Banned)
            {
                throw new ForbiddenException("Bu hesap platformdan yasaklanmıştır.");
            }

            if (user.Status == UserStatus.Suspended)
            {
                if (!user.SuspendedUntil.HasValue || user.SuspendedUntil.Value > DateTime.UtcNow)
                {
                    throw new ForbiddenException("Bu hesap geçici olarak askıya alınmıştır.");
                }

                user.Status = UserStatus.Active;
                user.SuspendedUntil = null;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new UnauthorizedAccesException("Kullanıcı hesabı güncellenemedi.");
                }
            }

            if (!user.EmailConfirmed && request is not Application.Common.Interfaces.IAllowUnverifiedEmail)
            {
                throw new EmailVerificationRequiredException("İşleme devam etmek için e-posta adresinizi doğrulamalısınız.");
            }

            return await next(cancellationToken);
        }
    }
}
