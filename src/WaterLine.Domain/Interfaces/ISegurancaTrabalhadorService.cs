using WaterLine.Domain.Entidades;

namespace WaterLine.Domain.Interfaces;

/// <summary>
/// Protege o trabalhador: ninguém é mandado (ou mantido) numa boca de lobo enquanto chove.
/// </summary>
public interface ISegurancaTrabalhadorService
{
    /// <summary>
    /// Se a leitura indica chuva, cancela os chamados de gari abertos ou aceitos daquela boca de lobo.
    /// Chamados da equipe de galeria não são afetados.
    /// </summary>
    /// <returns>Chamados cancelados (lista vazia se não estiver chovendo).</returns>
    IReadOnlyList<Chamado> CancelarChamadosDeGariPorChuva(Leitura leitura, IEnumerable<Chamado> chamados);
}
