1. BC integration additions

New DTOs in Integrations/BusinessCentral/DTOs/:

csharp
// BcAgedReceivablesRowDto.cs
namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcAgedReceivablesRowDto
{
    public Guid CustomerId { get; set; }          // empty GUID on the company "Total" row
    public string CustomerNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal BalanceDue { get; set; }       // sum of all buckets: the total outstanding
    public decimal CurrentAmount { get; set; }
    public decimal Period1Amount { get; set; }
    public decimal Period2Amount { get; set; }
    public decimal Period3Amount { get; set; }
    public DateOnly AgedAsOfDate { get; set; }
    public string PeriodLengthFilter { get; set; } = string.Empty;
}
csharp
// BcBalanceQuery.cs
namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public record BcBalanceQuery(decimal MinBalance, bool OverdueOnly, int Skip, int Top);

Add to IBusinessCentralClient:

csharp
Task<BcPagedList<BcCustomerFinancialDetailsDto></bccustomerfinancialdetailsdto>> QueryCustomerFinancialDetailsAsync(
    BcBalanceQuery query, CancellationToken ct = default);

Task<IReadOnlyList<BcCustomerDto></bccustomerdto>> GetCustomersByNumbersAsync(
    IReadOnlyCollection<string></string> numbers, CancellationToken ct = default);

Task<BcAgedReceivablesRowDto?> GetAgedReceivablesTotalAsync(CancellationToken ct = default);

Add to BusinessCentralClient:

csharp
public async Task<BcPagedList<BcCustomerFinancialDetailsDto></bccustomerfinancialdetailsdto>> QueryCustomerFinancialDetailsAsync(
    BcBalanceQuery q, CancellationToken ct = default)
{
    // Invariant culture matters: a decimal formatted as "1,5" would break the OData filter
    var clauses = new List<string></string> { FormattableString.Invariant($"balance gt {q.MinBalance}") };
    if (q.OverdueOnly) clauses.Add("overdueAmount gt 0");

    var parts = new List<string></string>
    {
        "$count=true",
        $"$top={q.Top}",
        $"$skip={q.Skip}",
        "$orderby=" + Uri.EscapeDataString("balance desc,number asc"),
        "$filter=" + Uri.EscapeDataString(string.Join(" and ", clauses))
    };

    var response = await GetAsync<BcListResponse<BcCustomerFinancialDetailsDto></bccustomerfinancialdetailsdto>>(
        CompanyPath("customerFinancialDetails?" + string.Join("&", parts)), ct);

    return new BcPagedList<BcCustomerFinancialDetailsDto></bccustomerfinancialdetailsdto>(
        response?.Value ?? new List<BcCustomerFinancialDetailsDto></bccustomerfinancialdetailsdto>(), response?.Count);
}

