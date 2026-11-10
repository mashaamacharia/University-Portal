using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;

namespace UniversityPortal.API.Common.Health;

[ApiController]
[Route("api/diagnostics/business-central")]
public class BusinessCentralDiagnosticsController(IBusinessCentralClient businessCentralClient) : ControllerBase
{
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        try
        {
            return Ok(await businessCentralClient.GetCompaniesAsync(ct));
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { error = "Could not reach Business Central", detail = ex.Message });
        }
    }
}
