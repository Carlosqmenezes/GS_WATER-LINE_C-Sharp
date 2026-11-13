using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Domain.Interfaces;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Servicos;

/// <summary>Busca os agentes disponíveis e aplica a regra de despacho do domínio.</summary>
public interface IDespachoAppService
{
    /// <summary>
    /// Designa o agente mais próximo para cada chamado aberto e sem agente. As alterações ficam
    /// no DbContext e são gravadas pelo chamador, na mesma transação da operação que criou os chamados.
    /// </summary>
    Task<IReadOnlyList<Designacao>> DespacharAsync(IReadOnlyCollection<ChamadoComBoca> chamados, CancellationToken cancellationToken);
}

public sealed class DespachoAppService : IDespachoAppService
{
    private readonly WaterLineDbContext _db;
    private readonly IDespachoService _despacho;
    private readonly ILogger<DespachoAppService> _logger;

    public DespachoAppService(WaterLineDbContext db, IDespachoService despacho, ILogger<DespachoAppService> logger)
    {
        _db = db;
        _despacho = despacho;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Designacao>> DespacharAsync(
        IReadOnlyCollection<ChamadoComBoca> chamados,
        CancellationToken cancellationToken)
    {
        if (chamados.Count == 0)
        {
            return Array.Empty<Designacao>();
        }

        // Só entram agentes em serviço e com localização; o tipo e o raio são conferidos no domínio.
        var agentesEmServico = await _db.Agentes
            .Where(a => a.EmServico && a.Latitude != null && a.Longitude != null)
            .ToListAsync(cancellationToken);

        var designacoes = _despacho.Despachar(chamados, agentesEmServico);

        foreach (var designacao in designacoes)
        {
            // Privacidade: o log registra só ids, nunca a localização ou a matrícula do agente.
            _logger.LogInformation(
                "Chamado {ChamadoId} designado ao agente {AgenteId}.",
                designacao.Chamado.Id, designacao.Agente.Id);
        }

        var semAgente = chamados.Count(c => c.Chamado.AgenteId is null);
        if (semAgente > 0)
        {
            _logger.LogInformation(
                "{Quantidade} chamado(s) sem agente do tipo certo num raio de {Raio} m; ficam abertos para quem se aproximar.",
                semAgente, _despacho.RaioMetros);
        }

        return designacoes;
    }
}
