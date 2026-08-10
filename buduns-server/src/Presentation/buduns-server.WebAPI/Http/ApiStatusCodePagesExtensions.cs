using buduns_server.WebAPI.Models;

namespace buduns_server.WebAPI.Http
{
    public static class ApiStatusCodePagesExtensions
    {
        // 401/403/404/405 gibi durumlar exception olarak akmaz; MVC ve
        // authentication katmanlari bunlari govdesiz dondurur. Bu katman
        // govdesiz kalan her hata durumunu ortak zarfa cevirir.
        public static IApplicationBuilder UseApiStatusCodePages(this IApplicationBuilder app)
            => app.UseStatusCodePages(async statusCodeContext =>
            {
                var context = statusCodeContext.HttpContext;
                var (code, message) = Describe(context.Response.StatusCode);

                await ApiErrorWriter.WriteAsync(context, context.Response.StatusCode, code, message);
            });

        private static (string Code, string Message) Describe(int statusCode) => statusCode switch
        {
            StatusCodes.Status401Unauthorized => (ApiErrorCodes.Unauthorized, "Bu işlem için kimlik doğrulaması gerekiyor."),
            StatusCodes.Status403Forbidden => (ApiErrorCodes.Forbidden, "Bu işlem için yetkiniz yok."),
            StatusCodes.Status404NotFound => (ApiErrorCodes.NotFound, "İstenen kaynak bulunamadı."),
            StatusCodes.Status405MethodNotAllowed => (ApiErrorCodes.MethodNotAllowed, "Bu HTTP metodu bu adres için desteklenmiyor."),
            _ when statusCode >= StatusCodes.Status500InternalServerError => (ApiErrorCodes.InternalServerError, "Beklenmeyen bir hata oluştu."),
            _ => (ApiErrorCodes.BadRequest, "İstek işlenemedi.")
        };
    }
}
