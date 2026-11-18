using Microsoft.AspNetCore.Mvc;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;

namespace WaterLine.Api.Controllers;

/// <summary>Previsões de chuva forte enviadas pelo modelo de ML.</summary>
[ApiController]
[Route("previsoes")]
[Produces("application/json")]
public sealed class PrevisoesController : ControllerBase
{
    private readonly IPrevisaoAppService _service;

    public PrevisoesController(IPrevisaoAppService service)
    {
        _service = service;
    }

    /// <param name="request">Horizonte, probabilidade e decisão de chuva forte.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="201">Previsão registrada e camada de prevenção aplicada.</response>
    /// <response code="400">Dados inválidos.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(PrevisaoProcessadaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PrevisaoProcessadaResponse>> Registrar(
        [FromBody] PrevisaoRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.RegistrarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }
}
