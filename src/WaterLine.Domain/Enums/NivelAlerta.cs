namespace WaterLine.Domain.Enums;

/// <summary>
/// Nível de um alerta, alinhado às camadas 2 e 3 do WaterLine.
/// </summary>
public enum NivelAlerta
{
    /// <summary>Camada 2: água subindo rápido. Aviso para moradores e comerciantes próximos.</summary>
    AlertaPrecoce = 0,

    /// <summary>Camada 3: nível crítico. Chamado de emergência para a central.</summary>
    Emergencia = 1
}