public async Task<IReadOnlyList<BcCustomerDto></bccustomerdto>> GetCustomersByNumbersAsync(
    IReadOnlyCollection<string></string> numbers, CancellationToken ct = default)
{
    if (numbers.Count == 0) return Array.Empty<BcCustomerDto></bccustomerdto>();

    var distinct = numbers.Distinct().ToList();
    var filter = Uri.EscapeDataString(string.Join(" or ",
        distinct.Select(n => $"number eq '{n.Replace("'", "''")}'")));

    var response = await GetAsync<BcListResponse<BcCustomerDto></bccustomerdto>>(
        CompanyPath($"customers?$filter={filter}&$top={distinct.Count}"), ct);
    return response?.Value ?? new List<BcCustomerDto></bccustomerdto>();
}

// BC computes this report across all customers, so it grows with the student count.
// We read every row and pick the company "Total" row (the one with no customer).
public async Task<BcAgedReceivablesRowDto?> GetAgedReceivablesTotalAsync(CancellationToken ct = default)
{
    var response = await GetAsync<BcListResponse<BcAgedReceivablesRowDto></bcagedreceivablesrowdto>>(
        CompanyPath("agedAccountsReceivables"), ct);
    return response?.Value.FirstOrDefault(r => r.CustomerId == Guid.Empty);
}
2. Finance module additions

Add to DTOs/FinanceDtos.cs:

csharp
public record AgingBucketDto(string Label, decimal Amount);

public record OfficerDashboardDto(
    DateOnly AsOfDate,
    decimal TotalOutstanding,
    IReadOnlyList<AgingBucketDto></agingbucketdto> Aging,
    int StudentsWithBalance,
    int StudentsWithOverdue,
    int DraftInvoices,
    int InvoicesInReview,
    DateTime GeneratedAtUtc);

public record OutstandingBalanceDto(
    string StudentNumber,
    string StudentName,
    string Email,
    string PhoneNumber,
    decimal Balance,
    decimal OverdueAmount);

public class OutstandingBalanceQuery
{
    [Range(typeof(decimal), "0", "1000000000")]
    public decimal MinBalance { get; set; } = 0;

    public bool OverdueOnly { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    // Capped at 50 because each page triggers a customer lookup that lists these numbers in the URL
    [Range(1, 50)]
    public int PageSize { get; set; } = 25;
}

Add to IOfficerFinanceService:

csharp
Task<OfficerDashboardDto></officerdashboarddto> GetDashboardAsync(CancellationToken ct);
Task<PagedResult<OutstandingBalanceDto></outstandingbalancedto>> GetOutstandingBalancesAsync(
    OutstandingBalanceQuery query, CancellationToken ct);

Add to OfficerFinanceService:

csharp
public async Task<OfficerDashboardDto></officerdashboarddto> GetDashboardAsync(CancellationToken ct)
{
    RequireOfficer();

    var agedTask = _bc.GetAgedReceivablesTotalAsync(ct);
    // Top = 1: we only want the total count BC reports, not the records
    var withBalanceTask = _bc.QueryCustomerFinancialDetailsAsync(new BcBalanceQuery(0, false, 0, 1), ct);
    var overdueTask = _bc.QueryCustomerFinancialDetailsAsync(new BcBalanceQuery(0, true, 0, 1), ct);
    var draftsTask = _bc.QuerySalesInvoicesAsync(
        new BcInvoiceQuery(null, new[] { "Draft" }, null, null, 0, 1), ct);
    var reviewTask = _bc.QuerySalesInvoicesAsync(
        new BcInvoiceQuery(null, new[] { "In Review" }, null, null, 0, 1), ct);

    await Task.WhenAll(agedTask, withBalanceTask, overdueTask, draftsTask, reviewTask);

    var aged = await agedTask;
    var now = _time.GetUtcNow().UtcDateTime;

    return new OfficerDashboardDto(
        AsOfDate: aged?.AgedAsOfDate ?? DateOnly.FromDateTime(now),
        TotalOutstanding: aged?.BalanceDue ?? 0,
        Aging: new List<AgingBucketDto></agingbucketdto>
        {
            new("Current (not yet due)", aged?.CurrentAmount ?? 0),
            new("1-30 days", aged?.Period1Amount ?? 0),
            new("31-60 days", aged?.Period2Amount ?? 0),
            new("Over 60 days", aged?.Period3Amount ?? 0)
        },
        StudentsWithBalance: (await withBalanceTask).TotalCount ?? 0,
        StudentsWithOverdue: (await overdueTask).TotalCount ?? 0,
        DraftInvoices: (await draftsTask).TotalCount ?? 0,
        InvoicesInReview: (await reviewTask).TotalCount ?? 0,
        GeneratedAtUtc: now);
}

public async Task<PagedResult<OutstandingBalanceDto></outstandingbalancedto>> GetOutstandingBalancesAsync(
    OutstandingBalanceQuery query, CancellationToken ct)
{
    RequireOfficer();

    var skip = (query.Page - 1) * query.PageSize;
    var page = await _bc.QueryCustomerFinancialDetailsAsync(
        new BcBalanceQuery(query.MinBalance, query.OverdueOnly, skip, query.PageSize), ct);

    // The balances carry no names, so look up this page's customers in one call
    var customers = await _bc.GetCustomersByNumbersAsync(
        page.Items.Select(i => i.Number).ToList(), ct);
    var byNumber = customers.ToDictionary(c => c.Number, StringComparer.Ordinal);

    var items = page.Items.Select(d =>
    {
        byNumber.TryGetValue(d.Number, out var c);
        return new OutstandingBalanceDto(
            d.Number, c?.DisplayName ?? string.Empty, c?.Email ?? string.Empty,
            c?.PhoneNumber ?? string.Empty, d.Balance, d.OverdueAmount);
    }).ToList();

    return new PagedResult<OutstandingBalanceDto></outstandingbalancedto>(
        items, query.Page, query.PageSize, page.TotalCount ?? skip + items.Count);
}

Add to OfficerFinanceController:

csharp
[HttpGet("dashboard")]
public async Task<ActionResult<OfficerDashboardDto></officerdashboarddto>> GetDashboard(CancellationToken ct) =>
    Ok(await _finance.GetDashboardAsync(ct));

[HttpGet("outstanding-balances")]
public async Task<ActionResult<PagedResult<OutstandingBalanceDto></outstandingbalancedto>>> GetOutstandingBalances(
    [FromQuery] OutstandingBalanceQuery query, CancellationToken ct) =>
    Ok(await _finance.GetOutstandingBalancesAsync(query, ct));
