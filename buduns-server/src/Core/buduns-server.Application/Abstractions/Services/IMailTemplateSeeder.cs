using buduns_server.Application.Dtos.Configurations;

namespace buduns_server.Application.Abstractions.Services
{
    public interface IMailTemplateSeeder
    {
        /// <summary>
        /// Kodda tanimli mail sablonlarindan veritabaninda olmayanlari yazar.
        /// Var olan bir sablonun icerigine dokunmaz.
        /// </summary>
        Task<MailTemplateSeedResult> SeedAsync(CancellationToken cancellationToken);
    }
}
