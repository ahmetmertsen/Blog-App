using buduns_server.Application.Abstractions.Services;
using buduns_server.Domain.Entities.Identity;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

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
    private Respawner? _respawner;

    public string ConnectionString => _postgres.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString,
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
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
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
        await _postgres.DisposeAsync();
        Dispose();
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public async Task ResetDatabaseAsync()
    {
        if (_respawner == null)
        {
            throw new InvalidOperationException("Integration test database has not been initialized.");
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        await ExecuteScopeAsync(DatabaseSeeder.SeedSystemRolesAsync);
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
