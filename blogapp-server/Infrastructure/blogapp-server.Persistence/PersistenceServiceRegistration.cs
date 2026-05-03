using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Repositories;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities.Identity;
using blogapp_server.Persistence.Context;
using blogapp_server.Persistence.Repositories;
using blogapp_server.Persistence.Services;
using blogapp_server.Persistence.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence
{
    public static class PersistenceServiceRegistration
    {
        public static IServiceCollection AddPersistenceService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BlogAppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<User, Role>()
                .AddEntityFrameworkStores<BlogAppDbContext>()
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

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();


            return services;
        }
    }
}
