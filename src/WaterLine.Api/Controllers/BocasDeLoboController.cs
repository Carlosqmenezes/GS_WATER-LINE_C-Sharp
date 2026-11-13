using Microsoft.AspNetCore.Mvc;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;

namespace WaterLine.Api.Controllers;

/// <summary>Cadastro das bocas de lobo monitoradas.</summary>
[ApiController]
[Route("bocas-de-lobo")]
[Produces("application/json")]
public sealed class BocasDeLoboController : ControllerBase
{
    private readonly IBocaDeLoboAppService _service;

    public BocasDeLoboController(IBocaDeLoboAppService service)
    {
        _service = service;
    }

    /// <summary>Lista as bocas de lobo com o último diagnóstico de cada uma.</summary>
    /// <param name="ativa">Filtro opcional: true para só as ativas, false para só as inativas.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BocaDeLoboResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BocaDeLoboResponse>>> Listar(
        [FromQuery] bool? ativa,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.ListarAsync(ativa, cancellationToken));
    }

    /// <summary>Obtém uma boca de lobo pelo id.</summary>
    /// <param name="id">Id da boca de lobo.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BocaDeLoboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BocaDeLoboResponse>> Obter(int id, CancellationToken cancellationToken)
    {
        return Ok(await _service.ObterAsync(id, cancellationToken));
    }

    /// <summary>Cadastra uma nova boca de lobo.</summary>
    /// <param name="request">Dados da boca de lobo.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BocaDeLoboResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BocaDeLoboResponse>> Criar(
        [FromBody] BocaDeLoboRequest request,
        CancellationToken cancellationToken)
    {
        var criada = await _service.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    /// <summary>Atualiza os dados de uma boca de lobo.</summary>
    /// <param name="id">Id da boca de lobo.</param>
    /// <param name="request">Novos dados.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(BocaDeLoboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BocaDeLoboResponse>> Atualizar(
        int id,
        [FromBody] BocaDeLoboRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.AtualizarAsync(id, request, cancellationToken));
    }

    /// <summary>Desativa uma boca de lobo (exclusão lógica; o histórico é preservado).</summary>
    /// <param name="id">Id da boca de lobo.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(int id, CancellationToken cancellationToken)
    {
        await _service.DesativarAsync(id, cancellationToken);
        return NoContent();
    }
}
