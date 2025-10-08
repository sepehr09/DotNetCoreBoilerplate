using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    private readonly ITenantService _tenantService;
    private readonly IConfiguration _configuration;

    private readonly IHostEnvironment _environment;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ITenantService tenantService, IConfiguration configuration, IHostEnvironment environment)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.MigrateAsync();

            // Load tenants into cache only if multi-tenancy is enabled
            if (_configuration.GetValue<bool>("IsMultiTenant"))
            {
                await _tenantService.LoadTenantsIntoCacheAsync();
            }
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
            // Check if we're in multi-tenant mode
            var isMultiTenant = _configuration.GetValue<bool>("IsMultiTenant");

            // Default roles
            var administratorRole = new IdentityRole(Roles.Administrator);

            try
            {
                if (!string.IsNullOrWhiteSpace(administratorRole.Name) && !await _roleManager.RoleExistsAsync(administratorRole.Name))
                {
                    await _roleManager.CreateAsync(administratorRole);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error creating role, continuing with seeding...");
            }

            // Seed default users only in development environment
            if (_environment.IsDevelopment())
            {
                // Default users
                var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

                try
                {
                    // Check if admin user already exists
                    bool hasAdminUser = await _userManager.Users.AnyAsync(u => u.UserName == administrator.UserName);

                    if (!hasAdminUser)
                    {
                        var result = await _userManager.CreateAsync(administrator, "Administrator1!");
                        if (result.Succeeded && !string.IsNullOrWhiteSpace(administratorRole.Name))
                        {
                            await _userManager.AddToRolesAsync(administrator, new[] { administratorRole.Name });
                        }
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error creating default user, continuing with seeding...");
                }
            }

            // Seed tenants only if not multi-tenant
            if (!_context.Tenants.Any() && isMultiTenant)
            {
                _context.Tenants.Add(new Tenant
                {
                    Id = 2,
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
            // Don't throw the exception to prevent application startup failure
            // Just log the error and continue
        }
    }
}
