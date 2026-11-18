using System.ComponentModel.DataAnnotations;
using WaterLine.Domain.Enums;

namespace WaterLine.Api.Dtos;

/// <summary>Leitura enviada pelo ESP32 de uma boca de lobo.</summary>
public sealed record LeituraRequest
{
    [Required(ErrorMessage = "O id da boca de lobo é obrigatório.")]
    [Range(1, int.MaxValue, ErrorMessage = MensagensValidacao.Intervalo)]
    public int? BocaDeLoboId { get; init; }

    public DateTimeOffset? DataHora { get; init; }

    [Required(ErrorMessage = "A distância externa é obrigatória.")]
    [Range(0.0, 1000.0, ErrorMessage = MensagensValidacao.Intervalo)]
    public double? DistanciaExternaCm { get; init; }

    [Required(ErrorMessage = "A distância interna é obrigatória.")]
    [Range(0.0, 1000.0, ErrorMessage = MensagensValidacao.Intervalo)]
    public double? DistanciaInternaCm { get; init; }

    [Required(ErrorMessage = "O indicador de chuva é obrigatório.")]
    public bool? Chovendo { get; init; }
}

public sealed record LeituraProcessadaResponse
{
    public int LeituraId { get; init; }

    public int BocaDeLoboId { get; init; }

    public DateTime DataHora { get; init; }

    public TipoDiagnostico Diagnostico { get; init; }

    public double NivelCm { get; init; }

    public double PercentualOcupacao { get; init; }

    public double? VelocidadeSubidaCmPorMinuto { get; init; }

    public bool EntradaBloqueada { get; init; }

    public int? ChamadoAbertoId { get; init; }

    public int? AgenteDesignadoId { get; init; }

    public IReadOnlyList<int> ChamadosCanceladosIds { get; init; } = Array.Empty<int>();

    public int? AlertaId { get; init; }

    public bool? AlertaEnviado { get; init; }
}
