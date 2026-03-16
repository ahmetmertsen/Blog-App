using blogapp_server.Application.Features.Bookmarks.Commands.Create;
using blogapp_server.Application.Mapping;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssemblyContaining<CreateBookmarksCommand>());
            services.AddAutoMapper(typeof(UserProfile).Assembly);

            return services;
        }
    }
}
