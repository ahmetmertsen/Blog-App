using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace buduns_server.Persistence.Services
{
    public class RoleSeeder : IRoleSeeder
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(RoleManager<Role> roleManager, ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            var createdRoles = new List<string>();

            foreach (var roleName in RoleConstants.SystemRoles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                if (await TryCreateRoleAsync(roleName))
                {
                    createdRoles.Add(roleName);
                }
            }

            if (createdRoles.Count == 0)
            {
                _logger.LogDebug("Sistem rolleri zaten mevcut, rol eklenmedi.");
                return;
            }

            _logger.LogInformation("Eksik sistem rolleri olusturuldu: {CreatedRoles}", string.Join(", ", createdRoles));
        }

        /// <summary>
        /// Rolun gercekten bu cagri tarafindan olusturulup olusturulmadigini
        /// dondurur. Ayni anda kalkan iki instance'ta biri unique indekse takilir;
        /// rol o an baskasi tarafindan eklenmisse hata degil, normal sonuctur.
        /// </summary>
        private async Task<bool> TryCreateRoleAsync(string roleName)
        {
            try
            {
                var result = await _roleManager.CreateAsync(new Role { Name = roleName });
                if (result.Succeeded)
                {
                    return true;
                }

                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    return false;
                }

                throw new InvalidOperationException($"'{roleName}' rolu olusturulamadi: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
            catch (DbUpdateException)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    return false;
                }

                throw;
            }
        }
    }
}
