using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Api.Integracoes;
using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Excecoes;
using WaterLine.Domain.Interfaces;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Servicos;

/// <summary>Processa as leituras enviadas pelos ESP32.</summary>
public interface ILeituraAppService
{
    Task<LeituraProcessadaResponse> RegistrarAsync(LeituraRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Orquestra o processamento de uma leitura, na ordem:
/// <list type="number">
/// <item>diagnóstico e atualização do status da boca de lobo;</item>
/// <item>cancelamento dos chamados de gari se estiver chovendo (segurança do trabalhador);</item>
/// <item>abertura e despacho do chamado de limpeza, se o diagnóstico exigir;</item>
/// <item>registro do alerta (água subindo ou nível crítico);</item>
/// <item>gravação de tudo numa única transação e, depois, envio do alerta ao serviço externo.</item>
/// </list>
/// As regras de negócio ficam nos serviços do domínio; aqui só se carrega, orquestra e grava.
/// </summary>
public sealed class LeituraAppService : ILeituraAppService
{
    /// <summary>Tolerância para relógios de dispositivos levemente adiantados.</summary>
    private static readonly TimeSpan ToleranciaFuturo = TimeSpan.FromMinutes(5);

    private readonly WaterLineDbContext _db;
    private readonly IDiagnosticoService _diagnostico;
    private readonly IAberturaChamadoService _aberturaChamado;
    private readonly ISegurancaTrabalhadorService _segurancaTrabalhador;
    private readonly IDespachoAppService _despacho;
    private readonly IAlertaClient _alertaClient;
    private readonly RegrasDiagnosticoOptions _regrasDiagnostico;
    private readonly AlertasOptions _opcoesAlertas;
    private readonly TimeProvider _relogio;
    private readonly ILogger<LeituraAppService> _logger;

    public LeituraAppService(
        WaterLineDbContext db,
        IDiagnosticoService diagnostico,
        IAberturaChamadoService aberturaChamado,
        ISegurancaTrabalhadorService segurancaTrabalhador,
        IDespachoAppService despacho,
        IAlertaClient alertaClient,
        RegrasDiagnosticoOptions regrasDiagnostico,
        AlertasOptions opcoesAlertas,
        TimeProvider relogio,
        ILogger<LeituraAppService> logger)
    {
        _db = db;
        _diagnostico = diagnostico;
        _aberturaChamado = aberturaChamado;
        _segurancaTrabalhador = segurancaTrabalhador;
        _despacho = despacho;
        _alertaClient = alertaClient;
        _regrasDiagnostico = regrasDiagnostico;
        _opcoesAlertas = opcoesAlertas;
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

        // 1) Diagnóstico.
        var historico = await CarregarHistoricoAsync(boca.Id, dataHora, cancellationToken);
        var diagnostico = _diagnostico.Diagnosticar(boca, leitura, historico);
        boca.AtualizarStatus(diagnostico.Tipo);
        _db.Leituras.Add(leitura);

        var chamadosAtivos = await _db.Chamados
            .Where(c => c.BocaDeLoboId == boca.Id
                        && (c.Status == StatusChamado.Aberto || c.Status == StatusChamado.Aceito))
            .ToListAsync(cancellationToken);

        // 2) Segurança do trabalhador: chovendo, os chamados de gari desta boca de lobo são cancelados.
        var cancelados = _segurancaTrabalhador.CancelarChamadosDeGariPorChuva(leitura, chamadosAtivos);

        // 3) Abertura e despacho do chamado de limpeza.
        var novoChamado = _aberturaChamado.AvaliarAbertura(boca, diagnostico, leitura.Chovendo, chamadosAtivos, agora);
        if (novoChamado is not null)
        {
            _db.Chamados.Add(novoChamado);
            await _despacho.DespacharAsync(new[] { new ChamadoComBoca(novoChamado, boca) }, cancellationToken);
        }

        // 4) Alerta para moradores (água subindo) ou para a central (nível crítico).
        var alerta = diagnostico.GeraAlerta
            ? await CriarAlertaSeNecessarioAsync(boca, diagnostico.Tipo, agora, cancellationToken)
            : null;

        // 5) Uma única gravação para leitura, status, cancelamentos, chamado e alerta.
        await _db.SaveChangesAsync(cancellationToken);

        RegistrarLog(leitura, boca, diagnostico.Tipo, cancelados, novoChamado, alerta);

        // O envio acontece depois de gravar: o alerta fica registrado mesmo se o serviço externo falhar.
        // CancellationToken.None: o alerta deve sair mesmo que o dispositivo encerre a conexão.
        bool? alertaEnviado = alerta is null
            ? null
            : await _alertaClient.EnviarAsync(alerta, boca, CancellationToken.None);

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
            ChamadoAbertoId = novoChamado?.Id,
            AgenteDesignadoId = novoChamado?.AgenteId,
            ChamadosCanceladosIds = cancelados.Select(c => c.Id).ToList(),
            AlertaId = alerta?.Id,
            AlertaEnviado = alertaEnviado
        };
    }

    /// <summary>
    /// Leituras anteriores numa janela do dobro do tempo exigido para tubo entupido:
    /// basta para medir a água parada e encontrar a leitura anterior (velocidade de subida).
    /// </summary>
    private async Task<List<Leitura>> CarregarHistoricoAsync(int bocaDeLoboId, DateTime dataHora, CancellationToken cancellationToken)
    {
        var inicioJanela = dataHora.AddHours(-2 * _regrasDiagnostico.HorasAguaParadaTubo);

        return await _db.Leituras
            .AsNoTracking()
            .Where(l => l.BocaDeLoboId == bocaDeLoboId && l.DataHora < dataHora && l.DataHora >= inicioJanela)
            .OrderByDescending(l => l.DataHora)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Cria o alerta, a menos que já exista um do mesmo nível para a mesma boca de lobo
    /// dentro do intervalo mínimo (evita um aviso a cada leitura durante a chuva).
    /// </summary>
    private async Task<Alerta?> CriarAlertaSeNecessarioAsync(
        BocaDeLobo boca,
        TipoDiagnostico diagnostico,
        DateTime agora,
        CancellationToken cancellationToken)
    {
        var candidato = Alerta.CriarPorDiagnostico(boca, diagnostico, agora);
        if (candidato is null)
        {
            return null;
        }

        var limite = agora.AddMinutes(-_opcoesAlertas.IntervaloMinimoMinutos);
        var jaAlertado = await _db.Alertas.AnyAsync(
            a => a.BocaDeLoboId == boca.Id && a.Nivel == candidato.Nivel && a.CriadoEm >= limite,
            cancellationToken);

        if (jaAlertado)
        {
            _logger.LogInformation(
                "Alerta {Nivel} da boca de lobo {BocaId} já emitido nos últimos {Minutos} min; não será repetido.",
                candidato.Nivel, boca.Id, _opcoesAlertas.IntervaloMinimoMinutos);
            return null;
        }

        _db.Alertas.Add(candidato);
        return candidato;
    }

    /// <summary>Privacidade: o log registra só ids e diagnósticos, nunca a localização ou a matrícula do agente.</summary>
    private void RegistrarLog(
        Leitura leitura,
        BocaDeLobo boca,
        TipoDiagnostico diagnostico,
        IReadOnlyList<Chamado> cancelados,
        Chamado? novoChamado,
        Alerta? alerta)
    {
        _logger.LogInformation(
            "Leitura {LeituraId} da boca de lobo {BocaId}: diagnóstico {Diagnostico}.",
            leitura.Id, boca.Id, diagnostico);

        if (cancelados.Count > 0)
        {
            _logger.LogInformation(
                "Chuva na boca de lobo {BocaId}: {Quantidade} chamado(s) de gari cancelado(s) por segurança: {Ids}.",
                boca.Id, cancelados.Count, string.Join(", ", cancelados.Select(c => c.Id)));
        }

        if (novoChamado is not null)
        {
            _logger.LogInformation(
                "Chamado {ChamadoId} ({Tipo}) aberto para a boca de lobo {BocaId}; agente designado: {AgenteId}.",
                novoChamado.Id, novoChamado.Tipo, boca.Id, novoChamado.AgenteId?.ToString() ?? "nenhum no raio");
        }

        if (alerta is not null)
        {
            _logger.LogInformation("Alerta {AlertaId} ({Nivel}) registrado para a boca de lobo {BocaId}.", alerta.Id, alerta.Nivel, boca.Id);
        }
    }
}
