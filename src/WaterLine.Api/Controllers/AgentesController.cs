using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaterLine.Api.Autenticacao;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;

namespace WaterLine.Api.Controllers;

/// <summary>Agentes públicos (garis e equipes de galeria). Exige o token do login.</summary>
[ApiController]
[Authorize]
[Route("agentes")]
[Produces("application/json")]
public sealed class AgentesController : ControllerBase
{
    private readonly IAgenteAppService _service;

    public AgentesController(IAgenteAppService service)
    {
        _service = service;
    }

    /// <summary>
    /// Entra em serviço (com a localização atual), atualiza a localização ou sai de serviço.
    /// Ao sair, a localização é apagada: o agente não é rastreado fora do expediente.
    /// O agente só altera a própria situação: o Id da rota precisa ser o do token.
    /// </summary>
    /// <param name="id">Id do agente.</param>
    /// <param name="request">Situação de serviço e localização.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpPost("{id:int}/servico")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AgenteServicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgenteServicoResponse>> AlterarServico(
        int id,
        [FromBody] AlterarServicoRequest request,
        CancellationToken cancellationToken)
    {
        User.GarantirMesmoAgente(id);
        return Ok(await _service.AlterarServicoAsync(id, request, cancellationToken));
    }
}
