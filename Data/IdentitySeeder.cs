
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace VERSUM.KitchenWareERP.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services)
        {
            // Get Identity services
            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                services.GetRequiredService<UserManager<IdentityUser>>();

            var configuration =
                services.GetRequiredService<IConfiguration>();

            // ==========================================
            // CREATE ADMIN AND STAFF ROLES
            // ==========================================

            string[] roles = { "Admin", "Staff" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(
                        new IdentityRole(role));

                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Unable to create role: " + role + " - " +
                            string.Join("; ",
                                roleResult.Errors.Select(
                                    e => e.Description)));
                    }
                }
            }

            // ==========================================
            // READ ADMIN CREDENTIALS SECURELY
            // ==========================================

            var adminEmail =
                configuration["SeedAdmin:Email"];

            var adminPassword =
                configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) ||
                string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            adminEmail = adminEmail.Trim();

            // ==========================================
            // CHECK EXISTING ADMIN ACCOUNT
            // ==========================================

            var existingAdmin =
                await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin != null)
            {
                if (!await userManager.IsInRoleAsync(
                    existingAdmin, "Admin"))
                {
                    throw new InvalidOperationException(
                        "The configured Admin email belongs " +
                        "to an existing non-Admin account.");
                }

                return;
            }

            // ==========================================
            // CREATE FIRST ADMIN ACCOUNT
            // ==========================================

            var admin = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                LockoutEnabled = true
            };

            var createResult =
                await userManager.CreateAsync(
                    admin, adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Unable to create Administrator account: " +
                    string.Join("; ",
                        createResult.Errors.Select(
                            e => e.Description)));
            }

            // ==========================================
            // ASSIGN ADMIN ROLE
            // ==========================================

            var assignResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            if (!assignResult.Succeeded)
            {
                var cleanupResult =
                    await userManager.DeleteAsync(admin);

                throw new InvalidOperationException(
                    "Unable to assign Admin role: " +
                    string.Join("; ",
                        assignResult.Errors.Select(
                            e => e.Description)) +
                    (cleanupResult.Succeeded
                        ? ""
                        : " Account cleanup also failed."));
            }
        }
    }
}
