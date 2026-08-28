using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TodoApp.Contracts;

namespace TodoApp.Tests;

public sealed class TodoApiTests
{
    [Fact]
    public async Task GetTodos_WhenEmpty_ReturnsEmptyList()
    {
        await using var factory = new TodoApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoResponse>>();
        Assert.NotNull(todos);
        Assert.Empty(todos);
    }

    [Fact]
    public async Task CreateTodo_ReturnsCreatedTodoAndLocation()
    {
        await using var factory = new TodoApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/todos",
            new CreateTodoRequest("Buy milk", DateTime.UtcNow.AddDays(1)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(todo);
        Assert.NotEqual(Guid.Empty, todo.Id);
        Assert.Equal("Buy milk", todo.Title);
        Assert.False(todo.IsCompleted);
        Assert.NotEqual(default, todo.CreatedAt);
        Assert.NotNull(todo.DueDate);
    }

    [Fact]
    public async Task GetTodo_ReturnsFoundTodoAndNotFoundForUnknownId()
    {
        await using var factory = new TodoApiFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodo(client, "Read a book");

        var foundResponse = await client.GetAsync($"/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, foundResponse.StatusCode);
        var found = await foundResponse.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal(created.Title, found.Title);

        var missingResponse = await client.GetAsync($"/todos/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateTodo_MutatesFieldsAndReturnsNotFoundForUnknownId()
    {
        await using var factory = new TodoApiFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodo(client, "Draft outline");
        var dueDate = DateTime.UtcNow.AddDays(3);

        var response = await client.PutAsJsonAsync(
            $"/todos/{created.Id}",
            new UpdateTodoRequest("Finish outline", true, dueDate));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Finish outline", updated.Title);
        Assert.True(updated.IsCompleted);
        Assert.Equal(dueDate, updated.DueDate);

        var missingResponse = await client.PutAsJsonAsync(
            $"/todos/{Guid.NewGuid()}",
            new UpdateTodoRequest("Missing", false, null));
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_ReturnsNoContentAndRemovesTodo()
    {
        await using var factory = new TodoApiFactory();
        using var client = factory.CreateClient();
        var created = await CreateTodo(client, "Archive notes");

        var deleteResponse = await client.DeleteAsync($"/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTodo_WithEmptyTitle_ReturnsBadRequest(string title)
    {
        await using var factory = new TodoApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/todos",
            new CreateTodoRequest(title, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<TodoResponse> CreateTodo(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/todos", new CreateTodoRequest(title, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoResponse>())!;
    }

    private sealed class TodoApiFactory : WebApplicationFactory<Program>
    {
        private readonly string databaseName = $"TodoTests-{Guid.NewGuid():N}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TodoDatabaseName"] = databaseName
                }));
        }
    }
}
