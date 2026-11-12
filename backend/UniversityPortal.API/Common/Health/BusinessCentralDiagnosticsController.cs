using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;

namespace UniversityPortal.API.Common.Health;

[ApiController]
[Route("api/diagnostics/business-central")]
public class BusinessCentralDiagnosticsController : ControllerBase
{
    private readonly IBusinessCentralClient _bc;

    public BusinessCentralDiagnosticsController(IBusinessCentralClient bc)
    {
        _bc = bc;
    }

    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies(CancellationToken ct) =>
        Ok(await _bc.GetCompaniesAsync(ct));

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers([FromQuery] int top = 10, CancellationToken ct = default) =>
        Ok(await _bc.GetCustomersAsync(top, 0, ct));

    [HttpGet("customers/{number}/financial-details")]
    public async Task<IActionResult> GetFinancialDetails(string number, CancellationToken ct)
    {
        var details = await _bc.GetCustomerFinancialDetailsAsync(number, ct);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("customers/{number}/invoices")]
    public async Task<IActionResult> GetInvoices(
        string number,
        [FromQuery] int top = 20,
        CancellationToken ct = default) =>
        Ok(await _bc.GetPostedSalesInvoicesAsync(number, top, ct));

    [HttpGet("invoices/{invoiceId:guid}/lines")]
    public async Task<IActionResult> GetInvoiceLines(Guid invoiceId, CancellationToken ct) =>
        Ok(await _bc.GetSalesInvoiceLinesAsync(invoiceId, ct));
}
