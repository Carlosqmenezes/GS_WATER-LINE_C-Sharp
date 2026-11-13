using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Interfaces;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Servicos;

/// <summary>Recebe as previsões do modelo de ML e aciona a camada de prevenção.</summary>
public interface IPrevisaoAppService
{
    Task<PrevisaoProcessadaResponse> RegistrarAsync(PrevisaoRequest request, CancellationToken cancellationToken);
}

public sealed class PrevisaoAppService : IPrevisaoAppService
{
    private static readonly TimeSpan ToleranciaFuturo = TimeSpan.FromMinutes(5);

    private static readonly TipoDiagnostico[] StatusDeEntupimento =
    {
        TipoDiagnostico.EntradaBloqueada,
        TipoDiagnostico.SedimentoAcumulando,
        TipoDiagnostico.TuboEntupido
    };

    private readonly WaterLineDbContext _db;
    private readonly IPrevencaoService _prevencao;
    private readonly IDespachoAppService _despacho;
    private readonly RegrasPrevencaoOptions _regrasPrevencao;
    private readonly RegrasDiagnosticoOptions _regrasDiagnostico;
    private readonly TimeProvider _relogio;
    private readonly ILogger<PrevisaoAppService> _logger;

    public PrevisaoAppService(
        WaterLineDbContext db,
        IPrevencaoService prevencao,
        IDespachoAppService despacho,
        RegrasPrevencaoOptions regrasPrevencao,
        RegrasDiagnosticoOptions regrasDiagnostico,
        TimeProvider relogio,
        ILogger<PrevisaoAppService> logger)
    {
        _db = db;
        _prevencao = prevencao;
        _despacho = despacho;
        _regrasPrevencao = regrasPrevencao;
        _regrasDiagnostico = regrasDiagnostico;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<PrevisaoProcessadaResponse> RegistrarAsync(PrevisaoRequest request, CancellationToken cancellationToken)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var geradaEm = request.GeradaEm?.UtcDateTime ?? agora;
        if (geradaEm > agora + ToleranciaFuturo)
        {
            throw new ArgumentException("A data de geração da previsão não pode estar no futuro.", nameof(request.GeradaEm));
        }

        var previsao = new PrevisaoChuva(geradaEm, request.HorizonteHoras!.Value, request.Probabilidade!.Value, request.ChuvaForte!.Value);
        _db.PrevisoesChuva.Add(previsao);

        var resultado = ResultadoPrevencao.Vazio;
        var designados = 0;

        if (previsao.ChuvaForte)
        {
            var chamadosAtivos = await _db.Chamados
                .Where(c => c.Status == StatusChamado.Aberto || c.Status == StatusChamado.Aceito)
                .ToListAsync(cancellationToken);

            var bocasComHistorico = await BuscarBocasComHistoricoAsync(agora, cancellationToken);

            resultado = _prevencao.AplicarPrevisao(previsao, chamadosAtivos, bocasComHistorico, agora);
            _db.Chamados.AddRange(resultado.ChamadosCriados);

            var bocasPorId = bocasComHistorico.ToDictionary(b => b.Id);
            var designacoes = await _despacho.DespacharAsync(
                resultado.ChamadosCriados.Select(c => new ChamadoComBoca(c, bocasPorId[c.BocaDeLoboId])).ToList(),
                cancellationToken);
            designados = designacoes.Count;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Previsão {PrevisaoId} registrada (chuva forte: {ChuvaForte}, horizonte {Horizonte} h): " +
            "{Priorizados} chamado(s) priorizado(s), {Criados} preventivo(s) criado(s), {Designados} designado(s).",
            previsao.Id, previsao.ChuvaForte, previsao.HorizonteHoras,
            resultado.ChamadosPriorizados.Count, resultado.ChamadosCriados.Count, designados);

        return new PrevisaoProcessadaResponse
        {
            PrevisaoId = previsao.Id,
            GeradaEm = previsao.GeradaEm,
            HorizonteHoras = previsao.HorizonteHoras,
            Probabilidade = previsao.Probabilidade,
            ChuvaForte = previsao.ChuvaForte,
            ChamadosPriorizados = resultado.ChamadosPriorizados.Count,
            ChamadosPreventivosCriados = resultado.ChamadosCriados.Select(c => c.Id).ToList(),
            ChamadosDesignados = designados
        };
    }

    /// <summary>
    /// Histórico de entupimento: boca de lobo ativa cujo status atual é de entupimento, ou que teve,
    /// na janela configurada, alguma leitura com entrada bloqueada ou com sedimento sem chuva.
    /// </summary>
    private async Task<List<BocaDeLobo>> BuscarBocasComHistoricoAsync(DateTime agora, CancellationToken cancellationToken)
    {
        var desde = agora.AddDays(-_regrasPrevencao.DiasHistoricoEntupimento);
        var fatorBloqueio = _regrasDiagnostico.PercentualEntradaBloqueada / 100.0;
        var fatorSedimento = _regrasDiagnostico.PercentualSedimentoMinimo / 100.0;

        var idsComLeituraDeEntupimento = _db.Leituras
            .Where(l => l.DataHora >= desde)
            .Where(l => l.DistanciaExternaCm < l.BocaDeLobo!.LarguraAberturaCm * fatorBloqueio
                        || (!l.Chovendo
                            && l.BocaDeLobo!.ProfundidadeCm - l.DistanciaInternaCm >= l.BocaDeLobo.ProfundidadeCm * fatorSedimento))
            .Select(l => l.BocaDeLoboId)
            .Distinct();

        return await _db.BocasDeLobo
            .Where(b => b.Ativa && (StatusDeEntupimento.Contains(b.Status) || idsComLeituraDeEntupimento.Contains(b.Id)))
            .OrderBy(b => b.Codigo)
            .ToListAsync(cancellationToken);
    }
}
