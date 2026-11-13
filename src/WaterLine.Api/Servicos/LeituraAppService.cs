using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Excecoes;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Api.Servicos;

/// <summary>Processa as leituras enviadas pelos ESP32.</summary>
public interface ILeituraAppService
{
    Task<LeituraProcessadaResponse> RegistrarAsync(LeituraRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Orquestra o processamento de uma leitura: carrega o histórico, diagnostica,
/// atualiza o status da boca de lobo e abre o chamado necessário, tudo numa única gravação.
/// As regras de negócio ficam nos serviços do domínio.
/// </summary>
public sealed class LeituraAppService : ILeituraAppService
{
    /// <summary>Tolerância para relógios de dispositivos levemente adiantados.</summary>
    private static readonly TimeSpan ToleranciaFuturo = TimeSpan.FromMinutes(5);

    private readonly WaterLineDbContext _db;
    private readonly IDiagnosticoService _diagnostico;
    private readonly IAberturaChamadoService _aberturaChamado;
    private readonly RegrasDiagnosticoOptions _regrasDiagnostico;
    private readonly TimeProvider _relogio;
    private readonly ILogger<LeituraAppService> _logger;

    public LeituraAppService(
        WaterLineDbContext db,
        IDiagnosticoService diagnostico,
        IAberturaChamadoService aberturaChamado,
        RegrasDiagnosticoOptions regrasDiagnostico,
        TimeProvider relogio,
        ILogger<LeituraAppService> logger)
    {
        _db = db;
        _diagnostico = diagnostico;
        _aberturaChamado = aberturaChamado;
        _regrasDiagnostico = regrasDiagnostico;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<LeituraProcessadaResponse> RegistrarAsync(LeituraRequest request, CancellationToken cancellationToken)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var dataHora = request.DataHora?.UtcDateTime ?? agora;
        if (dataHora > agora + ToleranciaFuturo)
        {
            throw new ArgumentException("A data e hora da leitura não pode estar no futuro.", nameof(request.DataHora));
        }

        var boca = await _db.BocasDeLobo.FirstOrDefaultAsync(b => b.Id == request.BocaDeLoboId, cancellationToken)
                   ?? throw new RecursoNaoEncontradoException("Boca de lobo", request.BocaDeLoboId!.Value);

        if (!boca.Ativa)
        {
            throw new RegraNegocioException($"A boca de lobo {boca.Codigo} está inativa e não aceita leituras.");
        }

        var leitura = new Leitura(
            boca.Id,
            dataHora,
            request.DistanciaExternaCm!.Value,
            request.DistanciaInternaCm!.Value,
            request.Chovendo!.Value);

        // Janela de histórico: o dobro do tempo exigido para tubo entupido, o que basta para
        // medir a água parada e encontrar a leitura anterior (velocidade de subida).
        var inicioJanela = dataHora.AddHours(-2 * _regrasDiagnostico.HorasAguaParadaTubo);
        var historico = await _db.Leituras
            .AsNoTracking()
            .Where(l => l.BocaDeLoboId == boca.Id && l.DataHora < dataHora && l.DataHora >= inicioJanela)
            .OrderByDescending(l => l.DataHora)
            .ToListAsync(cancellationToken);

        var diagnostico = _diagnostico.Diagnosticar(boca, leitura, historico);
        boca.AtualizarStatus(diagnostico.Tipo);
        _db.Leituras.Add(leitura);

        var chamadosAtivos = await _db.Chamados
            .Where(c => c.BocaDeLoboId == boca.Id
                        && (c.Status == StatusChamado.Aberto || c.Status == StatusChamado.Aceito))
            .ToListAsync(cancellationToken);

        var novoChamado = _aberturaChamado.AvaliarAbertura(boca, diagnostico, leitura.Chovendo, chamadosAtivos, agora);
        if (novoChamado is not null)
        {
            _db.Chamados.Add(novoChamado);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Leitura {LeituraId} da boca de lobo {BocaId}: diagnóstico {Diagnostico}.",
            leitura.Id, boca.Id, diagnostico.Tipo);

        if (novoChamado is not null)
        {
            _logger.LogInformation(
                "Chamado {ChamadoId} ({Tipo}) aberto para a boca de lobo {BocaId}.",
                novoChamado.Id, novoChamado.Tipo, boca.Id);
        }

        return new LeituraProcessadaResponse
        {
            LeituraId = leitura.Id,
            BocaDeLoboId = boca.Id,
            DataHora = leitura.DataHora,
            Diagnostico = diagnostico.Tipo,
            NivelCm = Math.Round(diagnostico.NivelCm, 2),
            PercentualOcupacao = Math.Round(diagnostico.PercentualOcupacao, 2),
            VelocidadeSubidaCmPorMinuto = diagnostico.VelocidadeSubidaCmPorMinuto is { } v ? Math.Round(v, 2) : null,
            EntradaBloqueada = diagnostico.EntradaBloqueada,
            ChamadoAbertoId = novoChamado?.Id
        };
    }
}
