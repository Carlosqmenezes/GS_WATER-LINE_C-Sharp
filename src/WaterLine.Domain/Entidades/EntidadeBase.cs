namespace WaterLine.Domain.Entidades;

/// <summary>
/// Base de todas as entidades persistidas: concentra a identidade (Id).
/// O Id é definido pelo banco; fora dele, só os testes podem atribuí-lo.
/// </summary>
public abstract class EntidadeBase
{
    public int Id { get; protected set; }

    /// <summary>Usado apenas pelos testes unitários para simular entidades já salvas.</summary>
    internal void DefinirId(int id) => Id = id;
}
