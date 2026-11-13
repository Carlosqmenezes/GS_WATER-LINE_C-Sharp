using Microsoft.AspNetCore.Mvc;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;

namespace WaterLine.Api.Controllers;

/// <summary>Recebe as leituras dos sensores das bocas de lobo (ESP32).</summary>
[ApiController]
[Route("leituras")]
[Produces("application/json")]
public sealed class LeiturasController : ControllerBase
{
    private readonly ILeituraAppService _service;

    public LeiturasController(ILeituraAppService service)
    {
        _service = service;
    }

    /// <summary>
    /// Registra uma leitura, diagnostica a boca de lobo e dispara as ações necessárias
    /// (chamado de limpeza, cancelamento por chuva ou alerta).
    /// </summary>
    /// <param name="request">Distâncias dos dois sensores e o indicador de chuva.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="201">Leitura registrada e diagnosticada.</response>
    /// <response code="400">Dados inválidos.</response>
    /// <response code="404">Boca de lobo inexistente.</response>
    /// <response code="422">Boca de lobo inativa.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(LeituraProcessadaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LeituraProcessadaResponse>> Registrar(
        [FromBody] LeituraRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _service.RegistrarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }
}
