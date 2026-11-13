using System.Text.Json.Serialization;
using ToDoApp.Application;
using ToDoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

// Swagger is public: no authentication and no environment restriction.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/api/health");

await app.Services.EnsureDatabaseCreatedAsync();

app.Run();

public partial class Program;
