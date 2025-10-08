using MyApp.Application.Common.Models;
using MyApp.Application.Common.Models.Auth;

namespace MyApp.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    Task<(Result Result, AuthResponse Response)> AuthenticateUserAsync(string email, string password);
}
