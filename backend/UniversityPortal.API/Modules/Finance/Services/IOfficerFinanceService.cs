using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Modules.Finance.Services;

public interface IOfficerFinanceService
{
    Task<IReadOnlyList<StudentLookupDto>> SearchStudentsAsync(StudentLookupQuery query, CancellationToken ct);
    Task<PagedResult<OfficerInvoiceDto>> GetInvoicesAsync(OfficerInvoiceQuery query, CancellationToken ct);
    Task<OfficerDashboardDto> GetDashboardAsync(CancellationToken ct);
    Task<PagedResult<OutstandingBalanceDto>> GetOutstandingBalancesAsync(
        OutstandingBalanceQuery query,
        CancellationToken ct);
    Task<PagedResult<OfficerPaymentDto>> GetPaymentsAsync(
        OfficerPaymentQuery query,
        CancellationToken ct);
}
