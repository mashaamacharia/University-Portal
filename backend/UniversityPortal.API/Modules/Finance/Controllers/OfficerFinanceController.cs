using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance.Controllers;

[ApiController]
[Route("api/officer/finance")]
public class OfficerFinanceController : ControllerBase
{
    private readonly IOfficerFinanceService _finance;

    public OfficerFinanceController(IOfficerFinanceService finance)
    {
        _finance = finance;
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResult<OfficerInvoiceDto>>> GetInvoices(
        [FromQuery] OfficerInvoiceQuery query,
        CancellationToken ct) =>
        Ok(await _finance.GetInvoicesAsync(query, ct));

    [HttpGet("dashboard")]
    public async Task<ActionResult<OfficerDashboardDto>> GetDashboard(CancellationToken ct) =>
        Ok(await _finance.GetDashboardAsync(ct));

    [HttpGet("outstanding-balances")]
    public async Task<ActionResult<PagedResult<OutstandingBalanceDto>>> GetOutstandingBalances(
        [FromQuery] OutstandingBalanceQuery query,
        CancellationToken ct) =>
        Ok(await _finance.GetOutstandingBalancesAsync(query, ct));
}
