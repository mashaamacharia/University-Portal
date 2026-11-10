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

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] int top = 10, CancellationToken ct = default)
    {
        try
        {
            return Ok(await businessCentralClient.GetCustomersAsync(top, 0, ct));
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new { error = "The top query parameter must be greater than zero." });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { error = "Could not reach Business Central", detail = ex.Message });
        }
    }

    [HttpGet("customers/{number}/financial-details")]
    public async Task<IActionResult> GetFinancialDetails(string number, CancellationToken ct)
    {
        try
        {
            var details = await businessCentralClient.GetCustomerFinancialDetailsAsync(number, ct);
            return details is null ? NotFound() : Ok(details);
        }
        catch (ArgumentException)
        {
            return BadRequest(new { error = "A customer number is required." });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { error = "Could not reach Business Central", detail = ex.Message });
        }
    }

    [HttpGet("customers/{number}/invoices")]
    public async Task<IActionResult> GetInvoices(
        string number,
        [FromQuery] int top = 20,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await businessCentralClient.GetPostedSalesInvoicesAsync(number, top, ct));
        }
        catch (ArgumentOutOfRangeException)
        {
            return BadRequest(new { error = "The top query parameter must be greater than zero." });
        }
        catch (ArgumentException)
        {
            return BadRequest(new { error = "A customer number is required." });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { error = "Could not reach Business Central", detail = ex.Message });
        }
    }

    [HttpGet("invoices/{invoiceId:guid}/lines")]
    public async Task<IActionResult> GetInvoiceLines(Guid invoiceId, CancellationToken ct)
    {
        try
        {
            return Ok(await businessCentralClient.GetSalesInvoiceLinesAsync(invoiceId, ct));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { error = "Could not reach Business Central", detail = ex.Message });
        }
    }
}
