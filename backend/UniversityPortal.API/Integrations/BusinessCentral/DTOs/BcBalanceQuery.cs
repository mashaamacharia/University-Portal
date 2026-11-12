namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public record BcBalanceQuery(decimal MinBalance, bool OverdueOnly, int Skip, int Top);
