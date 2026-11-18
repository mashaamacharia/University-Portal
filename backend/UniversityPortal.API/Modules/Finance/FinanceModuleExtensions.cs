using UniversityPortal.API.Modules.Finance.Services;
using UniversityPortal.API.Modules.Finance.Configuration;

namespace UniversityPortal.API.Modules.Finance;

public static class FinanceModuleExtensions
{
    public static IServiceCollection AddFinanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FinanceOptions>(configuration.GetSection(FinanceOptions.SectionName));
        services.AddScoped<IStudentFinanceService, StudentFinanceService>();
        services.AddScoped<IOfficerFinanceService, OfficerFinanceService>();
        services.AddScoped<IInvoiceWorkflowService, InvoiceWorkflowService>();
        return services;
    }
}
