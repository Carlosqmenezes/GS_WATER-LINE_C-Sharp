using WaterLine.Domain.Enums;

namespace WaterLine.Domain.Servicos;

/// <summary>
/// Resultado do diagnóstico de uma leitura, com os valores calculados que levaram a ele.
/// </summary>
/// <param name="Tipo">Diagnóstico final.</param>
/// <param name="NivelCm">Altura ocupada na caixa (água ou sedimento), em cm.</param>
/// <param name="PercentualOcupacao">Nível em relação à profundidade, de 0 a 100.</param>
/// <param name="VelocidadeSubidaCmPorMinuto">Variação do nível desde a leitura anterior; null se não houver leitura anterior.</param>
/// <param name="EntradaBloqueada">Indica se o sensor externo detectou a entrada tampada.</param>
public sealed record ResultadoDiagnostico(
    TipoDiagnostico Tipo,
    double NivelCm,
    double PercentualOcupacao,
    double? VelocidadeSubidaCmPorMinuto,
    bool EntradaBloqueada)
{
    /// <summary>Diagnósticos que geram alerta para moradores ou para a central, e não chamado de limpeza.</summary>
    public bool GeraAlerta => Tipo is TipoDiagnostico.AguaSubindo or TipoDiagnostico.NivelCritico;
}
