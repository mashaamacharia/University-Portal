using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniversityPortal.API.Modules.Administration.Models;

namespace UniversityPortal.API.Infrastructure.Persistence;

public class PortalDbContext(DbContextOptions<PortalDbContext> options)
    : IdentityDbContext<PortalUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PortalUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(200).IsRequired();
            entity.Property(user => user.StudentNumber).HasMaxLength(20);
            entity.HasIndex(user => user.StudentNumber).IsUnique();
        });
    }
}
