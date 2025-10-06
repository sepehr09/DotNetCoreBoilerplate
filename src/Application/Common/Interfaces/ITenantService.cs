using MyApp.Domain.Entities;

namespace MyApp.Application.Common.Interfaces;

public interface ITenantService
{
    // Tenant management operations
    Task<List<Tenant>> GetAllTenantsAsync();
    Task<Tenant?> GetTenantByIdAsync(string id);
    Task<Tenant?> GetTenantByIdentifierAsync(string identifier);
    Task<Tenant> CreateTenantAsync(Tenant tenant);
    Task<Tenant> UpdateTenantAsync(Tenant tenant);
    Task DeleteTenantAsync(string id);

    // Current tenant operations
    AppTenantInfo? GetCurrentTenant();
    string? GetCurrentTenantId();
    string? GetCurrentTenantIdentifier();
}
