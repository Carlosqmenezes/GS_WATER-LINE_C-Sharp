using System.ComponentModel.DataAnnotations;

namespace WaterLine.Api.Autenticacao;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    public const int TamanhoMinimoChave = 32;

    [Required(ErrorMessage = "Jwt:Emissor é obrigatório.")]
    public string Emissor { get; set; } = "WaterLine.Api";

    [Required(ErrorMessage = "Jwt:Audiencia é obrigatória.")]
    public string Audiencia { get; set; } = "WaterLine.App";

    [Required(ErrorMessage = "Jwt:Chave é obrigatória. Defina a variável de ambiente Jwt__Chave (veja o README).")]
    [MinLength(TamanhoMinimoChave, ErrorMessage = "Jwt:Chave precisa ter pelo menos 32 caracteres.")]
    public string Chave { get; set; } = string.Empty;

    /// <summary>Validade do token, em horas. O padrão cobre um turno de trabalho.</summary>
    [Range(1, 24, ErrorMessage = "Jwt:ExpiracaoHoras deve ficar entre 1 e 24.")]
    public int ExpiracaoHoras { get; set; } = 12;
}
