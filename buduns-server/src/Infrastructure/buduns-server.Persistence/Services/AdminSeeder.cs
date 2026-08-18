using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.Options;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace buduns_server.Persistence.Services
{
    /// <summary>
    /// Ilk admin'i yaratmaz, var olan bir hesabi yukseltir. Boylece sistem
    /// hicbir zaman sifre uretmez veya yapilandirmada sifre tasimaz; yapilandirma
    /// sizsa bile ele gecen sey bir e-posta adresidir.
    /// </summary>
    public class AdminSeeder : IAdminSeeder
    {
        private readonly UserManager<User> _userManager;
        private readonly IOptions<BootstrapAdminOptions> _options;
        private readonly ILogger<AdminSeeder> _logger;

        public AdminSeeder(UserManager<User> userManager, IOptions<BootstrapAdminOptions> options, ILogger<AdminSeeder> logger)
        {
            _userManager = userManager;
            _options = options;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken)
        {
            // Once admin var mi diye bakilir: yapilandirma eksikligini yalnizca
            // gercekten onemli oldugu durumda (sistemde hic admin yokken)
            // raporlayabilmek icin. Sistemde admin varsa bir daha karisilmaz;
            // aksi halde her acilis elle alinmis bir admin yetkisini geri getirirdi.
            var existingAdmins = await _userManager.GetUsersInRoleAsync(RoleConstants.Admin);
            if (existingAdmins.Count > 0)
            {
                _logger.LogDebug("Sistemde {AdminCount} admin mevcut, bootstrap yukseltmesi atlandi.", existingAdmins.Count);
                return;
            }

            var email = _options.Value.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                // Admin'i olmayan bir sistem yonetilemez. Bu durumda sessiz
                // kalmak, yapilandirmayi baska bir dosyaya yazip bekleyen
                // birini saatlerce oyalayabilir.
                _logger.LogWarning(
                    "Sistemde hic admin yok ve '{SettingName}' yapilandirilmamis. Yonetim uclari kullanilamayacak.",
                    $"{BootstrapAdminOptions.SectionName}:{nameof(BootstrapAdminOptions.Email)}");
                return;
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Bootstrap admin adayi {AdminEmail} kayitli degil. Hesap kayit olduktan sonraki acilista yukseltilecek.", email);
                return;
            }

            var result = await _userManager.AddToRoleAsync(user, RoleConstants.Admin);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"'{email}' hesabina Admin rolu atanamadi: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }

            // Sessizce yetki dagitan kod olmasin diye bilerek Warning.
            _logger.LogWarning("Bootstrap admin yukseltmesi yapildi. UserId: {UserId}, Email: {AdminEmail}", user.Id, email);
        }
    }
}
