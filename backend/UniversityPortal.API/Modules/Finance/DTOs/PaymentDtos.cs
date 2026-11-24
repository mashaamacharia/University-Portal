using System.ComponentModel.DataAnnotations;
using UniversityPortal.API.Modules.Finance.Models;

namespace UniversityPortal.API.Modules.Finance.DTOs;

public class RecordPaymentRequest : IValidatableObject
{
    [Required]
    public Guid? RequestId { get; set; }

    [Required, StringLength(20, MinimumLength = 1)]
    public string StudentNumber { get; set; } = string.Empty;

    [StringLength(20)]
    public string? InvoiceNumber { get; set; }

    [Required, Range(typeof(decimal), "0.01", "100000000")]
    public decimal? Amount { get; set; }

    [Required]
    public DateOnly? PaymentDate { get; set; }

    [Required, StringLength(30, MinimumLength = 1)]
    public string Method { get; set; } = string.Empty;

    [StringLength(60)]
    public string? Reference { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RequestId == Guid.Empty)
            yield return new ValidationResult("RequestId must not be empty.", [nameof(RequestId)]);

        if (string.IsNullOrWhiteSpace(StudentNumber))
            yield return new ValidationResult("StudentNumber is required.", [nameof(StudentNumber)]);

        if (string.IsNullOrWhiteSpace(Method))
            yield return new ValidationResult("Method is required.", [nameof(Method)]);

        if (Amount is { } amount && decimal.Round(amount, 2) != amount)
            yield return new ValidationResult(
                "Amount can have at most 2 decimal places.",
                [nameof(Amount)]);
    }
}

public record PaymentResultDto(
    Guid RequestId,
    PaymentRequestStatus Status,
    string? ReceiptNumber,
    string StudentNumber,
    string? InvoiceNumber,
    decimal Amount,
    DateOnly PaymentDate,
    string Method,
    string? Message);
