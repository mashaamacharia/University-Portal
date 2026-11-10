namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcCompanyDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
