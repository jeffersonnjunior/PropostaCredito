using Application.DependencyInjection;
using Infrastructure.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.DependencyInjectionApplication(builder.Configuration);
builder.Services.DependencyInjectionInfrastructure(builder.Configuration);
builder.Services.DependencyInjectionWorkers();

var host = builder.Build();

await host.Services.MigrateDatabaseAsync();

await host.RunAsync();
