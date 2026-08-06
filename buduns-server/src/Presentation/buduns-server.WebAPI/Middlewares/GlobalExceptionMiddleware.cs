using buduns_server.WebAPI.Models;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;

namespace buduns_server.WebAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (Application.Exceptions.ApplicationException ex)
            {
                await HandleApplicationExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                await HandleUnexpectedExceptionAsync(context, ex);
            }
        }

        private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogWarning(exception, "Validation error. TraceId: {TraceId}, Path: {Path}", traceId, context.Request.Path);

            var validationErrors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

            var response = new ApiResponse
            {
                IsSuccess = false,
                Error = new ErrorResponse
                {
                    Code = "VALIDATION_ERROR",
                    Message = "Validation hatası oluştu.",
                    HttpStatus = StatusCodes.Status400BadRequest,
                    TraceId = traceId,
                    ValidationErrors = validationErrors
                }
            };

            await WriteErrorResponseAsync(context, StatusCodes.Status400BadRequest, response);
        }

        private async Task HandleApplicationExceptionAsync(HttpContext context, Application.Exceptions.ApplicationException exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogWarning(
                exception, "Application error. TraceId: {TraceId}, Path: {Path}, ErrorCode: {ErrorCode}", traceId, context.Request.Path, exception.ErrorCode);

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

            await WriteErrorResponseAsync(context, exception.HttpStatusCode, response);
        }

        private async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogError(exception, "Unhandled error. TraceId: {TraceId}, Path: {Path}", traceId, context.Request.Path);

            var response = new ApiResponse
            {
                IsSuccess = false,
                Error = new ErrorResponse
                {
                    Code = "INTERNAL_SERVER_ERROR",
                    Message = "Beklenmeyen bir hata oluştu.",
                    HttpStatus = StatusCodes.Status500InternalServerError,
                    TraceId = traceId
                }
            };

            await WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError, response);
        }

        private static async Task WriteErrorResponseAsync(HttpContext context, int statusCode, ApiResponse response)
        {
            // Bu middleware pipeline'in en distaki katmanlarindan biri oldugu
            // icin, alttaki bir katman cevaba yazmaya baslamisken hata
            // firlatabilir. O noktada status kodu ve govde artik
            // degistirilemez; yazmaya calismak InvalidOperationException
            // uretir. Hata zaten loglandi, sessizce cikiliyor.
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
