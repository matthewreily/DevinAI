using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Contracts;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/todos").WithTags("Todos");

        group.MapGet("", GetTodos)
            .WithName("GetTodos")
            .Produces<List<TodoResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetTodo)
            .WithName("GetTodo")
            .Produces<TodoResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", CreateTodo)
            .WithName("CreateTodo")
            .Produces<TodoResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateTodo)
            .WithName("UpdateTodo")
            .Produces<TodoResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteTodo)
            .WithName("DeleteTodo")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Ok<List<TodoResponse>>> GetTodos(TodoDbContext db)
    {
        var todos = await db.Todos
            .AsNoTracking()
            .OrderBy(todo => todo.CreatedAt)
            .Select(todo => new TodoResponse(
                todo.Id,
                todo.Title,
                todo.IsCompleted,
                todo.CreatedAt,
                todo.DueDate))
            .ToListAsync();

        return TypedResults.Ok(todos);
    }

    private static async Task<Results<Ok<TodoResponse>, NotFound>> GetTodo(
        Guid id,
        TodoDbContext db)
    {
        var todo = await db.Todos.AsNoTracking().SingleOrDefaultAsync(todo => todo.Id == id);
        return todo is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(todo));
    }

    private static async Task<Results<Created<TodoResponse>, BadRequest<ProblemDetails>>> CreateTodo(
        CreateTodoRequest? request,
        TodoDbContext db)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Detail = "Title is required and cannot be empty."
            });
        }

        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            CreatedAt = DateTime.UtcNow,
            DueDate = request.DueDate
        };

        db.Todos.Add(todo);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/todos/{todo.Id}", ToResponse(todo));
    }

    private static async Task<
        Results<Ok<TodoResponse>, BadRequest<ProblemDetails>, NotFound>> UpdateTodo(
        Guid id,
        UpdateTodoRequest? request,
        TodoDbContext db)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Title))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Detail = "Title is required and cannot be empty."
            });
        }

        var todo = await db.Todos.SingleOrDefaultAsync(todo => todo.Id == id);
        if (todo is null)
        {
            return TypedResults.NotFound();
        }

        todo.Title = request.Title.Trim();
        todo.IsCompleted = request.IsCompleted;
        todo.DueDate = request.DueDate;
        await db.SaveChangesAsync();

        return TypedResults.Ok(ToResponse(todo));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteTodo(
        Guid id,
        TodoDbContext db)
    {
        var todo = await db.Todos.SingleOrDefaultAsync(todo => todo.Id == id);
        if (todo is null)
        {
            return TypedResults.NotFound();
        }

        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }

    private static TodoResponse ToResponse(Todo todo) =>
        new(todo.Id, todo.Title, todo.IsCompleted, todo.CreatedAt, todo.DueDate);
}
