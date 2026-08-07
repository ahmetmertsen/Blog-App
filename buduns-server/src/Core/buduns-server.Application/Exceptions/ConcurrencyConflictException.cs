namespace buduns_server.Application.Exceptions
{
    public class ConcurrencyConflictException : ApplicationException
    {
        public ConcurrencyConflictException(string message = "Kayıt başka bir işlem tarafından güncellendi. Güncel durumu yeniden yükleyin.")
            : base(message, 409, "CONCURRENCY_CONFLICT")
        {
        }
    }
}
