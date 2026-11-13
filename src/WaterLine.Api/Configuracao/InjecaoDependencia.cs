using Microsoft.Extensions.Options;
using WaterLine.Api.Armazenamento;
using WaterLine.Api.Servicos;
using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Interfaces;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Configuracao;

/// <summary>
/// Registro de todas as dependências da aplicação, para manter o Program.cs enxuto.
/// </summary>
public static class InjecaoDependencia
{
    /// <summary>
    /// Regras de negócio do domínio. Os serviços de domínio não guardam estado e por isso são singletons.
    /// Os parâmetros vêm do appsettings.json e são validados na inicialização.
    /// </summary>
    public static IServiceCollection AddRegrasDeNegocio(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        AdicionarOpcoes<RegrasDiagnosticoOptions>(services, configuration, RegrasDiagnosticoOptions.Secao);
        AdicionarOpcoes<RegrasDespachoOptions>(services, configuration, RegrasDespachoOptions.Secao);

        services.AddSingleton<IDiagnosticoService, DiagnosticoService>();
        services.AddSingleton<IAberturaChamadoService, AberturaChamadoService>();
        services.AddSingleton<IDespachoService, DespachoService>();

        return services;
    }

    /// <summary>Integrações de infraestrutura: armazenamento das fotos.</summary>
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        AdicionarOpcoes<ArmazenamentoFotosOptions>(services, configuration, ArmazenamentoFotosOptions.Secao);
        services.AddSingleton<IArmazenamentoFotos, ArmazenamentoFotosLocal>();

        return services;
    }

    /// <summary>Casos de uso da API (um por recurso). Usam o DbContext, por isso são scoped.</summary>
    public static IServiceCollection AddServicosDeAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IBocaDeLoboAppService, BocaDeLoboAppService>();
        services.AddScoped<ILeituraAppService, LeituraAppService>();
        services.AddScoped<IDespachoAppService, DespachoAppService>();
        services.AddScoped<IChamadoAppService, ChamadoAppService>();
        services.AddScoped<IAgenteAppService, AgenteAppService>();

        return services;
    }

    /// <summary>
    /// Associa a seção do appsettings à classe de opções, valida na inicialização e registra
    /// a instância pronta, para que o domínio não dependa de IOptions.
    /// </summary>
    private static void AdicionarOpcoes<TOpcoes>(IServiceCollection services, IConfiguration configuration, string secao)
        where TOpcoes : class
    {
        services.AddOptions<TOpcoes>()
            .Bind(configuration.GetSection(secao))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TOpcoes>>().Value);
    }
}
