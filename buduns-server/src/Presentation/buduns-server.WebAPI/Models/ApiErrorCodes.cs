namespace buduns_server.WebAPI.Models
{
    public static class ApiErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string BadRequest = "BAD_REQUEST";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string NotFound = "NOT_FOUND";
        public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string InternalServerError = "INTERNAL_SERVER_ERROR";
    }
}
