using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;

namespace UniversityPortal.API.Integrations.BusinessCentral.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessCentralIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(BusinessCentralOptions.SectionName);

        services.AddOptions<BusinessCentralOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(
                options => options.CompanyId != Guid.Empty,
                "BusinessCentral:CompanyId must be set to the id of the BC company.")
            .Validate(
                options => options.AuthMode == "Windows",
                "BusinessCentral:AuthMode must be 'Windows' (the only mode supported so far).")
            .ValidateOnStart();

        var attemptTimeout = Math.Clamp(
            section.Get<BusinessCentralOptions>()?.TimeoutSeconds ?? 30,
            1,
            120);
        const int retries = 2;

        services.AddTransient<BusinessCentralLoggingHandler>();

        var http = services.AddHttpClient<IBusinessCentralClient, BusinessCentralClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BusinessCentralOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        http.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BusinessCentralOptions>>().Value;
            return new HttpClientHandler
            {
                UseDefaultCredentials = string.Equals(
                    options.AuthMode,
                    "Windows",
                    StringComparison.OrdinalIgnoreCase)
            };
        });

        http.AddStandardResilienceHandler(options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(attemptTimeout);
            options.Retry.MaxRetryAttempts = retries;
            options.Retry.DisableForUnsafeHttpMethods();
            options.TotalRequestTimeout.Timeout =
                TimeSpan.FromSeconds(attemptTimeout * (retries + 1) + 10);
            options.CircuitBreaker.SamplingDuration =
                TimeSpan.FromSeconds(attemptTimeout * 2 + 5);
        });

        http.AddHttpMessageHandler<BusinessCentralLoggingHandler>();

        return services;
    }
}
