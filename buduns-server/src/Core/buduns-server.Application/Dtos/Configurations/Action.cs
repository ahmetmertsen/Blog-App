using buduns_server.Domain.Enums;

namespace buduns_server.Application.Dtos.Configurations
{
    public class Action
    {
        public required string ActionType { get; set; }
        public required string HttpType { get; set; }
        public required string Definition { get; set; }
        public required string Code { get; set; }

        /// <summary>Kodda bildirilen baslangic erisim seviyesi.</summary>
        public EndpointAccessLevel AccessLevel { get; set; }

        /// <summary>
        /// Seviyenin karsiligi olan rol adlari. Yonetim ekraninin "varsayilan"
        /// ile "elle atanmis" arasindaki farki gosterebilmesi icin tasiniyor.
        /// </summary>
        public IReadOnlyList<string> DefaultRoles { get; set; } = Array.Empty<string>();
    }
}
