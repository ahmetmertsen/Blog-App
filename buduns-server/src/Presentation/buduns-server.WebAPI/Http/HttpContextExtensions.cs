using System.Diagnostics;

namespace buduns_server.WebAPI.Http
{
    public static class HttpContextExtensions
    {
        public static string GetTraceId(this HttpContext context)
            => Activity.Current?.Id ?? context.TraceIdentifier;
    }
}
