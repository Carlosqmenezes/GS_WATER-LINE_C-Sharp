using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WaterLine.Domain.Excecoes;

namespace WaterLine.Api.Erros;

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
            CredenciaisInvalidasException e => (StatusCodes.Status401Unauthorized, "Login recusado", e.Message),
            AcessoNegadoException e => (StatusCodes.Status403Forbidden, "Acesso negado", e.Message),
            RecursoNaoEncontradoException e => (StatusCodes.Status404NotFound, "Recurso não encontrado", e.Message),
            ConflitoException e => (StatusCodes.Status409Conflict, "Conflito com o estado atual", e.Message),
            RegraNegocioException e => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflito de concorrência",
                "O registro foi alterado por outra operação ao mesmo tempo. Recarregue e tente novamente."),
            BadHttpRequestException e => (e.StatusCode, "Requisição inválida", e.Message),
            ArgumentException e => (StatusCodes.Status400BadRequest, "Dados inválidos", MensagemSemParametro(e)),
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

    private static string MensagemSemParametro(ArgumentException excecao)
    {
        var mensagem = excecao.Message;
        var indice = mensagem.IndexOf(" (Parameter '", StringComparison.Ordinal);
        return indice >= 0 ? mensagem[..indice] : mensagem;
    }
}
