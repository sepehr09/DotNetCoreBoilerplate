using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApp.Application.Common.Exceptions;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;

namespace MyApp.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMultiTenantStore<AppTenantInfo> _tenantStore;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IMultiTenantStore<AppTenantInfo> tenantStore,
        ILogger<TenantService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _tenantStore = tenantStore;
        _logger = logger;
    }

    public async Task<List<Tenant>> GetAllTenantsAsync()
    {
        return await _context.Tenants.ToListAsync();
    }

    public async Task<Tenant?> GetTenantByIdAsync(string id)
    {
        return await _context.Tenants.FindAsync(id);
    }

    public async Task<Tenant?> GetTenantByIdentifierAsync(string identifier)
    {
        return await _context.Tenants.FirstOrDefaultAsync(t => t.Identifier == identifier);
    }

    public async Task<Tenant> CreateTenantAsync(Tenant tenant)
    {
        // Ensure the tenant has a valid identifier
        if (string.IsNullOrWhiteSpace(tenant.Identifier))
        {
            throw new ArgumentException("Tenant identifier is required");
        }

        // Check if tenant with same identifier already exists
        var existingTenant = await GetTenantByIdentifierAsync(tenant.Identifier);
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"Tenant with identifier '{tenant.Identifier}' already exists");
        }

        tenant.Id = tenant.Id; // Keep the existing ID as string
        tenant.CreatedAt = DateTime.UtcNow;
        tenant.UpdatedAt = DateTime.UtcNow;

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Cache the newly created tenant
        await AddTenantToCacheAsync(tenant);

        return tenant;
    }

    public async Task<Tenant> UpdateTenantAsync(Tenant tenant)
    {
        var existingTenant = await _context.Tenants.FindAsync(tenant.Id);
        if (existingTenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID '{tenant.Id}' not found");
        }

        // Check if another tenant has the same identifier
        var conflictingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == tenant.Identifier && t.Id != tenant.Id);

        if (conflictingTenant != null)
        {
            throw new InvalidOperationException($"Tenant with identifier '{tenant.Identifier}' already exists");
        }

        existingTenant.Identifier = tenant.Identifier;
        existingTenant.Name = tenant.Name;
        existingTenant.IsActive = tenant.IsActive;
        existingTenant.UpdatedAt = DateTime.UtcNow;
        existingTenant.UpdatedBy = tenant.UpdatedBy;

        await _context.SaveChangesAsync();

        // Cache the updated tenant
        await UpdateTenantInCacheAsync(existingTenant);

        return existingTenant;
    }

    public async Task DeleteTenantAsync(string id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with ID '{id}' not found");
        }

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();

        // Remove the tenant from cache
        await RemoveTenantFromCacheAsync(id);
    }

    public AppTenantInfo GetCurrentTenant()
    {
        var multiTenantContext = _httpContextAccessor.HttpContext?.GetMultiTenantContext<AppTenantInfo>();
        var tenant = multiTenantContext?.TenantInfo;

        if (tenant == null)
        {
            throw new TenantNotDefinedException("Tenant is not defined");
        }

        return tenant;
    }

    public Task LoadTenantsIntoCacheAsync()
    {
        // Run in background to avoid blocking startup
        return Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Loading tenants from database into cache...");

                var tenants = await _context.Tenants.ToListAsync();

                if (!tenants.Any())
                {
                    _logger.LogInformation("No tenants found in database. Skipping cache loading.");
                    return;
                }

                var loadedCount = 0;
                var skippedCount = 0;

                foreach (var tenant in tenants)
                {
                    if (!tenant.IsActive)
                    {
                        _logger.LogWarning("Skipping inactive tenant: {Identifier}", tenant.Identifier);
                        skippedCount++;
                        continue;
                    }

                    var appTenantInfo = new AppTenantInfo
                    {
                        Id = tenant.Id.ToString(),
                        Identifier = tenant.Identifier,
                        Name = tenant.Name,
                        IsActive = tenant.IsActive,
                        CreatedAt = tenant.CreatedAt,
                        CreatedBy = tenant.CreatedBy,
                        UpdatedAt = tenant.UpdatedAt,
                        UpdatedBy = tenant.UpdatedBy
                    };

                    // Use TryAddAsync to add tenant to the store
                    var result = await _tenantStore.TryAddAsync(appTenantInfo);

                    if (result)
                    {
                        loadedCount++;
                        _logger.LogInformation("Loaded tenant into cache: {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
                    }
                    else
                    {
                        skippedCount++;
                        _logger.LogWarning("Tenant already exists in cache: {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
                    }
                }

                _logger.LogInformation("Tenant cache loading completed. Loaded: {LoadedCount}, Skipped: {SkippedCount}", loadedCount, skippedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading tenants into cache");
                // Don't throw the exception to prevent application startup failure
                // Just log the error and continue
            }
        });
    }

    private async Task AddTenantToCacheAsync(Tenant tenant)
    {
        if (!tenant.IsActive)
        {
            _logger.LogWarning("Skipping cache for inactive tenant: {Identifier}", tenant.Identifier);
            return;
        }

        var appTenantInfo = new AppTenantInfo
        {
            Id = tenant.Id.ToString(),
            Identifier = tenant.Identifier,
            Name = tenant.Name,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            UpdatedAt = tenant.UpdatedAt,
            UpdatedBy = tenant.UpdatedBy
        };

        try
        {
            var result = await _tenantStore.TryAddAsync(appTenantInfo);
            if (result)
            {
                _logger.LogInformation("Added tenant to cache: {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
            }
            else
            {
                _logger.LogWarning("Failed to add tenant to cache (already exists): {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tenant to cache: {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
        }
    }

    private async Task UpdateTenantInCacheAsync(Tenant tenant)
    {
        if (!tenant.IsActive)
        {
            _logger.LogWarning("Skipping cache for inactive tenant: {Identifier}", tenant.Identifier);
            return;
        }

        var appTenantInfo = new AppTenantInfo
        {
            Id = tenant.Id.ToString(),
            Identifier = tenant.Identifier,
            Name = tenant.Name,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            UpdatedAt = tenant.UpdatedAt,
            UpdatedBy = tenant.UpdatedBy
        };

        try
        {
            var result = await _tenantStore.TryUpdateAsync(appTenantInfo);
            if (result)
            {
                _logger.LogInformation("Updated tenant in cache: {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
            }
            else
            {
                _logger.LogWarning("Failed to update tenant in cache (not found): {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant in cache: {Identifier} (ID: {Id})", tenant.Identifier, tenant.Id);
        }
    }

    private async Task RemoveTenantFromCacheAsync(string id)
    {
        try
        {
            var result = await _tenantStore.TryRemoveAsync(id);
            if (result)
            {
                _logger.LogInformation("Removed tenant from cache: {Id}", id);
            }
            else
            {
                _logger.LogWarning("Tenant not found in cache for removal: {Id}", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing tenant from cache: {Id}", id);
        }
    }
}
