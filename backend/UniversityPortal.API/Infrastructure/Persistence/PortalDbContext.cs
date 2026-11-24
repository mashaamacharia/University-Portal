using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniversityPortal.API.Common.Auditing;
using UniversityPortal.API.Modules.Administration.Models;
using UniversityPortal.API.Modules.Finance.Models;

namespace UniversityPortal.API.Infrastructure.Persistence;

public class PortalDbContext(DbContextOptions<PortalDbContext> options)
    : IdentityDbContext<PortalUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<InvoiceRequest> InvoiceRequests => Set<InvoiceRequest>();

    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PortalUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(200).IsRequired();
            entity.Property(user => user.StudentNumber).HasMaxLength(20);
            entity.HasIndex(user => user.StudentNumber).IsUnique();
        });

        builder.ApplyConfigurationsFromAssembly(typeof(PortalDbContext).Assembly);
    }
}
