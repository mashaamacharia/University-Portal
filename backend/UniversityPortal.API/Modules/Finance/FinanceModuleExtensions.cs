using UniversityPortal.API.Modules.Finance.Configuration;
using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance;

public static class FinanceModuleExtensions
{
    public static IServiceCollection AddFinanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FinanceOptions>(configuration.GetSection(FinanceOptions.SectionName));
        services.AddOptions<PaymentOptions>()
            .Bind(configuration.GetSection(PaymentOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.MethodBatches.Count > 0 &&
                    options.MethodBatches.All(pair =>
                        !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value)) &&
                    options.MethodBatches.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() ==
                        options.MethodBatches.Count,
                "Payments:MethodBatches must list at least one payment method and journal batch.")
            .ValidateOnStart();
        services.AddScoped<IStudentFinanceService, StudentFinanceService>();
        services.AddScoped<IOfficerFinanceService, OfficerFinanceService>();
        services.AddScoped<IInvoiceWorkflowService, InvoiceWorkflowService>();
        services.AddScoped<IPaymentRecordingService, PaymentRecordingService>();
        return services;
    }
}
