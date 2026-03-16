namespace blogapp_server.WebAPI.Models
{
    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public ErrorResponse? Error { get; set; }
    }
}
