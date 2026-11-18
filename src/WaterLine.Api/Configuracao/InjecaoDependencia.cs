using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WaterLine.Api.Armazenamento;
using WaterLine.Api.Autenticacao;
using WaterLine.Api.Integracoes;
using WaterLine.Api.Servicos;
using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Interfaces;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Configuracao;


public static class InjecaoDependencia
{
    public const string PoliticaLimiteLogin = "login";

    private const int TentativasDeLoginPorMinuto = 10;

    public static IServiceCollection AddRegrasDeNegocio(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        AdicionarOpcoes<RegrasDiagnosticoOptions>(services, configuration, RegrasDiagnosticoOptions.Secao);
        AdicionarOpcoes<RegrasDespachoOptions>(services, configuration, RegrasDespachoOptions.Secao);
        AdicionarOpcoes<RegrasPrevencaoOptions>(services, configuration, RegrasPrevencaoOptions.Secao);

        services.AddSingleton<IDiagnosticoService, DiagnosticoService>();
        services.AddSingleton<IAberturaChamadoService, AberturaChamadoService>();
        services.AddSingleton<IDespachoService, DespachoService>();
        services.AddSingleton<IPrevencaoService, PrevencaoService>();
        services.AddSingleton<ISegurancaTrabalhadorService, SegurancaTrabalhadorService>();

        return services;
    }

    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        AdicionarOpcoes<ArmazenamentoFotosOptions>(services, configuration, ArmazenamentoFotosOptions.Secao);
        services.AddSingleton<IArmazenamentoFotos, ArmazenamentoFotosLocal>();

        AdicionarOpcoes<AlertasOptions>(services, configuration, AlertasOptions.Secao);
        var opcoesAlertas = configuration.GetSection(AlertasOptions.Secao).Get<AlertasOptions>() ?? new AlertasOptions();

        if (opcoesAlertas.UsaServicoExterno)
        {
            // Typed HttpClient: o IHttpClientFactory gerencia as conexões; timeout curto para não travar a leitura.
            services.AddHttpClient<IAlertaClient, HttpAlertaClient>(cliente =>
            {
                cliente.BaseAddress = new Uri(opcoesAlertas.BaseUrl!.TrimEnd('/') + "/");
                cliente.Timeout = TimeSpan.FromSeconds(opcoesAlertas.TimeoutSegundos);
            });
        }
        else
        {
            services.AddSingleton<IAlertaClient, LogAlertaClient>();
        }

        return services;
    }

    public static IServiceCollection AddAutenticacao(this IServiceCollection services, IConfiguration configuration)
    {
        AdicionarOpcoes<JwtOptions>(services, configuration, JwtOptions.Secao);

        services.AddSingleton<IPasswordHasher<Agente>, PasswordHasher<Agente>>();
        services.AddSingleton<ITokenService, TokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtOptions>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TokenService.CriarChave(jwt),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        services.AddRateLimiter(limitador =>
        {
            limitador.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limitador.AddPolicy(PoliticaLimiteLogin, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = TentativasDeLoginPorMinuto,
                    Window = TimeSpan.FromMinutes(1)
                }));
        });

        return services;
    }

    public static IServiceCollection AddServicosDeAplicacao(this IServiceCollection services)
    {
        services.AddScoped<IBocaDeLoboAppService, BocaDeLoboAppService>();
        services.AddScoped<ILeituraAppService, LeituraAppService>();
        services.AddScoped<IDespachoAppService, DespachoAppService>();
        services.AddScoped<IChamadoAppService, ChamadoAppService>();
        services.AddScoped<IAgenteAppService, AgenteAppService>();
        services.AddScoped<IAutenticacaoAppService, AutenticacaoAppService>();
        services.AddScoped<IPrevisaoAppService, PrevisaoAppService>();
        services.AddScoped<IAlertaAppService, AlertaAppService>();

        return services;
    }

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
