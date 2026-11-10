namespace UniversityPortal.API.Integrations.BusinessCentral.Configuration;

public class BusinessCentralOptions
{
    public const string SectionName = "BusinessCentral";

    public string BaseUrl { get; set; } = string.Empty;

    public string AuthMode { get; set; } = "Windows";

    public int TimeoutSeconds { get; set; } = 30;

    public Guid CompanyId { get; set; }
}
