using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Common.Responses;
using UniversityPortal.API.Common.Security;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Modules.Finance.Services;

public class StudentFinanceService : IStudentFinanceService
{
    private static readonly string[] PaymentTypes = { "Payment" };

    private readonly IBusinessCentralClient _bc;
    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;

    public StudentFinanceService(IBusinessCentralClient bc, ICurrentUser user, TimeProvider time)
    {
        _bc = bc;
        _user = user;
        _time = time;
    }

    private string RequireStudentNumber() =>
        _user.StudentNumber ?? throw new NotAuthenticatedException();

    private DateOnly Today() => DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);

    public async Task<FeeStatementDto> GetFeeStatementAsync(int top, CancellationToken ct)
    {
        var number = RequireStudentNumber();

        var customerTask = _bc.GetCustomerByNumberAsync(number, ct);
        var detailsTask = _bc.GetCustomerFinancialDetailsAsync(number, ct);
        var invoicesTask = _bc.GetPostedSalesInvoicesAsync(number, top, ct);
        var paymentsTask = _bc.QueryCustomerLedgerEntriesAsync(
            new BcLedgerEntryQuery(number, PaymentTypes, null, null, 0, 10),
            ct);
        await Task.WhenAll(customerTask, detailsTask, invoicesTask, paymentsTask);

        var customer = await customerTask
            ?? throw new NotFoundException($"No BC customer for student {number}");
        var details = await detailsTask;
        var today = Today();

        return new FeeStatementDto(
            Student: new StudentInfoDto(customer.Number, customer.DisplayName, customer.Email),
            Balance: new BalanceSummaryDto(
                customer.CurrencyCode,
                details?.Balance ?? 0,
                details?.OverdueAmount ?? 0),
            Invoices: (await invoicesTask).Select(i => FinanceMapper.ToSummary(i, today)).ToList(),
            RecentPayments: (await paymentsTask).Items
                .Select(payment => FinanceMapper.ToPayment(payment, customer.CurrencyCode))
                .ToList(),
            GeneratedAtUtc: _time.GetUtcNow().UtcDateTime);
    }

    public async Task<BalanceSummaryDto> GetBalanceAsync(CancellationToken ct)
    {
        var number = RequireStudentNumber();

        var customerTask = _bc.GetCustomerByNumberAsync(number, ct);
        var detailsTask = _bc.GetCustomerFinancialDetailsAsync(number, ct);
        await Task.WhenAll(customerTask, detailsTask);

        var customer = await customerTask
            ?? throw new NotFoundException($"No BC customer for student {number}");
        var details = await detailsTask;

        return new BalanceSummaryDto(
            customer.CurrencyCode,
            details?.Balance ?? 0,
            details?.OverdueAmount ?? 0);
    }

    public async Task<IReadOnlyList<InvoiceSummaryDto>> GetInvoicesAsync(int top, CancellationToken ct)
    {
        var number = RequireStudentNumber();
        var today = Today();

        var invoices = await _bc.GetPostedSalesInvoicesAsync(number, top, ct);
        return invoices.Select(i => FinanceMapper.ToSummary(i, today)).ToList();
    }

    public async Task<InvoiceDetailDto> GetInvoiceAsync(string invoiceNumber, CancellationToken ct)
    {
        var number = RequireStudentNumber();

        var invoice = await _bc.GetPostedSalesInvoiceByNumberAsync(number, invoiceNumber, ct)
            ?? throw new NotFoundException(
                $"Invoice {invoiceNumber} not found for student {number} (missing, not owned, or not posted)");

        var lines = await _bc.GetSalesInvoiceLinesAsync(invoice.Id, ct);

        return new InvoiceDetailDto(
            FinanceMapper.ToSummary(invoice, Today()),
            lines
                .Select(FinanceMapper.ToLine)
                .OfType<InvoiceLineDto>()
                .ToList());
    }

    public async Task<PagedResult<PaymentHistoryEntryDto>> GetPaymentHistoryAsync(
        PaymentHistoryQuery query,
        CancellationToken ct)
    {
        var number = RequireStudentNumber();
        var skip = (query.Page - 1) * query.PageSize;

        var customerTask = _bc.GetCustomerByNumberAsync(number, ct);
        var paymentsTask = _bc.QueryCustomerLedgerEntriesAsync(
            new BcLedgerEntryQuery(number, PaymentTypes, query.From, query.To, skip, query.PageSize),
            ct);
        await Task.WhenAll(customerTask, paymentsTask);

        var customer = await customerTask
            ?? throw new NotFoundException($"No BC customer for student {number}");
        var page = await paymentsTask;

        var items = page.Items
            .Select(payment => FinanceMapper.ToPayment(payment, customer.CurrencyCode))
            .ToList();
        return new PagedResult<PaymentHistoryEntryDto>(
            items,
            query.Page,
            query.PageSize,
            page.TotalCount ?? skip + items.Count);
    }
}
