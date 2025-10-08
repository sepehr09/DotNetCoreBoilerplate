using MyApp.Application.Common.Models;
using MyApp.Application.Common.Models.Auth;
using MyApp.Application.Users.Commands.LoginUser;
using MyApp.Application.Users.Commands.RegisterUser;

namespace MyApp.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "Register");
        groupBuilder.MapPost(Login, "Login");
    }

    public async Task<Result<AuthResponse>> Register(RegisterUserCommand command, ISender sender) => await sender.Send(command);
    public async Task<Result<AuthResponse>> Login(LoginUserCommand command, ISender sender) => await sender.Send(command);
}
