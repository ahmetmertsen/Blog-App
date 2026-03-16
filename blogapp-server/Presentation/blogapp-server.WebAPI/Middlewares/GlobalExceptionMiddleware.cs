using blogapp_server.WebAPI.Models;
using System.Diagnostics;
using System.Text.Json;

namespace blogapp_server.WebAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Application.Exceptions.ApplicationException ex)
            {
                await HandleApplicationExceptionAsync(context, ex);
            }
        }

        private static async Task HandleApplicationExceptionAsync(HttpContext context, Application.Exceptions.ApplicationException exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var response = new ApiResponse
            {
                IsSuccess = false,
                Error = new ErrorResponse
                {
                    Code = exception.ErrorCode,
                    Message = exception.Message,
                    HttpStatus = exception.HttpStatusCode,
                    TraceId = traceId
                }
            };

            context.Response.StatusCode = 200; // Her zaman HTTP 200 dön
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
