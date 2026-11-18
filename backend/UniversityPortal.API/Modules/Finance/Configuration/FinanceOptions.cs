namespace UniversityPortal.API.Modules.Finance.Configuration;

public class FinanceOptions
{
    public const string SectionName = "Finance";

    public bool PostingRequiresDifferentOfficer { get; set; } = true;
}
