using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Dtos.Configurations;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace buduns_server.Persistence.Services
{
    public class EndpointPermissionService : IEndpointPermissionService
    {
        private const string CacheKeyPrefix = "endpoint-permission:";

        private readonly IEndpointRepository _endpointRepository;
        private readonly UserManager<User> _userManager;
        private readonly ICacheService _cacheService;
        private readonly TimeSpan _timeToLive;

        public EndpointPermissionService(
            IEndpointRepository endpointRepository,
            UserManager<User> userManager,
            ICacheService cacheService,
            IOptions<CacheOptions> cacheOptions)
        {
            _endpointRepository = endpointRepository;
            _userManager = userManager;
            _cacheService = cacheService;
            _timeToLive = TimeSpan.FromSeconds(cacheOptions.Value.EndpointPermissionTtlSeconds);
        }

        public async Task<bool> HasAccessAsync(int userId, string code, IReadOnlyList<string> defaultRoles, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            // Kullanicinin rolleri onbelleklenmez: rol degisikligi bir sonraki
            // istekte gecerli olmali. Onbelleklenen taraf, nadiren degisen ve
            // atama aninda dusurulen endpoint -> rol eslemesi.
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Count == 0)
            {
                return false;
            }

            var roleSet = await GetRoleSetAsync(code, cancellationToken);

            var allowedRoles = roleSet.IsRegistered ? roleSet.Roles : defaultRoles;

            return userRoles.Intersect(allowedRoles, StringComparer.Ordinal).Any();
        }

        public Task InvalidateAsync(string code, CancellationToken cancellationToken = default) =>
            _cacheService.RemoveAsync(BuildCacheKey(code), cancellationToken);

        private Task<EndpointRoleSet> GetRoleSetAsync(string code, CancellationToken cancellationToken) =>
            _cacheService.GetOrSetAsync(
                BuildCacheKey(code),
                _timeToLive,
                async _ =>
                {
                    var endpoint = await _endpointRepository.GetRolesToEndpoint(code);
                    if (endpoint == null)
                    {
                        return EndpointRoleSet.NotRegistered();
                    }

                    return EndpointRoleSet.Registered(endpoint.Roles.Select(role => role.Name).OfType<string>());
                },
                cancellationToken);

        private static string BuildCacheKey(string code) => $"{CacheKeyPrefix}{code}";
    }
}
