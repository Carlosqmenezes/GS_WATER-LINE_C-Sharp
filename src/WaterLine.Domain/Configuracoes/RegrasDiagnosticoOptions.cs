using System.ComponentModel.DataAnnotations;

namespace WaterLine.Domain.Configuracoes;

/// <summary>
/// Parâmetros do diagnóstico (seção "Regras:Diagnostico" do appsettings.json).
/// Percentuais vão de 0 a 100.
/// </summary>
public sealed class RegrasDiagnosticoOptions : IValidatableObject
{
    public const string Secao = "Regras:Diagnostico";

    /// <summary>Entrada bloqueada quando a distância externa fica abaixo deste % da largura da abertura.</summary>
    [Range(1, 100)]
    public double PercentualEntradaBloqueada { get; set; } = 70;

    /// <summary>Sem chuva, ocupação a partir deste % indica sedimento acumulando.</summary>
    [Range(1, 100)]
    public double PercentualSedimentoMinimo { get; set; } = 30;

    /// <summary>Sem chuva, ocupação a partir deste % (por tempo prolongado) indica tubo entupido.</summary>
    [Range(1, 100)]
    public double PercentualTuboEntupido { get; set; } = 60;

    /// <summary>Tempo mínimo de água parada, sem chuva, para diagnosticar tubo entupido.</summary>
    [Range(1, 720)]
    public double HorasAguaParadaTubo { get; set; } = 24;

    /// <summary>Chovendo, subida a partir desta velocidade indica água subindo rápido.</summary>
    [Range(0.1, 1000)]
    public double VelocidadeSubidaCmPorMinuto { get; set; } = 5;

    /// <summary>Chovendo, ocupação a partir deste % indica nível crítico.</summary>
    [Range(1, 100)]
    public double PercentualNivelCritico { get; set; } = 85;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PercentualSedimentoMinimo >= PercentualTuboEntupido)
        {
            yield return new ValidationResult(
                "PercentualSedimentoMinimo deve ser menor que PercentualTuboEntupido.",
                new[] { nameof(PercentualSedimentoMinimo), nameof(PercentualTuboEntupido) });
        }
    }
}
