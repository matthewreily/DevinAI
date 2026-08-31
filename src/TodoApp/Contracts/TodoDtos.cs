namespace TodoApp.Contracts;

public sealed record CreateTodoRequest(string? Title, DateTime? DueDate);

public sealed record UpdateTodoRequest(string? Title, bool IsCompleted, DateTime? DueDate);

public sealed record TodoResponse(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? DueDate);
