using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WaterLine.Api.Configuracao;
using WaterLine.Api.Dtos;
using WaterLine.Api.Servicos;

namespace WaterLine.Api.Controllers;

/// <summary>Login do agente no app.</summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public sealed class AutenticacaoController : ControllerBase
{
    private readonly IAutenticacaoAppService _service;

    public AutenticacaoController(IAutenticacaoAppService service)
    {
        _service = service;
    }

    /// Confere a matrícula e a senha e devolve o token JWT que o app envia nas rotas do agente,
    /// <param name="request">Matrícula e senha.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <response code="200">Login aceito.</response>
    /// <response code="401">Matrícula ou senha incorretas.</response>
    /// <response code="429">Muitas tentativas seguidas; espere um minuto.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(InjecaoDependencia.PoliticaLimiteLogin)]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Entrar(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _service.EntrarAsync(request, cancellationToken));
    }
}
