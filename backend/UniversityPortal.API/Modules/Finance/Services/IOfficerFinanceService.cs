using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Modules.Finance.Services;

public interface IOfficerFinanceService
{
    Task<PagedResult<OfficerInvoiceDto>> GetInvoicesAsync(OfficerInvoiceQuery query, CancellationToken ct);
    Task<OfficerDashboardDto> GetDashboardAsync(CancellationToken ct);
    Task<PagedResult<OutstandingBalanceDto>> GetOutstandingBalancesAsync(
        OutstandingBalanceQuery query,
        CancellationToken ct);
    Task<PagedResult<OfficerPaymentDto>> GetPaymentsAsync(
        OfficerPaymentQuery query,
        CancellationToken ct);
}
