﻿using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Application.TodoLists.Commands.CreateTodoList;
using MyApp.Application.TodoLists.Commands.DeleteTodoList;
using MyApp.Application.TodoLists.Commands.UpdateTodoList;
using MyApp.Application.TodoLists.Queries.GetTodos;

namespace MyApp.Web.Endpoints;

public class TodoLists : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTodoLists).RequireAuthorization();
        groupBuilder.MapPost(CreateTodoList); // .RequireAuthorization();
        groupBuilder.MapPut(UpdateTodoList, "{id}"); // .RequireAuthorization();
        groupBuilder.MapDelete(DeleteTodoList, "{id}"); // .RequireAuthorization();
    }

    public async Task<Ok<TodosVm>> GetTodoLists(ISender sender)
    {
        var vm = await sender.Send(new GetTodosQuery());

        return TypedResults.Ok(vm);
    }

    public async Task<Created<int>> CreateTodoList(ISender sender, CreateTodoListCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(TodoLists)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateTodoList(ISender sender, int id, UpdateTodoListCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteTodoList(ISender sender, int id)
    {
        await sender.Send(new DeleteTodoListCommand(id));

        return TypedResults.NoContent();
    }
}
