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
        if (!_user.IsAuthenticated)
            throw new NotAuthenticatedException();

        if (!_user.IsInRole(Roles.FinanceOfficer))
            throw new ForbiddenException("Caller is not in the FinanceOfficer role");
    }

    public async Task<PagedResult<OfficerInvoiceDto>> GetInvoicesAsync(OfficerInvoiceQuery query, CancellationToken ct)
    {
        RequireOfficer();

        var skip = (query.Page - 1) * query.PageSize;
        var statuses = query.Status is { } status ? new[] { FinanceMapper.ToBcStatus(status) } : null;

        var page = await _bc.QuerySalesInvoicesAsync(
            new BcInvoiceQuery(query.CustomerNumber, statuses, query.From, query.To, skip, query.PageSize),
            ct);

        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        var items = page.Items.Select(i => FinanceMapper.ToOfficerInvoice(i, today)).ToList();

        return new PagedResult<OfficerInvoiceDto>(
            items,
            query.Page,
            query.PageSize,
            page.TotalCount ?? skip + items.Count);
    }

    public async Task<OfficerDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        RequireOfficer();

        var agedTask = _bc.GetAgedReceivablesTotalAsync(ct);
        var withBalanceTask = _bc.QueryCustomerFinancialDetailsAsync(new BcBalanceQuery(0, false, 0, 1), ct);
        var overdueTask = _bc.QueryCustomerFinancialDetailsAsync(new BcBalanceQuery(0, true, 0, 1), ct);
        var draftsTask = _bc.QuerySalesInvoicesAsync(
            new BcInvoiceQuery(null, new[] { "Draft" }, null, null, 0, 1),
            ct);
        var reviewTask = _bc.QuerySalesInvoicesAsync(
            new BcInvoiceQuery(null, new[] { "In Review" }, null, null, 0, 1),
            ct);

        await Task.WhenAll(agedTask, withBalanceTask, overdueTask, draftsTask, reviewTask);

        var aged = await agedTask;
        var now = _time.GetUtcNow().UtcDateTime;

        return new OfficerDashboardDto(
            AsOfDate: aged?.AgedAsOfDate ?? DateOnly.FromDateTime(now),
            TotalOutstanding: aged?.BalanceDue ?? 0,
            Aging: new List<AgingBucketDto>
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

    public async Task<PagedResult<OutstandingBalanceDto>> GetOutstandingBalancesAsync(
        OutstandingBalanceQuery query,
        CancellationToken ct)
    {
        RequireOfficer();

        var skip = (query.Page - 1) * query.PageSize;
        var page = await _bc.QueryCustomerFinancialDetailsAsync(
            new BcBalanceQuery(query.MinBalance, query.OverdueOnly, skip, query.PageSize),
            ct);

        var customers = await _bc.GetCustomersByNumbersAsync(
            page.Items.Select(i => i.Number).ToList(),
            ct);
        var byNumber = customers.ToDictionary(c => c.Number, StringComparer.Ordinal);

        var items = page.Items.Select(d =>
        {
            byNumber.TryGetValue(d.Number, out var c);
            return new OutstandingBalanceDto(
                d.Number,
                c?.DisplayName ?? string.Empty,
                c?.Email ?? string.Empty,
                c?.PhoneNumber ?? string.Empty,
                d.Balance,
                d.OverdueAmount);
        }).ToList();

        return new PagedResult<OutstandingBalanceDto>(
            items,
            query.Page,
            query.PageSize,
            page.TotalCount ?? skip + items.Count);
    }
}
