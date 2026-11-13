using WaterLine.Domain.Enums;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Trabalhador de limpeza: tira à mão o lixo que tampa a entrada da boca de lobo.
/// </summary>
public sealed class Gari : Agente
{
    /// <summary>Tipos de chamado atendidos por qualquer gari.</summary>
    public static readonly IReadOnlyCollection<TipoChamado> TiposPermitidos =
        new[] { TipoChamado.Preventivo };

    /// <summary>Construtor usado pelo Entity Framework.</summary>
    private Gari()
    {
    }

    public Gari(string nome, string matricula) : base(nome, matricula)
    {
    }

    public override IReadOnlyCollection<TipoChamado> TiposDeChamadoAtendidos => TiposPermitidos;

    public override string Funcao => "Gari";

    /// <summary>Indica se um chamado desse tipo é trabalho de gari.</summary>
    public static bool EhTipoDeGari(TipoChamado tipo) => TiposPermitidos.Contains(tipo);
}
