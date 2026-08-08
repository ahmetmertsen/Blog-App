using System.Globalization;

namespace buduns_server.Application.Common.Consts
{
    public static class CacheKeys
    {
        /// <summary>
        /// Anahtara Turkiye tarihi girer: gun donduğunde TTL'in dolmasi
        /// beklenmeden yeni anahtara gecilir, dunun listesi servis edilmez.
        /// </summary>
        public static string DailyTopPosts(DateTime dateInTurkey, int limit) =>
            string.Create(CultureInfo.InvariantCulture, $"posts:daily-top:{dateInTurkey:yyyy-MM-dd}:{limit}");
    }
}
