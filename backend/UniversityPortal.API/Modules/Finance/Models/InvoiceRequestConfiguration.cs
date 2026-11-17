using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UniversityPortal.API.Modules.Finance.Models;

public class InvoiceRequestConfiguration : IEntityTypeConfiguration<InvoiceRequest>
{
    public void Configure(EntityTypeBuilder<InvoiceRequest> builder)
    {
        builder.ToTable("InvoiceRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.IdempotencyKey).HasMaxLength(35).IsRequired();
        builder.HasIndex(request => request.IdempotencyKey).IsUnique();

        builder.Property(request => request.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(request => request.StudentNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(request => request.StudentNumber);

        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(request => request.BcInvoiceId);

        builder.Property(request => request.BcInvoiceNumber).HasMaxLength(50);
        builder.Property(request => request.LastError).HasMaxLength(1000);
        builder.Property(request => request.Version).IsRowVersion();
    }
}
