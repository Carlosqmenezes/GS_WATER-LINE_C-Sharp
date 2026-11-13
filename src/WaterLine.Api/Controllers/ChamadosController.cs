using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;

namespace WaterLine.Api.Controllers;

/// <summary>Chamados de limpeza e manutenção, consumidos pelo app do gari.</summary>
[ApiController]
[Route("chamados")]
[Produces("application/json")]
public sealed class ChamadosController : ControllerBase
{
    private readonly IChamadoAppService _service;

    public ChamadosController(IChamadoAppService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lista os chamados abertos no raio de despacho do ponto informado,
    /// ordenados por prioridade (maior primeiro) e distância (mais perto primeiro).
    /// </summary>
    /// <param name="lat">Latitude atual do agente.</param>
    /// <param name="lng">Longitude atual do agente.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpGet("proximos")]
    [ProducesResponseType(typeof(IReadOnlyList<ChamadoProximoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ChamadoProximoResponse>>> ListarProximos(
        [FromQuery, Required, Range(-90.0, 90.0)] double? lat,
        [FromQuery, Required, Range(-180.0, 180.0)] double? lng,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarProximosAsync(lat!.Value, lng!.Value, cancellationToken));
    }
}
