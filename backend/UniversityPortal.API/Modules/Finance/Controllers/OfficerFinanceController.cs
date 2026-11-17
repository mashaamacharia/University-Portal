using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance.Controllers;

[ApiController]
[Route("api/officer/finance")]
[Authorize(Roles = Roles.FinanceOfficer)]
public class OfficerFinanceController : ControllerBase
{
    private readonly IOfficerFinanceService _finance;
    private readonly IInvoiceWorkflowService _workflow;

    public OfficerFinanceController(IOfficerFinanceService finance, IInvoiceWorkflowService workflow)
    {
        _finance = finance;
        _workflow = workflow;
    }

    [HttpGet("invoices")]
    public async Task<ActionResult<PagedResult<OfficerInvoiceDto>>> GetInvoices(
        [FromQuery] OfficerInvoiceQuery query,
        CancellationToken ct) =>
        Ok(await _finance.GetInvoicesAsync(query, ct));

    [HttpGet("invoices/{bcInvoiceId:guid}")]
    public async Task<ActionResult<OfficerInvoiceDetailDto>> GetInvoiceForReview(
        Guid bcInvoiceId,
        CancellationToken ct) =>
        Ok(await _workflow.GetInvoiceAsync(bcInvoiceId, ct));

    [HttpPost("invoices")]
    public async Task<ActionResult<OfficerInvoiceDetailDto>> CreateInvoice(
        CreateInvoiceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await _workflow.CreateDraftAsync(request, idempotencyKey, ct);
        return result.Created
            ? CreatedAtAction(
                nameof(GetInvoiceForReview),
                new { bcInvoiceId = result.Invoice.BcInvoiceId },
                result.Invoice)
            : Ok(result.Invoice);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<OfficerDashboardDto>> GetDashboard(CancellationToken ct) =>
        Ok(await _finance.GetDashboardAsync(ct));

    [HttpGet("outstanding-balances")]
    public async Task<ActionResult<PagedResult<OutstandingBalanceDto>>> GetOutstandingBalances(
        [FromQuery] OutstandingBalanceQuery query,
        CancellationToken ct) =>
        Ok(await _finance.GetOutstandingBalancesAsync(query, ct));

    [HttpGet("payments")]
    public async Task<ActionResult<PagedResult<OfficerPaymentDto>>> GetPayments(
        [FromQuery] OfficerPaymentQuery query,
        CancellationToken ct) =>
        Ok(await _finance.GetPaymentsAsync(query, ct));
}
