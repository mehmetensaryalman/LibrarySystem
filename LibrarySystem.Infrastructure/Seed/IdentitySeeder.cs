using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Seed;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles =
        [
            RoleNames.User,
            RoleNames.Admin
        ];

        foreach (var role in roles)
        {
            if (!await roleManager
                    .RoleExistsAsync(role))
            {
                var result =
                    await roleManager.CreateAsync(
                        new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"'{role}' rolü oluşturulamadı.");
                }
            }
        }
    }

    public static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        string? email,
        string? password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "AdminSeed:Email configuration is missing.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "AdminSeed:Password configuration is missing.");
        }

        var admin =
            await userManager.FindByEmailAsync(
                email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(
                    admin,
                    password);

            if (!createResult.Succeeded)
            {
                var errors =
                    string.Join(
                        " ",
                        createResult.Errors.Select(
                            error =>
                                error.Description));

                throw new InvalidOperationException(
                    $"Admin kullanıcısı oluşturulamadı: {errors}");
            }
        }

        if (await userManager.IsInRoleAsync(
                admin,
                RoleNames.User))
        {
            var removeUserRoleResult =
                await userManager.RemoveFromRoleAsync(
                    admin,
                    RoleNames.User);

            if (!removeUserRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin kullanıcısından User rolü kaldırılamadı.");
            }
        }

        if (!await userManager.IsInRoleAsync(
                admin,
                RoleNames.Admin))
        {
            var adminRoleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    RoleNames.Admin);

            if (!adminRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Admin kullanıcısına Admin rolü atanamadı.");
            }
        }
    }

    public static async Task SeedExistingUserRolesAsync(
        UserManager<ApplicationUser> userManager)
    {
        var users =
            await userManager.Users.ToListAsync();

        foreach (var user in users)
        {
            var isAdmin =
                await userManager.IsInRoleAsync(
                    user,
                    RoleNames.Admin);

            var isUser =
                await userManager.IsInRoleAsync(
                    user,
                    RoleNames.User);

            if (isAdmin)
            {
                if (isUser)
                {
                    var removeUserRoleResult =
                        await userManager
                            .RemoveFromRoleAsync(
                                user,
                                RoleNames.User);

                    if (!removeUserRoleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"'{user.Email}' kullanıcısından User rolü kaldırılamadı.");
                    }
                }

                continue;
            }

            if (isUser)
            {
                continue;
            }

            var addUserRoleResult =
                await userManager.AddToRoleAsync(
                    user,
                    RoleNames.User);

            if (!addUserRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"'{user.Email}' kullanıcısına User rolü atanamadı.");
            }
        }
    }
}