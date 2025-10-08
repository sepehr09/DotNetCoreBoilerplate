using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MyApp.Application.Common.Exceptions;
using MyApp.Application.Common.Interfaces;
using MyApp.Application.Common.Models;
using MyApp.Application.Common.Models.Auth;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITenantService _tenantService;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        IJwtTokenService jwtTokenService,
        ITenantService tenantService)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _jwtTokenService = jwtTokenService;
        _tenantService = tenantService;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }

    public async Task<(Result Result, AuthResponse Response)> AuthenticateUserAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return (Result.Failure(new[] { "Invalid credentials" }), null!);
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);

        if (!isValidPassword)
        {
            return (Result.Failure(new[] { "Invalid credentials" }), null!);
        }

        // Get current tenant for JWT authority
        AppTenantInfo? currentTenant = null;
        try
        {
            currentTenant = _tenantService.GetCurrentTenant();
        }
        catch (TenantNotDefinedException)
        {
            // If no tenant is defined, use default JWT settings
        }

        var token = _jwtTokenService.GenerateJwtToken(
            user,
            currentTenant?.JwtAuthority,
            currentTenant?.JwtIssuer,
            currentTenant?.JwtAudience);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        return (Result.Success(), new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken
        });
    }
}
