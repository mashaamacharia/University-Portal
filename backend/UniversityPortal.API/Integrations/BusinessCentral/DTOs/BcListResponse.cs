using System.Text.Json.Serialization;

namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcListResponse<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = [];

    [JsonPropertyName("@odata.count")]
    public int? Count { get; set; }
}
