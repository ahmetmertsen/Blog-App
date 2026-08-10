using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos.Role;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace buduns_server.Persistence.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;

        public RoleService(RoleManager<Role> roleManager, UserManager<User> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<List<RoleDto>> GetAllRoles(CancellationToken cancellationToken)
        {
            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(role => role.Name).ToListAsync(cancellationToken);
            return roles.ToDtoList();
        }

        public async Task<List<RoleDto>> GetRolesByUsername(string userName, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var userRoleNames = await _userManager.GetRolesAsync(user);
            if (userRoleNames.Count == 0)
            {
                return new List<RoleDto>();
            }

            var roles = await _roleManager.Roles.AsNoTracking().Where(role => role.Name != null && userRoleNames.Contains(role.Name)).OrderBy(role => role.Name).ToListAsync(cancellationToken);
            return roles.ToDtoList();
        }

        public async Task<RoleDto> GetRoleById(int id, CancellationToken cancellationToken)
        {
            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
            if (role == null)
            {
                throw new NotFoundException("Rol bulunamadı.");
            }

            return role.ToDto();
        }

        public async Task CreateRole(string name, CancellationToken cancellationToken)
        {
            var roleName = name.Trim();
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                throw new BadRequestException("Bu rol adı zaten kullanılıyor.");
            }

            var result = await _roleManager.CreateAsync(new Role { Name = roleName });
            EnsureSucceeded(result, "Rol oluşturulamadı");
        }

        public async Task DeleteRole(int id, CancellationToken cancellationToken)
        {
            var role = await _roleManager.Roles.Include(item => item.Endpoints).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (role == null)
            {
                throw new NotFoundException("Rol bulunamadı.");
            }

            EnsureRoleCanBeChanged(role);

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count > 0)
            {
                throw new BadRequestException("Kullanıcılara atanmış bir rol silinemez. Önce kullanıcı rol atamalarını kaldırın.");
            }

            if (role.Endpoints != null && role.Endpoints.Count > 0)
            {
                throw new BadRequestException("Endpoint yetkilerine bağlı bir rol silinemez. Önce endpoint rol atamalarını kaldırın.");
            }

            var result = await _roleManager.DeleteAsync(role);
            EnsureSucceeded(result, "Rol silinemedi");
        }

        public async Task UpdateRole(int id, string name, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                throw new NotFoundException("Rol bulunamadı.");
            }

            EnsureRoleCanBeChanged(role);

            var roleName = name.Trim();
            var normalizedRoleName = _roleManager.NormalizeKey(roleName);
            var duplicateExists = await _roleManager.Roles.AsNoTracking().AnyAsync(existingRole => existingRole.Id != id && existingRole.NormalizedName == normalizedRoleName, cancellationToken);
            if (duplicateExists)
            {
                throw new BadRequestException("Bu rol adı zaten kullanılıyor.");
            }

            role.Name = roleName;
            var result = await _roleManager.UpdateAsync(role);
            EnsureSucceeded(result, "Rol güncellenemedi");
        }

        private static void EnsureRoleCanBeChanged(Role role)
        {
            if (RoleConstants.IsSystemRole(role.Name))
            {
                throw new BadRequestException("Sistem rolleri güncellenemez veya silinemez.");
            }
        }

        private static void EnsureSucceeded(IdentityResult result, string message)
        {
            if (!result.Succeeded)
            {
                throw new BadRequestException($"{message}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
    }
}
