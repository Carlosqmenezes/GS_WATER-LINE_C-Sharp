using System.ComponentModel.DataAnnotations;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dtos;

public sealed record PrevisaoRequest
{
    public DateTimeOffset? GeradaEm { get; init; }

    [Required(ErrorMessage = "O horizonte em horas é obrigatório.")]
    [Range(PrevisaoChuva.HorizonteMinimoHoras, PrevisaoChuva.HorizonteMaximoHoras, ErrorMessage = MensagensValidacao.Intervalo)]
    public int? HorizonteHoras { get; init; }

    [Required(ErrorMessage = "A probabilidade é obrigatória.")]
    [Range(0.0, 1.0, ErrorMessage = MensagensValidacao.Intervalo)]
    public double? Probabilidade { get; init; }

    [Required(ErrorMessage = "Informe se há previsão de chuva forte.")]
    public bool? ChuvaForte { get; init; }
}

public sealed record PrevisaoProcessadaResponse
{
    public int PrevisaoId { get; init; }

    public DateTime GeradaEm { get; init; }

    public int HorizonteHoras { get; init; }

    public double Probabilidade { get; init; }

    public bool ChuvaForte { get; init; }

    public int ChamadosPriorizados { get; init; }

    public IReadOnlyList<int> ChamadosPreventivosCriados { get; init; } = Array.Empty<int>();

    public int ChamadosDesignados { get; init; }
}
