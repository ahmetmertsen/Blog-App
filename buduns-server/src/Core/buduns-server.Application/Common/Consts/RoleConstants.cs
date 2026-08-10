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
    }
}
