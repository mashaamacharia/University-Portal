using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using UniversityPortal.API.Integrations.BusinessCentral.Configuration;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public class BusinessCentralClient(
    HttpClient http,
    IOptions<BusinessCentralOptions> options) : IBusinessCentralClient
{
    private static readonly string[] PostedStatuses = ["Open", "Paid", "Corrective"];
    private static readonly HashSet<string> PostedStatusSet =
        new(PostedStatuses, StringComparer.OrdinalIgnoreCase);

    private readonly BusinessCentralOptions _options = options.Value;

    private string CompanyPath(string resource) =>
        $"companies({_options.CompanyId})/{resource}";

    public async Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default)
    {
        var response = await http.GetFromJsonAsync<BcListResponse<BcCompanyDto>>("companies", ct);
        return response?.Value ?? [];
    }

    public async Task<IReadOnlyList<BcCustomerDto>> GetCustomersAsync(
        int top = 50,
        int skip = 0,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);
        ArgumentOutOfRangeException.ThrowIfNegative(skip);

        var url = CompanyPath($"customers?$top={top}&$skip={skip}");
        var response = await http.GetFromJsonAsync<BcListResponse<BcCustomerDto>>(url, ct);
        return response?.Value ?? [];
    }

    public async Task<BcCustomerFinancialDetailsDto?> GetCustomerFinancialDetailsAsync(
        string customerNumber,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerNumber);

        var safeCustomerNumber = customerNumber.Replace("'", "''", StringComparison.Ordinal);
        var filter = Uri.EscapeDataString($"number eq '{safeCustomerNumber}'");
        var url = CompanyPath($"customerFinancialDetails?$filter={filter}");

        var response = await http.GetFromJsonAsync<BcListResponse<BcCustomerFinancialDetailsDto>>(url, ct);
        return response?.Value.FirstOrDefault();
    }

    public async Task<IReadOnlyList<BcSalesInvoiceDto>> GetPostedSalesInvoicesAsync(
        string customerNumber,
        int top = 50,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(top);

        var safeCustomerNumber = customerNumber.Replace("'", "''", StringComparison.Ordinal);
        var statusFilter = string.Join(" or ", PostedStatuses.Select(status => $"status eq '{status}'"));
        var filter = Uri.EscapeDataString(
            $"customerNumber eq '{safeCustomerNumber}' and ({statusFilter})");
        var orderBy = Uri.EscapeDataString("postingDate desc");
        var url = CompanyPath($"salesInvoices?$filter={filter}&$orderby={orderBy}&$top={top}");

        var response = await http.GetFromJsonAsync<BcListResponse<BcSalesInvoiceDto>>(url, ct);
        return response?.Value
            .Where(invoice => PostedStatusSet.Contains(invoice.Status))
            .ToList() ?? [];
    }

    public async Task<IReadOnlyList<BcSalesInvoiceLineDto>> GetSalesInvoiceLinesAsync(
        Guid invoiceId,
        CancellationToken ct = default)
    {
        var url = CompanyPath($"salesInvoices({invoiceId})/salesInvoiceLines");
        var response = await http.GetFromJsonAsync<BcListResponse<BcSalesInvoiceLineDto>>(url, ct);
        return response?.Value ?? [];
    }
}
