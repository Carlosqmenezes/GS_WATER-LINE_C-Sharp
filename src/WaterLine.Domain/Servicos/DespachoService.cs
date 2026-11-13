using WaterLine.Domain.Configuracoes;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Geo;
using WaterLine.Domain.Interfaces;

namespace WaterLine.Domain.Servicos;

/// <summary>
/// Regra de despacho: agente em serviço, do tipo certo (polimorfismo de <see cref="Agente.PodeAtender"/>),
/// mais próximo pela fórmula de Haversine e dentro do raio configurado.
/// </summary>
public sealed class DespachoService : IDespachoService
{
    private readonly RegrasDespachoOptions _regras;

    public DespachoService(RegrasDespachoOptions regras)
    {
        _regras = regras ?? throw new ArgumentNullException(nameof(regras));
    }

    public double RaioMetros => _regras.RaioMetros;

    public Agente? EscolherAgente(Chamado chamado, BocaDeLobo boca, IEnumerable<Agente> agentes) =>
        CandidatosOrdenados(chamado, boca, agentes).Select(c => c.Agente).FirstOrDefault();

    public IReadOnlyList<Designacao> Despachar(IEnumerable<ChamadoComBoca> chamados, IEnumerable<Agente> agentes)
    {
        ArgumentNullException.ThrowIfNull(chamados);
        var listaAgentes = agentes?.ToList() ?? throw new ArgumentNullException(nameof(agentes));

        // Os chamados mais prioritários escolhem primeiro; empate: o mais antigo primeiro.
        var designacoes = chamados
            .Where(c => c.Chamado.Status == StatusChamado.Aberto && c.Chamado.AgenteId is null)
            .OrderByDescending(c => c.Chamado.Prioridade)
            .ThenBy(c => c.Chamado.AbertoEm)
            .Select(c => new { c.Chamado, Melhor = CandidatosOrdenados(c.Chamado, c.Boca, listaAgentes).FirstOrDefault() })
            .Where(x => x.Melhor is not null)
            .Select(x => new Designacao(x.Chamado, x.Melhor!.Agente, x.Melhor.DistanciaMetros))
            .ToList();

        foreach (var designacao in designacoes)
        {
            designacao.Chamado.AtribuirAgente(designacao.Agente);
        }

        return designacoes;
    }

    public IReadOnlyList<ChamadoProximo> ListarChamadosProximos(double latitude, double longitude, IEnumerable<ChamadoComBoca> chamados)
    {
        ArgumentNullException.ThrowIfNull(chamados);

        return chamados
            .Where(c => c.Chamado.Status == StatusChamado.Aberto)
            .Select(c => new ChamadoProximo(
                c.Chamado,
                c.Boca,
                Haversine.DistanciaEmMetros(latitude, longitude, c.Boca.Latitude, c.Boca.Longitude)))
            .Where(p => p.DistanciaMetros <= _regras.RaioMetros)
            .OrderByDescending(p => p.Chamado.Prioridade)
            .ThenBy(p => p.DistanciaMetros)
            .ToList();
    }

    public double? DistanciaMetros(Agente agente, BocaDeLobo boca)
    {
        ArgumentNullException.ThrowIfNull(agente);
        ArgumentNullException.ThrowIfNull(boca);

        if (!agente.TemLocalizacao)
        {
            return null;
        }

        return Haversine.DistanciaEmMetros(agente.Latitude!.Value, agente.Longitude!.Value, boca.Latitude, boca.Longitude);
    }

    public bool EstaNoRaio(Agente agente, BocaDeLobo boca) =>
        DistanciaMetros(agente, boca) is { } distancia && distancia <= _regras.RaioMetros;

    private IEnumerable<Candidato> CandidatosOrdenados(
        Chamado chamado,
        BocaDeLobo boca,
        IEnumerable<Agente> agentes)
    {
        ArgumentNullException.ThrowIfNull(chamado);
        ArgumentNullException.ThrowIfNull(boca);
        ArgumentNullException.ThrowIfNull(agentes);

        return agentes
            .Where(a => a.TemLocalizacao && a.PodeAtender(chamado.Tipo))
            .Select(a => new Candidato(a, DistanciaMetros(a, boca)!.Value))
            .Where(c => c.DistanciaMetros <= _regras.RaioMetros)
            .OrderBy(c => c.DistanciaMetros)
            .ThenBy(c => c.Agente.Id);
    }

    /// <summary>Agente apto a atender o chamado e a distância dele até a boca de lobo.</summary>
    private sealed record Candidato(Agente Agente, double DistanciaMetros);
}
