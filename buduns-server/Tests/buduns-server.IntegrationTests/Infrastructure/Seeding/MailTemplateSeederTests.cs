using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos.Configurations;
using buduns_server.Domain.Entities;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.Persistence.Context;
using buduns_server.Persistence.MailTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Infrastructure.Seeding;

/// <summary>
/// Mail sablonlari Utilities tablosunda duruyor ve MailService onlari
/// bulamazsa mail gonderemiyor. Bu testler acilistaki seeder'in sozlesmesini
/// gercek veritabaninda dogruluyor.
/// </summary>
public sealed class MailTemplateSeederTests : IntegrationTestBase
{
    public MailTemplateSeederTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Seeding_should_create_every_declared_template()
    {
        var stored = await GetStoredTemplatesAsync();

        stored.Keys.Should().BeEquivalentTo(MailTemplateKeys.All);
    }

    [Fact]
    public async Task Stored_bodies_should_match_the_ones_declared_in_code()
    {
        var stored = await GetStoredTemplatesAsync();

        foreach (var key in MailTemplateKeys.All)
        {
            stored[key].Should().Be(MailTemplateCatalog.GetBody(key), key);
        }
    }

    [Fact]
    public async Task Running_the_seeder_again_should_not_duplicate_templates()
    {
        await RunSeederAsync();
        var result = await RunSeederAsync();

        result.CreatedKeys.Should().BeEmpty();
        result.DivergedKeys.Should().BeEmpty();
        (await GetStoredTemplatesAsync()).Should().HaveCount(MailTemplateKeys.All.Count);
    }

    /// <summary>
    /// Sablon icerigi duzenlenebilir bir metin; her acilista koddaki surume
    /// donmesi duzenlemeyi anlamsiz kilardi.
    /// </summary>
    [Fact]
    public async Task Seeding_should_not_overwrite_a_manually_edited_template()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            var template = await context.Utilities.SingleAsync(item => item.Name == MailTemplateKeys.MailVerify);
            template.Value = "<p>elle duzenlenmis {full_name} {verification_code} {app_name}</p>";
            await context.SaveChangesAsync();
        });

        var result = await RunSeederAsync();

        result.CreatedKeys.Should().BeEmpty();
        result.DivergedKeys.Should().BeEquivalentTo(new[] { MailTemplateKeys.MailVerify });
        (await GetStoredTemplatesAsync())[MailTemplateKeys.MailVerify]
            .Should().Be("<p>elle duzenlenmis {full_name} {verification_code} {app_name}</p>");
    }

    [Fact]
    public async Task Seeding_should_restore_a_deleted_template()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            var template = await context.Utilities.SingleAsync(item => item.Name == MailTemplateKeys.ForgotPassword);
            context.Utilities.Remove(template);
            await context.SaveChangesAsync();
        });

        var result = await RunSeederAsync();

        result.CreatedKeys.Should().BeEquivalentTo(new[] { MailTemplateKeys.ForgotPassword });
        (await GetStoredTemplatesAsync())[MailTemplateKeys.ForgotPassword]
            .Should().Be(MailTemplateCatalog.GetBody(MailTemplateKeys.ForgotPassword));
    }

    /// <summary>
    /// Faz 1.5'teki kodlama tuzagi burada da gecerli: sablon metni
    /// veritabanina yazilip geri okunurken Turkce karakterler bozulmamali.
    /// </summary>
    [Fact]
    public async Task Turkish_characters_should_survive_the_round_trip()
    {
        var stored = await GetStoredTemplatesAsync();

        foreach (var key in MailTemplateKeys.All)
        {
            stored[key].Should().Contain("geçerlidir", key);
        }
    }

    /// <summary>
    /// MailService'in sablonu okuyan yolu; seeder ile arasindaki anahtar
    /// uyusmazligi burada gorunur.
    /// </summary>
    [Fact]
    public async Task Mail_service_lookup_should_find_every_declared_template()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var utilityRepository = services.GetRequiredService<Application.Repositories.IUtilityRepository>();

            foreach (var key in MailTemplateKeys.All)
            {
                Utility? template = await utilityRepository.GetByNameAsync(key);
                template.Should().NotBeNull(key);
            }
        });
    }

    private Task<MailTemplateSeedResult> RunSeederAsync() =>
        Factory.ExecuteScopeAsync(services => services.GetRequiredService<IMailTemplateSeeder>().SeedAsync(CancellationToken.None));

    private Task<Dictionary<string, string>> GetStoredTemplatesAsync() =>
        Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Utilities.AsNoTracking()
                .ToDictionaryAsync(utility => utility.Name, utility => utility.Value));
}
