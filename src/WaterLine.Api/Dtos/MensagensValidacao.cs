namespace WaterLine.Api.Dtos;

/// <summary>Mensagens de validação em português, usadas nos atributos dos DTOs e dos parâmetros.</summary>
public static class MensagensValidacao
{
    /// <summary>{0} = campo; {1} e {2} = limites.</summary>
    public const string Intervalo = "O campo {0} deve estar entre {1} e {2}.";

    /// <summary>{0} = campo.</summary>
    public const string Obrigatorio = "O campo {0} é obrigatório.";

    /// <summary>{0} = campo; {1} = máximo; {2} = mínimo.</summary>
    public const string Tamanho = "O campo {0} deve ter entre {2} e {1} caracteres.";
}
