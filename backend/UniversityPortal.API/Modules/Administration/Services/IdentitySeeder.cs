using Microsoft.AspNetCore.Identity;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Modules.Administration.Models;

namespace UniversityPortal.API.Modules.Administration.Services;

public static class IdentitySeeder
{
    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<PortalUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        foreach (var role in new[] { Roles.Student, Roles.FinanceOfficer, Roles.Administrator })
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var created = await roleManager.CreateAsync(
                new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role {role}: " +
                    string.Join("; ", created.Errors.Select(error => error.Description)));
            }
        }

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("No seed administrator configured; skipping.");
            return;
        }

        if ((await userManager.GetUsersInRoleAsync(Roles.Administrator)).Count > 0)
            return;

        var admin = new PortalUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "System Administrator",
            CreatedAtUtc = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not create the seed administrator: " +
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        var addedToRole = await userManager.AddToRoleAsync(admin, Roles.Administrator);
        if (!addedToRole.Succeeded)
        {
            throw new InvalidOperationException(
                "Could not assign the seed administrator role: " +
                string.Join("; ", addedToRole.Errors.Select(error => error.Description)));
        }

        logger.LogWarning("Seed administrator created. Remove Seed:AdminPassword from configuration now.");
    }
}
