using buduns_server.Application;
using buduns_server.Infrastructure;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Abstractions.Services;
using buduns_server.Persistence;
using buduns_server.WebAPI.Http;
using buduns_server.WebAPI.Models;
using buduns_server.WebAPI.Configurations.Serilog.ColumnWriters;
using buduns_server.WebAPI.Configurations.RateLimiting;
using buduns_server.WebAPI.Filters;
using buduns_server.WebAPI.Middlewares;
using buduns_server.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace buduns_server.WebAPI
{
    public class Program
    {
        private const string CorsPolicyName = "BudunsCorsPolicy";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<RolePermissionFilter>();
            });

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = ApiValidationProblemFactory.Create;
            });

            builder.Services.AddScoped<IClientContext, HttpClientContext>();

            builder.Services.Configure<SensitiveEndpointRateLimitOptions>(
                builder.Configuration.GetSection("SensitiveEndpointRateLimit"));

            builder.Services.Configure<ReportPolicyOptions>(
                builder.Configuration.GetSection(ReportPolicyOptions.SectionName));

            builder.Services.Configure<BootstrapAdminOptions>(
                builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));

            builder.Services.Configure<MailOptions>(
                builder.Configuration.GetSection(MailOptions.SectionName));

            builder.Services.AddOptions<JwtTokenOptions>()
                .Bind(builder.Configuration.GetSection(JwtTokenOptions.SectionName))
                .Validate(options => !string.IsNullOrWhiteSpace(options.SecurityKey), "'Token:SecurityKey' yapilandirmasi zorunlu.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "'Token:Audience' yapilandirmasi zorunlu.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "'Token:Issuer' yapilandirmasi zorunlu.")
                .ValidateOnStart();


            #region CORS
            builder.Services.AddCors();
            builder.Services.AddOptions<CorsOptions>().Configure<IConfiguration>((corsOptions, configuration) =>
            {
                var allowedOrigins = configuration
                    .GetSection("Cors:AllowedOrigins")
                    .Get<string[]>() ?? Array.Empty<string>();

                if (allowedOrigins.Length == 0)
                {
                    Log.Warning("CORS icin izin verilen origin tanimlanmamis. Cross-origin istekler reddedilecek. Cors:AllowedOrigins ayarini kontrol edin.");
                }

                corsOptions.AddPolicy(CorsPolicyName, policy =>
                {
                    // Origin tanimli degilse politika bos kalir; sessizce
                    // herkese acik hale gelmemesi icin.
                    if (allowedOrigins.Length == 0)
                    {
                        return;
                    }

                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
            #endregion

            #region Serilog
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .WriteTo.File(
                    path: "logs/buduns-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10_000_000,
                    rollOnFileSizeLimit: true,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/errors/error-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 60,
                    restrictedToMinimumLevel: LogEventLevel.Error)
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);

            var logConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(logConnectionString))
            {
                loggerConfiguration.WriteTo.PostgreSQL(logConnectionString, "logs", needAutoCreateTable: true,
                    columnOptions: new Dictionary<string, ColumnWriterBase>
                    {
                        {"message", new RenderedMessageColumnWriter() },
                        {"message_template", new MessageTemplateColumnWriter() },
                        {"level", new LevelColumnWriter() },
                        {"time_stamp", new TimestampColumnWriter() },
                        {"exception", new ExceptionColumnWriter() },
                        {"log_event", new LogEventSerializedColumnWriter() },
                        {"user_name", new UsernameColumnWriter() }
                    },
                    restrictedToMinimumLevel: LogEventLevel.Warning);
            }

            var seqServerUrl = builder.Configuration["Seq:ServerURL"];
            if (!string.IsNullOrWhiteSpace(seqServerUrl))
            {
                loggerConfiguration.WriteTo.Seq(seqServerUrl);
            }

            Logger log = loggerConfiguration.CreateLogger();

            Log.Logger = log;

            builder.Host.UseSerilog(log);
            #endregion

            #region Swagger
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "budunsAPI",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "JWT Authorization header kullanımı: token"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
                {
                    {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
                });
            });
            #endregion

            builder.Services.AddPersistenceService(builder.Configuration);
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddApplicationService();

            #region Authentication-Authorization
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new()
                    {
                        // Dogrulanacak degerler
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        // Audience/Issuer/IssuerSigningKey asagida IOptions uzerinden
                        // atanir; token'i ureten TokenHandler ile ayni kaynagi okumalari
                        // icin yapilandirmanin burada erken okunmamasi gerekiyor.
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userIdValue = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                                ?? context.Principal?.FindFirst("sub")?.Value;
                            var sessionIdValue = context.Principal?.FindFirst("sid")?.Value
                                ?? context.Principal?.FindFirst(ClaimTypes.Sid)?.Value;

                            if (!int.TryParse(userIdValue, out var userId) ||
                                !Guid.TryParse(sessionIdValue, out var sessionId))
                            {
                                context.Fail("Geçerli kullanıcı veya oturum bilgisi bulunamadı.");
                                return;
                            }

                            var authSessionService = context.HttpContext.RequestServices
                                .GetRequiredService<IAuthSessionService>();
                            var isActive = await authSessionService.IsSessionActiveAsync(
                                userId,
                                sessionId,
                                context.HttpContext.RequestAborted);

                            if (!isActive)
                            {
                                context.Fail("Oturum geçersiz veya süresi dolmuş.");
                            }
                        }
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme
                )
                .RequireAuthenticatedUser()
                .Build();
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

            // Token dogrulama degerleri, tum yapilandirma kaynaklari birlestikten
            // sonra IOptions uzerinden atanir.
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtTokenOptions>>((jwtBearerOptions, tokenOptions) =>
                {
                    var token = tokenOptions.Value;
                    jwtBearerOptions.TokenValidationParameters.ValidAudience = token.Audience;
                    jwtBearerOptions.TokenValidationParameters.ValidIssuer = token.Issuer;
                    jwtBearerOptions.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(token.SecurityKey));
                });
            #endregion

            var app = builder.Build();

            SeedRoles(app);

            SeedEndpoints(app);

            SeedMailTemplates(app);

            WarnIfMailIsNotConfigured(app);

            SeedBootstrapAdmin(app);

            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    var userName = httpContext.User?.Identity?.IsAuthenticated == true
                        ? httpContext.User.Identity.Name
                        : null;

                    if (!string.IsNullOrWhiteSpace(userName))
                    {
                        diagnosticContext.Set("user_name", userName);
                    }
                };
            });

            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseApiStatusCodePages();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseCors(CorsPolicyName);

            app.UseMiddleware<SensitiveEndpointRateLimitMiddleware>();

            app.UseAuthentication();

            app.UseMiddleware<UserNameLogContextMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Sistem rolleri olmadan kayit ve yetkilendirme calismaz; eksikse
        /// uygulama sessizce ayakta kalmak yerine acilmadan hata verir.
        /// Semayi kurmak seeder'in isi degil, migration zaten uygulanmis olmali.
        /// </summary>
        private static void SeedRoles(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IRoleSeeder>();

            try
            {
                seeder.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Sistem rolleri olusturulamadi, uygulama baslatilmiyor.");
                throw;
            }
        }

        private static void SeedEndpoints(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IEndpointSeeder>();

            try
            {
                var result = seeder.SeedAsync(typeof(Program), CancellationToken.None).GetAwaiter().GetResult();

                if (result.HasChanges)
                {
                    Log.Information(
                        "Yetki katalogu esitlendi. CreatedMenus: {CreatedMenus}, CreatedEndpoints: {CreatedEndpoints}, UpdatedEndpoints: {UpdatedEndpoints}",
                        result.CreatedMenuCount,
                        result.CreatedEndpointCount,
                        result.UpdatedEndpointCount);
                }

                if (result.OrphanCodes.Count > 0)
                {
                    Log.Warning(
                        "Veritabaninda kodda karsiligi olmayan yetki kaydi var. Bir uc kaldirilmis ya da tanimi degisip kodu kaymis olabilir. OrphanCount: {OrphanCount}, OrphanCodes: {OrphanCodes}",
                        result.OrphanCodes.Count,
                        string.Join(", ", result.OrphanCodes));
                }
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Yetki katalogu esitlenemedi, uygulama baslatilmiyor.");
                throw;
            }
        }

        private static void SeedMailTemplates(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IMailTemplateSeeder>();

            try
            {
                var result = seeder.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();

                if (result.CreatedKeys.Count > 0)
                {
                    Log.Information("Eksik mail sablonlari olusturuldu: {CreatedKeys}", string.Join(", ", result.CreatedKeys));
                }

                if (result.DivergedKeys.Count > 0)
                {
                    Log.Warning(
                        "Veritabanindaki mail sablonu koddakinden farkli. Icerik ezilmedi; veritabanindaki surum kullanilmaya devam edecek. DivergedKeys: {DivergedKeys}",
                        string.Join(", ", result.DivergedKeys));
                }
            }
            catch (Exception exception)
            {
                Log.Fatal(exception, "Mail sablonlari olusturulamadi, uygulama baslatilmiyor.");
                throw;
            }
        }

        /// <summary>
        /// SMTP yapilandirmasi zorunlu tutulmaz: hesabi olmayan bir gelistirici
        /// uygulamayi calistirabilmeli. Ama sessiz de kalmamali; eksiklik aksi
        /// halde ancak ilk mail gonderiminde, istegin ortasinda ortaya cikar.
        /// </summary>
        private static void WarnIfMailIsNotConfigured(WebApplication app)
        {
            var mailOptions = app.Services.GetRequiredService<IOptions<MailOptions>>().Value;

            if (mailOptions.IsConfigured)
            {
                return;
            }

            Log.Warning(
                "SMTP yapilandirmasi eksik; dogrulama ve sifre sifirlama mailleri gonderilemeyecek. MissingSettings: {MissingSettings}",
                string.Join(", ", mailOptions.GetMissingSettings()));
        }

        private static void SeedBootstrapAdmin(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<IAdminSeeder>();

            try
            {
                seeder.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Bootstrap admin yukseltmesi yapilamadi.");
            }
        }
    }
}
