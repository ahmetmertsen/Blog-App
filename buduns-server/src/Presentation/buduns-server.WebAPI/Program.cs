using buduns_server.Application;
using buduns_server.Infrastructure;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Abstractions.Services;
using buduns_server.Persistence;
using buduns_server.WebAPI.Configurations.Serilog.ColumnWriters;
using buduns_server.WebAPI.Configurations.RateLimiting;
using buduns_server.WebAPI.Filters;
using buduns_server.WebAPI.Middlewares;
using buduns_server.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<RolePermissionFilter>();
            });
            builder.Services.AddScoped<IClientContext, HttpClientContext>();
            builder.Services.Configure<SensitiveEndpointRateLimitOptions>(
                builder.Configuration.GetSection("SensitiveEndpointRateLimit"));
            builder.Services.Configure<ReportPolicyOptions>(
                builder.Configuration.GetSection(ReportPolicyOptions.SectionName));
            
            #region CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowVueDev", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:8080",
                            "http://127.0.0.1:8080"
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
            #endregion

            #region Serilog
            Logger log = new LoggerConfiguration()
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
                .WriteTo.PostgreSQL(builder.Configuration.GetConnectionString("DefaultConnection"), "logs", needAutoCreateTable:true,
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
                    restrictedToMinimumLevel: LogEventLevel.Warning
                )
                .WriteTo.Seq(builder.Configuration["Seq:ServerURL"])
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .CreateLogger();

            builder.Host.UseSerilog(log);
            #endregion

            #region Swagger
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "blogAppAPI",
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

                        ValidAudience = builder.Configuration["Token:Audience"],
                        ValidIssuer = builder.Configuration["Token:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:SecurityKey"])),
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
            #endregion

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            // app.UseHttpsRedirection();
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseCors("AllowVueDev");

            app.UseAuthentication();

            app.UseMiddleware<UserNameLogContextMiddleware>();

            app.UseSerilogRequestLogging();

            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseMiddleware<SensitiveEndpointRateLimitMiddleware>();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
