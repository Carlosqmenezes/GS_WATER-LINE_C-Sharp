using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Domain.Enums;

namespace WaterLine.Api.Servicos;

/// <summary>Consulta dos alertas gerados (o painel faz o papel da central na demonstração).</summary>
public interface IAlertaAppService
{
    Task<IReadOnlyList<AlertaResponse>> ListarAsync(int? bocaDeLoboId, NivelAlerta? nivel, int limite, CancellationToken cancellationToken);
}

public sealed class AlertaAppService : IAlertaAppService
{
    private readonly WaterLineDbContext _db;

    public AlertaAppService(WaterLineDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AlertaResponse>> ListarAsync(
        int? bocaDeLoboId,
        NivelAlerta? nivel,
        int limite,
        CancellationToken cancellationToken)
    {
        var consulta = _db.Alertas.AsNoTracking();

        if (bocaDeLoboId.HasValue)
        {
            consulta = consulta.Where(a => a.BocaDeLoboId == bocaDeLoboId.Value);
        }

        if (nivel.HasValue)
        {
            consulta = consulta.Where(a => a.Nivel == nivel.Value);
        }

        // Mais recentes primeiro, com o código da boca de lobo para exibição.
        var alertas = await consulta
            .OrderByDescending(a => a.CriadoEm)
            .ThenByDescending(a => a.Id)
            .Take(limite)
            .Select(a => new { Alerta = a, Boca = a.BocaDeLobo! })
            .ToListAsync(cancellationToken);

        return alertas.Select(x => AlertaResponse.De(x.Alerta, x.Boca)).ToList();
    }
}
