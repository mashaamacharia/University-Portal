using System.ComponentModel.DataAnnotations;

namespace UniversityPortal.API.Modules.Finance.Configuration;

public class ReceiptOptions
{
    public const string SectionName = "Receipts";

    [Required]
    public string UniversityName { get; set; } = string.Empty;

    public List<string> AddressLines { get; set; } = [];

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? FooterNote { get; set; }
}
