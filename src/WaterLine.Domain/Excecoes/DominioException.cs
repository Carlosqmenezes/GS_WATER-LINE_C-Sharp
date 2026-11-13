namespace WaterLine.Domain.Excecoes;

/// <summary>
/// Base das exceções lançadas pelo domínio. A API converte cada tipo num status HTTP.
/// </summary>
public abstract class DominioException : Exception
{
    protected DominioException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>
/// Uma regra de negócio impede a operação (ex.: concluir chamado sem foto). Vira HTTP 422.
/// </summary>
public sealed class RegraNegocioException : DominioException
{
    public RegraNegocioException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>
/// A operação conflita com o estado atual do recurso (ex.: aceitar chamado já aceito). Vira HTTP 409.
/// </summary>
public sealed class ConflitoException : DominioException
{
    public ConflitoException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>
/// O recurso pedido não existe. Vira HTTP 404.
/// </summary>
public sealed class RecursoNaoEncontradoException : DominioException
{
    public RecursoNaoEncontradoException(string recurso, int id)
        : base($"{recurso} com id {id} não foi encontrado(a).")
    {
    }
}
