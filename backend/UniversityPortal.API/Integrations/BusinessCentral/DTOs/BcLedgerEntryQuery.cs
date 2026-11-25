namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public record BcLedgerEntryQuery(
    string? CustomerNumber,
    IReadOnlyList<string>? DocumentTypes,
    DateOnly? From,
    DateOnly? To,
    int Skip,
    int Top,
    string? ExternalDocumentNumber = null,
    string? DocumentNumber = null,
    int? ClosedByEntryNumber = null);
