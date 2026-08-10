namespace buduns_server.WebAPI.Models
{
    // Iki tip kardes; kalitim bilerek yok. ApiResponse<T> bir ApiResponse'a
    // upcast edilebilseydi, System.Text.Json bildirilen tip uzerinden
    // serialize edip "data" alanini sessizce dusururdu.
    public sealed class ApiResponse
    {
        public bool IsSuccess { get; init; }
        public ErrorResponse? Error { get; init; }
        public string TraceId { get; init; } = string.Empty;
    }

    public sealed class ApiResponse<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public ErrorResponse? Error { get; init; }
        public string TraceId { get; init; } = string.Empty;
    }
}
