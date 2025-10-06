﻿using MyApp.Application.WeatherForecasts.Queries.GetWeatherForecasts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MyApp.Web.Endpoints;

public class WeatherForecasts : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetWeatherForecasts);
    }

    public async Task<Ok<IEnumerable<WeatherForecast>>> GetWeatherForecasts(ISender sender)
    {
        var forecasts = await sender.Send(new GetWeatherForecastsQuery());

        return TypedResults.Ok(forecasts);
    }

}
