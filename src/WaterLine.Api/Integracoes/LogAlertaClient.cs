using WaterLine.Domain.Entidades;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Api.Integracoes;

public sealed class LogAlertaClient : IAlertaClient
{
    private readonly ILogger<LogAlertaClient> _logger;

    public LogAlertaClient(ILogger<LogAlertaClient> logger)
    {
        _logger = logger;
    }

    public Task<bool> EnviarAsync(Alerta alerta, BocaDeLobo boca, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "ALERTA {Nivel} na boca de lobo {Codigo} (alerta {AlertaId}): {Mensagem} [serviço de alertas não configurado: registrado só em log]",
            alerta.Nivel, boca.Codigo, alerta.Id, alerta.Mensagem);

        return Task.FromResult(true);
    }
}
