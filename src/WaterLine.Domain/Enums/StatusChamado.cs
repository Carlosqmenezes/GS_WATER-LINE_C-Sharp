namespace WaterLine.Domain.Enums;

/// <summary>
/// Ciclo de vida de um chamado: Aberto → Aceito → Concluido, ou Cancelado.
/// </summary>
public enum StatusChamado
{
    Aberto = 0,
    Aceito = 1,
    Concluido = 2,
    Cancelado = 3
}
