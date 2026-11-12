namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public record BcPagedList<T>(IReadOnlyList<T> Items, int? TotalCount);
