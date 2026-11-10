
This step: read posted invoices for a student

The rule from earlier gets enforced in the integration layer: the client only ever returns posted invoices, so no module can accidentally show a draft to a student. Of BC’s invoice statuses, Draft and In Review are not posted yet, so we allow Open, Paid and Corrective.

New DTO, Integrations/BusinessCentral/DTOs/BcSalesInvoiceDto.cs:

csharp
namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcSalesInvoiceDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string ExternalDocumentNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly PostingDate { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal TotalAmountExcludingTax { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalAmountIncludingTax { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastModifiedDateTime { get; set; }
}

Add to IBusinessCentralClient:

csharp
Task<IReadOnlyList<BcSalesInvoiceDto></bcsalesinvoicedto>> GetPostedSalesInvoicesAsync(
    string customerNumber, int top = 50, CancellationToken ct = default);

Add to BusinessCentralClient:

csharp
// Only these statuses are posted. Draft and In Review must never reach students.
private static readonly string[] PostedStatuses = { "Open", "Paid", "Corrective" };

public async Task<IReadOnlyList<BcSalesInvoiceDto></bcsalesinvoicedto>> GetPostedSalesInvoicesAsync(
    string customerNumber, int top = 50, CancellationToken ct = default)
{
    var safe = customerNumber.Replace("'", "''");
    var statusFilter = string.Join(" or ", PostedStatuses.Select(s => $"status eq '{s}'"));
    var filter = Uri.EscapeDataString($"customerNumber eq '{safe}' and ({statusFilter})");
    var orderBy = Uri.EscapeDataString("postingDate desc");

    var url = CompanyPath($"salesInvoices?$filter={filter}&$orderby={orderBy}&$top={top}");
    var response = await _http.GetFromJsonAsync<BcListResponse<BcSalesInvoiceDto></bcsalesinvoicedto>>(url, ct);
    return response?.Value ?? new List<BcSalesInvoiceDto></bcsalesinvoicedto>();
}

Add to the diagnostics controller, using the same try/catch as the other actions:

csharp
[HttpGet("customers/{number}/invoices")]
public async Task<IActionResult></iactionresult> GetInvoices(string number, [FromQuery] int top = 20, CancellationToken ct = default)
{
    try { return Ok(await _bc.GetPostedSalesInvoicesAsync(number, top, ct)); }
    catch (HttpRequestException ex)
    {
        return StatusCode(502, new { error = "Could not reach Business Central", detail = ex.Message });
    }
}
