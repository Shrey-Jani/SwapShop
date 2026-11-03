using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SwapShop.Core.Constants;
using SwapShop.Data;

    public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services,
        string managerEmail,
        string managerPassword)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { Roles.Manager, Roles.Staff })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var manager = await userManager.FindByEmailAsync(managerEmail);
        if(manager is null)
        {
            manager = new ApplicationUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                EmailConfirmed = true,
                FirstName = "Default",
                LastName = "Manager",
            };
            var result = await userManager.CreateAsync(manager, managerPassword);
            if(!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not create the seed manager: " + 
                    string.Join(";", result.Errors.Select(e => e.Description))
                );
            }
        }
        if(!await userManager.IsInRoleAsync(manager, Roles.Manager))
        {
            await userManager.AddToRoleAsync(manager, Roles.Manager);
        }
    }
}