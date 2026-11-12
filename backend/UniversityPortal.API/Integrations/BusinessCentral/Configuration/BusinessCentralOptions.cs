using System.ComponentModel.DataAnnotations;

namespace UniversityPortal.API.Integrations.BusinessCentral.Configuration;

public class BusinessCentralOptions
{
    public const string SectionName = "BusinessCentral";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string AuthMode { get; set; } = "Windows";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;

    public Guid CompanyId { get; set; }
}
