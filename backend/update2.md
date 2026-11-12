
1. Shared pieces

Common/Constants/Roles.cs

csharp
namespace UniversityPortal.API.Common.Constants;

public static class Roles
{
    public const string Student = "Student";
    public const string FinanceOfficer = "FinanceOfficer";
    public const string Administrator = "Administrator";
}

Common/Responses/PagedResult.cs

csharp
namespace UniversityPortal.API.Common.Responses;

public record PagedResult<T></t>(IReadOnlyList<T></t> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

Common/Exceptions/ForbiddenException.cs

csharp
namespace UniversityPortal.API.Common.Exceptions;

public class ForbiddenException : PortalException
{
    public ForbiddenException(string logMessage)
        : base(403, "Forbidden", "You do not have permission to perform this action.", logMessage) { }
}

Replace Common/Security/ICurrentUser.cs and the two stand-in classes:

csharp
namespace UniversityPortal.API.Common.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// 


    string? StudentNumber { get; }

    bool IsInRole(string role);
}

/// 


public class DevCurrentUser : ICurrentUser
{
    private readonly HashSet<string></string> _roles;

    public DevCurrentUser(IConfiguration configuration)
    {
        StudentNumber = configuration["DevIdentity:StudentNumber"];
        _roles = (configuration.GetSection("DevIdentity:Roles").Get<string[]>() ?? Array.Empty<string></string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public string? StudentNumber { get; }
    public bool IsAuthenticated => StudentNumber is not null || _roles.Count > 0;
    public bool IsInRole(string role) => _roles.Contains(role);
}

/// 


public class AnonymousCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => false;
    public string? StudentNumber => null;
    public bool IsInRole(string role) => false;
}

(If you kept the stand-ins in one file, replace that file’s contents. The names haven’t changed.)

2. BC integration: a filtered, counted invoice query

Add to BcListResponse<T></t>, beside Value:

csharp
[JsonPropertyName("@odata.count")]
public int? Count { get; set; }

New DTOs in Integrations/BusinessCentral/DTOs/:

csharp
// BcPagedList.cs
namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public record BcPagedList<T></t>(IReadOnlyList<T></t> Items, int? TotalCount);
csharp
// BcInvoiceQuery.cs
namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

/// 


public record BcInvoiceQuery(
    string? CustomerNumber,
    IReadOnlyList<string></string>? Statuses,
    DateOnly? From,
    DateOnly? To,
    int Skip,
    int Top);

Add to IBusinessCentralClient:

csharp
Task<BcPagedList<BcSalesInvoiceDto></bcsalesinvoicedto>> QuerySalesInvoicesAsync(
    BcInvoiceQuery query, CancellationToken ct = default);

Add to BusinessCentralClient:

csharp
// For officer views: any status, drafts included. Never expose this directly to students.
public async Task<BcPagedList<BcSalesInvoiceDto></bcsalesinvoicedto>> QuerySalesInvoicesAsync(
    BcInvoiceQuery q, CancellationToken ct = default)
{
    var clauses = new List<string></string>();

    if (!string.IsNullOrWhiteSpace(q.CustomerNumber))
        clauses.Add($"customerNumber eq '{q.CustomerNumber.Replace("'", "''")}'");

    if (q.Statuses is { Count: > 0 })
        clauses.Add("(" + string.Join(" or ",
            q.Statuses.Select(s => $"status eq '{s.Replace("'", "''")}'")) + ")");

    // invoiceDate is set on drafts too, unlike postingDate on some documents
    if (q.From is { } from) clauses.Add($"invoiceDate ge {from:yyyy-MM-dd}");
    if (q.To is { } to) clauses.Add($"invoiceDate le {to:yyyy-MM-dd}");

    var parts = new List<string></string>
    {
        "$count=true",
        $"$top={q.Top}",
        $"$skip={q.Skip}",
        // number as tie-breaker keeps page boundaries stable
        "$orderby=" + Uri.EscapeDataString("invoiceDate desc,number desc")
    };
    if (clauses.Count > 0)
        parts.Add("$filter=" + Uri.EscapeDataString(string.Join(" and ", clauses)));

    var response = await GetAsync<BcListResponse<BcSalesInvoiceDto></bcsalesinvoicedto>>(
        CompanyPath("salesInvoices?" + string.Join("&", parts)), ct);

    return new BcPagedList<BcSalesInvoiceDto></bcsalesinvoicedto>(
        response?.Value ?? new List<BcSalesInvoiceDto></bcsalesinvoicedto>(), response?.Count);
}
3. Finance module additions

Add to DTOs/FinanceDtos.cs (plus using System.ComponentModel.DataAnnotations; at the top):

csharp
public enum InvoiceStatus { Draft, InReview, Open, Paid, Corrective, Canceled }

public record OfficerInvoiceDto(
    string InvoiceNumber,
    string StudentNumber,
    string StudentName,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string CurrencyCode,
    decimal TotalAmount,
    decimal AmountRemaining,
    InvoiceStatus Status,
    PaymentStatus? PaymentStatus);   // null until the invoice is posted

public class OfficerInvoiceQuery : IValidatableObject
{
    public InvoiceStatus? Status { get; set; }

    [StringLength(20)]
    public string? CustomerNumber { get; set; }

    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 25;

    public IEnumerable<ValidationResult></validationresult> Validate(ValidationContext context)
    {
        if (From is { } f && To is { } t && f > t)
            yield return new ValidationResult(
                "'From' must not be after 'To'.", new[] { nameof(From), nameof(To) });
    }
}

Add to FinanceMapper (plus using UniversityPortal.API.Integrations.BusinessCentral.Exceptions;):

csharp
public static string ToBcStatus(InvoiceStatus status) => status switch
{
    InvoiceStatus.Draft => "Draft",
    InvoiceStatus.InReview => "In Review",
    InvoiceStatus.Open => "Open",
    InvoiceStatus.Paid => "Paid",
    InvoiceStatus.Corrective => "Corrective",
    InvoiceStatus.Canceled => "Canceled",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
};

// An unknown status must fail loudly: guessing could treat an unposted invoice as posted
public static InvoiceStatus ParseStatus(string bcStatus) => bcStatus switch
{
    "Draft" => InvoiceStatus.Draft,
    "In Review" => InvoiceStatus.InReview,
    "Open" => InvoiceStatus.Open,
    "Paid" => InvoiceStatus.Paid,
    "Corrective" => InvoiceStatus.Corrective,
    "Canceled" => InvoiceStatus.Canceled,
    _ => throw new BcIntegrationException(
        BcFailureKind.InvalidResponse, $"Unknown BC invoice status '{bcStatus}'")
};

public static OfficerInvoiceDto ToOfficerInvoice(BcSalesInvoiceDto bc, DateOnly today)
{
    var status = ParseStatus(bc.Status);
    var posted = status is InvoiceStatus.Open or InvoiceStatus.Paid or InvoiceStatus.Corrective;

    return new OfficerInvoiceDto(
        InvoiceNumber: bc.Number,
        StudentNumber: bc.CustomerNumber,
        StudentName: bc.CustomerName,
        InvoiceDate: bc.InvoiceDate,
        DueDate: bc.DueDate,
        CurrencyCode: bc.CurrencyCode,
        TotalAmount: bc.TotalAmountIncludingTax,
        AmountRemaining: bc.RemainingAmount,
        Status: status,
        PaymentStatus: posted
            ? ComputeStatus(bc.TotalAmountIncludingTax, bc.RemainingAmount, bc.DueDate, today)
            : null);
}

Services/IOfficerFinanceService.cs

csharp
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Modules.Finance.Services;

public interface IOfficerFinanceService
{
    Task<PagedResult<OfficerInvoiceDto></officerinvoicedto>> GetInvoicesAsync(OfficerInvoiceQuery query, CancellationToken ct);
}

Services/OfficerFinanceService.cs

csharp
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Common.Security;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Modules.Finance.Services;

public class OfficerFinanceService : IOfficerFinanceService
{
    private readonly IBusinessCentralClient _bc;
    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;

    public OfficerFinanceService(IBusinessCentralClient bc, ICurrentUser user, TimeProvider time)
    {
        _bc = bc;
        _user = user;
        _time = time;
    }

    private void RequireOfficer()
    {
        if (!_user.IsAuthenticated) throw new NotAuthenticatedException();
        if (!_user.IsInRole(Roles.FinanceOfficer))
            throw new ForbiddenException("Caller is not in the FinanceOfficer role");
    }

    public async Task<PagedResult<OfficerInvoiceDto></officerinvoicedto>> GetInvoicesAsync(
        OfficerInvoiceQuery query, CancellationToken ct)
    {
        RequireOfficer();

        var skip = (query.Page - 1) * query.PageSize;
        var statuses = query.Status is { } s ? new[] { FinanceMapper.ToBcStatus(s) } : null;

        var page = await _bc.QuerySalesInvoicesAsync(
            new BcInvoiceQuery(query.CustomerNumber, statuses, query.From, query.To, skip, query.PageSize), ct);

        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        var items = page.Items.Select(i => FinanceMapper.ToOfficerInvoice(i, today)).ToList();

        // If BC ever omits the count, fall back to what we know rather than reporting 0
        return new PagedResult<OfficerInvoiceDto></officerinvoicedto>(
            items, query.Page, query.PageSize, page.TotalCount ?? skip + items.Count);
    }
}

Controllers/OfficerFinanceController.cs

csharp
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Services;

namespace UniversityPortal.API.Modules.Finance.Controllers;

// S5 adds [Authorize(Roles = Roles.FinanceOfficer)] here
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
    public async Task<ActionResult<PagedResult<OfficerInvoiceDto></officerinvoicedto>>> GetInvoices(
        [FromQuery] OfficerInvoiceQuery query, CancellationToken ct) =>
        Ok(await _finance.GetInvoicesAsync(query, ct));
}

Register it in FinanceModuleExtensions, by adding the line services.AddScoped<IOfficerFinanceService, OfficerFinanceService>(); below the student one.

Dev identity in appsettings.Development.json. Keep the student number so both sides can be tested:

json
"DevIdentity": {
  "StudentNumber": "20000",
  "Roles": [ "Student", "FinanceOfficer" ]
}
