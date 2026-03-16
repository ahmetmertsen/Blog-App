namespace blogapp_server.WebAPI.Models
{
    public class ErrorResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int HttpStatus { get; set; }
        public string TraceId { get; set; } = string.Empty;
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
    }
}
