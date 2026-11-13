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

    /// <summary>
    /// O agente aceita o chamado. Ele precisa estar em serviço, ser do tipo certo
    /// e estar dentro do raio de despacho da boca de lobo.
    /// </summary>
    /// <param name="id">Id do chamado.</param>
    /// <param name="request">Agente que está aceitando.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="200">Chamado aceito.</response>
    /// <response code="404">Chamado ou agente inexistente.</response>
    /// <response code="409">Chamado não está aberto, é de outro agente ou foi alterado ao mesmo tempo.</response>
    /// <response code="422">Agente fora de serviço, de outro tipo ou fora do raio.</response>
    [HttpPost("{id:int}/aceitar")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ChamadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChamadoResponse>> Aceitar(
        int id,
        [FromBody] AceitarChamadoRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.AceitarAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Conclui o chamado com a foto de antes e a de depois (multipart/form-data).
    /// Só conclui se a última leitura do sensor mostrar a entrada livre.
    /// </summary>
    /// <param name="id">Id do chamado.</param>
    /// <param name="request">Agente e as duas fotos.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="200">Chamado concluído.</response>
    /// <response code="400">Foto em formato inválido ou grande demais.</response>
    /// <response code="404">Chamado inexistente.</response>
    /// <response code="409">Chamado não está aceito.</response>
    /// <response code="422">Falta foto, agente diferente do que aceitou ou sensor sem confirmar a entrada livre.</response>
    [HttpPost("{id:int}/concluir")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(ChamadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChamadoResponse>> Concluir(
        int id,
        [FromForm] ConcluirChamadoRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.ConcluirAsync(id, request, cancellationToken));
    }
}
