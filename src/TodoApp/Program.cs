using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TodoApp.Data;
using TodoApp.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseInMemoryDatabase(
        builder.Configuration["TodoDatabaseName"] ?? TodoDbContext.DefaultDatabaseName));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar/v1");
}

app.MapTodoEndpoints();

app.Run();

public partial class Program;
