using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Modules.Finance.Services;

public interface IStudentFinanceService
{
    Task<FeeStatementDto> GetFeeStatementAsync(int top, CancellationToken ct);
    Task<BalanceSummaryDto> GetBalanceAsync(CancellationToken ct);
    Task<IReadOnlyList<InvoiceSummaryDto>> GetInvoicesAsync(int top, CancellationToken ct);
    Task<InvoiceDetailDto> GetInvoiceAsync(string invoiceNumber, CancellationToken ct);
    Task<PagedResult<PaymentHistoryEntryDto>> GetPaymentHistoryAsync(
        PaymentHistoryQuery query,
        CancellationToken ct);
}
