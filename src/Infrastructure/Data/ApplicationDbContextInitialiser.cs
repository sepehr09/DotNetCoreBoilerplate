using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Constants;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ITenantService tenantService)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
    }

    private readonly ITenantService _tenantService;

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        try
        {
            // Load tenants into cache
            await _tenantService.LoadTenantsIntoCacheAsync();
            // Default roles
            var administratorRole = new IdentityRole(Roles.Administrator);

            if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
            {
                await _roleManager.CreateAsync(administratorRole);
            }

            // Default users
            var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

            if (_userManager.Users.All(u => u.UserName != administrator.UserName))
            {
                await _userManager.CreateAsync(administrator, "Administrator1!");
                if (!string.IsNullOrWhiteSpace(administratorRole.Name))
                {
                    await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
                }
            }

            // Seed, if necessary
            if (!_context.Tenants.Any())
            {
                _context.Tenants.Add(new Tenant
                {
                    Id = 1,
                    Identifier = "TestTenant",
                    Name = "Tenant just for test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }
}
