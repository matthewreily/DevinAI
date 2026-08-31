# TodoApp

A .NET 10 Web API for managing todos, backed by Entity Framework Core's
in-memory database provider.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build, run, and test

```bash
dotnet build DevinAI.sln
dotnet test DevinAI.sln
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://127.0.0.1:5080 \
dotnet run --project src/TodoApp --no-launch-profile
```

The API listens on `http://localhost:5080` in the example above. The
in-memory database is reset when the application stops.

## API

| Method | Path | Description |
| --- | --- | --- |
| GET | `/todos` | List all todos |
| GET | `/todos/{id}` | Get a todo by ID |
| POST | `/todos` | Create a todo |
| PUT | `/todos/{id}` | Update a todo |
| DELETE | `/todos/{id}` | Delete a todo |

In Development, the interactive Scalar API reference is available at
[`http://localhost:5080/scalar/v1`](http://localhost:5080/scalar/v1), backed by
the OpenAPI document at `/openapi/v1.json`.

## Example requests

Run these commands while the application is running on port 5080:

```bash
curl http://localhost:5080/todos

TODO_ID=$(curl -s -X POST http://localhost:5080/todos \
  -H 'Content-Type: application/json' \
  -d '{"title":"Buy groceries","dueDate":"2030-01-01T00:00:00Z"}' \
  | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')

curl http://localhost:5080/todos/$TODO_ID

curl -X PUT http://localhost:5080/todos/$TODO_ID \
  -H 'Content-Type: application/json' \
  -d '{"title":"Buy groceries and coffee","isCompleted":true,"dueDate":null}'

curl -i -X DELETE http://localhost:5080/todos/$TODO_ID

curl -i http://localhost:5080/scalar/v1
```
