using UniversityPortal.API.Integrations.BusinessCentral.DTOs;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public interface IBusinessCentralClient
{
    Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default);
}
