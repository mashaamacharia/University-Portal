using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniversityPortal.API.Modules.Finance.Models;

namespace UniversityPortal.API.Infrastructure.Persistence.Configurations;

public class ReceiptIssueConfiguration : IEntityTypeConfiguration<ReceiptIssue>
{
    public void Configure(EntityTypeBuilder<ReceiptIssue> builder)
    {
        builder.ToTable("ReceiptIssues");
        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.ReceiptNumber).HasMaxLength(30).IsRequired();
        builder.Property(issue => issue.StudentNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(issue => new
        {
            issue.ReceiptNumber,
            issue.StudentNumber,
            issue.CopyNumber
        }).IsUnique();
    }
}
