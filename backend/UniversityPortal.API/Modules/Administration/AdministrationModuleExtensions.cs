using Microsoft.AspNetCore.Identity;
using UniversityPortal.API.Infrastructure.Persistence;
using UniversityPortal.API.Modules.Administration.Models;

namespace UniversityPortal.API.Modules.Administration;

public static class AdministrationModuleExtensions
{
    public static IServiceCollection AddAdministrationModule(this IServiceCollection services)
    {
        services
            .AddIdentityCore<PortalUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<PortalDbContext>();

        return services;
    }
}
