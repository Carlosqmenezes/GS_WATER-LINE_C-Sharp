namespace WaterLine.Domain.Enums;

/// <summary>
/// Resultado do diagnóstico de uma boca de lobo a partir dos dois sensores e do indicador de chuva.
/// </summary>
public enum TipoDiagnostico
{
    /// <summary>Entrada livre e fundo dentro do normal.</summary>
    Normal = 0,

    /// <summary>Lixo tampando a entrada (sensor externo). Resolve o gari.</summary>
    EntradaBloqueada = 1,

    /// <summary>Fundo subindo aos poucos sem chuva. Manutenção programada da equipe de galeria.</summary>
    SedimentoAcumulando = 2,

    /// <summary>Água parada há mais de 24 h sem chuva. Chamado urgente para a equipe de galeria.</summary>
    TuboEntupido = 3,

    /// <summary>Chovendo e água subindo rápido: alerta precoce para moradores.</summary>
    AguaSubindo = 4,

    /// <summary>Chovendo e nível crítico: emergência para a central.</summary>
    NivelCritico = 5
}
