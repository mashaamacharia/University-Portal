using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Models;
using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance.Controllers;

[ApiController]
[Route("api/officer/finance")]
[Authorize(Roles = Roles.FinanceOfficer)]
public class PaymentRecordingController(IPaymentRecordingService payments) : ControllerBase
{
    [HttpPost("payments")]
    public async Task<IActionResult> Record(RecordPaymentRequest request, CancellationToken ct) =>
        ToActionResult(await payments.RecordAsync(request, ct));

    [HttpGet("payment-requests/{id:guid}")]
    public async Task<ActionResult<PaymentResultDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await payments.GetAsync(id, ct));

    [HttpPost("payment-requests/{id:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(Guid id, CancellationToken ct) =>
        ToActionResult(await payments.ReconcileAsync(id, ct));

    private IActionResult ToActionResult(PaymentResultDto result) => result.Status switch
    {
        PaymentRequestStatus.Posted => Ok(result),
        PaymentRequestStatus.Failed => Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Payment not recorded",
            detail: result.Message),
        _ => Accepted(result)
    };
}
