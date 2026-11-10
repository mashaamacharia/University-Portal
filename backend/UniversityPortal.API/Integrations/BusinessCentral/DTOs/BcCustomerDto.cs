namespace UniversityPortal.API.Integrations.BusinessCentral.DTOs;

public class BcCustomerDto
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal BalanceDue { get; set; }

    public decimal CreditLimit { get; set; }

    public string Blocked { get; set; } = string.Empty;

    public DateTime LastModifiedDateTime { get; set; }

    public bool IsBlocked =>
        !string.IsNullOrWhiteSpace(Blocked) && Blocked != "_x0020_";
}
