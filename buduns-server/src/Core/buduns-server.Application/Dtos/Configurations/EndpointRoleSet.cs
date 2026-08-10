namespace buduns_server.Application.Dtos.Configurations
{
    /// <summary>
    /// Bir yetki kodunun onbellege alinan rol kumesi. "Kayit yok" durumu da
    /// onbelleklenebilsin diye null yerine <see cref="IsRegistered"/> ile
    /// tasiniyor; aksi halde kaydi olmayan her uc her istekte veritabanina
    /// gitmeye devam ederdi.
    /// </summary>
    public sealed class EndpointRoleSet
    {
        public bool IsRegistered { get; set; }

        public string[] Roles { get; set; } = Array.Empty<string>();

        public static EndpointRoleSet NotRegistered() => new();

        public static EndpointRoleSet Registered(IEnumerable<string> roles) => new()
        {
            IsRegistered = true,
            Roles = roles.ToArray()
        };
    }
}
