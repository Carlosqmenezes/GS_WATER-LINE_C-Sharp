using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Geo;
using WaterLine.Domain.Interfaces;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Servicos;

/// <summary>Casos de uso dos chamados usados pelo app do gari.</summary>
public interface IChamadoAppService
{
    Task<IReadOnlyList<ChamadoProximoResponse>> ListarProximosAsync(double latitude, double longitude, CancellationToken cancellationToken);
}

public sealed class ChamadoAppService : IChamadoAppService
{
    private readonly WaterLineDbContext _db;
    private readonly IDespachoService _despacho;

    public ChamadoAppService(WaterLineDbContext db, IDespachoService despacho)
    {
        _db = db;
        _despacho = despacho;
    }

    public async Task<IReadOnlyList<ChamadoProximoResponse>> ListarProximosAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        // Pré-filtro no banco por uma caixa ao redor do ponto; a distância exata (Haversine)
        // e a ordenação por prioridade e distância ficam no domínio.
        var caixa = Haversine.CaixaAoRedor(latitude, longitude, _despacho.RaioMetros);

        var candidatos = await _db.Chamados
            .AsNoTracking()
            .Where(c => c.Status == StatusChamado.Aberto)
            .Join(
                _db.BocasDeLobo.Where(b => b.Ativa
                                           && b.Latitude >= caixa.LatitudeMinima && b.Latitude <= caixa.LatitudeMaxima
                                           && b.Longitude >= caixa.LongitudeMinima && b.Longitude <= caixa.LongitudeMaxima),
                chamado => chamado.BocaDeLoboId,
                boca => boca.Id,
                (chamado, boca) => new { chamado, boca })
            .ToListAsync(cancellationToken);

        var proximos = _despacho.ListarChamadosProximos(
            latitude,
            longitude,
            candidatos.Select(x => new ChamadoComBoca(x.chamado, x.boca)));

        return proximos.Select(ChamadoProximoResponse.De).ToList();
    }
}
