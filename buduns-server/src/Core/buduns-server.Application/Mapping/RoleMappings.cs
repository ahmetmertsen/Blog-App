using buduns_server.Application.Dtos.Role;
using buduns_server.Domain.Entities.Identity;

namespace buduns_server.Application.Mapping
{
    public static class RoleMappings
    {
        public static RoleDto ToDto(this Role role) => new()
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty
        };

        public static List<RoleDto> ToDtoList(this IEnumerable<Role> roles) => roles.Select(ToDto).ToList();
    }
}
