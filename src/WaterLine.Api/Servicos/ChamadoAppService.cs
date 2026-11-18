using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Armazenamento;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Excecoes;
using WaterLine.Domain.Geo;
using WaterLine.Domain.Interfaces;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Servicos;

/// <summary>Casos de uso dos chamados usados pelo app do gari.</summary>
public interface IChamadoAppService
{
    Task<IReadOnlyList<ChamadoProximoResponse>> ListarProximosAsync(double latitude, double longitude, CancellationToken cancellationToken);

    Task<ChamadoResponse> AceitarAsync(int chamadoId, AceitarChamadoRequest request, CancellationToken cancellationToken);

    Task<ChamadoResponse> ConcluirAsync(int chamadoId, ConcluirChamadoRequest request, CancellationToken cancellationToken);
}

public sealed class ChamadoAppService : IChamadoAppService
{
    private const string EtapaAntes = "antes";
    private const string EtapaDepois = "depois";

    private readonly WaterLineDbContext _db;
    private readonly IDespachoService _despacho;
    private readonly IDiagnosticoService _diagnostico;
    private readonly IArmazenamentoFotos _fotos;
    private readonly TimeProvider _relogio;
    private readonly ILogger<ChamadoAppService> _logger;

    public ChamadoAppService(
        WaterLineDbContext db,
        IDespachoService despacho,
        IDiagnosticoService diagnostico,
        IArmazenamentoFotos fotos,
        TimeProvider relogio,
        ILogger<ChamadoAppService> logger)
    {
        _db = db;
        _despacho = despacho;
        _diagnostico = diagnostico;
        _fotos = fotos;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChamadoProximoResponse>> ListarProximosAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
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

    public async Task<ChamadoResponse> AceitarAsync(int chamadoId, AceitarChamadoRequest request, CancellationToken cancellationToken)
    {
        var chamado = await ObterChamadoComBocaAsync(chamadoId, cancellationToken);
        var agenteId = request.AgenteId!.Value;

        var agente = await _db.Agentes.FirstOrDefaultAsync(a => a.Id == agenteId, cancellationToken)
                     ?? throw new RecursoNaoEncontradoException("Agente", agenteId);

        // O domínio confere status, tipo de agente, se está em serviço e se o chamado é de outro agente.
        chamado.Aceitar(agente, _relogio.GetUtcNow().UtcDateTime);

        // Só aceita quem está perto: a alteração acima não é gravada se a regra do raio falhar.
        if (!_despacho.EstaNoRaio(agente, chamado.BocaDeLobo!))
        {
            throw new RegraNegocioException(
                $"O agente está fora do raio de {_despacho.RaioMetros} m da boca de lobo e não pode aceitar este chamado.");
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Chamado {ChamadoId} aceito pelo agente {AgenteId}.", chamado.Id, agente.Id);
        return ChamadoResponse.De(chamado);
    }

    public async Task<ChamadoResponse> ConcluirAsync(int chamadoId, ConcluirChamadoRequest request, CancellationToken cancellationToken)
    {
        var chamado = await ObterChamadoComBocaAsync(chamadoId, cancellationToken);
        var boca = chamado.BocaDeLobo!;
        var agenteId = request.AgenteId!.Value;

        var ultimaLeitura = await _db.Leituras
            .AsNoTracking()
            .Where(l => l.BocaDeLoboId == boca.Id)
            .OrderByDescending(l => l.DataHora)
            .FirstOrDefaultAsync(cancellationToken);

        var entradaLivre = ultimaLeitura is not null && _diagnostico.EntradaLivre(boca, ultimaLeitura);

        // 1) Regras de negócio antes de tocar no disco: status, agente, fotos presentes e sensor.
        chamado.ValidarConclusao(agenteId, request.FotoAntes is not null, request.FotoDepois is not null, entradaLivre);

        // 2) Formato e tamanho das duas fotos.
        _fotos.Validar(request.FotoAntes!, nameof(request.FotoAntes));
        _fotos.Validar(request.FotoDepois!, nameof(request.FotoDepois));

        // 3) Grava as fotos e conclui; se algo falhar, apaga o que foi gravado.
        var fotosGravadas = new List<string>(capacity: 2);
        try
        {
            var caminhoAntes = await _fotos.SalvarAsync(chamado.Id, EtapaAntes, request.FotoAntes!, cancellationToken);
            fotosGravadas.Add(caminhoAntes);

            var caminhoDepois = await _fotos.SalvarAsync(chamado.Id, EtapaDepois, request.FotoDepois!, cancellationToken);
            fotosGravadas.Add(caminhoDepois);

            chamado.Concluir(agenteId, caminhoAntes, caminhoDepois, entradaLivre, _relogio.GetUtcNow().UtcDateTime);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            fotosGravadas.ForEach(_fotos.Remover);
            throw;
        }

        _logger.LogInformation("Chamado {ChamadoId} concluído pelo agente {AgenteId}.", chamado.Id, agenteId);
        return ChamadoResponse.De(chamado);
    }

    private async Task<Chamado> ObterChamadoComBocaAsync(int chamadoId, CancellationToken cancellationToken) =>
        await _db.Chamados
            .Include(c => c.BocaDeLobo)
            .FirstOrDefaultAsync(c => c.Id == chamadoId, cancellationToken)
        ?? throw new RecursoNaoEncontradoException("Chamado", chamadoId);
}
