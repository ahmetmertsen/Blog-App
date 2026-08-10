using buduns_server.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Http
{
    public static class ApiValidationProblemFactory
    {
        // Model binding hatalari FluentValidation'a ulasmadan MVC tarafindan
        // yakalanir ve varsayilan olarak ProblemDetails uretir. Bu fabrika
        // onlari da ortak zarfa cevirir.
        public static IActionResult Create(ActionContext context)
        {
            var validationErrors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Geçersiz değer."
                            : error.ErrorMessage)
                        .ToArray());

            var response = ApiErrorWriter.Build(
                context.HttpContext,
                ApiErrorCodes.ValidationError,
                "Validation hatası oluştu.",
                validationErrors);

            return new ObjectResult(response) { StatusCode = StatusCodes.Status400BadRequest };
        }
    }
}
