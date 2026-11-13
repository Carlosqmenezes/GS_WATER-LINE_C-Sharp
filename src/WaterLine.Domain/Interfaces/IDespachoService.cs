using WaterLine.Domain.Entidades;
using WaterLine.Domain.Servicos;

namespace WaterLine.Domain.Interfaces;

/// <summary>
/// Escolhe o agente certo mais próximo e ordena os chamados por prioridade e distância.
/// </summary>
public interface IDespachoService
{
    /// <summary>Raio máximo de despacho, em metros.</summary>
    double RaioMetros { get; }

    /// <summary>
    /// Escolhe, entre os agentes em serviço que atendem o tipo do chamado, o mais próximo
    /// dentro do raio. Devolve null quando ninguém se encaixa.
    /// </summary>
    Agente? EscolherAgente(Chamado chamado, BocaDeLobo boca, IEnumerable<Agente> agentes);

    /// <summary>
    /// Designa um agente para cada chamado aberto e ainda sem agente, começando pelos de maior prioridade.
    /// Devolve só os chamados que conseguiram agente.
    /// </summary>
    IReadOnlyList<Designacao> Despachar(IEnumerable<ChamadoComBoca> chamados, IEnumerable<Agente> agentes);

    /// <summary>
    /// Chamados abertos dentro do raio do ponto informado, ordenados por prioridade (maior primeiro)
    /// e depois por distância (mais perto primeiro).
    /// </summary>
    IReadOnlyList<ChamadoProximo> ListarChamadosProximos(double latitude, double longitude, IEnumerable<ChamadoComBoca> chamados);

    /// <summary>Distância entre o agente e a boca de lobo, ou null se o agente não tem localização.</summary>
    double? DistanciaMetros(Agente agente, BocaDeLobo boca);

    /// <summary>Indica se o agente está em serviço e dentro do raio da boca de lobo.</summary>
    bool EstaNoRaio(Agente agente, BocaDeLobo boca);
}
