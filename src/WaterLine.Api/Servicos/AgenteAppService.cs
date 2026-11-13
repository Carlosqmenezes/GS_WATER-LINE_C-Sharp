using Microsoft.EntityFrameworkCore;
using WaterLine.Api.Dados;
using WaterLine.Api.Dtos;
using WaterLine.Domain.Excecoes;

namespace WaterLine.Api.Servicos;

/// <summary>Casos de uso dos agentes (entrar e sair de serviço).</summary>
public interface IAgenteAppService
{
    Task<AgenteServicoResponse> AlterarServicoAsync(int agenteId, AlterarServicoRequest request, CancellationToken cancellationToken);
}

public sealed class AgenteAppService : IAgenteAppService
{
    private readonly WaterLineDbContext _db;
    private readonly ILogger<AgenteAppService> _logger;

    public AgenteAppService(WaterLineDbContext db, ILogger<AgenteAppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AgenteServicoResponse> AlterarServicoAsync(
        int agenteId,
        AlterarServicoRequest request,
        CancellationToken cancellationToken)
    {
        var agente = await _db.Agentes.FirstOrDefaultAsync(a => a.Id == agenteId, cancellationToken)
                     ?? throw new RecursoNaoEncontradoException("Agente", agenteId);

        if (request.EmServico == true)
        {
            var jaEstavaEmServico = agente.EmServico;
            agente.EntrarEmServico(request.Latitude!.Value, request.Longitude!.Value);

            // Privacidade: nunca registrar a localização nem a matrícula, só o id.
            if (jaEstavaEmServico)
            {
                _logger.LogInformation("Agente {AgenteId} atualizou a localização.", agente.Id);
            }
            else
            {
                _logger.LogInformation("Agente {AgenteId} entrou em serviço.", agente.Id);
            }
        }
        else
        {
            agente.SairDeServico();
            _logger.LogInformation("Agente {AgenteId} saiu de serviço; localização apagada.", agente.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return AgenteServicoResponse.De(agente);
    }
}
