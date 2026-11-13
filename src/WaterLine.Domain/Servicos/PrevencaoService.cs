using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Domain.Servicos;

/// <summary>Resultado da aplicação de uma previsão de chuva.</summary>
/// <param name="ChamadosPriorizados">Chamados ativos que tiveram a prioridade elevada.</param>
/// <param name="ChamadosCriados">Chamados preventivos criados para bocas de lobo com histórico.</param>
public sealed record ResultadoPrevencao(IReadOnlyList<Chamado> ChamadosPriorizados, IReadOnlyList<Chamado> ChamadosCriados)
{
    public static ResultadoPrevencao Vazio { get; } = new(Array.Empty<Chamado>(), Array.Empty<Chamado>());
}

/// <summary>Regra de prevenção do WaterLine (camada 1).</summary>
public sealed class PrevencaoService : IPrevencaoService
{
    private readonly RegrasPrevencaoOptions _regras;

    public PrevencaoService(RegrasPrevencaoOptions regras)
    {
        _regras = regras ?? throw new ArgumentNullException(nameof(regras));
    }

    public ResultadoPrevencao AplicarPrevisao(
        PrevisaoChuva previsao,
        IEnumerable<Chamado> chamadosAtivos,
        IEnumerable<BocaDeLobo> bocasComHistorico,
        DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(previsao);
        ArgumentNullException.ThrowIfNull(chamadosAtivos);
        ArgumentNullException.ThrowIfNull(bocasComHistorico);

        if (!previsao.ChuvaForte)
        {
            return ResultadoPrevencao.Vazio;
        }

        var ativos = chamadosAtivos.Where(c => c.EstaAtivo).ToList();

        // 1) Todo chamado ativo sobe de prioridade, sem passar do teto.
        foreach (var chamado in ativos)
        {
            chamado.ElevarPrioridade(_regras.IncrementoPrioridade);
        }

        // 2) Bocas de lobo com histórico ganham verificação preventiva do gari,
        //    a menos que já tenham um chamado preventivo ativo (sem duplicar).
        var bocasComPreventivoAtivo = ativos
            .Where(c => c.Tipo == TipoChamado.Preventivo)
            .Select(c => c.BocaDeLoboId)
            .ToHashSet();

        var prioridadePreventivo = Chamado.PrioridadePadrao(TipoChamado.Preventivo) + _regras.IncrementoPrioridade;

        var criados = bocasComHistorico
            .Where(b => b.Ativa && !bocasComPreventivoAtivo.Contains(b.Id))
            .DistinctBy(b => b.Id)
            .Select(b => new Chamado(b.Id, TipoChamado.Preventivo, agora, prioridadePreventivo))
            .ToList();

        return new ResultadoPrevencao(ativos, criados);
    }
}
