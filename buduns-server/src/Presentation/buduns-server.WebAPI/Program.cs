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
                    Description = "JWT Authorization header kullan�m�: token"
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
                        // Do�rulamas� gereken de�erler
                        ValidateAudience = true, //Olu�turulacak token de�erini kimlerin/hangi originlerin/sitelerin kullan�c���n� belirledi�imiz de�erdir.
                        ValidateIssuer = true, // Olu�turulacak token de�erini kimin da��tt�n� ifade edece�imiz alan�d�r.
                        ValidateLifetime = true, //Olu�turulan token de�erinin s�resini kontrol edecek olan do�rulamad�r.
                        ValidateIssuerSigningKey = true, //�retilecek token de�erinin uygulamam�za ait bir de�er oldu�unu ifade eden security key verisinin do�rulanmas�d�r.

                        // Audience/Issuer/IssuerSigningKey asagida IOptions uzerinden
                        // atanir; token'i ureten TokenHandler ile ayni kaynagi okumalari
                        // icin yapilandirmanin burada erken okunmamasi gerekiyor.
                        NameClaimType = ClaimTypes.Name, //Jwt �zerinden gelen Name claimine kar��l�k gelen de�eri User.Identity.Name propertysinden elde edilir.
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
                                context.Fail("Ge�erli kullan�c� veya oturum bilgisi bulunamad�.");
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
                                context.Fail("Oturum ge�ersiz veya s�resi dolmu�.");
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
    }
}
