namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcCustomerFinancialDetailsDto
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public decimal OverdueAmount { get; set; }

    public decimal TotalSalesExcludingTax { get; set; }
}
