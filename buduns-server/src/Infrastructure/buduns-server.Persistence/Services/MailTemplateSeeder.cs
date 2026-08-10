using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos.Configurations;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Persistence.MailTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace buduns_server.Persistence.Services
{
    /// <summary>
    /// Mail sablonlarini acilista veritabanina yazar. Sablonlar
    /// <c>Utilities</c> tablosunda ad/deger cifti olarak duruyor; sablon yoksa
    /// <c>MailService</c> mail gonderemez ve kullanici hesabini dogrulayamaz.
    /// </summary>
    public class MailTemplateSeeder : IMailTemplateSeeder
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MailTemplateSeeder> _logger;

        public MailTemplateSeeder(IUnitOfWork unitOfWork, ILogger<MailTemplateSeeder> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<MailTemplateSeedResult> SeedAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await SynchronizeAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                // Iki instance ayni anda kalkarsa ikisi de ayni sablonu
                // eklemeye calisabilir. Ikinci denemede sablon yerinde bulunur.
                _logger.LogWarning(exception, "Mail sablonlari yazilirken cakisma olustu, bir kez daha deneniyor.");

                return await SynchronizeAsync(cancellationToken);
            }
        }

        private async Task<MailTemplateSeedResult> SynchronizeAsync(CancellationToken cancellationToken)
        {
            var createdKeys = new List<string>();
            var divergedKeys = new List<string>();

            foreach (var key in MailTemplateKeys.All)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var body = MailTemplateCatalog.GetBody(key);
                var stored = await _unitOfWork.UtilityRepository.GetByNameAsync(key);

                if (stored == null)
                {
                    await _unitOfWork.UtilityRepository.AddAsync(new Utility
                    {
                        Name = key,
                        Value = body,
                        CreatedAt = DateTime.UtcNow,
                        isActive = true,
                        isDeleted = false
                    });

                    createdKeys.Add(key);
                    continue;
                }

                // Icerik ezilmez: sablonlar duzenlenebilir metin, kod bir
                // baslangic degeri veriyor. Ama farki gormeden birakmak
                // "kodda degistirdim, mail eskisi gibi geliyor" sorusunu
                // cevapsiz birakirdi.
                if (!string.Equals(stored.Value, body, StringComparison.Ordinal))
                {
                    divergedKeys.Add(key);
                }
            }

            if (createdKeys.Count > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new MailTemplateSeedResult
            {
                CreatedKeys = createdKeys,
                DivergedKeys = divergedKeys
            };
        }
    }
}
