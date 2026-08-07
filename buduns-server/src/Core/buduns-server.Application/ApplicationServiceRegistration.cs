using buduns_server.Application.Common.Behaviors;
using buduns_server.Application.Features.Bookmarks.Commands.Create;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplicationService(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssemblyContaining<CreateBookmarksCommand>();
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(CurrentUserBehavior<,>));
                cfg.AddOpenBehavior(typeof(AccountStatusBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

            return services;
        }
    }
}
