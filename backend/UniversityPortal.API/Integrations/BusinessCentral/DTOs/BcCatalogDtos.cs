namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcItemDto
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public bool Blocked { get; set; }

    public decimal UnitPrice { get; set; }
}

public class BcAccountDto
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool DirectPosting { get; set; }

    public bool Blocked { get; set; }
}
