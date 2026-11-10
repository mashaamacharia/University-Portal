using UniversityPortal.API.Integrations.BusinessCentral.DTOs;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public interface IBusinessCentralClient
{
    Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<BcCustomerDto>> GetCustomersAsync(
        int top = 50,
        int skip = 0,
        CancellationToken ct = default);

    Task<BcCustomerFinancialDetailsDto?> GetCustomerFinancialDetailsAsync(
        string customerNumber,
        CancellationToken ct = default);

    Task<IReadOnlyList<BcSalesInvoiceDto>> GetPostedSalesInvoicesAsync(
        string customerNumber,
        int top = 50,
        CancellationToken ct = default);

    Task<IReadOnlyList<BcSalesInvoiceLineDto>> GetSalesInvoiceLinesAsync(
        Guid invoiceId,
        CancellationToken ct = default);
}
