namespace buduns_server.Application.Common.Options
{
    // Ad "Token" degil "JwtToken": Microsoft.AspNetCore.Identity.TokenOptions ile
    // karismasin diye.
    public class JwtTokenOptions
    {
        public const string SectionName = "Token";

        public string Audience { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string SecurityKey { get; set; } = string.Empty;
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 30;
    }
}
