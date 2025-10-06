using MyApp.Infrastructure.Identity;

namespace MyApp.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder
            .MapIdentityApi<ApplicationUser>();
    }
}
