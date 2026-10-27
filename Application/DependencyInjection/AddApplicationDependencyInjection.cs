using Application.Propostas.GerarProposta;
using Application.Propostas.ObterProposta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.DependencyInjection;

public static class AddApplicationDependencyInjection
{
    public static void DependencyInjectionApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services.AddScoped<GerarPropostaFromClienteCadastradoHandler>();
        services.AddScoped<ObterPropostaHandler>();
    }
}
