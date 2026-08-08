using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Abstractions.Services.Configurations;
using buduns_server.Application.Abstractions.Token;
using buduns_server.Application.Common.Options;
using buduns_server.Infrastructure.Services.Caching;
using buduns_server.Infrastructure.Services.Configurations;
using buduns_server.Infrastructure.Services.Mail;
using buduns_server.Infrastructure.Services.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace buduns_server.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITokenHandler, TokenHandler>();
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IApplicationService, ApplicationService>();

            services.AddCaching(configuration);

            return services;
        }

        private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

            var redisConnectionString = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                // Redis tanimli degilse uygulama onbelleksiz calisir. Lokal
                // gelistirmede Redis ayaga kaldirmak zorunlu olmasin diye.
                services.AddSingleton<ICacheService, NullCacheService>();
                return services;
            }

            var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

            services.AddStackExchangeRedisCache(options =>
            {
                var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);

                // Redis acilista ayakta degilse uygulama patlamasin; baglanti
                // geri geldiginde istemci kendi kendine toparlar.
                redisConfiguration.AbortOnConnectFail = false;
                redisConfiguration.ConnectRetry = 3;
                redisConfiguration.ConnectTimeout = 5000;

                options.ConfigurationOptions = redisConfiguration;
                options.InstanceName = cacheOptions.InstanceName;
            });

            services.AddSingleton<ICacheService, DistributedCacheService>();

            return services;
        }
    }
}
