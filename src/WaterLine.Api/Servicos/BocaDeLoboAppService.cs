using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Excecoes;

namespace WaterLine.Api.Servicos;

/// <summary>Casos de uso do cadastro de bocas de lobo.</summary>
public interface IBocaDeLoboAppService
{
    Task<IReadOnlyList<BocaDeLoboResponse>> ListarAsync(bool? ativa, CancellationToken cancellationToken);

    Task<BocaDeLoboResponse> ObterAsync(int id, CancellationToken cancellationToken);

    Task<BocaDeLoboResponse> CriarAsync(BocaDeLoboRequest request, CancellationToken cancellationToken);

    Task<BocaDeLoboResponse> AtualizarAsync(int id, BocaDeLoboRequest request, CancellationToken cancellationToken);

    Task DesativarAsync(int id, CancellationToken cancellationToken);
}

public sealed class BocaDeLoboAppService : IBocaDeLoboAppService
{
    private readonly WaterLineDbContext _db;
    private readonly ILogger<BocaDeLoboAppService> _logger;

    public BocaDeLoboAppService(WaterLineDbContext db, ILogger<BocaDeLoboAppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BocaDeLoboResponse>> ListarAsync(bool? ativa, CancellationToken cancellationToken)
    {
        var consulta = _db.BocasDeLobo.AsNoTracking();

        if (ativa.HasValue)
        {
            consulta = consulta.Where(b => b.Ativa == ativa.Value);
        }

        var bocas = await consulta.OrderBy(b => b.Codigo).ToListAsync(cancellationToken);
        return bocas.Select(BocaDeLoboResponse.De).ToList();
    }

    public async Task<BocaDeLoboResponse> ObterAsync(int id, CancellationToken cancellationToken)
    {
        var boca = await _db.BocasDeLobo.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                   ?? throw new RecursoNaoEncontradoException("Boca de lobo", id);

        return BocaDeLoboResponse.De(boca);
    }

    public async Task<BocaDeLoboResponse> CriarAsync(BocaDeLoboRequest request, CancellationToken cancellationToken)
    {
        await GarantirCodigoUnicoAsync(request.Codigo, idAtual: null, cancellationToken);

        var boca = new BocaDeLobo(
            request.Codigo,
            request.Endereco,
            request.Latitude!.Value,
            request.Longitude!.Value,
            request.ProfundidadeCm!.Value,
            request.LarguraAberturaCm!.Value);

        _db.BocasDeLobo.Add(boca);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Boca de lobo {Codigo} cadastrada com id {Id}.", boca.Codigo, boca.Id);
        return BocaDeLoboResponse.De(boca);
    }

    public async Task<BocaDeLoboResponse> AtualizarAsync(int id, BocaDeLoboRequest request, CancellationToken cancellationToken)
    {
        var boca = await _db.BocasDeLobo.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                   ?? throw new RecursoNaoEncontradoException("Boca de lobo", id);

        await GarantirCodigoUnicoAsync(request.Codigo, id, cancellationToken);

        boca.AtualizarDados(
            request.Codigo,
            request.Endereco,
            request.Latitude!.Value,
            request.Longitude!.Value,
            request.ProfundidadeCm!.Value,
            request.LarguraAberturaCm!.Value);

        if (request.Ativa)
        {
            boca.Ativar();
        }
        else
        {
            boca.Desativar();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return BocaDeLoboResponse.De(boca);
    }

    /// <summary>Exclusão lógica: preserva o histórico de leituras, chamados e alertas.</summary>
    public async Task DesativarAsync(int id, CancellationToken cancellationToken)
    {
        var boca = await _db.BocasDeLobo.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
                   ?? throw new RecursoNaoEncontradoException("Boca de lobo", id);

        boca.Desativar();
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Boca de lobo {Id} desativada.", id);
    }

    private async Task GarantirCodigoUnicoAsync(string codigo, int? idAtual, CancellationToken cancellationToken)
    {
        var codigoNormalizado = codigo.Trim().ToUpperInvariant();

        var emUso = await _db.BocasDeLobo
            .AnyAsync(b => b.Codigo == codigoNormalizado && (idAtual == null || b.Id != idAtual), cancellationToken);

        if (emUso)
        {
            throw new ConflitoException($"Já existe uma boca de lobo com o código {codigoNormalizado}.");
        }
    }
}
