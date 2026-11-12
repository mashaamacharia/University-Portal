namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public record BcInvoiceQuery(
    string? CustomerNumber,
    IReadOnlyList<string>? Statuses,
    DateOnly? From,
    DateOnly? To,
    int Skip,
    int Top);
