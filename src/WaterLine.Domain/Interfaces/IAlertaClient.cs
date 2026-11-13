using WaterLine.Domain.Entidades;

namespace WaterLine.Domain.Interfaces;

/// <summary>
/// Envia um alerta para o serviço externo de alertas (moradores e central).
/// A implementação fica na camada de infraestrutura (HTTP ou somente log).
/// </summary>
public interface IAlertaClient
{
    /// <summary>
    /// Envia o alerta. Não lança exceção em falha de comunicação: devolve false
    /// para que a leitura continue sendo processada e o alerta fique registrado no banco.
    /// </summary>
    Task<bool> EnviarAsync(Alerta alerta, BocaDeLobo boca, CancellationToken cancellationToken);
}
