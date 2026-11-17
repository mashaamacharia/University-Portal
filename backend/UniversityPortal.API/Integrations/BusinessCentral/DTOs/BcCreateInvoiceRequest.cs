using System.Text.Json.Serialization;

namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcCreateInvoiceRequest
{
    [JsonPropertyName("customerNumber")]
    public string CustomerNumber { get; set; } = string.Empty;

    [JsonPropertyName("externalDocumentNumber")]
    public string ExternalDocumentNumber { get; set; } = string.Empty;

    [JsonPropertyName("invoiceDate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? InvoiceDate { get; set; }

    [JsonPropertyName("salesInvoiceLines")]
    public List<BcCreateInvoiceLine> Lines { get; set; } = new();
}

public class BcCreateInvoiceLine
{
    [JsonPropertyName("lineType")]
    public string LineType { get; set; } = string.Empty;

    [JsonPropertyName("lineObjectNumber")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unitPrice"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? UnitPrice { get; set; }

    [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}
