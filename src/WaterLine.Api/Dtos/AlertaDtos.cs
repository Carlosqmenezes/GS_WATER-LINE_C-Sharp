using WaterLine.Domain.Entidades;
using WaterLine.Domain.Enums;

namespace WaterLine.Api.Dtos;

/// <summary>Alerta devolvido pela API.</summary>
public sealed record AlertaResponse(
    int Id,
    int BocaDeLoboId,
    string CodigoBocaDeLobo,
    NivelAlerta Nivel,
    string Mensagem,
    DateTime CriadoEm)
{
    public static AlertaResponse De(Alerta alerta, BocaDeLobo boca) =>
        new(alerta.Id, boca.Id, boca.Codigo, alerta.Nivel, alerta.Mensagem, alerta.CriadoEm);
}
