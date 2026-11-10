using Microsoft.Extensions.Options;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;

namespace UniversityPortal.API.Integrations.BusinessCentral.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessCentralIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BusinessCentralOptions>()
            .Bind(configuration.GetSection(BusinessCentralOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "BusinessCentral:BaseUrl must be an absolute HTTP or HTTPS URL.")
            .Validate(
                options => options.TimeoutSeconds > 0,
                "BusinessCentral:TimeoutSeconds must be greater than zero.")
            .Validate(
                options => string.Equals(options.AuthMode, "Windows", StringComparison.OrdinalIgnoreCase),
                "BusinessCentral:AuthMode currently supports only Windows.")
            .ValidateOnStart();

        services.AddHttpClient<IBusinessCentralClient, BusinessCentralClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<BusinessCentralOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
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

        return services;
    }
}
