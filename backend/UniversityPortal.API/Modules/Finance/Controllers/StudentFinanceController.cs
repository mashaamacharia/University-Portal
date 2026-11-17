using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance.Controllers;

[ApiController]
[Route("api/student/finance")]
[Authorize(Roles = Roles.Student)]
public class StudentFinanceController : ControllerBase
{
    private readonly IStudentFinanceService _finance;

    public StudentFinanceController(IStudentFinanceService finance)
    {
        _finance = finance;
    }

    [HttpGet("statement")]
    public async Task<ActionResult<FeeStatementDto>> GetStatement(
        [FromQuery, Range(1, 100)] int top = 50,
        CancellationToken ct = default) =>
        Ok(await _finance.GetFeeStatementAsync(top, ct));

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceSummaryDto>> GetBalance(CancellationToken ct) =>
        Ok(await _finance.GetBalanceAsync(ct));

    [HttpGet("invoices")]
    public async Task<ActionResult<IReadOnlyList<InvoiceSummaryDto>>> GetInvoices(
        [FromQuery, Range(1, 100)] int top = 50,
        CancellationToken ct = default) =>
        Ok(await _finance.GetInvoicesAsync(top, ct));

    [HttpGet("invoices/{invoiceNumber}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetInvoice(string invoiceNumber, CancellationToken ct) =>
        Ok(await _finance.GetInvoiceAsync(invoiceNumber, ct));

    [HttpGet("payments")]
    public async Task<ActionResult<PagedResult<PaymentHistoryEntryDto>>> GetPayments(
        [FromQuery] PaymentHistoryQuery query,
        CancellationToken ct) =>
        Ok(await _finance.GetPaymentHistoryAsync(query, ct));
}
