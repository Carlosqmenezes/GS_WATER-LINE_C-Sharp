using System.ComponentModel.DataAnnotations;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;

namespace WaterLine.Api.Dtos;

/// <summary>Dados para cadastrar ou atualizar uma boca de lobo.</summary>
public sealed record BocaDeLoboRequest
{
    /// <summary>Código de campo, único (ex.: BL-0006).</summary>
    [Required(ErrorMessage = "O código é obrigatório.")]
    [StringLength(BocaDeLobo.TamanhoMaximoCodigo, MinimumLength = 3)]
    public string Codigo { get; init; } = string.Empty;

    /// <summary>Endereço de referência (fictício na demonstração).</summary>
    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [StringLength(BocaDeLobo.TamanhoMaximoEndereco, MinimumLength = 5)]
    public string Endereco { get; init; } = string.Empty;

    [Required(ErrorMessage = "A latitude é obrigatória.")]
    [Range(-90.0, 90.0)]
    public double? Latitude { get; init; }

    [Required(ErrorMessage = "A longitude é obrigatória.")]
    [Range(-180.0, 180.0)]
    public double? Longitude { get; init; }

    /// <summary>Distância da tampa até o fundo da caixa, em cm.</summary>
    [Required(ErrorMessage = "A profundidade é obrigatória.")]
    [Range(10.0, 1000.0)]
    public double? ProfundidadeCm { get; init; }

    /// <summary>Largura da abertura atravessada pelo sensor externo, em cm.</summary>
    [Required(ErrorMessage = "A largura da abertura é obrigatória.")]
    [Range(5.0, 500.0)]
    public double? LarguraAberturaCm { get; init; }

    /// <summary>Usado só na atualização: permite reativar ou desativar a boca de lobo.</summary>
    public bool Ativa { get; init; } = true;
}

/// <summary>Boca de lobo devolvida pela API.</summary>
public sealed record BocaDeLoboResponse(
    int Id,
    string Codigo,
    string Endereco,
    double Latitude,
    double Longitude,
    double ProfundidadeCm,
    double LarguraAberturaCm,
    TipoDiagnostico Status,
    bool Ativa)
{
    public static BocaDeLoboResponse De(BocaDeLobo boca) => new(
        boca.Id,
        boca.Codigo,
        boca.Endereco,
        boca.Latitude,
        boca.Longitude,
        boca.ProfundidadeCm,
        boca.LarguraAberturaCm,
        boca.Status,
        boca.Ativa);
}
