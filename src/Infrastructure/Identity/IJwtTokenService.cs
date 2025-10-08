
namespace MyApp.Infrastructure.Identity;

public interface IJwtTokenService
{
    string GenerateJwtToken(ApplicationUser user, string? tenantAuthority = null, string? tenantIssuer = null, string? tenantAudience = null);
    string GenerateRefreshToken();
}
