namespace buduns_server.Application.Dtos.Configurations
{
    /// <summary>
    /// Acilistaki mail sablonu senkronizasyonunun sonucu.
    /// </summary>
    public sealed class MailTemplateSeedResult
    {
        /// <summary>Veritabaninda olmadigi icin bu acilista yazilan sablonlar.</summary>
        public IReadOnlyList<string> CreatedKeys { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Veritabaninda olan ama icerigi kodla ayni olmayan sablonlar. Uzerine
        /// yazilmaz: icerik, elle duzenlenmis olabilir. Yalnizca raporlanir ki
        /// "kodda degistirdim ama mail eskisi gibi geliyor" sessiz kalmasin.
        /// </summary>
        public IReadOnlyList<string> DivergedKeys { get; init; } = Array.Empty<string>();
    }
}
