using buduns_server.Application.Abstractions.Services;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Infrastructure.Services.Caching;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Respawn.Graph;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace buduns_server.IntegrationTests.Fixtures;

public sealed class BudunsWebApplicationFactory : WebApplicationFactory<WebAPI.Program>, IAsyncLifetime
{
    private const string TestSecurityKey = "integration-test-security-key-at-least-thirty-two-characters";

    /// <summary>
    /// Testlerde CORS politikasinin izin verdigi tek origin. appsettings.json
    /// icindeki Cors:AllowedOrigins bos oldugu icin buradan veriliyor.
    /// </summary>
    public const string AllowedCorsOrigin = "https://allowed.integration.test";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").WithDatabase("buduns_integration_tests").WithUsername("postgres").WithPassword("postgres").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private Respawner? _respawner;

    public string ConnectionString => _postgres.GetConnectionString();

    public string RedisConnectionString => _redis.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            ["ConnectionStrings:Redis"] = RedisConnectionString,
            ["Cache:InstanceName"] = "buduns-tests:",
            ["Token:Audience"] = "buduns-integration-tests",
            ["Token:Issuer"] = "buduns-integration-tests",
            ["Token:SecurityKey"] = TestSecurityKey,
            ["Token:AccessTokenExpirationMinutes"] = "15",
            ["Token:RefreshTokenExpirationDays"] = "30",
            ["Seq:ServerURL"] = "http://127.0.0.1:5341",
            ["Cors:AllowedOrigins:0"] = AllowedCorsOrigin
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<BudunsDbContext>>();
            services.RemoveAll<IMailService>();
            services.AddDbContext<BudunsDbContext>(options => options.UseNpgsql(ConnectionString));
            services.AddSingleton<IMailService, TestMailService>();

            // Onbellek kaydi, yapilandirmanin uygulama kaydina yetisip
            // yetismedigine bagli kalmasin diye burada acikca test
            // container'ina baglaniyor.
            services.RemoveAll<ICacheService>();
            services.RemoveAll<IDistributedCache>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = ConfigurationOptions.Parse(RedisConnectionString);
                options.InstanceName = "buduns-tests:";
            });
            services.AddSingleton<ICacheService, DistributedCacheService>();
        });
    }

    public async Task InitializeAsync()
    {
        // Konteynerler, Services'e ilk erisimde host kurulmadan once ayakta
        // olmali; baglanti dizeleri o anda okunuyor.
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BudunsDbContext>();
        await context.Database.MigrateAsync();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new[] { new Table("__EFMigrationsHistory"), new Table("logs") },
            WithReseed = true
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
        Dispose();
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    /// <summary>
    /// Veritabanini ve onbellegi bosaltir. Onbellek singleton oldugu icin
    /// testler arasi yasar; temizlenmezse bir testin yazdigi liste digerine
    /// sizar.
    /// </summary>
    public async Task ResetStateAsync()
    {
        if (_respawner == null)
        {
            throw new InvalidOperationException("Integration test database has not been initialized.");
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        await ClearCacheAsync();
        await ExecuteScopeAsync(DatabaseSeeder.SeedSystemRolesAsync);
    }

    public async Task ClearCacheAsync()
    {
        var result = await _redis.ExecAsync(new[] { "redis-cli", "FLUSHALL" });
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Redis could not be flushed. {result.Stderr}");
        }
    }

    public async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    public async Task<T> ExecuteScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider);
    }
}
