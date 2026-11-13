using WaterLine.Domain.Comum;

namespace WaterLine.Domain.Entidades;

/// <summary>
/// Previsão enviada pelo modelo de ML: probabilidade de chuva forte nas próximas N horas.
/// </summary>
public class PrevisaoChuva : EntidadeBase
{
    public const int HorizonteMinimoHoras = 1;
    public const int HorizonteMaximoHoras = 72;

    /// <summary>Construtor usado pelo Entity Framework.</summary>
    protected PrevisaoChuva()
    {
    }

    public PrevisaoChuva(DateTime geradaEm, int horizonteHoras, double probabilidade, bool chuvaForte)
    {
        if (horizonteHoras < HorizonteMinimoHoras || horizonteHoras > HorizonteMaximoHoras)
        {
            throw new ArgumentOutOfRangeException(
                nameof(horizonteHoras),
                horizonteHoras,
                $"O horizonte deve estar entre {HorizonteMinimoHoras} e {HorizonteMaximoHoras} horas.");
        }

        if (!double.IsFinite(probabilidade) || probabilidade < 0 || probabilidade > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(probabilidade), probabilidade, "A probabilidade deve estar entre 0 e 1.");
        }

        GeradaEm = Validacao.EmUtc(geradaEm);
        HorizonteHoras = horizonteHoras;
        Probabilidade = probabilidade;
        ChuvaForte = chuvaForte;
    }

    public DateTime GeradaEm { get; private set; }

    /// <summary>Quantas horas à frente a previsão cobre (ex.: 3 ou 6).</summary>
    public int HorizonteHoras { get; private set; }

    /// <summary>Probabilidade de chuva forte, de 0 a 1.</summary>
    public double Probabilidade { get; private set; }

    /// <summary>Decisão do modelo: true quando prevê chuva forte no horizonte.</summary>
    public bool ChuvaForte { get; private set; }
}
