namespace buduns_server.WebAPI.Models
{
    public sealed class ErrorResponse
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public Dictionary<string, string[]>? ValidationErrors { get; init; }
    }
}
