using System.Net.Http.Json;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public class BusinessCentralClient(HttpClient http) : IBusinessCentralClient
{
    public async Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<BcListResponse<BcCompanyDto>>("companies", ct);
        return response?.Value ?? [];
    }
}
