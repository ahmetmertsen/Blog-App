using buduns_server.WebAPI.Http;
using buduns_server.WebAPI.Models;
using FluentValidation;

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
            var traceId = context.GetTraceId();

            _logger.LogWarning(exception, "Validation error. TraceId: {TraceId}, Path: {Path}", traceId, context.Request.Path);

            var validationErrors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

            await ApiErrorWriter.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ApiErrorCodes.ValidationError,
                "Validation hatası oluştu.",
                validationErrors);
        }

        private async Task HandleApplicationExceptionAsync(HttpContext context, Application.Exceptions.ApplicationException exception)
        {
            var traceId = context.GetTraceId();

            _logger.LogWarning(
                exception, "Application error. TraceId: {TraceId}, Path: {Path}, ErrorCode: {ErrorCode}", traceId, context.Request.Path, exception.ErrorCode);

            await ApiErrorWriter.WriteAsync(
                context,
                exception.HttpStatusCode,
                exception.ErrorCode,
                exception.Message);
        }

        private async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.GetTraceId();

            _logger.LogError(exception, "Unhandled error. TraceId: {TraceId}, Path: {Path}", traceId, context.Request.Path);

            await ApiErrorWriter.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ApiErrorCodes.InternalServerError,
                "Beklenmeyen bir hata oluştu.");
        }
    }
}
