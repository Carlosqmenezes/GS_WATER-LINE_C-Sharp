using WaterLine.Domain.Entidades;
using WaterLine.Domain.Servicos;

namespace WaterLine.Domain.Interfaces;

/// <summary>
/// Camada de prevenção: reage a uma previsão de chuva forte antes que ela chegue.
/// </summary>
public interface IPrevencaoService
{
    /// <summary>
    /// Com chuva forte prevista, eleva a prioridade dos chamados ativos e cria chamados
    /// preventivos para as bocas de lobo com histórico de entupimento que ainda não têm um.
    /// Sem chuva forte, não faz nada.
    /// </summary>
    /// <param name="previsao">Previsão recebida do modelo de ML.</param>
    /// <param name="chamadosAtivos">Chamados abertos ou aceitos.</param>
    /// <param name="bocasComHistorico">Bocas de lobo ativas com histórico de entupimento.</param>
    /// <param name="agora">Momento da abertura dos novos chamados.</param>
    ResultadoPrevencao AplicarPrevisao(
        PrevisaoChuva previsao,
        IEnumerable<Chamado> chamadosAtivos,
        IEnumerable<BocaDeLobo> bocasComHistorico,
        DateTime agora);
}
