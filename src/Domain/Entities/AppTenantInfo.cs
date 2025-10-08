using Finbuckle.MultiTenant;

namespace MyApp.Domain.Entities;

public class AppTenantInfo : TenantInfo
{
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? JwtAuthority { get; set; }
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
}
