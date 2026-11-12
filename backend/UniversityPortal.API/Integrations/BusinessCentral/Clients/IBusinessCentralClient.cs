using UniversityPortal.API.Integrations.BusinessCentral.DTOs;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public interface IBusinessCentralClient
{
    Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<BcCustomerDto>> GetCustomersAsync(
        int top = 50,
        int skip = 0,
        CancellationToken ct = default);

    Task<BcCustomerDto?> GetCustomerByNumberAsync(string customerNumber, CancellationToken ct = default);

    Task<BcCustomerFinancialDetailsDto?> GetCustomerFinancialDetailsAsync(
        string customerNumber,
        CancellationToken ct = default);

    Task<IReadOnlyList<BcSalesInvoiceDto>> GetPostedSalesInvoicesAsync(
        string customerNumber,
        int top = 50,
        CancellationToken ct = default);

    Task<BcSalesInvoiceDto?> GetPostedSalesInvoiceByNumberAsync(
        string customerNumber,
        string invoiceNumber,
        CancellationToken ct = default);

    Task<BcPagedList<BcSalesInvoiceDto>> QuerySalesInvoicesAsync(
        BcInvoiceQuery query,
        CancellationToken ct = default);

    Task<BcPagedList<BcCustomerFinancialDetailsDto>> QueryCustomerFinancialDetailsAsync(
        BcBalanceQuery query,
        CancellationToken ct = default);

    Task<IReadOnlyList<BcCustomerDto>> GetCustomersByNumbersAsync(
        IReadOnlyCollection<string> numbers,
        CancellationToken ct = default);

    Task<BcAgedReceivablesRowDto?> GetAgedReceivablesTotalAsync(CancellationToken ct = default);

    Task<IReadOnlyList<BcSalesInvoiceLineDto>> GetSalesInvoiceLinesAsync(
        Guid invoiceId,
        CancellationToken ct = default);
}
