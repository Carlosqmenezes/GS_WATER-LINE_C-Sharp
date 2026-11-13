using WaterLine.Domain.Entidades;
using WaterLine.Domain.Servicos;

namespace WaterLine.Domain.Interfaces;

/// <summary>
/// Aplica a tabela de diagnóstico: cruza o sensor externo, o sensor interno e a chuva.
/// </summary>
public interface IDiagnosticoService
{
    /// <summary>
    /// Diagnostica a leitura atual.
    /// </summary>
    /// <param name="boca">Boca de lobo da leitura (profundidade e largura da abertura).</param>
    /// <param name="atual">Leitura recebida agora.</param>
    /// <param name="historico">
    /// Leituras anteriores da mesma boca de lobo, em qualquer ordem. São usadas para a velocidade
    /// de subida (leitura imediatamente anterior) e para saber há quanto tempo a água está parada.
    /// </param>
    ResultadoDiagnostico Diagnosticar(BocaDeLobo boca, Leitura atual, IEnumerable<Leitura> historico);

    /// <summary>Indica se o sensor externo mostra a entrada livre nesta leitura.</summary>
    bool EntradaLivre(BocaDeLobo boca, Leitura leitura);

    /// <summary>Altura ocupada na caixa, em cm (nunca negativa nem maior que a profundidade).</summary>
    double CalcularNivelCm(BocaDeLobo boca, Leitura leitura);

    /// <summary>Ocupação da caixa em relação à profundidade, de 0 a 100.</summary>
    double CalcularPercentualOcupacao(BocaDeLobo boca, Leitura leitura);
}
