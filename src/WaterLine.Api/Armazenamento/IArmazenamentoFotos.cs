namespace WaterLine.Api.Armazenamento;

/// <summary>Guarda as fotos de antes e depois enviadas pelo app do gari.</summary>
public interface IArmazenamentoFotos
{
    /// <summary>
    /// Confere formato e tamanho da foto. Lança <see cref="ArgumentException"/> (HTTP 400) se for inválida.
    /// </summary>
    void Validar(IFormFile foto, string nomeCampo);

    /// <summary>Grava a foto e devolve o caminho relativo que fica salvo no chamado.</summary>
    Task<string> SalvarAsync(int chamadoId, string etapa, IFormFile foto, CancellationToken cancellationToken);

    /// <summary>Remove uma foto gravada (usado para desfazer quando a conclusão falha).</summary>
    void Remover(string caminhoRelativo);
}
