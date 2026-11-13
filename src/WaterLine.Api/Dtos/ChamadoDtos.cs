using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;
using WaterLine.Domain.Servicos;

namespace WaterLine.Api.Dtos;

/// <summary>Agente que está aceitando o chamado.</summary>
public sealed record AceitarChamadoRequest
{
    [Required(ErrorMessage = "O id do agente é obrigatório.")]
    [Range(1, int.MaxValue)]
    public int? AgenteId { get; init; }
}

/// <summary>Conclusão do chamado (multipart/form-data) com as fotos de antes e de depois.</summary>
public sealed class ConcluirChamadoRequest
{
    /// <summary>Id do agente que aceitou o chamado.</summary>
    [Required(ErrorMessage = "O id do agente é obrigatório.")]
    [Range(1, int.MaxValue)]
    [FromForm(Name = "agenteId")]
    public int? AgenteId { get; set; }

    /// <summary>Foto tirada antes da limpeza (JPEG, PNG ou WebP).</summary>
    [FromForm(Name = "fotoAntes")]
    public IFormFile? FotoAntes { get; set; }

    /// <summary>Foto tirada depois da limpeza (JPEG, PNG ou WebP).</summary>
    [FromForm(Name = "fotoDepois")]
    public IFormFile? FotoDepois { get; set; }
}

/// <summary>Chamado devolvido pela API.</summary>
public sealed record ChamadoResponse
{
    public int Id { get; init; }

    public int BocaDeLoboId { get; init; }

    public int? AgenteId { get; init; }

    public TipoChamado Tipo { get; init; }

    public StatusChamado Status { get; init; }

    /// <summary>De 1 (baixa) a 5 (máxima).</summary>
    public int Prioridade { get; init; }

    public DateTime AbertoEm { get; init; }

    public DateTime? AceitoEm { get; init; }

    public DateTime? ConcluidoEm { get; init; }

    public bool PossuiFotoAntes { get; init; }

    public bool PossuiFotoDepois { get; init; }

    public static ChamadoResponse De(Chamado chamado) => new()
    {
        Id = chamado.Id,
        BocaDeLoboId = chamado.BocaDeLoboId,
        AgenteId = chamado.AgenteId,
        Tipo = chamado.Tipo,
        Status = chamado.Status,
        Prioridade = chamado.Prioridade,
        AbertoEm = chamado.AbertoEm,
        AceitoEm = chamado.AceitoEm,
        ConcluidoEm = chamado.ConcluidoEm,
        PossuiFotoAntes = !string.IsNullOrEmpty(chamado.FotoAntes),
        PossuiFotoDepois = !string.IsNullOrEmpty(chamado.FotoDepois)
    };
}

/// <summary>Chamado próximo do agente, com os dados da boca de lobo e a distância.</summary>
public sealed record ChamadoProximoResponse
{
    public int Id { get; init; }

    public TipoChamado Tipo { get; init; }

    public StatusChamado Status { get; init; }

    public int Prioridade { get; init; }

    public DateTime AbertoEm { get; init; }

    /// <summary>Agente já designado pelo despacho (null se ainda livre).</summary>
    public int? AgenteId { get; init; }

    public int BocaDeLoboId { get; init; }

    public string CodigoBocaDeLobo { get; init; } = string.Empty;

    public string Endereco { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    /// <summary>Tipo de entupimento diagnosticado na boca de lobo.</summary>
    public TipoDiagnostico Diagnostico { get; init; }

    /// <summary>Distância do ponto consultado até a boca de lobo, em metros.</summary>
    public double DistanciaMetros { get; init; }

    public static ChamadoProximoResponse De(ChamadoProximo proximo) => new()
    {
        Id = proximo.Chamado.Id,
        Tipo = proximo.Chamado.Tipo,
        Status = proximo.Chamado.Status,
        Prioridade = proximo.Chamado.Prioridade,
        AbertoEm = proximo.Chamado.AbertoEm,
        AgenteId = proximo.Chamado.AgenteId,
        BocaDeLoboId = proximo.Boca.Id,
        CodigoBocaDeLobo = proximo.Boca.Codigo,
        Endereco = proximo.Boca.Endereco,
        Latitude = proximo.Boca.Latitude,
        Longitude = proximo.Boca.Longitude,
        Diagnostico = proximo.Boca.Status,
        DistanciaMetros = Math.Round(proximo.DistanciaMetros, 1)
    };
}
