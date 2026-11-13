using Microsoft.EntityFrameworkCore;

namespace UniversityPortal.API.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPortalPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PortalDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:PortalDb is not configured. Set it with 'dotnet user-secrets'.");

        services.AddDbContext<PortalDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
