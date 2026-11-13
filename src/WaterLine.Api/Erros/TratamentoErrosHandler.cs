using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WaterLine.Domain.Excecoes;

namespace WaterLine.Api.Erros;

/// <summary>
/// Converte as exceções em respostas ProblemDetails (RFC 7807) com o status HTTP adequado.
/// Erros inesperados viram 500 com mensagem genérica, sem expor detalhes internos.
/// </summary>
public sealed class TratamentoErrosHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<TratamentoErrosHandler> _logger;

    public TratamentoErrosHandler(IProblemDetailsService problemDetailsService, ILogger<TratamentoErrosHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, titulo, detalhe) = exception switch
        {
            RecursoNaoEncontradoException e => (StatusCodes.Status404NotFound, "Recurso não encontrado", e.Message),
            ConflitoException e => (StatusCodes.Status409Conflict, "Conflito com o estado atual", e.Message),
            RegraNegocioException e => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
            ArgumentException e => (StatusCodes.Status400BadRequest, "Dados inválidos", e.Message),
            OperationCanceledException => (499, "Requisição cancelada", "A requisição foi cancelada pelo cliente."),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado. Tente novamente mais tarde.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation("Requisição recusada com {Status}: {Motivo}", status, detalhe);
        }

        httpContext.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = titulo,
                Detail = detalhe,
                Instance = httpContext.Request.Path
            }
        });
    }
}
