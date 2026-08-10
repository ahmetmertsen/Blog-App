namespace buduns_server.Application.Common.Options
{
    public class CacheOptions
    {
        public const string SectionName = "Cache";

        /// <summary>
        /// Redis anahtarlarinin onune eklenen ayrac. Ayni Redis ornegini birden
        /// fazla ortam paylasirsa anahtarlarin cakismamasini saglar.
        /// </summary>
        public string InstanceName { get; set; } = "buduns:";

        public int DailyTopPostsTtlSeconds { get; set; } = 60;

        /// <summary>
        /// Endpoint -> rol eslemesinin onbellekte kalma suresi. Atama yapildigi
        /// anda ilgili anahtar zaten dusuruluyor; bu sure yalnizca dogrudan
        /// veritabanina yazan durumlara karsi ust sinirdir.
        /// </summary>
        public int EndpointPermissionTtlSeconds { get; set; } = 300;
    }
}
