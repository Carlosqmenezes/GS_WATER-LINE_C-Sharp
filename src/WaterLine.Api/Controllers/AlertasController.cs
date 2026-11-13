using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;
using WaterLine.Domain.Enums;

namespace WaterLine.Api.Controllers;

/// <summary>Alertas de água subindo (alerta precoce) e de nível crítico (emergência).</summary>
[ApiController]
[Route("alertas")]
[Produces("application/json")]
public sealed class AlertasController : ControllerBase
{
    private readonly IAlertaAppService _service;

    public AlertasController(IAlertaAppService service)
    {
        _service = service;
    }

    /// <summary>Lista os alertas mais recentes primeiro.</summary>
    /// <param name="bocaDeLoboId">Filtro opcional por boca de lobo.</param>
    /// <param name="nivel">Filtro opcional por nível (AlertaPrecoce ou Emergencia).</param>
    /// <param name="limite">Quantidade máxima de alertas (1 a 200, padrão 50).</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AlertaResponse>>> Listar(
        [FromQuery, Range(1, int.MaxValue)] int? bocaDeLoboId,
        [FromQuery] NivelAlerta? nivel,
        [FromQuery, Range(1, 200)] int limite = 50,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ListarAsync(bocaDeLoboId, nivel, limite, cancellationToken));
    }
}
