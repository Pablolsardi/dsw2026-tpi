using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Api.Configurations;

public static class IdentitySeedConfigurationExtensions
{
    public static async Task SeedAdminUser(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = app.Configuration["AdminUser:Email"];
        var password = app.Configuration["AdminUser:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        if (await userManager.FindByEmailAsync(email) is not null) return;

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Administrator);
            app.Logger.LogInformation("Usuario admin creado: {Email}", email);
        }
        else
        {
            app.Logger.LogError("Fallo el seed del admin: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}