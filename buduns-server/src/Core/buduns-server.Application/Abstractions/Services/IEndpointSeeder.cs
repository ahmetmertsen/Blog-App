using buduns_server.Application.Dtos.Configurations;

namespace buduns_server.Application.Abstractions.Services
{
    public interface IEndpointSeeder
    {
        /// <summary>
        /// Koddaki [AuthorizeDefinition] tanimlarini veritabanindaki katalogla
        /// esitler. Yeni uclar erisim seviyelerinin karsiligi olan rollerle
        /// olusturulur; var olan kayitlarin rollerine dokunulmaz.
        /// </summary>
        /// <param name="assemblyType">Controller'larin bulundugu assembly'den herhangi bir tip.</param>
        Task<EndpointSeedResult> SeedAsync(Type assemblyType, CancellationToken cancellationToken);
    }
}
