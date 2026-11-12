namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcAgedReceivablesRowDto
{
    public Guid CustomerId { get; set; }

    public string CustomerNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal BalanceDue { get; set; }

    public decimal CurrentAmount { get; set; }

    public decimal Period1Amount { get; set; }

    public decimal Period2Amount { get; set; }

    public decimal Period3Amount { get; set; }

    public DateOnly AgedAsOfDate { get; set; }

    public string PeriodLengthFilter { get; set; } = string.Empty;
}
