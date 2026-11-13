namespace WaterLine.Domain.Enums;

/// <summary>
/// Tipo do chamado. Cada tipo de agente informa quais tipos consegue atender.
/// </summary>
public enum TipoChamado
{
    /// <summary>Limpeza ou verificação da entrada da boca de lobo, feita pelo gari.</summary>
    Preventivo = 0,

    /// <summary>Limpeza interna programada (sedimento), feita pela equipe de galeria.</summary>
    Manutencao = 1,

    /// <summary>Desentupimento urgente do tubo, feito pela equipe de galeria.</summary>
    Urgente = 2,

    /// <summary>Emergência tratada pela central da cidade (CGE / Defesa Civil).</summary>
    Emergencia = 3
}
