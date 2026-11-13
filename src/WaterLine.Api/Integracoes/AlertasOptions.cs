using System.ComponentModel.DataAnnotations;

namespace WaterLine.Api.Integracoes;

/// <summary>Configuração da integração com o serviço externo de alertas (seção "Alertas").</summary>
public sealed class AlertasOptions : IValidatableObject
{
    public const string Secao = "Alertas";

    /// <summary>
    /// URL base do serviço de alertas (ex.: http://localhost:5090). Vazia: os alertas só são registrados em log.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>Tempo máximo de espera da chamada HTTP, em segundos.</summary>
    [Range(1, 60)]
    public int TimeoutSegundos { get; set; } = 5;

    /// <summary>
    /// Intervalo mínimo, em minutos, entre dois alertas do mesmo nível para a mesma boca de lobo,
    /// para não disparar um aviso a cada leitura durante a chuva.
    /// </summary>
    [Range(0, 240)]
    public int IntervaloMinimoMinutos { get; set; } = 10;

    public bool UsaServicoExterno => !string.IsNullOrWhiteSpace(BaseUrl);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UsaServicoExterno && !Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            yield return new ValidationResult("Alertas:BaseUrl deve ser uma URL absoluta.", new[] { nameof(BaseUrl) });
        }
    }
}
