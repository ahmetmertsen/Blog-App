using blogapp_server.Application.Common.Interfaces;
using blogapp_server.Application.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Common.Behaviors
{
    public class CurrentUserBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : class
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserBehavior(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is ICurrentUserRequest currentUserRequest)
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated != true)
                {
                    throw new UnauthorizedAccesException("Kullanıcı doğrulanamadı.");
                }

                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                    user.FindFirst("sub")?.Value;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    throw new UnauthorizedAccesException("Geçerli kullanıcı bilgisi bulunamadı.");
                }

                currentUserRequest.UserId = userId;
            }
            return await next();
        }
    }
}
