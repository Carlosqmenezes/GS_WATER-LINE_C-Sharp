using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaterLine.Api.Autenticacao;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;

namespace WaterLine.Api.Controllers;

[ApiController]
[Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[Route("chamados")]
[Produces("application/json")]
public sealed class ChamadosController : ControllerBase
{
    private readonly IChamadoAppService _service;

    public ChamadosController(IChamadoAppService service)
    {
        _service = service;
    }

    /// <param name="lat">Latitude atual do agente.</param>
    /// <param name="lng">Longitude atual do agente.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpGet("proximos")]
    [ProducesResponseType(typeof(IReadOnlyList<ChamadoProximoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ChamadoProximoResponse>>> ListarProximos(
        [FromQuery, Required(ErrorMessage = MensagensValidacao.Obrigatorio), Range(-90.0, 90.0, ErrorMessage = MensagensValidacao.Intervalo)] double? lat,
        [FromQuery, Required(ErrorMessage = MensagensValidacao.Obrigatorio), Range(-180.0, 180.0, ErrorMessage = MensagensValidacao.Intervalo)] double? lng,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarProximosAsync(lat!.Value, lng!.Value, cancellationToken));
    }

    /// <param name="id">Id do chamado.</param>
    /// <param name="request">Agente que está aceitando.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="200">Chamado aceito.</response>
    /// <response code="403">O agente informado não é o dono do token.</response>
    /// <response code="404">Chamado ou agente inexistente.</response>
    /// <response code="409">Chamado não está aberto, é de outro agente ou foi alterado ao mesmo tempo.</response>
    /// <response code="422">Agente fora de serviço, de outro tipo ou fora do raio.</response>
    [HttpPost("{id:int}/aceitar")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ChamadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChamadoResponse>> Aceitar(
        int id,
        [FromBody] AceitarChamadoRequest request,
        CancellationToken cancellationToken)
    {
        User.GarantirMesmoAgente(request.AgenteId!.Value);
        return Ok(await _service.AceitarAsync(id, request, cancellationToken));
    }

    /// <param name="id">Id do chamado.</param>
    /// <param name="request">Agente e as duas fotos.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="200">Chamado concluído.</response>
    /// <response code="400">Foto em formato inválido ou grande demais.</response>
    /// <response code="403">O agente informado não é o dono do token.</response>
    /// <response code="404">Chamado inexistente.</response>
    /// <response code="409">Chamado não está aceito.</response>
    /// <response code="422">Falta foto, agente diferente do que aceitou ou sensor sem confirmar a entrada livre.</response>
    [HttpPost("{id:int}/concluir")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(ChamadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ChamadoResponse>> Concluir(
        int id,
        [FromForm] ConcluirChamadoRequest request,
        CancellationToken cancellationToken)
    {
        User.GarantirMesmoAgente(request.AgenteId!.Value);
        return Ok(await _service.ConcluirAsync(id, request, cancellationToken));
    }
}
