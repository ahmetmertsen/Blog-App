using buduns_server.WebAPI.Models;
using System.Text.Json;

namespace buduns_server.WebAPI.Http
{
    // Hata zarfinin tek kurma noktasi. Basari zarfini ApiControllerBase kurar.
    public static class ApiErrorWriter
    {
        // Her hatada yeni JsonSerializerOptions uretmek System.Text.Json'in
        // tip metadata onbellegini her seferinde bastan kurmaya zorlar.
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static ApiResponse Build(
            HttpContext context,
            string code,
            string message,
            Dictionary<string, string[]>? validationErrors = null)
            => new()
            {
                IsSuccess = false,
                TraceId = context.GetTraceId(),
                Error = new ErrorResponse
                {
                    Code = code,
                    Message = message,
                    ValidationErrors = validationErrors
                }
            };

        public static async Task WriteAsync(
            HttpContext context,
            int statusCode,
            string code,
            string message,
            Dictionary<string, string[]>? validationErrors = null)
        {
            // Bu yol pipeline'in en distaki katmanlarindan cagrilir; alttaki bir
            // katman cevaba yazmaya baslamisken hata firlatabilir. O noktada
            // status kodu ve govde artik degistirilemez.
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = Build(context, code, message, validationErrors);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }
    }
}
