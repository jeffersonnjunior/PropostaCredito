using Api.Middlewares;
using Application.DependencyInjection;
using Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.DependencyInjectionApplication(configuration);
builder.Services.DependencyInjectionInfrastructure(configuration);

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
