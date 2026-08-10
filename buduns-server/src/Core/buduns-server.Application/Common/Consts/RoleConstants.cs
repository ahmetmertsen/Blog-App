using buduns_server.Domain.Enums;

namespace buduns_server.Application.Common.Consts
{
    public static class RoleConstants
    {
        public const string Admin = "Admin";
        public const string Moderator = "Moderator";
        public const string User = "User";

        // Uygulama acilisinda seed edilen, silinemeyen ve adi degistirilemeyen roller.
        public static readonly IReadOnlyList<string> SystemRoles = new[] { Admin, Moderator, User };

        public static bool IsSystemRole(string? roleName)
        {
            return roleName != null && SystemRoles.Any(systemRole => systemRole.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Erisim seviyesinin karsiligi olan rol listesi. Admin hicbir listede
        /// yok; yetki filtresi Admin'i zaten kontrolden once geciriyor.
        /// Seviye -> rol esleme burada tek yerde durur; endpoint'ler rol adi
        /// degil seviye bildirir.
        /// </summary>
        public static IReadOnlyList<string> GetDefaultRoles(EndpointAccessLevel accessLevel) => accessLevel switch
        {
            EndpointAccessLevel.Member => new[] { User, Moderator },
            EndpointAccessLevel.Moderator => new[] { Moderator },
            _ => Array.Empty<string>()
        };
    }
}
