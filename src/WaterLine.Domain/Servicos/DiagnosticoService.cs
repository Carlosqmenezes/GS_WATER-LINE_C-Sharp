using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Domain.Servicos;

/// <summary>
/// Regra de diagnóstico do WaterLine.
/// </summary>
/// <remarks>
/// Ordem de avaliação (o primeiro que se aplica vence):
/// <list type="number">
/// <item>Chovendo: nível crítico → água subindo rápido → entrada bloqueada → normal.</item>
/// <item>Sem chuva: tubo entupido → entrada bloqueada → sedimento acumulando → normal.</item>
/// </list>
/// Sem chuva, ocupação acima do limite do tubo há menos tempo que o mínimo exigido é tratada
/// como normal: pode ser só a água da última chuva escoando, e evita alarme falso.
/// </remarks>
public sealed class DiagnosticoService : IDiagnosticoService
{
    private readonly RegrasDiagnosticoOptions _regras;

    public DiagnosticoService(RegrasDiagnosticoOptions regras)
    {
        _regras = regras ?? throw new ArgumentNullException(nameof(regras));
    }

    public ResultadoDiagnostico Diagnosticar(BocaDeLobo boca, Leitura atual, IEnumerable<Leitura> historico)
    {
        ArgumentNullException.ThrowIfNull(boca);
        ArgumentNullException.ThrowIfNull(atual);
        ArgumentNullException.ThrowIfNull(historico);

        // Só interessam as leituras anteriores à atual, da mais recente para a mais antiga.
        var anteriores = historico
            .Where(l => l.DataHora < atual.DataHora)
            .OrderByDescending(l => l.DataHora)
            .ToList();

        var nivel = CalcularNivelCm(boca, atual);
        var percentual = CalcularPercentualOcupacao(boca, atual);
        var velocidade = CalcularVelocidadeSubida(boca, atual, anteriores.FirstOrDefault());
        var entradaBloqueada = !EntradaLivre(boca, atual);

        var tipo = atual.Chovendo
            ? DiagnosticarComChuva(percentual, velocidade, entradaBloqueada)
            : DiagnosticarSemChuva(boca, atual, anteriores, percentual, entradaBloqueada);

        return new ResultadoDiagnostico(tipo, nivel, percentual, velocidade, entradaBloqueada);
    }

    public bool EntradaLivre(BocaDeLobo boca, Leitura leitura)
    {
        ArgumentNullException.ThrowIfNull(boca);
        ArgumentNullException.ThrowIfNull(leitura);

        var limiteCm = boca.LarguraAberturaCm * _regras.PercentualEntradaBloqueada / 100.0;
        return leitura.DistanciaExternaCm >= limiteCm;
    }

    public double CalcularNivelCm(BocaDeLobo boca, Leitura leitura)
    {
        ArgumentNullException.ThrowIfNull(boca);
        ArgumentNullException.ThrowIfNull(leitura);

        // nível = profundidade - distância interna. Limitado ao intervalo físico da caixa,
        // porque o ultrassônico pode medir um pouco além do fundo ou por cima da tampa.
        return Math.Clamp(boca.ProfundidadeCm - leitura.DistanciaInternaCm, 0, boca.ProfundidadeCm);
    }

    public double CalcularPercentualOcupacao(BocaDeLobo boca, Leitura leitura) =>
        CalcularNivelCm(boca, leitura) / boca.ProfundidadeCm * 100.0;

    private TipoDiagnostico DiagnosticarComChuva(double percentual, double? velocidade, bool entradaBloqueada)
    {
        if (percentual >= _regras.PercentualNivelCritico)
        {
            return TipoDiagnostico.NivelCritico;
        }

        if (velocidade >= _regras.VelocidadeSubidaCmPorMinuto)
        {
            return TipoDiagnostico.AguaSubindo;
        }

        return entradaBloqueada ? TipoDiagnostico.EntradaBloqueada : TipoDiagnostico.Normal;
    }

    private TipoDiagnostico DiagnosticarSemChuva(
        BocaDeLobo boca,
        Leitura atual,
        IReadOnlyList<Leitura> anterioresDaMaisRecente,
        double percentual,
        bool entradaBloqueada)
    {
        if (percentual >= _regras.PercentualTuboEntupido)
        {
            var aguaParadaDesde = InicioDaAguaParada(boca, atual, anterioresDaMaisRecente);
            if (atual.DataHora - aguaParadaDesde > TimeSpan.FromHours(_regras.HorasAguaParadaTubo))
            {
                return TipoDiagnostico.TuboEntupido;
            }
        }

        if (entradaBloqueada)
        {
            return TipoDiagnostico.EntradaBloqueada;
        }

        if (percentual >= _regras.PercentualSedimentoMinimo && percentual < _regras.PercentualTuboEntupido)
        {
            return TipoDiagnostico.SedimentoAcumulando;
        }

        return TipoDiagnostico.Normal;
    }

    /// <summary>
    /// Volta no histórico enquanto as leituras continuam sem chuva e acima do limite do tubo;
    /// a mais antiga dessa sequência ininterrupta marca o início da água parada.
    /// </summary>
    private DateTime InicioDaAguaParada(BocaDeLobo boca, Leitura atual, IEnumerable<Leitura> anterioresDaMaisRecente)
    {
        var inicio = atual.DataHora;

        foreach (var leitura in anterioresDaMaisRecente)
        {
            var continuaParada = !leitura.Chovendo
                                 && CalcularPercentualOcupacao(boca, leitura) >= _regras.PercentualTuboEntupido;
            if (!continuaParada)
            {
                break;
            }

            inicio = leitura.DataHora;
        }

        return inicio;
    }

    private double? CalcularVelocidadeSubida(BocaDeLobo boca, Leitura atual, Leitura? anterior)
    {
        if (anterior is null)
        {
            return null;
        }

        var minutos = (atual.DataHora - anterior.DataHora).TotalMinutes;
        if (minutos <= 0)
        {
            return null;
        }

        return (CalcularNivelCm(boca, atual) - CalcularNivelCm(boca, anterior)) / minutos;
    }
}
