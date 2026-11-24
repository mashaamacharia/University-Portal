using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityPortal.API.Modules.Finance.Models;

namespace UniversityPortal.API.Infrastructure.Persistence.Configurations;

public class PaymentRequestConfiguration : IEntityTypeConfiguration<PaymentRequest>
{
    public void Configure(EntityTypeBuilder<PaymentRequest> builder)
    {
        builder.ToTable("PaymentRequests");
        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.StudentNumber).HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.InvoiceNumber).HasMaxLength(20);
        builder.Property(payment => payment.Method).HasMaxLength(30).IsRequired();
        builder.Property(payment => payment.Reference).HasMaxLength(60);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.PayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(payment => payment.ReceiptNumber).HasMaxLength(30);
        builder.Property(payment => payment.FailureReason).HasMaxLength(500);

        builder.HasIndex(payment => new { payment.StudentNumber, payment.CreatedAtUtc });
        builder.HasIndex(payment => payment.Status);
    }
}
