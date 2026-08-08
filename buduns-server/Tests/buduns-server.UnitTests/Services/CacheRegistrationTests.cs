using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Options;
using buduns_server.Infrastructure;
using buduns_server.Infrastructure.Services.Caching;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Entegrasyon testleri onbellek kaydini kendi container'ina yonlendirmek icin
/// eziyor; dolayisiyla uygulamanin kendi kayit yolu orada dogrulanmiyor. Bu
/// testler o boslugu kapatir: yapilandirma varken Redis'e, yokken NullCache'e
/// dusuldugunu garanti eder.
/// </summary>
public class CacheRegistrationTests
{
    [Fact]
    public void AddInfrastructureServices_WithoutRedisConnectionString_ShouldRegisterNullCache()
    {
        using var provider = BuildProvider(redisConnectionString: null);

        Assert.IsType<NullCacheService>(provider.GetRequiredService<ICacheService>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddInfrastructureServices_WithBlankRedisConnectionString_ShouldRegisterNullCache(string redisConnectionString)
    {
        using var provider = BuildProvider(redisConnectionString);

        Assert.IsType<NullCacheService>(provider.GetRequiredService<ICacheService>());
    }

    [Fact]
    public void AddInfrastructureServices_WithRedisConnectionString_ShouldRegisterDistributedCache()
    {
        using var provider = BuildProvider("localhost:6379");

        Assert.IsType<DistributedCacheService>(provider.GetRequiredService<ICacheService>());

        var redisOptions = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        Assert.Equal("buduns-test:", redisOptions.InstanceName);
        Assert.NotNull(redisOptions.ConfigurationOptions);

        // Redis acilista ayakta olmayabilir; istemci baglanamadiginda uygulama
        // patlamamali.
        Assert.False(redisOptions.ConfigurationOptions!.AbortOnConnectFail);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldBindCacheOptions()
    {
        using var provider = BuildProvider("localhost:6379");

        var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        Assert.Equal("buduns-test:", cacheOptions.InstanceName);
        Assert.Equal(15, cacheOptions.DailyTopPostsTtlSeconds);
    }

    [Fact]
    public void CacheOptions_ShouldFallBackToDefaultsWhenSectionMissing()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServices(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();
        var cacheOptions = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        Assert.Equal("buduns:", cacheOptions.InstanceName);
        Assert.Equal(60, cacheOptions.DailyTopPostsTtlSeconds);
    }

    private static ServiceProvider BuildProvider(string? redisConnectionString)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Cache:InstanceName"] = "buduns-test:",
            ["Cache:DailyTopPostsTtlSeconds"] = "15"
        };

        if (redisConnectionString != null)
        {
            settings["ConnectionStrings:Redis"] = redisConnectionString;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        return services.BuildServiceProvider();
    }
}
