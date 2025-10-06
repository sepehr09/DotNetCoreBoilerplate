using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;

namespace MyApp.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
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
    }

    public AppTenantInfo? GetCurrentTenant()
    {

        var multiTenantContext = _httpContextAccessor.HttpContext?.GetMultiTenantContext<AppTenantInfo>();
        return multiTenantContext?.TenantInfo;
    }

    public string? GetCurrentTenantId()
    {
        return GetCurrentTenant()?.Id;
    }

    public string? GetCurrentTenantIdentifier()
    {
        return GetCurrentTenant()?.Identifier;
    }
}
