using System.Net.Http.Json;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Api.Integracoes;

/// <summary>
/// Envia os alertas ao serviço de alertas (SOA) por HTTP:
/// alerta precoce em POST /alertas e emergência em POST /alertas/emergencia.
/// </summary>
public sealed class HttpAlertaClient : IAlertaClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpAlertaClient> _logger;

    public HttpAlertaClient(HttpClient http, ILogger<HttpAlertaClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> EnviarAsync(Alerta alerta, BocaDeLobo boca, CancellationToken cancellationToken)
    {
        var rota = alerta.Nivel == NivelAlerta.Emergencia ? "alertas/emergencia" : "alertas";
        var payload = AlertaExternoPayload.De(alerta, boca);

        try
        {
            using var resposta = await _http.PostAsJsonAsync(rota, payload, cancellationToken);

            if (resposta.IsSuccessStatusCode)
            {
                _logger.LogInformation("Alerta {AlertaId} ({Nivel}) enviado ao serviço de alertas.", alerta.Id, alerta.Nivel);
                return true;
            }

            _logger.LogWarning(
                "Serviço de alertas respondeu {StatusCode} para o alerta {AlertaId}; o alerta segue registrado no banco.",
                (int)resposta.StatusCode, alerta.Id);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao enviar o alerta {AlertaId} ao serviço de alertas; o alerta segue registrado no banco.", alerta.Id);
            return false;
        }
    }

    /// <summary>Contrato enviado ao serviço de alertas. Contém só dados da boca de lobo (infraestrutura pública).</summary>
    private sealed record AlertaExternoPayload(
        int AlertaId,
        int BocaDeLoboId,
        string CodigoBocaDeLobo,
        string Endereco,
        double Latitude,
        double Longitude,
        string Nivel,
        string Mensagem,
        DateTime CriadoEm)
    {
        public static AlertaExternoPayload De(Alerta alerta, BocaDeLobo boca) => new(
            alerta.Id,
            boca.Id,
            boca.Codigo,
            boca.Endereco,
            boca.Latitude,
            boca.Longitude,
            alerta.Nivel.ToString(),
            alerta.Mensagem,
            alerta.CriadoEm);
    }
}
