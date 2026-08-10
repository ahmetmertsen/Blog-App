namespace buduns_server.Domain.Enums
{
    /// <summary>
    /// Bir endpoint'in acilista hangi rollere acilacagini belirler. Yalnizca
    /// baslangic degeridir; yonetim ucundan yapilan atamalar bunu ezer ve
    /// seeder var olan kaydin rollerine bir daha dokunmaz.
    /// </summary>
    public enum EndpointAccessLevel
    {
        /// <summary>
        /// Hicbir role acilmaz. Admin, yetki filtresini zaten atladigi icin
        /// yalnizca yonetim uclari bu seviyede kalir. Varsayilan deger budur:
        /// seviyesi belirtilmeyen yeni bir endpoint acik degil, kapali dogar.
        /// </summary>
        AdminOnly = 0,

        /// <summary>Moderasyon uclari.</summary>
        Moderator = 1,

        /// <summary>Kayitli her kullanicinin erisebildigi uclar.</summary>
        Member = 2
    }
}
