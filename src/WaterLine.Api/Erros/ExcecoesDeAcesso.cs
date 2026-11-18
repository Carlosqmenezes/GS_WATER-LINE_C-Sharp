namespace WaterLine.Api.Erros;

public sealed class CredenciaisInvalidasException : Exception
{
    public CredenciaisInvalidasException() : base("Matrícula ou senha incorretas.")
    {
    }
}

public sealed class AcessoNegadoException : Exception
{
    public AcessoNegadoException(string mensagem) : base(mensagem)
    {
    }
}
