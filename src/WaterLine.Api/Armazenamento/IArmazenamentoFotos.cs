namespace WaterLine.Api.Armazenamento;


public interface IArmazenamentoFotos
{
   
    void Validar(IFormFile foto, string nomeCampo);

    /// Grava a foto e devolve o caminho relativo que fica salvo no chamado
    Task<string> SalvarAsync(int chamadoId, string etapa, IFormFile foto, CancellationToken cancellationToken);

    ///Remove uma foto gravada (usado para desfazer quando a conclusão falha
    void Remover(string caminhoRelativo);
}
