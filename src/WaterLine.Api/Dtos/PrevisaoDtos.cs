using System.ComponentModel.DataAnnotations;
using WaterLine.Domain.Entidades;

namespace WaterLine.Api.Dtos;

/// <summary>Previsão enviada pelo modelo de ML.</summary>
public sealed record PrevisaoRequest
{
    /// <summary>Momento em que a previsão foi gerada (ISO 8601 com fuso). Se omitido, usa a hora do servidor.</summary>
    public DateTimeOffset? GeradaEm { get; init; }

    /// <summary>Horizonte da previsão, em horas (ex.: 3 ou 6).</summary>
    [Required(ErrorMessage = "O horizonte em horas é obrigatório.")]
    [Range(PrevisaoChuva.HorizonteMinimoHoras, PrevisaoChuva.HorizonteMaximoHoras, ErrorMessage = MensagensValidacao.Intervalo)]
    public int? HorizonteHoras { get; init; }

    /// <summary>Probabilidade de chuva forte, de 0 a 1.</summary>
    [Required(ErrorMessage = "A probabilidade é obrigatória.")]
    [Range(0.0, 1.0, ErrorMessage = MensagensValidacao.Intervalo)]
    public double? Probabilidade { get; init; }

    /// <summary>Decisão do modelo: true quando prevê chuva forte no horizonte.</summary>
    [Required(ErrorMessage = "Informe se há previsão de chuva forte.")]
    public bool? ChuvaForte { get; init; }
}

/// <summary>Resultado do processamento de uma previsão.</summary>
public sealed record PrevisaoProcessadaResponse
{
    public int PrevisaoId { get; init; }

    public DateTime GeradaEm { get; init; }

    public int HorizonteHoras { get; init; }

    public double Probabilidade { get; init; }

    public bool ChuvaForte { get; init; }

    /// <summary>Quantidade de chamados ativos que tiveram a prioridade elevada.</summary>
    public int ChamadosPriorizados { get; init; }

    /// <summary>Ids dos chamados preventivos criados.</summary>
    public IReadOnlyList<int> ChamadosPreventivosCriados { get; init; } = Array.Empty<int>();

    /// <summary>Quantos dos chamados preventivos já saíram com agente designado.</summary>
    public int ChamadosDesignados { get; init; }
}
