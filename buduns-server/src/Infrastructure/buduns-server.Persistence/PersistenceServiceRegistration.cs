using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Persistence.Context;
using buduns_server.Persistence.Repositories;
using buduns_server.Persistence.Services;
using buduns_server.Persistence.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Persistence
{
    public static class PersistenceServiceRegistration
    {
        public static IServiceCollection AddPersistenceService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BudunsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<User, Role>(options =>
                {
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<BudunsDbContext>()
                .AddDefaultTokenProviders(); // Password reset vs. tokenları için

            services.AddScoped<IBookmarkRepository, BookmarkRepository>();
            services.AddScoped<ICommentRepository, CommentRepository>();
            services.AddScoped<IFollowerRepository, FollowerRepository>();
            services.AddScoped<ILikeRepository, LikeRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IUtilityRepository, UtilityRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<IModerationActionRepository, ModerationActionRepository>();
            services.AddScoped<IMenuRepository, MenuRepository>();
            services.AddScoped<IEndpointRepository, EndpointRepository>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAuthSessionService, AuthSessionService>();
            services.AddScoped<IVerificationChallengeService, VerificationChallengeService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IRoleSeeder, RoleSeeder>();
            services.AddScoped<IAdminSeeder, AdminSeeder>();
            services.AddScoped<IAuthorizationEndpointService, AuthorizationEndpointService>();

            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();


            return services;
        }
    }
}
