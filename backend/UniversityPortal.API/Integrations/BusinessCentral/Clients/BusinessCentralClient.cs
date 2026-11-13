using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UniversityPortal.API.Integrations.BusinessCentral.Configuration;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Integrations.BusinessCentral.Exceptions;
using UniversityPortal.API.Common.Exceptions;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public class BusinessCentralClient : IBusinessCentralClient
{
    private static readonly string[] PostedStatuses = { "Open", "Paid", "Corrective" };
    private static readonly HashSet<string> PostedStatusSet = new(PostedStatuses, StringComparer.OrdinalIgnoreCase);

    private readonly HttpClient _http;
    private readonly BusinessCentralOptions _options;

    public BusinessCentralClient(HttpClient http, IOptions<BusinessCentralOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    private string CompanyPath(string resource) => $"companies({_options.CompanyId})/{resource}";

    private string CustomApiCompanyPath(string resource) =>
        $"../techworks/universityPortal/v1.0/companies({_options.CompanyId})/{resource}";

    public async Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default)
    {
        var response = await GetAsync<BcListResponse<BcCompanyDto>>("companies", ct);
        return response?.Value ?? [];
    }

    public async Task<IReadOnlyList<BcCustomerDto>> GetCustomersAsync(
        int top = 50,
        int skip = 0,
        CancellationToken ct = default)
    {
        ValidatePage(top, skip);

        var url = CompanyPath($"customers?$top={top}&$skip={skip}");
        var response = await GetAsync<BcListResponse<BcCustomerDto>>(url, ct);
        return response?.Value ?? [];
    }

    public async Task<BcCustomerDto?> GetCustomerByNumberAsync(
        string customerNumber,
        CancellationToken ct = default)
    {
        ValidateCustomerNumber(customerNumber);

        var safeCustomerNumber = customerNumber.Replace("'", "''", StringComparison.Ordinal);
        var filter = Uri.EscapeDataString($"number eq '{safeCustomerNumber}'");
        var url = CompanyPath($"customers?$filter={filter}&$top=1");

        var response = await GetAsync<BcListResponse<BcCustomerDto>>(url, ct);
        return response?.Value.FirstOrDefault();
    }

    public async Task<BcCustomerFinancialDetailsDto?> GetCustomerFinancialDetailsAsync(
        string customerNumber,
        CancellationToken ct = default)
    {
        ValidateCustomerNumber(customerNumber);

        var safeCustomerNumber = customerNumber.Replace("'", "''", StringComparison.Ordinal);
        var filter = Uri.EscapeDataString($"number eq '{safeCustomerNumber}'");
        var url = CompanyPath($"customerFinancialDetails?$filter={filter}");

        var response = await GetAsync<BcListResponse<BcCustomerFinancialDetailsDto>>(url, ct);
        return response?.Value.FirstOrDefault();
    }

    public async Task<IReadOnlyList<BcSalesInvoiceDto>> GetPostedSalesInvoicesAsync(
        string customerNumber,
        int top = 50,
        CancellationToken ct = default)
    {
        ValidateCustomerNumber(customerNumber);
        ValidatePage(top, 0);

        var safeCustomerNumber = customerNumber.Replace("'", "''", StringComparison.Ordinal);
        var statusFilter = string.Join(" or ", PostedStatuses.Select(status => $"status eq '{status}'"));
        var filter = Uri.EscapeDataString(
            $"customerNumber eq '{safeCustomerNumber}' and ({statusFilter})");
        var orderBy = Uri.EscapeDataString("postingDate desc");
        var url = CompanyPath($"salesInvoices?$filter={filter}&$orderby={orderBy}&$top={top}");

        var response = await GetAsync<BcListResponse<BcSalesInvoiceDto>>(url, ct);
        return response?.Value
            .Where(invoice => PostedStatusSet.Contains(invoice.Status))
            .ToList() ?? [];
    }

    public async Task<BcSalesInvoiceDto?> GetPostedSalesInvoiceByNumberAsync(
        string customerNumber,
        string invoiceNumber,
        CancellationToken ct = default)
    {
        ValidateCustomerNumber(customerNumber);
        ValidateInvoiceNumber(invoiceNumber);

        var safeCustomerNumber = customerNumber.Replace("'", "''", StringComparison.Ordinal);
        var safeInvoiceNumber = invoiceNumber.Replace("'", "''", StringComparison.Ordinal);
        var statusFilter = string.Join(" or ", PostedStatuses.Select(status => $"status eq '{status}'"));
        var filter = Uri.EscapeDataString(
            $"customerNumber eq '{safeCustomerNumber}' and number eq '{safeInvoiceNumber}' and ({statusFilter})");

        var response = await GetAsync<BcListResponse<BcSalesInvoiceDto>>(
            CompanyPath($"salesInvoices?$filter={filter}&$top=1"),
            ct);

        return response?.Value.FirstOrDefault();
    }

    public async Task<BcPagedList<BcSalesInvoiceDto>> QuerySalesInvoicesAsync(
        BcInvoiceQuery q,
        CancellationToken ct = default)
    {
        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(q.CustomerNumber))
            clauses.Add($"customerNumber eq '{q.CustomerNumber.Replace("'", "''")}'");

        if (q.Statuses is { Count: > 0 })
        {
            var statusGroup = "(" + string.Join(" or ", q.Statuses
                .Select(s => $"status eq '{s.Replace("'", "''")}'")) + ")";
            clauses.Add(statusGroup);
        }

        if (q.From is { } from)
            clauses.Add($"invoiceDate ge {from:yyyy-MM-dd}");

        if (q.To is { } to)
            clauses.Add($"invoiceDate le {to:yyyy-MM-dd}");

        var parts = new List<string>
        {
            "$count=true",
            $"$top={q.Top}",
            $"$skip={q.Skip}",
            "$orderby=" + Uri.EscapeDataString("invoiceDate desc,number desc")
        };

        if (clauses.Count > 0)
            parts.Add("$filter=" + Uri.EscapeDataString(string.Join(" and ", clauses)));

        var response = await GetAsync<BcListResponse<BcSalesInvoiceDto>>(
            CompanyPath("salesInvoices?" + string.Join("&", parts)),
            ct);

        return new BcPagedList<BcSalesInvoiceDto>(
            response?.Value ?? [],
            response?.Count);
    }

    public async Task<BcPagedList<BcCustomerFinancialDetailsDto>> QueryCustomerFinancialDetailsAsync(
        BcBalanceQuery q,
        CancellationToken ct = default)
    {
        var clauses = new List<string>
        {
            FormattableString.Invariant($"balance gt {q.MinBalance}")
        };

        if (q.OverdueOnly)
            clauses.Add("overdueAmount gt 0");

        var parts = new List<string>
        {
            "$count=true",
            $"$top={q.Top}",
            $"$skip={q.Skip}",
            "$orderby=" + Uri.EscapeDataString("balance desc,number asc"),
            "$filter=" + Uri.EscapeDataString(string.Join(" and ", clauses))
        };

        var response = await GetAsync<BcListResponse<BcCustomerFinancialDetailsDto>>(
            CompanyPath("customerFinancialDetails?" + string.Join("&", parts)),
            ct);

        return new BcPagedList<BcCustomerFinancialDetailsDto>(
            response?.Value ?? [],
            response?.Count);
    }

    public async Task<BcPagedList<BcCustomerLedgerEntryDto>> QueryCustomerLedgerEntriesAsync(
        BcLedgerEntryQuery q,
        CancellationToken ct = default)
    {
        ValidatePage(q.Top, q.Skip);

        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(q.CustomerNumber))
            clauses.Add($"customerNumber eq '{q.CustomerNumber.Replace("'", "''", StringComparison.Ordinal)}'");

        if (q.DocumentTypes is { Count: > 0 })
        {
            var documentTypeGroup = "(" + string.Join(" or ", q.DocumentTypes
                .Select(type => $"documentType eq '{type.Replace("'", "''", StringComparison.Ordinal)}'")) + ")";
            clauses.Add(documentTypeGroup);
        }

        if (q.From is { } from)
            clauses.Add($"postingDate ge {from:yyyy-MM-dd}");

        if (q.To is { } to)
            clauses.Add($"postingDate le {to:yyyy-MM-dd}");

        var parts = new List<string>
        {
            "$count=true",
            $"$top={q.Top}",
            $"$skip={q.Skip}",
            "$orderby=" + Uri.EscapeDataString("postingDate desc,entryNumber desc")
        };
        if (clauses.Count > 0)
            parts.Add("$filter=" + Uri.EscapeDataString(string.Join(" and ", clauses)));

        var response = await GetAsync<BcListResponse<BcCustomerLedgerEntryDto>>(
            CustomApiCompanyPath("customerLedgerEntries?" + string.Join("&", parts)),
            ct);

        return new BcPagedList<BcCustomerLedgerEntryDto>(
            response?.Value ?? [],
            response?.Count);
    }

    public async Task<IReadOnlyList<BcCustomerDto>> GetCustomersByNumbersAsync(
        IReadOnlyCollection<string> numbers,
        CancellationToken ct = default)
    {
        if (numbers.Count == 0)
            return [];

        var distinct = numbers.Distinct().ToList();
        var filter = Uri.EscapeDataString(string.Join(" or ", distinct.Select(n => $"number eq '{n.Replace("'", "''")}'")));

        var response = await GetAsync<BcListResponse<BcCustomerDto>>(
            CompanyPath($"customers?$filter={filter}&$top={distinct.Count}"),
            ct);

        return response?.Value ?? [];
    }

    public async Task<BcAgedReceivablesRowDto?> GetAgedReceivablesTotalAsync(CancellationToken ct = default)
    {
        var response = await GetAsync<BcListResponse<BcAgedReceivablesRowDto>>(
            CompanyPath("agedAccountsReceivables"),
            ct);

        return response?.Value.FirstOrDefault(r => r.CustomerId == Guid.Empty);
    }

    public async Task<IReadOnlyList<BcSalesInvoiceLineDto>> GetSalesInvoiceLinesAsync(
        Guid invoiceId,
        CancellationToken ct = default)
    {
        var url = CompanyPath($"salesInvoices({invoiceId})/salesInvoiceLines");
        var response = await GetAsync<BcListResponse<BcSalesInvoiceLineDto>>(url, ct);
        return response?.Value ?? [];
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        var path = url.Split('?')[0];

        try
        {
            using var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var snippet = body.Length > 300 ? body[..300] : body;
                throw new BcIntegrationException(
                    MapKind(response.StatusCode),
                    $"BC returned {(int)response.StatusCode} for GET {path}. Body: {snippet}",
                    response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new BcIntegrationException(
                BcFailureKind.Timeout,
                $"BC timed out (after retries) for GET {path}",
                inner: ex);
        }
        catch (BrokenCircuitException ex)
        {
            throw new BcIntegrationException(
                BcFailureKind.Unreachable,
                $"BC circuit is open for GET {path}",
                inner: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new BcIntegrationException(
                BcFailureKind.Unreachable,
                $"Could not reach BC for GET {path}",
                inner: ex);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new BcIntegrationException(
                BcFailureKind.Timeout,
                $"BC timed out for GET {path}",
                inner: ex);
        }
        catch (JsonException ex)
        {
            throw new BcIntegrationException(
                BcFailureKind.InvalidResponse,
                $"BC returned unreadable JSON for GET {path}",
                inner: ex);
        }
    }

    private static void ValidateCustomerNumber(string customerNumber)
    {
        if (string.IsNullOrWhiteSpace(customerNumber))
        {
            throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Invalid customer number",
                "A customer number is required.");
        }
    }

    private static void ValidateInvoiceNumber(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Invalid invoice number",
                "An invoice number is required.");
        }
    }

    private static void ValidatePage(int top, int skip)
    {
        if (top <= 0 || skip < 0)
        {
            throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Invalid pagination",
                "The top value must be greater than zero and skip cannot be negative.");
        }
    }

    private static BcFailureKind MapKind(HttpStatusCode code) => code switch
    {
        HttpStatusCode.NotFound => BcFailureKind.NotFound,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => BcFailureKind.AccessDenied,
        _ when (int)code >= 500 => BcFailureKind.ServerError,
        _ => BcFailureKind.Rejected
    };
}
