using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance;

public static class FinanceModuleExtensions
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddScoped<IStudentFinanceService, StudentFinanceService>();
        services.AddScoped<IOfficerFinanceService, OfficerFinanceService>();
        return services;
    }
}
