using WaterLine.Domain.Enums;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Equipe de galeria da subprefeitura: limpa o sedimento e desentope o tubo da boca de lobo.
/// </summary>
public sealed class EquipeGaleria : Agente
{
    /// <summary>Tipos de chamado atendidos por qualquer equipe de galeria.</summary>
    public static readonly IReadOnlyCollection<TipoChamado> TiposPermitidos =
        new[] { TipoChamado.Manutencao, TipoChamado.Urgente };

    /// <summary>Construtor usado pelo Entity Framework.</summary>
    private EquipeGaleria()
    {
    }

    public EquipeGaleria(string nome, string matricula) : base(nome, matricula)
    {
    }

    public override IReadOnlyCollection<TipoChamado> TiposDeChamadoAtendidos => TiposPermitidos;

    public override string Funcao => "Equipe de galeria";
}
