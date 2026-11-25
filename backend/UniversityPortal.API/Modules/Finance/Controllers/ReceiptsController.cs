using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance.Controllers;

[ApiController]
[Route("api/student/finance/receipts")]
[Authorize(Roles = Roles.Student)]
public class StudentReceiptsController : ControllerBase
{
    private readonly IReceiptService _receipts;

    public StudentReceiptsController(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    [HttpGet("{receiptNumber}")]
    public async Task<ActionResult<ReceiptDto>> Get(
        [FromRoute, Required, StringLength(30, MinimumLength = 1)] string receiptNumber,
        CancellationToken ct) =>
        Ok(await _receipts.GetForStudentAsync(receiptNumber, ct));

    [HttpPost("{receiptNumber}/print")]
    public async Task<ActionResult<ReceiptDto>> Print(
        [FromRoute, Required, StringLength(30, MinimumLength = 1)] string receiptNumber,
        CancellationToken ct) =>
        Ok(await _receipts.PrintForStudentAsync(receiptNumber, ct));
}

[ApiController]
[Route("api/officer/finance/receipts")]
[Authorize(Roles = Roles.FinanceOfficer)]
public class OfficerReceiptsController : ControllerBase
{
    private readonly IReceiptService _receipts;

    public OfficerReceiptsController(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    [HttpGet("{receiptNumber}")]
    public async Task<ActionResult<ReceiptDto>> Get(
        [FromRoute, Required, StringLength(30, MinimumLength = 1)] string receiptNumber,
        [FromQuery, Required, StringLength(20, MinimumLength = 1)] string studentNumber,
        CancellationToken ct) =>
        Ok(await _receipts.GetForOfficerAsync(studentNumber, receiptNumber, ct));

    [HttpPost("{receiptNumber}/print")]
    public async Task<ActionResult<ReceiptDto>> Print(
        [FromRoute, Required, StringLength(30, MinimumLength = 1)] string receiptNumber,
        [FromQuery, Required, StringLength(20, MinimumLength = 1)] string studentNumber,
        CancellationToken ct) =>
        Ok(await _receipts.PrintForOfficerAsync(studentNumber, receiptNumber, ct));
}
