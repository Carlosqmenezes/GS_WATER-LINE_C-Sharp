using System.ComponentModel.DataAnnotations;
using WaterLine.Domain.Enums;

namespace WaterLine.Api.Dtos;

/// <summary>Leitura enviada pelo ESP32 de uma boca de lobo.</summary>
public sealed record LeituraRequest
{
    [Required(ErrorMessage = "O id da boca de lobo é obrigatório.")]
    [Range(1, int.MaxValue)]
    public int? BocaDeLoboId { get; init; }

    /// <summary>Momento da medição (ISO 8601 com fuso). Se omitido, usa a hora do servidor.</summary>
    public DateTimeOffset? DataHora { get; init; }

    /// <summary>Distância medida pelo sensor externo (horizontal, atravessando a abertura), em cm.</summary>
    [Required(ErrorMessage = "A distância externa é obrigatória.")]
    [Range(0.0, 1000.0)]
    public double? DistanciaExternaCm { get; init; }

    /// <summary>Distância da tampa até a superfície (água ou sedimento), em cm.</summary>
    [Required(ErrorMessage = "A distância interna é obrigatória.")]
    [Range(0.0, 1000.0)]
    public double? DistanciaInternaCm { get; init; }

    /// <summary>Indicador de chuva do dispositivo.</summary>
    [Required(ErrorMessage = "O indicador de chuva é obrigatório.")]
    public bool? Chovendo { get; init; }
}

/// <summary>Resultado do processamento de uma leitura.</summary>
public sealed record LeituraProcessadaResponse
{
    public int LeituraId { get; init; }

    public int BocaDeLoboId { get; init; }

    public DateTime DataHora { get; init; }

    public TipoDiagnostico Diagnostico { get; init; }

    /// <summary>Altura ocupada na caixa, em cm.</summary>
    public double NivelCm { get; init; }

    /// <summary>Ocupação da caixa, de 0 a 100.</summary>
    public double PercentualOcupacao { get; init; }

    /// <summary>Velocidade de subida desde a leitura anterior, em cm/min (null na primeira leitura).</summary>
    public double? VelocidadeSubidaCmPorMinuto { get; init; }

    public bool EntradaBloqueada { get; init; }

    /// <summary>Chamado aberto por esta leitura, se houver.</summary>
    public int? ChamadoAbertoId { get; init; }
}
