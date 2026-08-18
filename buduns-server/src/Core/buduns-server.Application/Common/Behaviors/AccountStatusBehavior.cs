using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace buduns_server.Application.Common.Behaviors
{
    public class AccountStatusBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AccountStatusBehavior(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
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

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
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
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (!user.EmailConfirmed && request is not Application.Common.Interfaces.IAllowUnverifiedEmail)
            {
                throw new EmailVerificationRequiredException("İşleme devam etmek için e-posta adresinizi doğrulamalısınız.");
            }

            return await next(cancellationToken);
        }
    }
}
