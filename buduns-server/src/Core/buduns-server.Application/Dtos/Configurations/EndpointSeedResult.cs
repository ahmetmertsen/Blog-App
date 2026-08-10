namespace buduns_server.Application.Dtos.Configurations
{
    /// <summary>
    /// Acilistaki katalog senkronizasyonunun sonucu. Cagiran taraf bunu
    /// loglar; ozellikle <see cref="OrphanCodes"/> sessiz yetki kaybinin tek
    /// gorunur isaretidir.
    /// </summary>
    public sealed class EndpointSeedResult
    {
        public int CreatedMenuCount { get; init; }
        public int CreatedEndpointCount { get; init; }
        public int UpdatedEndpointCount { get; init; }

        /// <summary>
        /// Veritabaninda kaydi olan ama kodda karsiligi kalmayan yetki kodlari.
        /// Silinmez: bir endpoint gercekten kaldirilmis da olabilir, bir
        /// Definition metni degistigi icin kod kaymis da olabilir. Ikisi de
        /// insan karari gerektirir.
        /// </summary>
        public IReadOnlyList<string> OrphanCodes { get; init; } = Array.Empty<string>();

        public bool HasChanges => CreatedMenuCount > 0 || CreatedEndpointCount > 0 || UpdatedEndpointCount > 0;
    }
}
