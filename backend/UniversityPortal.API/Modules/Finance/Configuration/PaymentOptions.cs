using System.ComponentModel.DataAnnotations;

namespace UniversityPortal.API.Modules.Finance.Configuration;

public class PaymentOptions
{
    public const string SectionName = "Payments";

    public Dictionary<string, string> MethodBatches { get; set; } = new();

    [Range(0, 365)]
    public int MaxBackdateDays { get; set; } = 60;
}
